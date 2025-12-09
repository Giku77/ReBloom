using UnityEngine;

public class LaserRendererHandler : MonoBehaviour
{
    private LineRenderer lr;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.enabled = false;
    }

    public void FireLaser(Vector3 origin, Vector3 direction, float length)
    {
        StopAllCoroutines();
        StartCoroutine(FireRoutine(origin, direction, length));
    }

    private System.Collections.IEnumerator FireRoutine(Vector3 origin, Vector3 direction, float length)
    {
        lr.enabled = true;
        lr.SetPosition(0, origin);
        lr.SetPosition(1, origin + direction * length);

        yield return new WaitForSeconds(0.1f);

        lr.enabled = false;
    }
}

