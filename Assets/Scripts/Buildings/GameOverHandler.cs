using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GameOverHandler : NetworkBehaviour
{
    public static event Action ServerOnGameOver;

    public static event Action<string> ClientOnGameOver;

    [SerializeField] private List<UnitBase> Bases = null;


    #region Server
    public override void OnStartServer()
    {
        UnitBase.ServerOnBaseSpawned += ServerHandleBaseSpawned;
        UnitBase.ServerOnBaseDespawned += ServerHandleBaseDespawned;
    }

    private void OnDestroy()        //because OnStopServer is never called... and as a result though the go is destroyed but it is still listening for the unitbase events
    {
        UnitBase.ServerOnBaseSpawned -= ServerHandleBaseSpawned;
        UnitBase.ServerOnBaseDespawned -= ServerHandleBaseDespawned;
    }

    [Server]
    private void ServerHandleBaseSpawned(UnitBase unitBase)
    {
        Bases.Add(unitBase);
    }

    [Server]
    private void ServerHandleBaseDespawned(UnitBase unitBase)
    {
        Bases.Remove(unitBase);
        if (Bases.Count != 1) return;
        int playerID = Bases[0].connectionToClient.connectionId;
        RpcGameOver($"Player {playerID}");
        ServerOnGameOver?.Invoke();
    }
    #endregion



    #region Client

    [ClientRpc]
    private void RpcGameOver(string winner)
    {
        ClientOnGameOver?.Invoke(winner);
    }

    #endregion
}
