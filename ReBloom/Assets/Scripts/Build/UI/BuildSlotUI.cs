using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtTier;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private List<TextMeshProUGUI> txtMaterials; 
    [SerializeField] private List<TextMeshProUGUI> ingredientsQuantity; 
    [SerializeField] private List<Image> materialsIcons; 

    [SerializeField] private GameObject testPrefab;
    [SerializeField] private GameObject lockView;

    private BuildUI parentUI;
    private BuildToolTip toolTip;

    private ArcData arcData;

    [SerializeField] private Button buildButton;

    private GameObject player;

    private bool isUnlocked = false;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        parentUI = GetComponentInParent<BuildUI>();
    }

    public void Init(BuildToolTip toolTip)
    {
        this.toolTip = toolTip;
    }

    public void OnIconPointerEnter(PointerEventData eventData)
    {
        if (toolTip == null || arcData == null) return;

        string info = arcData.text;
        toolTip.Show(info, eventData.position);
    }

    public void OnIconPointerMove(PointerEventData eventData)
    {
        if (toolTip == null) return;
        toolTip.SetPosition(eventData.position);
    }

    public void OnIconPointerExit(PointerEventData eventData)
    {
        if (toolTip == null) return;
        toolTip.Hide();
    }

    
    public void UpdateUnlockState(bool unlocked)
    {
        isUnlocked = unlocked;

        if (lockView != null)
            lockView.SetActive(!unlocked);
    }

    public void Set(ArcData arc, ArcRecipe recipe, bool unlocked)
    {
        arcData = arc;

        txtName.text = arc.name;
        txtTier.text = arc.tier.ToString();

        if (arc.icon != null)
        {
            icon.sprite = arc.icon;
           // Debug.Log($"[BuildSlotUI] {arc.name}: {icon.sprite.name}");
        }
        else
        {
            icon.sprite = defaultIcon;
            Debug.LogWarning($"[BuildSlotUI] {arc.name}: 아이콘 없음");
        }

        lockView.SetActive(!unlocked);

        buildButton.onClick.AddListener(() =>
        {
            // var playerPos = player.transform.position;
            // playerPos += player.transform.forward * 2.0f;
            // BuildManager.I.TryBuild(arc.arcId, playerPos, Quaternion.identity);
            //var previewPrefab = arc.previewPrefab != null ? arc.previewPrefab : BuildManager.I.prefab;
            BuildPlacementController.I.StartPlacement(arc, recipe, testPrefab);
            parentUI.Toggle();
        });

        if (recipe == null)
        {
            // 재료 없으면 비우거나 숨기기
            foreach (var t in txtMaterials)
                t.text = string.Empty;
            return;
        }

        for (int i = 0; i < txtMaterials.Count; i++)
        {
            if (i < recipe.materials.Count)
            {
                var mat = recipe.materials[i];
                var item = ItemDatabase.I.GetItem(mat.itemId);
                var itemName = item.itemName ?? "Unknown";
                materialsIcons[i].sprite = item.icon;
                txtMaterials[i].text = $"{itemName} : <#7e7e7e><b>";
                ingredientsQuantity[i].text = $"{mat.amount} </b></color>";
            }
            else
            {
                txtMaterials[i].text = string.Empty;
                materialsIcons[i].gameObject.SetActive(false);
                ingredientsQuantity[i].gameObject.SetActive(false);
            }
        }
    }
}
