using UnityEngine;

[ExecuteAlways]
public class PostProcessingScanOrigin : MonoBehaviour
{
    public Material material;

    void LateUpdate()
    {
        // Null 체크 추가
        if (material == null)
            return;

        material.SetVector("_Origin", transform.position);
    }
}