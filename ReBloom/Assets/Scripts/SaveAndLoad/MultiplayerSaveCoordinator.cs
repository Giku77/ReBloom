using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerSaveCoordinator : MonoBehaviour
{
    public static MultiplayerSaveCoordinator I { get; private set; }
    public static bool IsLoadFlowComplete { get; private set; } = true;

    public static MultiplayerSaveCoordinator EnsureInstance()
    {
        if (I != null)
            return I;

        var go = new GameObject(nameof(MultiplayerSaveCoordinator));
        I = go.AddComponent<MultiplayerSaveCoordinator>();
        return I;
    }

    public static void BeginPendingLoadFlow()
    {
        IsLoadFlowComplete = GameStartContext.StartMode != GameStartContext.Mode.Continue;
    }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (I == this)
            I = null;
    }

    public async UniTask HandlePostSceneLoadAsync()
    {
        if (GameStartContext.StartMode != GameStartContext.Mode.Continue)
        {
            IsLoadFlowComplete = true;
            return;
        }

        IsLoadFlowComplete = false;

        try
        {
            if (SaveManager.I != null)
                SaveManager.I.SetActiveSlot(GameStartContext.SlotId, GameStartContext.SlotDisplayName);

            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening || !networkManager.IsServer)
                return;

            if (SaveManager.I == null)
                return;

            bool loaded = await SaveManager.I.LoadAsync(GameStartContext.SlotId);
            if (!loaded)
                Debug.LogWarning($"[MultiplayerSaveCoordinator] Failed to load selected slot: {GameStartContext.SlotId}");
        }
        finally
        {
            IsLoadFlowComplete = true;
        }
    }
}