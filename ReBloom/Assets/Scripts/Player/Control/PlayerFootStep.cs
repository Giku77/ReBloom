using UnityEngine;
using System;

public class PlayerFootstep : MonoBehaviour
{
    public static event Action<Vector3, float> OnFootstep;
    [SerializeField] private float stepInterval = 0.5f;
    private float stepTimer = 0f;

    private PlayerController playerController;
    private StageDetector stageDetector;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        stageDetector = GetComponent<StageDetector>();
    }

    private void Update()
    {
        if (playerController.currentSpeed > 0.1f) // 움직일 때만
        {
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval && stageDetector.CurrentStage.stageID != 400)
            {
                stepTimer = 0f;
                float loudness = playerController.isSlow ? 0.3f : 1.0f;
                OnFootstep?.Invoke(transform.position, loudness);
                //Debug.Log("발소리 발생: " + transform.position);
            }
        }
        else
        {
            stepTimer = 0f; // 멈추면 타이머 초기화
        }
    }
}

