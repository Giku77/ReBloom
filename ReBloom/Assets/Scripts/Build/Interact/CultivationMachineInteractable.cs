using UnityEngine;

public class CultivationMachineInteractable : BuildingInteractableBase
{
    [Header("Refs")]
    [SerializeField] private CultivationMachine machine;

    private ThirdPersonCamera tpsCam;
    private CultivationUI cultivationUI;

    private int lastHighlightedIndex = -1;

    private void Start()
    {
        cultivationUI = UIManager.Instance.GetUI<CultivationUI>(UIType.Cultivation);
        tpsCam = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
    }

    public override void Interact(PlayerController player)
    {
        if (machine == null || player == null || tpsCam == null) return;

        if (UIManager.Instance != null && UIManager.Instance.IsUIOpen(UIType.Cultivation))
            return;

        int focusIndex = -1;

        OpenCultivationUI(player, focusIndex);
    }
    private void OpenCultivationUI(PlayerController player, int slotIndex)
    {
        if (cultivationUI == null)
        {
            ToastMessageUI.Instance?.Show("CultivationUI가 연결되지 않았습니다.");
            return;
        }

        cultivationUI.Open(machine, player);
    }
}
