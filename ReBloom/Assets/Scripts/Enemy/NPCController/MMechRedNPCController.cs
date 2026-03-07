using Unity.Netcode;
using UnityEngine;

public class MMechRedNPCController : BaseNPCController
{
    private const int FxHit = 1;

    [Header("Pastrol Settings")]
    public Transform[] patrolPoints;

    private int index = -1;

    [Header("Stun Setting")]
    [SerializeField] private float stunTime = 2f;

    private MMechNPCSound sound;

    protected override void InitializeState()
    {
        sound = GetComponentInChildren<MMechNPCSound>();

        MoveNext();
    }

    protected override void Update()
    {
        base.Update();

        if (!HasServerAuthority)
            return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            MoveNext();
        }

        UpdateAnimation();
    }

    protected override void UpdateAnimation()
    {
        if (animator != null && agent != null)
        {
            bool isMoving = !agent.isStopped && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;
            float speed = isMoving ? agent.velocity.magnitude : 0f;
            animator.SetFloat("Speed", speed);
        }
    }

    protected override void HandleFxEvent(int fxEventId, Vector3 position, Vector3 direction, float value)
    {
        if (fxEventId == FxHit)
            sound?.PlayHit();
    }

    private void MoveNext()
    {
        index = (index + 1) % patrolPoints.Length;
        agent.isStopped = false;
        agent.SetDestination(patrolPoints[index].position);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            return;

        if (other.gameObject.layer != LayerMask.NameToLayer("Player"))
            return;

        NetworkPlayerOwnerGate gate = other.gameObject.GetComponent<NetworkPlayerOwnerGate>();
        if (gate == null)
            return;

        gate.ApplyAuthoritativeStun(stunTime);
        BroadcastFxEvent(FxHit);
    }
}
