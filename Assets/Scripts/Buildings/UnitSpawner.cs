using System;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// listens for mouse click on unitSpawner and instantiate 'unit' & set its authority.
/// <para>If the Client clicks on the 'unitSpawner' instance and also got the authority of it then the server will spawn 'unit' instance</para>
/// <para>in all clients at the position defined by 'unitSpawnPoint' & set its authority to the client who clicked.</para>
/// </summary>
public class UnitSpawner : NetworkBehaviour, IPointerClickHandler
{
    [SerializeField] private Unit unitPrefab = null;
    [SerializeField] private Transform unitSpawnPoint = null;
    [SerializeField] private Health health = null;
    [SerializeField] private GameObject unitSpawningCanvas = null;
    [SerializeField] private TMP_Text remainingUnitsText = null;
    [SerializeField] private Image unitProgressImage = null;
    [SerializeField] private int maxUnitQueue = 5;
    [SerializeField] private float spawnMoveRange = 4f;
    [SerializeField] private float unitSpawnDuration = 5f;



    [SyncVar (hook = nameof(ClientHandleQueuedUnitsUpdated))]
    private int queuedUnits;

    [SyncVar]
    private float unitTimer;

    private float progressImageVelocity;

    private void Update()
    {
        if (isServer)
        {
            ProduceUnits();
        }
        if (isClient)
        {
            UpdateTimerDisplay();
        }
    }


    #region Server
    public override void OnStartServer()
    {
        health.ServerOnDie += ServerHandleDie;
    }

    public override void OnStopServer()
    {
        health.ServerOnDie -= ServerHandleDie;
    }

    [Server]
    private void ServerHandleDie()
    {
        NetworkServer.Destroy(gameObject);
    }


    [Server]
    private void ProduceUnits()
    {
        if (queuedUnits == 0) return;
        unitTimer += Time.deltaTime;
        if (unitTimer < unitSpawnDuration) return;      //when unitTimer becomes 5second according to deltatime  spawn new unit
        GameObject unitInstance = Instantiate(
            unitPrefab.gameObject,
            unitSpawnPoint.position,
            unitSpawnPoint.rotation);
        NetworkServer.Spawn(unitInstance, connectionToClient);  //giving authority of this unit to the connectionToClient, uptill here things are happening only in that client no server or other client kows abt it

        //my method
        TargetSelectSpawnedUnit(connectionToClient, unitInstance);

        Vector3 spawnOffset = Random.insideUnitSphere * spawnMoveRange;
        spawnOffset.y = unitSpawnPoint.position.y;
        UnitMovement unitMovement = unitInstance.GetComponent<UnitMovement>();
        unitMovement.ServerMove(unitSpawnPoint.position+spawnOffset);

        queuedUnits--;
        unitTimer = 0f;
    }


    /// <summary>
    /// Called on a server, when a client clicks his own 'unitSpawner' Gameobject.
    /// <para>Instantiating 'unit' on all clients & give authority to this particular client (particular 'this' window on whch game is running).</para>
    /// <para>And also select that unit and add it to the 'selectedUnit' list in unitSelectionHandler component.</para>
    /// </summary>
    [Command]
    private void CmdSpawnUnit()
    {
        if (queuedUnits == maxUnitQueue) return;
        RTSPlayer player = connectionToClient.identity.GetComponent<RTSPlayer>();
        if (player.GetResources() < unitPrefab.GetResourceCost()) return;
        queuedUnits++;
        player.SetResources(player.GetResources() - unitPrefab.GetResourceCost());
    }

    [TargetRpc]
    private void TargetSelectSpawnedUnit(NetworkConnection target, GameObject unitInstance)
    {
        //complete it.
        Unit unit = unitInstance.GetComponent<Unit>();
        List<Unit> selectedUnits = GameObject.FindObjectOfType<UnitSelectionHandler>().SelectedUnits;
        foreach (Unit selectedUnit in selectedUnits)
        {
            selectedUnit.Deselect();        //Deselect all units in the 'selectedUnits' list
        }
        selectedUnits.Clear();
        selectedUnits.Add(unit);
        unit.Select();
    }
    #endregion




    #region Client

    private void UpdateTimerDisplay()
    {
        float newProgress = unitTimer / unitSpawnDuration;
        if (newProgress < unitProgressImage.fillAmount)
        {
            unitProgressImage.fillAmount = newProgress;
        }
        else
        {
            unitProgressImage.fillAmount = Mathf.SmoothDamp(
                unitProgressImage.fillAmount,
                newProgress,
                ref progressImageVelocity,
                0.1f);
        }
    }

    private void ClientHandleQueuedUnitsUpdated(int oldUnit, int newUnit)
    {
        remainingUnitsText.text = newUnit.ToString();
    }


    /// <summary>
    /// Called on client- this particular gameObject's method when this gameObject is clicked.
    /// <para>Calls a function on Server for spawning unit- if client or window who clicked has the authority of 'this' component's Gameobject.</para>
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Middle) return;
        if (!hasAuthority) return;                  //actually RtsnetworkManager set this unitSpawner's authority only to the player on who's addition it is spawned for all else its negative. now every client has its own code which has an entire copy of what is in the game scene but some authrity is set and some not.
        CmdSpawnUnit();
    }
    #endregion
}

/*
 * 
 * now upon run every client will have all clients unitSpawner (no authority) in its heirarchy along with his own unitSpawner
 * (authority) but when you click it the unit will check, which game object is clicked among all unitSpawner and call the 
 * clicked gameObject method , which will check if we had the authorty of it- if yes we spawn the unit.
 * 
*/