using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using MatchTest;
using System.Linq;
using Random = UnityEngine.Random;
using System.Collections;
using UnityEngine.SceneManagement;

namespace MatchServerCollection
{
    [Serializable]
    public struct MatchInfo
    {
        public string title;
        public string matchid;
        public byte players;
        public byte maxplayers;
    }

    [Serializable]
    public struct PlayerInfo
    {
        public string matchid;
        public bool isLeader;
        public string name;
    }

    [Serializable]
    public enum ServerCall : byte
    {
        None,
        Create,
        Join,
        Leave,
        LoadGame
    }

    [Serializable]
    public enum ClientCall : byte
    {
        Error,
        Created,
        Joined,
        Left,
        List,
        Hosted,
        UpdateRoom,
        LoadedGame
    }

    public struct ClientNetworkMessage : NetworkMessage
    {
        public ClientCall call;
        public string matchid;
        public MatchInfo[] matches;
        public PlayerInfo[] players;
        public string message;
    }

    public struct ServerNetworkMessage : NetworkMessage
    {
        public ServerCall call;
        public string matchid;
        public MatchInfo match;
    }

    public class ServerManager : MonoBehaviour
    {
        public readonly Dictionary<string, MatchInfo> _openMatches = new Dictionary<string, MatchInfo>();
        public readonly Dictionary<string, HashSet<NetworkConnection>> _matchConnections = new Dictionary<string, HashSet<NetworkConnection>>();

        public readonly List<NetworkConnectionToClient> _pendingMatch = new List<NetworkConnectionToClient>();
        public readonly Dictionary<NetworkConnection, PlayerInfo> _playerConfigs = new Dictionary<NetworkConnection, PlayerInfo>();

        [Header("Properties")]
        [Scene, SerializeField]
        private string _gameScene;

        [Header("Diagnostics (do not modify)")]
        public string _selectedMatchId = string.Empty;
        public string joinedMatch = string.Empty;

        public static ServerManager Instance;

        private void Awake()
        {
            Instance = this;
        }

        protected string getRandomMatchID
        {
            get
            {
                int length = 5;
                string id = string.Empty;
                for (int i = 0; i < length; i++)
                {
                    var num = Random.Range(0, 36);

                    if (num < 26)
                    {
                        id += (char)(num + 65);
                    }
                    else
                    {
                        id += (num - 26).ToString();
                    }
                }

                Debug.Log($"New random match ID generated : {id}");
                return id;
            }
        }

        [Client]
        internal void SelectMatch(string matchid)
        {
            if (string.IsNullOrEmpty(matchid))
            {
                _selectedMatchId = string.Empty;
                UILobby.Instance.matchSelected = false;
                return;
            }

            _selectedMatchId = matchid;
            UILobby.Instance.matchSelected = true;
        }

        internal void OnStartServer()
        {
            if (NetworkServer.active == false) { return; }

            NetworkServer.RegisterHandler<ServerNetworkMessage>(OnServerReceiveMatchRequest, false);
        }

        internal void OnStartClient()
        {
            if (NetworkClient.active == false) { return; }

            NetworkClient.RegisterHandler<ClientNetworkMessage>(OnClientMatchResponse, false);
        }

        internal void OnStopServer()
        {
            if (NetworkServer.active == false) { return; }
            NetworkServer.UnregisterHandler<ServerNetworkMessage>();
        }

        internal void OnStopClient()
        {
            if (NetworkClient.active == false) { return; }
            NetworkClient.UnregisterHandler<ClientNetworkMessage>();
        }

        internal void OnServerAuthenticated(NetworkConnectionToClient conn)
        {
            StartCoroutine(OnServerAuthenticatedRoutine(conn));
        }

        private IEnumerator OnServerAuthenticatedRoutine(NetworkConnectionToClient conn)
        {
            if (NetworkServer.active == false) { yield break; }

            if ((string)(conn.authenticationData) == "" || conn.isAuthenticated == false)
                conn.Disconnect();

            _playerConfigs.Add(conn, new PlayerInfo { name = conn.authenticationData as string, isLeader = false });
            _pendingMatch.Add(conn);

            yield return null;
            yield return null;

            RefreshMatchList(conn);
        }

        internal void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            if (NetworkServer.active == false) { return; }
            _pendingMatch.Remove(conn);
        }

        [Client]
        internal void RequestCreateRoom(string title = "")
        {
            if (NetworkClient.active == false)
                return;

            var newId = getRandomMatchID;

            MatchInfo match = new MatchInfo
            {
                title = string.IsNullOrEmpty(title) == false ? title : $"NewRoom {newId}",
                matchid = newId,
                players = 1,
                maxplayers = 10,
            };

            NetworkClient.connection.Send(new ServerNetworkMessage { call = ServerCall.Create, matchid = newId, match = match});
        }

