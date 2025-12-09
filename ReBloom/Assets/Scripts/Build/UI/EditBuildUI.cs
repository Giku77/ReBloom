using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class EditBuildUI : UIBase
{
    [SerializeField] private TextMeshProUGUI selectBuildingName;

    private BuildingInstance currentTarget;

    private void OnEnable()
    {
        RefreshImmediate();
    }

    private void Update()
    {
        RefreshIfChanged();
    }

    private void RefreshIfChanged()
    {
        var ctrl = BuildPlacementController.I;
        if (ctrl == null)
        {
            SetEmpty();
            return;
        }

        if (!ctrl.IsEditMode)
        {
            SetEmpty();
            return;
        }

        var target = ctrl.CurrentEditingTarget;

        if (target == currentTarget)
            return;  

        currentTarget = target;
        UpdateUI(target);
    }

    private void RefreshImmediate()
    {
        currentTarget = null;
        RefreshIfChanged();
    }

    private void UpdateUI(BuildingInstance inst)
    {
        if (inst == null)
        {
            SetEmpty();
            return;
        }

        var bm = BuildManager.I;
        if (bm == null)
        {
            SetEmpty();
            return;
        }

        if (!bm.ArcDB.TryGet(inst.ArcId, out var arcData))
        {
            selectBuildingName.text = $"Unknown ({inst.ArcId})";
            return;
        }
        selectBuildingName.text = arcData.name;        
    }

    private void SetEmpty()
    {
        selectBuildingName.text = "";
        currentTarget = null;
    }

}
