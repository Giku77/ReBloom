using TMPro;
using UnityEngine;

public class BuildInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtTypeName;
    [SerializeField] private Transform slotParent; // GridLayoutGroup 달린 애

    public Transform SlotParent => slotParent;

    public void SetTypeName(string name)
    {
        txtTypeName.text = name;
    }
}
