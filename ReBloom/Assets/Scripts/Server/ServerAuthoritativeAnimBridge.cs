using Unity.Netcode;
using UnityEngine;

public class ServerAuthoritativeAnimBridge : NetworkBehaviour
{
    [SerializeField] private PlayerAnimation anim;
    [SerializeField] private Rigidbody rb;

    [SerializeField] private float sendInterval = 0.05f; // 20Hz
    [SerializeField] private float clientSmooth = 18f;   // 클라에서 보간 강하게
    [SerializeField] private float minDeltaToSend = 0.005f;

    private float _nextSendTime;
    private float _lastSent;

    // 서버가 쓰고 모두가 읽는 목표 speed
    private NetworkVariable<float> _serverTargetSpeed =
        new NetworkVariable<float>(0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private float _displaySpeed; // 각 클라가 최종 표시할 보간 값

    private void Awake()
    {
        if (!anim) anim = GetComponent<PlayerAnimation>();
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // 오너: 로컬 즉시 반영(예측) + 서버에 주기적으로 제출
        if (IsOwner)
        {
            float speed = 0f;
            if (rb != null)
            {
                var v = rb.linearVelocity;
                v.y = 0f;
                speed = v.magnitude;
            }

            // 로컬 즉시 적용(체감 부드러움 ↑)
            // (오너는 서버값을 기다리지 말고 바로 애니 돌려도 됨)
            anim?.SetSpeed(speed);

            if (Time.unscaledTime >= _nextSendTime)
            {
                _nextSendTime = Time.unscaledTime + sendInterval;

                if (Mathf.Abs(speed - _lastSent) >= minDeltaToSend)
                {
                    _lastSent = speed;

                    if (IsServer) _serverTargetSpeed.Value = speed;
                    else SubmitSpeedServerRpc(speed);
                }
            }
        }

        // 비오너(원격 프록시 포함): 서버 목표값을 로컬에서 매 프레임 스무딩
        if (!IsOwner)
        {
            _displaySpeed = Mathf.Lerp(_displaySpeed, _serverTargetSpeed.Value, Time.deltaTime * clientSmooth);
            anim?.SetSpeed(_displaySpeed);
        }
    }

    // 여기서는 "서버 목표값만 갱신"
    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    private void SubmitSpeedServerRpc(float speed)
    {
        _serverTargetSpeed.Value = speed;
    }
}
