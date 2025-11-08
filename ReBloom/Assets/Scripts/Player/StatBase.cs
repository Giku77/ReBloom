using Unity.Android.Gradle.Manifest;
using UnityEngine;

public abstract class StatBase
{
    protected PlayerStats owner;
    protected float value;
    protected float maxValue;
    public float Value => value;
    public float MaxValue => maxValue;

    private float lastLogTime = 0f;

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

        if (Time.time - lastLogTime >= 1f)
        {
            Debug.Log($"{GetType().Name} : {value}");
            lastLogTime = Time.time;
        }
    }

    public virtual void Set(float newValue)
    {
        float old = value;
        value = Mathf.Clamp(newValue, 0, maxValue);
        owner.InvokeStatChanged(this, old, value);
    }

    public abstract void Tick();
}
