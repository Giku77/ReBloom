using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private Transform followDummy;   // 위치/각도만 옮길 빈 오브젝트

    public async UniTask MoveToTargetAsync(Transform target, Vector3 offset, float duration)
    {
        if (target == null) return;

        var token = this.GetCancellationTokenOnDestroy();

        Vector3 startPos = followDummy.position;
        Vector3 endPos   = target.position + offset;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / duration);

            followDummy.position = Vector3.Lerp(startPos, endPos, alpha);
            followDummy.LookAt(target);

            await UniTask.Yield(token);
        }

        followDummy.position = endPos;
        followDummy.LookAt(target);
    }
}
