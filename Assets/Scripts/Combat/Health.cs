using System;
using Mirror;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    [SyncVar(hook = nameof(HealthUpdated))]
    [SerializeField] private int currentHealth;

    public event Action ServerOnDie;
    public event Action<int, int> ClientOnHealthUpdated;

    #region Server
    public override void OnStartServer()
    {
        currentHealth = maxHealth;
        UnitBase.ServerOnPlayerDie += ServerHandlePlayerDie;
    }

    public override void OnStopServer()
    {
        UnitBase.ServerOnPlayerDie -= ServerHandlePlayerDie;
    }

    private void ServerHandlePlayerDie(int connectionId)
    {
        if (connectionId == connectionToClient.connectionId) DealDamage(currentHealth);
    }

    [Server]
    public void DealDamage(int damage)
    {
        if (currentHealth == 0) return;
        currentHealth = Mathf.Max(currentHealth - damage, 0);
        if (currentHealth == 0)
        {
            ServerOnDie?.Invoke();
        }
    }
    #endregion

    #region Client
    private void HealthUpdated(int oldHealth, int newHealth)        
    {
        ClientOnHealthUpdated?.Invoke(newHealth, maxHealth);        //invoke this event when syncvar currenthealth is changed
    }
    #endregion

}
