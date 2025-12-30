using UnityEngine;

public class ResearchManagerSaveable : MonoBehaviour, ISaveable
{
    [SerializeField] private ResearchManager rm;

    public string EntityGuid => "research";

    private void Awake()
    {
        if (rm == null) rm = ResearchManager.I;
    }

    public void Capture(SaveGameDTO root)
    {
        root.research = rm.Capture();
    }

    public void Restore(SaveGameDTO root)
    {
        rm.Apply(root.research);
    }
}
