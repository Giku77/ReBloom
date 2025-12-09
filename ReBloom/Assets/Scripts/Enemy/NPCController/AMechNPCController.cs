using System;
using UnityEngine;

public class AMechNPCController : BaseNPCController
{
    [Header("Pastrol Settings")]
    public Transform[] patrolPoints;

    [Header("Detection")]
    public float detectionRange = 4f;
    public float detectionAngle = 60f;

    [Header("CallReinforcement")]
    public float callInterval = 7f;

    private int index = -1;
    private bool waiting = false;
    private float waitTime = 2f;
    private float timer = 0f;

    public static event Action OnPlayerDetected;

    protected override void InitializeState()
    {
        MoveNext();
    }

    protected override void Update()
    {
        base.Update();

        CheckVision();

        if (waiting)
        {
            timer += Time.deltaTime;
            if (timer >= waitTime)
            {
                waiting = false;
                MoveNext();
            }
            return;
        }
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
                OnPlayerDetected?.Invoke();
        }
    }

    private void MoveNext()
    {
        index = (index + 1) % patrolPoints.Length;
        agent.isStopped = false;
        agent.SetDestination(patrolPoints[index].position);
    }
}
