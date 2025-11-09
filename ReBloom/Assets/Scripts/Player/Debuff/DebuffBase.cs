using UnityEngine;

public abstract class DebuffBase : IDebuff
{
    protected DebuffData data;
    protected PlayerStats target;
    protected float currentDuration;
    protected bool isActive;
    
    public int ID => data.id;
    public string Name => data.name;
    public int Category => data.debuffCat;
    public bool IsActive => isActive;
    
    public DebuffBase(DebuffData data, PlayerStats target)
    {
        this.data = data;
        this.target = target;
        this.currentDuration = data.duration;
    }
    
    public virtual void Apply(PlayerStats target)
    {
        isActive = true;
        OnApply();
        Debug.Log($"[Debuff] {data.name} Apply");
    }
    
    public virtual void Remove(PlayerStats target)
    {
        isActive = false;
        OnRemove();
        Debug.Log($"[Debuff] {data.name} Remove");
    }
    
    public virtual void Tick(float deltaTime)
    {
        if (!isActive) return;
        
        if (data.duration > 0)
        {
            currentDuration -= deltaTime;
        }
        
        if (data.hpLoss > 0)
        {
            target.Health.Modify(-data.hpLoss * deltaTime);
        }
        
        OnTick(deltaTime);
    }
    
    public virtual bool ShouldRemove()
    {
        if (data.duration > 0 && currentDuration <= 0)
        {
            return true;
        }
        
        return false;
    }
    
    protected abstract void OnApply();
    protected abstract void OnRemove();
    protected abstract void OnTick(float deltaTime);
}