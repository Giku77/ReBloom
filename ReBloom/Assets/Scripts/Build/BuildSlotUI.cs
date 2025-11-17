using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtTier;
    [SerializeField] private List<TextMeshProUGUI> txtMaterials; 

    private BuildUI parentUI;

    [SerializeField] private Button buildButton;

    private GameObject player;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        parentUI = GetComponentInParent<BuildUI>();
    }

    public void Set(ArcData arc, ArcRecipe recipe)
    {
        txtName.text = arc.name;
        txtTier.text = arc.tier.ToString();

        buildButton.onClick.AddListener(() =>
        {
            var playerPos = player.transform.position;
            playerPos += player.transform.forward * 2.0f;
            BuildManager.I.TryBuild(arc.arcId, playerPos, Quaternion.identity);
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
                var itemName = ItemDatabase.I.GetItem(mat.itemId)?.itemName ?? "Unknown";
                txtMaterials[i].text = $"{itemName} : <#7e7e7e><b>{mat.amount}</b></color>";
            }
            else
            {
                txtMaterials[i].text = string.Empty;
            }
        }
    }
}
