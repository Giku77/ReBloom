using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header ("Light")]
    [SerializeField] private Light sun;
    [SerializeField] private float dayLengthInSeconds = 2160f;
    [SerializeField] private float currentTime = 800f;


    private void Start()
    {
        currentTime = 800f;
    }

    private void Update()
    {
        currentTime += Time.deltaTime;

        float t = (currentTime / dayLengthInSeconds) % 1f;

        sun.transform.rotation = Quaternion.Euler(t * 360f - 90f, 0f, 0f);
    }
}
