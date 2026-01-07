using Unity.Netcode;
using UnityEngine;

public class ServerAuthoritativeAnimBridge : NetworkBehaviour
{
    [SerializeField] private PlayerAnimation anim;
    [SerializeField] private Rigidbody rb;

    // 오너 -> 서버로 보낼 때
    private float _lastSent;

    // 서버가 최종적으로 애니에 넣을 값
    private float _serverTargetSpeed;
    private float _serverSmoothedSpeed;

    [SerializeField] private float sendInterval = 0.05f;   // 20Hz
    private float _nextSendTime;

    [SerializeField] private float serverSmooth = 12f;     // 클수록 더 빠르게 따라감

    private void Awake()
    {
        if (!anim) anim = GetComponent<PlayerAnimation>();
        if (!rb) rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // ===== 오너에서 speed 계산/전송 =====
        if (IsOwner)
        {
            float speed = 0f;
            if (rb != null)
            {
                var v = rb.linearVelocity;
                v.y = 0f;
                speed = v.magnitude;
            }

            // 전송 주기 제한(프레임마다 보내지 않기)
            if (Time.unscaledTime >= _nextSendTime)
            {
                _nextSendTime = Time.unscaledTime + sendInterval;

                // 아주 작은 변화는 무시(선택)
                if (Mathf.Abs(speed - _lastSent) >= 0.02f)
                {
                    _lastSent = speed;

                    if (IsServer) _serverTargetSpeed = speed;
                    else SubmitSpeedServerRpc(speed);
                }
            }
        }

        // ===== 서버에서 애니 값 스무딩 =====
        if (IsServer)
        {
            _serverSmoothedSpeed = Mathf.Lerp(_serverSmoothedSpeed, _serverTargetSpeed, Time.deltaTime * serverSmooth);
            anim?.SetSpeed(_serverSmoothedSpeed);
        }
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    private void SubmitSpeedServerRpc(float speed)
    {
        _serverTargetSpeed = speed;
    }
}
