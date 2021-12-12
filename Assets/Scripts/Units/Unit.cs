using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class Unit : NetworkBehaviour
{
    [SerializeField] private int resourceCost = 10;
    [SerializeField] private UnitMovement unitMovement = null;
    [SerializeField] private Targeter targeter = null;
    [SerializeField] private Health health = null;

    [SerializeField] private UnityEvent onSelected = null;
    [SerializeField] private UnityEvent onDeselected = null;

    public static event Action<Unit> ServerOnUnitSpawned;      
    public static event Action<Unit> ServerOnUnitDespawned;   

    public static event Action<Unit> AuthorityOnUnitSpawned;
    public static event Action<Unit> AuthorityOnUnitDespawned;


    public int GetResourceCost() => resourceCost;

    public UnitMovement GetUnitMovement() => unitMovement;

    public Targeter GetTargeter() => targeter;

    #region Server


    /// <Summary>
    /// Called on a server by Network Manager, when any 'unit' is dynamically spawned from unitSpawner.
    /// <para>Broadcasting to everyone- that this event happened with reference of 'this' particular unit with it.</para>
    /// </Summary>
    public override void OnStartServer()    //Since no involvement of client hence no[server] tag is used.
    {
        ServerOnUnitSpawned?.Invoke(this);
        health.ServerOnDie += ServerHandleDie;      //Destroy unit when health goes 0
    }

    /// <Summary>
    /// Called on a server when any 'unit' goes out of its existence.
    /// <para>Broadcasting to everyone- that this event happened with reference of 'this' particular unit with it.</para>
    /// </Summary>
    public override void OnStopServer()
    {
        ServerOnUnitDespawned?.Invoke(this);        //listen for these events and when invoked do the methods.
        health.ServerOnDie -= ServerHandleDie;      //Destroy unit when health goes 0
    }

    [Server]
    private void ServerHandleDie()
    {
        NetworkServer.Destroy(gameObject);
    }

    #endregion




    #region Client

    /// <Summary>
    /// Called on all existing unit's Client when a 'unit' is dynamically spawned via NetworkServer.Spawn().
    /// <para>if the client is client only & if 'this' client/window-(game instance) owns the unit.)</para>
    /// <para>Broadcasting to everyone- that this event happened with reference of 'this' particular unit with it.</para>
    /// </Summary>
    public override void OnStartClient()
    {
        if (!hasAuthority) return;
        AuthorityOnUnitSpawned?.Invoke(this);           //called repetetively once by server and once by client
    }


    /// <Summary>
    /// Called on all existing unit's Client when any 'unit' goes out of its existence.
    /// <para>if the client is client only & if 'this' client/window-(game instance) owns the unit.)</para>
    /// <para>Broadcasting to everyone- that this event happened with reference of 'this' particular unit with it.</para>
    /// </Summary>
    public override void OnStopClient()
    {
        //print("unit isClientOnly: " + isClientOnly);   //onStopServer is called before onStopClient, so it will be true
        if (!hasAuthority) return;
        AuthorityOnUnitDespawned?.Invoke(this);
    }



    [Client]                            //this will happen in the client only and no one else will know.
    public void Select()
    {
        if (!hasAuthority) return;      //if this client/window -(game instance) owns this particular unit which is defined by 
                                        //NetworkServer.Spawn(unitInstance, connectionToClient) in unitSpawner script.
        onSelected?.Invoke();
    }

    [Client]
    public void Deselect()
    {
        if (!hasAuthority) return;      //if this client/window -(game instance) owns this particular unit which is defined by 
                                        //NetworkServer.Spawn(unitInstance, connectionToClient) in unitSpawner script.
        onDeselected?.Invoke();
    }
    #endregion
}