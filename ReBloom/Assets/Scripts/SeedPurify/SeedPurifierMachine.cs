using System;
using UnityEngine;

public class SeedPurifierMachine : MonoBehaviour
{
    public enum State
    {
        Idle,
        Processing,
        OutputReady
    }

    [Header("Rules")]
    [SerializeField] private float processSeconds = 5f;

    [Header("Refs")]
    [SerializeField] private GameInventory inventory;  

    private SeedPurifyDB db;

    [Header("Debug")]
    [SerializeField] private bool autoFindRefs = true;

    // Slots (간단히: 입력 1개, 출력 1개)
    [SerializeField] private int inputItemId = 0;
    [SerializeField] private int inputCount = 0;
    [SerializeField] private int outputItemId = 0;

    [SerializeField] private State state = State.Idle;
    public State CurrentState => state;

    // 타이머
    [SerializeField] private float endTime = -1f;

    // 롤 재현용 시드(저장/로드 고려)
    [SerializeField] private int rollSeed = 0;
    [SerializeField] private bool rolled = false;

    public event Action OnChanged;          // UI 갱신용
    public event Action<float> OnProgress;  // 0~1 진행도

    private void Awake()
    {
        if (autoFindRefs)
        {
            if (inventory == null) inventory = FindFirstObjectByType<GameInventory>();
        }
    }

    private void Start()
    {
        db = FarmPrefabProvider.I.SeedPurifyDB;
        RaiseChanged();
    }

    private void Update()
    {
        if (state != State.Processing) return;

        float remain = Mathf.Max(0f, endTime - Time.time);
        float t = 1f - (remain / Mathf.Max(0.01f, processSeconds));
        t = Mathf.Clamp01(t);

        OnProgress?.Invoke(t);

        if (Time.time >= endTime)
        {
            FinishProcess();
        }
    }

    // -----------------------
    // Public API (UI에서 호출)
    // -----------------------

    /// <summary>
    /// 인벤에서 미확인 종자 1개 넣기(UseItemId 기준)
    /// </summary>
    public bool TryInsertInput(int useItemId, int amount = 1)
    {
        if (state != State.Idle) return false;
        if (inputItemId != 0) return false;
        if (inventory == null) return false;
        if (amount <= 0) return false;

        if (!inventory.HasItem(useItemId, amount))
            return false;

        inventory.RemoveItem(useItemId, amount);
        inputItemId = useItemId;
        inputCount = amount; 

        RaiseChanged();
        return true;
    }


    public bool CanStart()
    {
        if (state != State.Idle) return false;
        if (inputItemId == 0) return false;
        if (db == null || !db.IsLoaded) return false;

        return db.TryGetTable(inputItemId, out var table) && table.Entries.Count > 0;
    }

    public bool StartProcess()
    {
        if (!CanStart()) return false;

        state = State.Processing;
        endTime = Time.time + processSeconds;

        // 재현 가능한 시드 저장
        rollSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        rolled = false;

        OnProgress?.Invoke(0f);
        RaiseChanged();
        return true;
    }

    public bool SetInput(int itemId, int count)
    {
        if (CurrentState != State.Idle) return false;
        if (inputItemId != 0) return false;

        inputItemId = itemId;
        inputCount = count;

        OnChanged?.Invoke();
        return true;
    }


    /// <summary>
    /// 정화 취소: 진행중이면 입력 종자 돌려주고 Idle로
    /// </summary>
    public bool CancelProcess()
    {
        if (state != State.Processing) return false;
        if (inventory == null) return false;

        // 입력 되돌리기
        if (inputItemId != 0 && inputCount > 0)
            inventory.AddItemFromWorld(inputItemId, inputCount, true);

        ResetAll();
        RaiseChanged();
        return true;
    }

    /// <summary>
    /// 결과 회수: OutputReady이면 인벤에 넣고 초기화
    /// </summary>
    public bool TakeOutput()
    {
        if (state != State.OutputReady) return false;
        if (outputItemId == 0) return false;
        if (inventory == null) return false;

        inventory.AddItemFromWorld(outputItemId, 1, true);

        ResetAll();
        RaiseChanged();
        return true;
    }

    /// <summary>
    /// 외부에서 강제 회수/텔레포트 같은 용도에 쓰기 쉬운 유틸
    /// </summary>
    public int PeekOutputItemId() => outputItemId;
    public int PeekInputItemId() => inputItemId;

    // -----------------------
    // Internals
    // -----------------------

    private void FinishProcess()
    {
        if (state != State.Processing) return;
        if (db == null || !db.IsLoaded)
        {
            // DB 없으면 입력을 보존한 채로 다시 시도 가능하게 처리
            state = State.Idle;
            endTime = -1f;
            RaiseChanged();
            return;
        }

        if (!db.TryGetTable(inputItemId, out var table) || table.Entries.Count == 0)
        {
            if (inventory != null && inputItemId != 0 && inputCount > 0)
                inventory.AddItemFromWorld(inputItemId, inputCount, true);

            ResetAll();
            RaiseChanged();
            return;
        }

        if (!rolled)
        {
            var rng = new System.Random(rollSeed);
            outputItemId = table.Roll(rng);
            rolled = true;
        }

        inputItemId = 0;
        inputCount = 0;

        state = State.OutputReady;
        endTime = -1f;

        OnProgress?.Invoke(1f);
        RaiseChanged();
    }

    private void ResetAll()
    {
        state = State.Idle;
        inputItemId = 0;
        outputItemId = 0;
        endTime = -1f;
        rollSeed = 0;
        rolled = false;

        OnProgress?.Invoke(0f);
    }

    private void RaiseChanged() => OnChanged?.Invoke();
}
