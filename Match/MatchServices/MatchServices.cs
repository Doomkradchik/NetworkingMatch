using System;
using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class MatchServices : NetworkBehaviour
{
    [Serializable]
    public struct MatchConfig
    {
        public Guid matchId;
        public int maxplayers;
        public List<PlayerConfig> players;
    }

    [Serializable]
    public struct PlayerConfig
    {
        public string name;
        public bool ready;
    }

    public SyncDictionary<Guid, MatchConfig> _openMatches = new SyncDictionary<Guid, MatchConfig>();
    //server side
    public static Dictionary<Guid, HashSet<NetworkConnectionToClient>> _matchConnections;

    [Command]
    public void CmdCreateMatch(PlayerConfig player)
    {
        var newGuid = Guid.NewGuid();
        MatchConfig match = new MatchConfig
        {
            matchId = newGuid,
            maxplayers = 10,
            players = new List<PlayerConfig>() { player },
        };

        _openMatches.Add(newGuid, match);
        _matchConnections.Add(newGuid, new HashSet<NetworkConnectionToClient>() { connectionToClient });

        TargetOnMatchCreated(connectionToClient, match);
    }

    [TargetRpc]
    protected void TargetOnMatchCreated(NetworkConnection conn, MatchConfig newMatch)
    {
        
    }




}
