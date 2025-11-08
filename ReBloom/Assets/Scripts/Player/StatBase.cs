using Unity.Android.Gradle.Manifest;
using UnityEngine;

public abstract class StatBase
{
    protected PlayerStats owner;
    protected float value;
    protected float maxValue;
    public float Value => value;
    public float MaxValue => maxValue;

    public StatBase(PlayerStats owner, float maxValue)
    { 
        this.owner = owner;
        this.maxValue = maxValue;
        this.value = maxValue;
    }

    public virtual void Modify(float amount)
    {
        float old = value;
        value = Mathf.Clamp(value + amount, 0, maxValue);
        owner.InvokeStatChanged(this, old, value);

        Debug.Log($"[Stat] {GetType().Name} changed: {old} -> {value}");
    }

    public virtual void Set(float newValue)
    {
        float old = value;
        value = Mathf.Clamp(newValue, 0, maxValue);
        owner.InvokeStatChanged(this, old, value);
    }

    public abstract void Tick();
}