        [Client]
        internal void RequestJoinRoom(string matchid = "")
        {
            if (NetworkClient.active == false)
                return;

            if (string.IsNullOrEmpty(matchid) && string.IsNullOrEmpty(_selectedMatchId))
            {
                NetworkClient.connection.Send(new ServerNetworkMessage { call = ServerCall.None });
                return;
            }
         
            NetworkClient.connection.Send(new ServerNetworkMessage 
            {
                call = ServerCall.Join, 
                matchid = string.IsNullOrEmpty(matchid) == false ? matchid : _selectedMatchId,
            });
        }

        [Client]
        internal void RequestLeaveRoom()
        {
            if (NetworkClient.active == false)
                return;

            NetworkClient.connection.Send(new ServerNetworkMessage
            {
                call = ServerCall.Leave,
            });
        }

        [Client]
        internal void RequestLoadGame()
        {
            if (NetworkClient.active == false)
                return;

            NetworkClient.connection.Send(new ServerNetworkMessage { call = ServerCall.LoadGame });
        }

        private void OnServerReceiveMatchRequest(NetworkConnectionToClient conn, ServerNetworkMessage message)
        {
            switch (message.call)
            {
                case ServerCall.None:
                    break;
                case ServerCall.Create:
                    ServerCreateMatch(conn, message);
                    break;
                case ServerCall.Join:
                    ServerJoinMatch(conn, message);
                    break;
                case ServerCall.Leave:
                    ServerLeaveMatch(conn, message);
                    break;
                case ServerCall.LoadGame:
                    ServerLoadGame(conn, message);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected token");
            }
        }

        private void ServerLoadGame(NetworkConnectionToClient conn, ServerNetworkMessage message)
        {
            if (NetworkServer.active == false) { return; }

            var config = _playerConfigs[conn];
            if (_openMatches.ContainsKey(config.matchid) == false)
            {
                // send certain error
                return;
            }

            if(config.isLeader == false)
            {
                // send certain error
                return;
            }

            var playerConnections = _matchConnections[config.matchid];

            foreach(var pConn in playerConnections)
            {
                pConn.Send(new ClientNetworkMessage { call = ClientCall.LoadedGame });
            }

            _openMatches.Remove(config.matchid);
            RefreshMatchList();
        }

        private void ServerLeaveMatch(NetworkConnectionToClient conn, ServerNetworkMessage message)
        {
            if (NetworkServer.active == false)
                return;

            NetworkConnection pendingHostingConnection = null;
            PlayerInfo[] playerInfos;

            var playerMatchId = _playerConfigs[conn].matchid;

            if (_openMatches.ContainsKey(playerMatchId) == false)
            {
                conn.Send(new ClientNetworkMessage
                {
                    call = ClientCall.Error,
                    message = $"Match {playerMatchId} does not exist"
                });
                return;
            }

            conn.identity.GetComponent<NetworkMatch>().matchId = Guid.Empty;
            _matchConnections[playerMatchId].Remove(conn);

            var currentmatch = _openMatches[playerMatchId];
            currentmatch.players--;

            if (currentmatch.players <= 0)
                _openMatches.Remove(playerMatchId);
            else
            {
                _openMatches[playerMatchId] = currentmatch;

                if (_playerConfigs[conn].isLeader)
                {
                    pendingHostingConnection = _matchConnections[playerMatchId].First();
                    var playerToLeader = _playerConfigs[pendingHostingConnection];
                    playerToLeader.isLeader = true;
                    _playerConfigs[pendingHostingConnection] = playerToLeader;

                    playerInfos = _matchConnections[playerMatchId].Select(conn => _playerConfigs[conn]).ToArray();
                    pendingHostingConnection.Send(new ClientNetworkMessage { call = ClientCall.Hosted, players = playerInfos });
                }
            }
            
            var data = _playerConfigs[conn];
            data.matchid = string.Empty;
            data.isLeader = false;
            _playerConfigs[conn] = data;


            conn.Send(new ClientNetworkMessage { call = ClientCall.Left, matches = _openMatches.Values.ToArray() });
            playerInfos = _matchConnections[playerMatchId].Select(conn => _playerConfigs[conn]).ToArray();

            foreach (var matchConn in _matchConnections[playerMatchId])
            {
                if (pendingHostingConnection == matchConn)
                    continue;

                matchConn.Send(new ClientNetworkMessage
                {
                    call = ClientCall.UpdateRoom,
                    players = playerInfos,
                });
            }

            RefreshMatchList();
            _pendingMatch.Add(conn);
        }

