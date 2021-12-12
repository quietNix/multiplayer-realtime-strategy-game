using Mirror;
using UnityEngine;

public class UnitFiring : NetworkBehaviour
{
    [SerializeField] private Targeter targeter = null;
    [SerializeField] private GameObject projectilePrefab = null;
    [SerializeField] private Transform projectileSpawnPoint = null;
    [SerializeField] private float fireRange = 2f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float rotationSpeed = 20f;

    private float lastFireTime;

    #region Server

    [ServerCallback]        //only call it on server...but unity will try to call it; but mirror wont allow...hence for no warninh...serverCallback
    private void Update()
    {
        Targetable target = targeter.GetTarget();

        if (target == null) return;

        if (!CanFireAtTarget()) return;

        Quaternion targetRotation = Quaternion.LookRotation(target.transform.position - transform.position);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if(Time.time>(1/fireRate) + lastFireTime)
        {
            Quaternion projectileRotation = Quaternion.LookRotation(
                target.GetAimPoint().position - projectileSpawnPoint.position);

            GameObject projectileInstance = Instantiate(
                projectilePrefab, projectileSpawnPoint.position, projectileRotation);

            NetworkServer.Spawn(projectileInstance, connectionToClient);

            lastFireTime = Time.time;
        }
        
    }

    [Server]        //since we are calling so no callback
    private bool CanFireAtTarget()
    {
        return ((targeter.GetTarget().transform.position - transform.position).sqrMagnitude <= fireRange * fireRange);
    }
    #endregion

}
