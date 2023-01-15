using Mirror;
using UnityEngine;
using MatchServerCollection;

public class MatchManager20 : NetworkManager
{

    [Scene, SerializeField]
    protected string _gameScene;

    public override void OnStartServer() => ServerManager.Instance.OnStartServer();

    public override void OnStartClient() => ServerManager.Instance.OnStartClient();

    public override void OnStopServer() => ServerManager.Instance.OnStopServer();

    public override void OnStopClient() => ServerManager.Instance.OnStopClient();

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        ServerManager.Instance.OnServerDisconnect(conn);
        base.OnServerDisconnect(conn);
    }


}
