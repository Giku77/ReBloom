using UnityEngine;

public class BuildMobileButtons : MonoBehaviour
{
    public void OnConfirmPressed()
    {
        BuildPlacementController.I?.TryConfirmBuild();
    }

    public void OnCancelPressed()
    {
        BuildPlacementController.I?.TryCancelBuild();
    }
}
