using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ServerAuthoritativeAnimBridge : NetworkBehaviour
{
    [SerializeField] private PlayerAnimation anim;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private float sendInterval = 0.05f;
    [SerializeField] private float clientSmooth = 18f;
    [SerializeField] private float minDeltaToSend = 0.005f;

    private readonly Dictionary<int, bool> lastSentBools = new();
    private readonly Dictionary<int, int> lastSentInts = new();

    private float nextSendTime;
    private float lastSentSpeed;
    private float displaySpeed;

    private readonly NetworkVariable<float> serverTargetSpeed =
        new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (!anim) anim = GetComponent<PlayerAnimation>();
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (IsOwner)
        {
            float speed = 0f;
            if (rb != null)
            {
                var velocity = rb.linearVelocity;
                velocity.y = 0f;
                speed = velocity.magnitude;
            }

            anim?.ApplyFloatLocal(PlayerAnimation.Speed, speed);

            if (Time.unscaledTime >= nextSendTime)
            {
                nextSendTime = Time.unscaledTime + sendInterval;

                if (Mathf.Abs(speed - lastSentSpeed) >= minDeltaToSend)
                {
                    lastSentSpeed = speed;

                    if (IsServer)
                        serverTargetSpeed.Value = speed;
                    else
                        SubmitSpeedServerRpc(speed);
                }
            }
        }

        if (!IsOwner)
        {
            displaySpeed = Mathf.Lerp(displaySpeed, serverTargetSpeed.Value, Time.deltaTime * clientSmooth);
            anim?.ApplyFloatLocal(PlayerAnimation.Speed, displaySpeed);
        }
    }

    public void ReportBoolParam(int hash, bool value)
    {
        if (!ShouldForwardClientOwnedAnimation())
            return;

        if (lastSentBools.TryGetValue(hash, out var prev) && prev == value)
            return;

        lastSentBools[hash] = value;
        SubmitBoolParamServerRpc(hash, value);
    }

    public void ReportIntParam(int hash, int value)
    {
        if (!ShouldForwardClientOwnedAnimation())
            return;

        if (lastSentInts.TryGetValue(hash, out var prev) && prev == value)
            return;

        lastSentInts[hash] = value;
        SubmitIntParamServerRpc(hash, value);
    }

    public void ReportTriggerParam(int hash)
    {
        if (!ShouldForwardClientOwnedAnimation())
            return;

        SubmitTriggerParamServerRpc(hash);
    }

    private bool ShouldForwardClientOwnedAnimation()
    {
        return IsSpawned && IsOwner && !IsServer;
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    private void SubmitSpeedServerRpc(float speed)
    {
        serverTargetSpeed.Value = speed;
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable)]
    private void SubmitBoolParamServerRpc(int hash, bool value)
    {
        anim?.ApplyBoolLocal(hash, value);
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable)]
    private void SubmitIntParamServerRpc(int hash, int value)
    {
        anim?.ApplyIntLocal(hash, value);
    }

    [ServerRpc(Delivery = RpcDelivery.Reliable)]
    private void SubmitTriggerParamServerRpc(int hash)
    {
        anim?.ApplyTriggerLocal(hash);
    }
}
