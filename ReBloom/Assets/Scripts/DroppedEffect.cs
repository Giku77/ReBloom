using UnityEngine;

public class DroppedEffect : MonoBehaviour
{
    Rigidbody rb;
    bool settled = false;
    float settleCheckTime = 0.5f; // 이 시간 동안 거의 안 움직이면 정착
    float timer = 0f;

    [SerializeField] float velocityThreshold = 0.05f;
    [SerializeField] float angularVelocityThreshold = 0.05f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false; // 드랍될 때 활성화
    }

    void Update()
    {
        if (settled) return;

        // 속도가 충분히 느린지 체크
        if (rb.linearVelocity.sqrMagnitude < velocityThreshold * velocityThreshold &&
            rb.angularVelocity.sqrMagnitude < angularVelocityThreshold * angularVelocityThreshold)
        {
            timer += Time.deltaTime;
            if (timer >= settleCheckTime)
            {
                Settle();
            }
        }
        else
        {
            timer = 0f;
        }
    }

    void Settle()
    {
        settled = true;
        rb.isKinematic = true; // 더 이상 물리 계산 안 함 (굴러가지도 않음)
    }
}