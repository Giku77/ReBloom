using UnityEngine;

public class DebugSize : MonoBehaviour
{
    private void Start()
    {
        var mr = GetComponentInChildren<MeshRenderer>();
        if (mr != null)
        {
            Debug.Log($"Mesh size: {mr.bounds.size}");
        }

        var col = GetComponentInChildren<Collider>();
        if (col != null)
        {
            Debug.Log($"Collider size: {col.bounds.size}");
        }
    }
}
