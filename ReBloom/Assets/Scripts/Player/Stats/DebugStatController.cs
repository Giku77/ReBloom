using UnityEngine;
using UnityEngine.UI;

public class DebugStatController : MonoBehaviour
{
    [SerializeField] private Button thirstButton;
    [SerializeField] private Button hungerButton;
    [SerializeField] private Button pollutionButton;
    
    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStats>();
        
        if (playerStats == null)
        {
            Debug.LogError("[DebugStatController] PlayerStats를 찾을 수 없습니다!");
            return;
        }
        
        if (thirstButton != null)
            thirstButton.onClick.AddListener(IncreaseThirst);
        
        if (hungerButton != null)
            hungerButton.onClick.AddListener(IncreaseHunger);
        
        if (pollutionButton != null)
            pollutionButton.onClick.AddListener(IncreasePollution);
    }

    private void IncreaseThirst()
    {
        playerStats.Thirst.Modify(10f);
    }

    private void IncreaseHunger()
    {
        playerStats.Hunger.Modify(10f);
    }

    private void IncreasePollution()
    {
        playerStats.Pollution.Modify(10f);
    }

    private void OnDestroy()
    {
        if (thirstButton != null)
            thirstButton.onClick.RemoveListener(IncreaseThirst);
        
        if (hungerButton != null)
            hungerButton.onClick.RemoveListener(IncreaseHunger);
        
        if (pollutionButton != null)
            pollutionButton.onClick.RemoveListener(IncreasePollution);
    }
}
