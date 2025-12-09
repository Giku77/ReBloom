using UnityEngine;

public abstract class NPCState
{
    protected BaseNPCController controller;

    public NPCState(BaseNPCController controller)
    {
        this.controller = controller;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
    public virtual void HandleFootstep(Vector3 footPos, float loudness) { }
}