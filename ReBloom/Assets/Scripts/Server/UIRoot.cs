using UnityEngine;

public class UIRoot : MonoBehaviour
{
    public static UIRoot I { get; private set; }

    [SerializeField] private EquipmentUI[] equipmentUI;
    //[SerializeField] private CraftingUI craftingUI;
    //[SerializeField] private StorageUI storageUI;

    [Header("EquipManager")]
    [SerializeField] private EquipmentUI pcEquipmentUI;
    [SerializeField] private EquipmentUI mobileEquipmentUI;
    [SerializeField] private GameObject equipInventoryRoot;
    [SerializeField] private GameInventory localInventory; 

    void Awake() { I = this; }

    public void BindLocalPlayer(PlayerController pc)
    {
        foreach (var equipmentUI in equipmentUI)
            equipmentUI?.Bind(pc);
        var equip = pc.GetComponent<PlayerEquipManager>();
        if (equip == null) return;

        equip.BindUI(pcEquipmentUI, mobileEquipmentUI, equipInventoryRoot, localInventory);
    }
}
