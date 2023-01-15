using Mirror;
using System.Collections;
using UnityEngine;

public class AutheticationMatch : NetworkAuthenticator
{
    [SerializeField]
    internal string _nickname;
    public struct PlayerInfoMessage : NetworkMessage
    {
        public string _name;
    }

    public struct ResponeMessage : NetworkMessage
    {
        public int code;
        public string message;
    }

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<ResponeMessage>(OnAuthenticationResponse, false);
    }

    public override void OnStopClient()
    {
        NetworkClient.UnregisterHandler<ResponeMessage>();
    }

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<PlayerInfoMessage>(OnPlayerInfoReceived, false);
        OnServerAuthenticated.AddListener(MatchServerCollection.ServerManager.Instance.OnServerAuthenticated);
    }

    [Client]
    public override void OnStopServer()
    {
        NetworkServer.UnregisterHandler<PlayerInfoMessage>();
        OnServerAuthenticated.RemoveListener(MatchServerCollection.ServerManager.Instance.OnServerAuthenticated);
    }

    public void RegisterUser(string name)
    {
        _nickname = name;
    }

    public override void OnClientAuthenticate()
    {
        var message = new PlayerInfoMessage
        {
            _name = _nickname,
        };

        NetworkClient.connection.Send(message);
    }

    public override void OnServerAuthenticate(NetworkConnectionToClient conn) { }



    protected void OnPlayerInfoReceived(NetworkConnectionToClient conn, PlayerInfoMessage message)
    {
        ResponeMessage callback;

        if (string.IsNullOrEmpty(name))
        {
            callback = new ResponeMessage
            {
                code = 101,
                message = "Registration failed. Disconnect...",
            };

            conn.Send(callback);
            StartCoroutine(Disconnect(conn));
            conn.isAuthenticated = false;
            return;
        }

        callback = new ResponeMessage
        {
            code = 200,
            message = $"Registered new player. Name: {message._name} ",
        };

        conn.Send(callback);
        conn.authenticationData = message._name;
        conn.isAuthenticated = true;
        ServerAccept(conn);
    }

    protected void OnAuthenticationResponse(ResponeMessage message)
    {
        Debug.Log(message.message);

        if (message.code == 200)
            ClientAccept();
    }

    private IEnumerator Disconnect(NetworkConnectionToClient conn)
    {
        yield return null;
        yield return null;

        conn.Disconnect();
    }
}
