using Mirror;
using UnityEngine;

public class UnitProjectile : NetworkBehaviour
{
    [SerializeField] private Rigidbody rb = null;
    [SerializeField] private int damageToDeal = 20;
    [SerializeField] private float destroyAfterSeconds = 5f;
    [SerializeField] private float launchForce = 10f;

    private void Start()
    {
        rb.velocity = transform.forward * launchForce;
    }

    public override void OnStartServer()
    {
        Invoke(nameof(DestroySelf), destroyAfterSeconds);
    }

    [ServerCallback]        //unity will call it...but mirror will ytell never minfd...not to call on client
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<NetworkIdentity>(out NetworkIdentity networkIdentity)) return;
        //if (networkIdentity.hasAuthority) return;                             //if it was running in client then use it
        if (networkIdentity.connectionToClient == connectionToClient) return;   //but since it running in server it wont work.
        if(!other.TryGetComponent<Health>(out Health health)) return;
        health.DealDamage(damageToDeal);
        DestroySelf();
    }

    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}
