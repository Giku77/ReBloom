using System;
using UnityEngine;

public class AMechNPCController : BaseNPCController
{
    private const int FxDetection = 1;

    [Header("Pastrol Settings")]
    public Transform[] patrolPoints;

    [Header("Detection")]
    public float detectionRange = 4f;
    public float detectionAngle = 60f;

    [Header("CallReinforcement")]
    public float callInterval = 7f;

    public AMechNPCSound sound;

    private int index = -1;

    public static event Action OnPlayerDetected;

    protected override void InitializeState()
    {
        sound = GetComponentInChildren<AMechNPCSound>();

        MoveNext();
    }

    protected override void Update()
    {
        base.Update();

        if (!HasServerAuthority)
            return;

        CheckVision();

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            MoveNext();
        }
    }

    protected override void HandleFxEvent(int fxEventId, Vector3 position, Vector3 direction, float value)
    {
        if (fxEventId == FxDetection)
            sound?.PlayDetection();
    }

    public void PlayDetectionFx()
    {
        BroadcastFxEvent(FxDetection);
    }

    private void CheckVision()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return;

        Vector3 dir = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > detectionAngle * 0.5f) return;

        if (Physics.Raycast(transform.position, dir, out RaycastHit hit, detectionRange))
        {
            if (hit.transform == player)
            {
                PlayDetectionFx();
                OnPlayerDetected?.Invoke();
            }
        }
    }

    private void MoveNext()
    {
        index = (index + 1) % patrolPoints.Length;
        agent.isStopped = false;
        agent.SetDestination(patrolPoints[index].position);
    }
}