        private void ServerCreateMatch(NetworkConnectionToClient conn, ServerNetworkMessage message)
        {
            if (NetworkServer.active == false || string.IsNullOrWhiteSpace(message.matchid))
                return;

            if (_openMatches.ContainsKey(message.matchid))
            {
                conn.Send(new ClientNetworkMessage
                {
                    call = ClientCall.Error,
                    message = $"Match {message.matchid} is already created"
                });
                return;
            }

            var sender = _playerConfigs[conn];
            sender.isLeader = true;
            sender.matchid = message.matchid;
            _playerConfigs[conn] = sender;

            _openMatches.Add(message.matchid, message.match);
            _matchConnections.Add(message.matchid, new HashSet<NetworkConnection> { conn });

            conn.Send(new ClientNetworkMessage
            {
                call = ClientCall.Created,
                matchid = message.matchid,
                players = new PlayerInfo[] { _playerConfigs[conn] }
            });

            conn.identity.GetComponent<NetworkMatch>().matchId = message.matchid.ToGuid();

            joinedMatch = message.matchid;
            _pendingMatch.Remove(conn);
            RefreshMatchList();
        }

        protected void RefreshMatchList(NetworkConnectionToClient conn = null)
        {
            if (NetworkServer.active == false)
                return;

            if(conn != null)
            {
                conn.Send(new ClientNetworkMessage { call = ClientCall.List, matches = _openMatches.Values.ToArray() });
            }
            else
            {
                foreach(var pendingConn in _pendingMatch)
                {
                    pendingConn.Send(new ClientNetworkMessage { call = ClientCall.List, matches = _openMatches.Values.ToArray() });
                }
            }
        }

        private void ServerJoinMatch(NetworkConnectionToClient conn, ServerNetworkMessage message)
        {
            if (NetworkServer.active == false || string.IsNullOrWhiteSpace(message.matchid))
                return;

            if (_openMatches.ContainsKey(message.matchid) == false)
            {
                conn.Send(new ClientNetworkMessage
                {
                    call = ClientCall.Error,
                    message = $"Match {message.matchid} does not exist"
                });
                return;
            }

            var match = _openMatches[message.matchid];
            match.players++;
            _openMatches[message.matchid] = match;
            _matchConnections[message.matchid].Add(conn);
            var playerInfos = _matchConnections[message.matchid].Select(conn => _playerConfigs[conn]).ToArray();

             var sender = _playerConfigs[conn];
            sender.matchid = message.matchid;
            _playerConfigs[conn] = sender;

            conn.Send(new ClientNetworkMessage { call = ClientCall.Joined, matchid = message.matchid, players = playerInfos });

            foreach (var matchConn in _matchConnections[message.matchid])
                matchConn.Send(new ClientNetworkMessage
                {
                    call = ClientCall.UpdateRoom,
                    players = playerInfos,
                });


            conn.identity.GetComponent<NetworkMatch>().matchId = message.matchid.ToGuid();

            _pendingMatch.Remove(conn);
            joinedMatch = message.matchid;
            RefreshMatchList();
        }

        private void OnClientMatchResponse(ClientNetworkMessage message)
        {
            switch (message.call)
            {
                case ClientCall.Error:
                    Debug.Log(message.message);
                    return;
                case ClientCall.Created:
                    UILobby.Instance.ShowRoom(message.matchid, true);
                    UILobby.Instance.RefreshPlayersInRoom(message.players);
                    return;
                case ClientCall.Joined:
                    UILobby.Instance.ShowRoom(message.matchid);
                    UILobby.Instance.RefreshPlayersInRoom(message.players);
                    return;
                case ClientCall.List:
                    UILobby.Instance.RefreshMatchList(message.matches);
                    return;
                case ClientCall.UpdateRoom:
                    UILobby.Instance.RefreshPlayersInRoom(message.players);
                    return;
                case ClientCall.Left:
                    SelectMatch(string.Empty);
                    UILobby.Instance.ShowLobby();
                    UILobby.Instance.RefreshMatchList(message.matches);
                    return;
                case ClientCall.Hosted:
                    SelectMatch(string.Empty);
                    UILobby.Instance.isLeader = true;
                    UILobby.Instance.RefreshPlayersInRoom(message.players);
                    return;
                case ClientCall.LoadedGame:
                    OnGameLoadCall();
                    return;
                default:
                    throw new InvalidOperationException("Unexpected token");
            }
        }

        [Client]
        protected void OnGameLoadCall()
        {
            SceneManager.LoadScene(_gameScene, LoadSceneMode.Single);
        }
        
    }
}

