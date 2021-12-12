using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class RTSPlayer : NetworkBehaviour
{
    [SerializeField] private Transform mainCameraTransform = null;
    [SerializeField] private LayerMask buildingLayerMask = new LayerMask();
    [SerializeField] private Building[] buildings = new Building[0];
    [SerializeField] private float buildingRangeLimit = 4f;


    [SyncVar (hook=nameof(ClientHandleResourcesUpdated))]
    private int resources = 500;
    [SyncVar (hook =nameof(AuthorityHandlePartyOwnerStateUpdated))]
    private bool ispartyOwner = false;
    [SyncVar (hook =nameof(ClientHandleDisplayNameUpdated))]
    private string displayName = null;


    public event Action<int> ClientOnResourcesUpdated;

    public static event Action<bool> AuthorityOnPartyOwnerStateUpdated;
    public static event Action ClientOnInfoUpdated;

    private Color teamColor = new Color();
    private List<Unit> myUnits = new List<Unit>();
    [SerializeField]
    private List<Building> myBuilding = new List<Building>();

    public Transform GetCameraTransform() => mainCameraTransform;
    public int GetResources() => resources;
    public List<Unit> GetPlayerUnits() => myUnits;
    public List<Building> GetPlayerBuildings() => myBuilding;
    public Color GetTeamColor() => teamColor;
    public bool GetIsPartyOwner() => ispartyOwner;
    public string GetDisplayName() => displayName;



    public bool CanPlaceBuilding(BoxCollider buildingCollider, Vector3 point)
    {
        if (Physics.CheckBox(point + buildingCollider.center,
            buildingCollider.size / 2,
            Quaternion.identity,
            buildingLayerMask)) return false;
        foreach (Building building in myBuilding)
        {
            print(building.GetId());
            if ((point - building.transform.position).sqrMagnitude <= buildingRangeLimit * buildingRangeLimit)return true;
        }
        return false;
    }


    #region Server      //the unitlist will only be shown to server or lets say host cause he is also technically server + client , and it will unit of each player

    /// <summary>
    /// Called on server in all different client's 'RTSPlayer' component when any 'unit' is dynamically spawned from unitSpawner.
    /// <para>listen for event in 'unit' component and when that happens- subscribe that specific unit with the method</para>
    /// <para>add that unit to that RTSPlayer's myUnit list whose client owns that specfic unit (which is set in unitSpawner).</para>
    /// </summary>
    public override void OnStartServer()
    {
        Unit.ServerOnUnitSpawned += ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned += ServerHandleUnitDespawned;

        Building.ServerOnBuildingSpawned += ServerHandleBuildingSpawned;
        Building.ServerOnBuildingDespawned += ServerHandleBuildingDespawned;

        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Called on a server of all 'RTSPlayer' component when any 'unit' goes out of its existence.
    /// <para>listen for event in 'unit' component and when that happens- unsubscribe that specific unit from the method</para>
    /// <para>remove that unit from that RTSPlayer's myUnit list whose client owns that specfic unit (which is set in unitSpawner).</para>
    /// </summary>
    public override void OnStopServer()
    {
        Unit.ServerOnUnitSpawned -= ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned -= ServerHandleUnitDespawned;

        Building.ServerOnBuildingSpawned -= ServerHandleBuildingSpawned;
        Building.ServerOnBuildingDespawned -= ServerHandleBuildingDespawned;
    }

    [Server]
    public void SetDisplayName(string newDisplayName)
    {
        displayName = newDisplayName;
    }

    [Server]
    public void SetPartyOwner(bool state)
    {
        ispartyOwner = state;
    }

    [Server]
    public void SetTeamColor(Color newTeamColor)
    {
        teamColor = newTeamColor;
    }

    [Server]
    public void SetResources(int newResources)
    {
        resources = newResources;
    }

    [Command]
    public void CmdStartGame()
    {
        if (!ispartyOwner) return;
        ((RTSNetworkManager)NetworkManager.singleton).StartGame();
    }

    [Command]
    public void CmdTryPlaceBuilding(int buildingId, Vector3 point)
    {
        Building buildingToPlace = null;

        foreach (Building building in buildings)
        {
            if (building.GetId() == buildingId)
            {
                buildingToPlace = building;
                break;
            }
        }

        if (buildingToPlace == null) return;
        if (resources < buildingToPlace.GetPrice()) return;

        BoxCollider buildingCollider = buildingToPlace.GetComponent<BoxCollider>();

        if (!CanPlaceBuilding(buildingCollider, point)) return;

        GameObject buildingInstance =
            Instantiate(buildingToPlace.gameObject, point, buildingToPlace.transform.rotation);

        NetworkServer.Spawn(buildingInstance, connectionToClient);

        SetResources(resources - buildingToPlace.GetPrice());
    }




    private void ServerHandleUnitSpawned(Unit unit)
    {
        //if (!hasAuthority) return;        //since it is server which will have all clients copy and no authority(set in unitSpawner) 
                                          //so to keep the information of unit spawned we dont use it.
        if (unit.connectionToClient.connectionId != connectionToClient.connectionId) return;
                                                //but we check out of those all clients i.e different RTSPlayer script copy which one 
                                                //has the same client id. generally the process is...unit is spawned server calls this 
                                                //method in all client's rtsplayer script...so 1 to many script call...check if that 
                                                //unit has same client in all client...whichever matches  ...its added
                                                //and this information is stored in server only and only known by server
        myUnits.Add(unit);
    }

    private void ServerHandleUnitDespawned(Unit unit)
    {
        if (unit.connectionToClient.connectionId != connectionToClient.connectionId) return;
        myUnits.Remove(unit);
    }

    private void ServerHandleBuildingSpawned(Building building)
    {
        if (building.connectionToClient.connectionId != connectionToClient.connectionId) return;
        myBuilding.Add(building);
    }

    private void ServerHandleBuildingDespawned(Building building)
    {
        if (building.connectionToClient.connectionId != connectionToClient.connectionId) return;
        myBuilding.Remove(building);
    }


    #endregion



    #region Client


    private void ClientHandleResourcesUpdated(int oldResource, int newResource)
    {
        ClientOnResourcesUpdated?.Invoke(newResource);
    }

    private void ClientHandleDisplayNameUpdated(string oldDisplayName, string newDisplayName)
    {
        ClientOnInfoUpdated?.Invoke();
    }


    public override void OnStartClient()        //or use onStartAuthority
    {
        
        if (NetworkServer.active || !isClientOnly) return;       //repetetive since its already happening in unit script, dont even subscribe this methodif this is server or host since onServersTart is taking care of it.
                                                                                  //since every client will have one rtsplayer for every client so checking which player we had authority of.

        ((RTSNetworkManager)NetworkManager.singleton).Players.Add(this);
        DontDestroyOnLoad(gameObject);

        if (!hasAuthority) return;

        //if (unit.connectionToClient.connectionId != connectionToClient.connectionId) return;    //we are not doing this because we are client and we dpont know different connection to different client.. and actually its not required, ...also a clent can only call it on its own object so no need
        Unit.AuthorityOnUnitSpawned += AuthorityHandleUnitSpawned;        //listen for event in unit and when that happens subscribe 
        Unit.AuthorityOnUnitDespawned += AuthorityHandleUnitDespawned;      //or unsubscribe from the method

        Building.AuthorityOnBuildingSpawned += AuthorityHandleBuildingSpawned;
        Building.AuthorityOnBuildingDespawned += AuthorityHandleBuildingDespawned;
    }

    public override void OnStopClient()
    {
        ClientOnInfoUpdated?.Invoke();

        if (!isClientOnly) return; //since every client will have one rtsplayer for every client so checking which player we had authority of.

        ((RTSNetworkManager)NetworkManager.singleton).Players.Remove(this);

        if (!hasAuthority) return;

        Unit.AuthorityOnUnitSpawned -= AuthorityHandleUnitSpawned;        //listen for event in unit and when that happens subscribe 
        Unit.AuthorityOnUnitDespawned -= AuthorityHandleUnitDespawned;      //or unsubscribe from the method

        Building.AuthorityOnBuildingSpawned -= AuthorityHandleBuildingSpawned;
        Building.AuthorityOnBuildingDespawned -= AuthorityHandleBuildingDespawned;
    }

    private void AuthorityHandleBuildingSpawned(Building building)
    {
        myBuilding.Add(building);
    }

    private void AuthorityHandleBuildingDespawned(Building building)
    {
        myBuilding.Remove(building);
    }

    private void AuthorityHandleUnitSpawned(Unit unit)
    {
        myUnits.Add(unit);
    }

    private void AuthorityHandleUnitDespawned(Unit unit)
    {
        myUnits.Remove(unit);
    }

    private void AuthorityHandlePartyOwnerStateUpdated(bool oldState, bool newState)
    {
        //here you can tranfer ownership....todo
        if (!hasAuthority) return;
        AuthorityOnPartyOwnerStateUpdated.Invoke(newState);
    }
    #endregion




}

/*
 * 
 * Since upon NetworkManager- or server will have all client's player copy in it... and i.e that many rtsPlayer component script
 * so when any unit is spawned- the onStartServer is called in all those server's rtsPlayerScript and whose connection id matches 
 * that of unit which is set in unitSpawner...it add it into its players unitList.
 * 
 * now every client will also have every clients player which is spawned by server 
 *  now upon run every client will have all others clients unitSpawner (no authority) in its heirarchy along with his own unitSpawner
 * (authority) and the client to know abt which unit belongs to it... simply hasAuthrity is enough
 * 
 */