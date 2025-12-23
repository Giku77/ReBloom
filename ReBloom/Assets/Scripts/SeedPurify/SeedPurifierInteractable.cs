using UnityEngine;

public class SeedPurifierInteractable : BuildingInteractableBase
{
    private float holdTime = 1f;
    public override float HoldTime => holdTime;

    private ArcData arcData;
    private SeedPurifierMachine machine;

    private void Start()
    {
        arcData = BuildManager.I != null && BuildManager.I.ArcDB != null
            && BuildManager.I.ArcDB.TryGet(building.arcId, out var data) ? data : null;

        holdTime = (arcData != null && arcData.interactTime > 0f) ? arcData.interactTime : 1.0f;

        // 정화기 로직 컴포넌트
        machine = GetComponent<SeedPurifierMachine>();
        if (machine == null)
            Debug.LogWarning("[SeedPurifierInteractable] SeedPurifierMachine not found on same object.");
    }

    public override void Interact(PlayerController player)
    {
        if (machine == null)
        {
            ToastMessageUI.Instance?.Show("정화기 로직이 연결되지 않았습니다.");
            return;
        }

        OpenPurifierUI(player);
    }

    private void OpenPurifierUI(PlayerController player)
    {
        var ui = UIManager.Instance.GetUI<SeedPurifierUI>(UIType.SeedPurifier);
        if (ui == null)
        {
            ToastMessageUI.Instance?.Show("정화 UI를 찾지 못했습니다.");
            return;
        }

        UIManager.Instance.ShowUI(UIType.SeedPurifier);

        // 머신 바인딩(아래 2번에서 Bind 함수 추가할거임)
        ui.Bind(machine, player);
    }
}
