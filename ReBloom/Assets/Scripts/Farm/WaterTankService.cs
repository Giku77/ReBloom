using UnityEngine;

public class WaterTankService : MonoBehaviour
{
    public static WaterTankService I { get; private set; }

    [SerializeField] private GameInventory inventory; 
    public WaterTankManager Manager { get; private set; }

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        Manager = new WaterTankManager(inventory);
    }

    private void Update()
    {
        Manager?.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        Manager?.Dispose();
    }
}
