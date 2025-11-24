using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DebuffIconUI : MonoBehaviour
{
    [System.Serializable]
    public class DebuffIconData
    {
        public int debuffID;
        public Sprite icon;
    }
    
    [Header("References")]
    [SerializeField] private DebuffManager debuffManager;
    [SerializeField] private Transform iconContainer;
    [SerializeField] private GameObject iconPrefab;
    
    [Header("Icon Settings")]
    [SerializeField] private List<DebuffIconData> debuffIcons = new List<DebuffIconData>();
    [SerializeField] private Sprite defaultIcon;
    
    private Dictionary<int, GameObject> activeIconObjects = new Dictionary<int, GameObject>();
    
    private void Start()
    {
        if (debuffManager == null)
        {
            debuffManager = FindFirstObjectByType<PlayerStats>()?.GetComponent<DebuffManager>();
        }
        
        if (debuffManager != null)
        {
            debuffManager.OnDebuffApplied += HandleDebuffApplied;
            debuffManager.OnDebuffRemoved += HandleDebuffRemoved;
        }
    }
    
    private void OnDestroy()
    {
        if (debuffManager != null)
        {
            debuffManager.OnDebuffApplied -= HandleDebuffApplied;
            debuffManager.OnDebuffRemoved -= HandleDebuffRemoved;
        }
    }
    
    private void HandleDebuffApplied(IDebuff debuff)
    {
        if (activeIconObjects.ContainsKey(debuff.ID))
            return;
        
        GameObject iconObj = Instantiate(iconPrefab, iconContainer);
        Image iconImage = iconObj.GetComponent<Image>();
        
        if (iconImage != null)
        {
            Sprite icon = GetDebuffIcon(debuff.ID);
            iconImage.sprite = icon;
        }
        
        activeIconObjects[debuff.ID] = iconObj;
    }
    
    private void HandleDebuffRemoved(IDebuff debuff)
    {
        if (activeIconObjects.TryGetValue(debuff.ID, out GameObject iconObj))
        {
            Destroy(iconObj);
            activeIconObjects.Remove(debuff.ID);
        }
    }
    
    private Sprite GetDebuffIcon(int debuffID)
    {
        foreach (var data in debuffIcons)
        {
            if (data.debuffID == debuffID)
                return data.icon;
        }
        
        return defaultIcon;
    }
}