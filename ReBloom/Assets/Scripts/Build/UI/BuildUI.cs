using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildUI : UIBase
{
    [Header("References")]
    [SerializeField] private Transform slotContainer; // ScrollView Content
    [SerializeField] private BuildInfoUI buildInfoPrefab;
    [SerializeField] private BuildSlotUI slotItemPrefab;
    [SerializeField] private BuildToolTip toolTip;
    [SerializeField] private TextMeshProUGUI researchPoint;


    [Header("Tab Buttons")]
    [SerializeField] private Button[] tabButtons; // 인스펙터에서 순서대로: 기본, 바이오, 에너지, 기술
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = Color.gray;
    private readonly string[] arcTypeNames = { "전체", "기본", "바이오", "에너지", "기술" };

    private readonly Dictionary<int, List<BuildSlotUI>> slotsUIsByType = new();
    private readonly Dictionary<int, BuildSlotUI> slotUIsByArcId = new();
    private readonly List<BuildSlotUI> allSlots = new();

    private int currentTypeFilter = 0; // 현재 선택된 타입

    private ArcDB arcDB;
    private ArcRecipeDB arcRecipeDB;

    private void OnEnable()
    {
        if (ResearchManager.I != null)
            ResearchManager.I.OnProgressChanged += UpdateResearchPointDisplay;

        // arcDB가 null이 아니고 로드 완료됐을 때만
        if (arcDB != null && arcDB.IsLoaded)
        {
            UpdateResearchPointDisplay(ResearchManager.I.CurrentProgress);
        }
    }

    private void OnDisable()
    {
        if (ResearchManager.I != null) ResearchManager.I.OnProgressChanged -= UpdateResearchPointDisplay;
    }

    private async void Start()
    {
        Debug.Log("[BuildUI] Start 시작");

        await UniTask.WaitUntil(() => BuildManager.I != null && BuildManager.I.IsInitialized);
        Debug.Log("[BuildUI] BuildManager 초기화 완료");

        arcDB = BuildManager.I.ArcDB;
        arcRecipeDB = BuildManager.I.RecipeDB;

        if (arcDB == null)
        {
            Debug.LogError("[BuildUI] ArcDB가 null!");
            return;
        }

        Debug.Log($"[BuildUI] arcDB.IsLoaded = {arcDB.IsLoaded}");
        await UniTask.WaitUntil(() => arcDB.IsLoaded);
        Debug.Log("[BuildUI] ArcDB 로드 완료");

        Debug.Log($"[BuildUI] arcDB 건물 수: {arcDB.GetAll().Count}");

        BuildAll();
        SetupTabButtons();
        SelectTab(1);

        Debug.Log("[BuildUI] Start 완료");
        UpdateResearchPointDisplay(ResearchManager.I.CurrentProgress);
    }
    private void OnArcDBLoaded()
    {
        arcDB.OnLoadComplete -= OnArcDBLoaded;
        BuildAll();
    }

    /// <summary>
    /// 탭 버튼 클릭 이벤트 연결
    /// </summary>
    private void SetupTabButtons()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int typeIndex = i;
            tabButtons[i].gameObject.GetComponentInChildren<TextMeshProUGUI>().text = arcTypeNames[typeIndex];
            tabButtons[i].onClick.AddListener(() => SelectTab(typeIndex));
        }
    }

    /// <summary>
    /// 탭 선택 시 호출 - 해당 타입만 보이게 필터링
    /// </summary>
    public void SelectTab(int arcType)
    {
        Debug.Log($"[BuildUI] SelectTab({arcType}) 호출");

        currentTypeFilter = arcType;

        foreach (var pair in slotsUIsByType)
        {
            foreach (var slot in pair.Value)
            {
                slot.gameObject.SetActive(false);
            }
        }

        if (arcType == 0)
        {
            // 전체: 모든 슬롯 활성화
            Debug.Log($"[BuildUI] 전체: {allSlots.Count}개 슬롯 활성화");
            foreach (var slot in allSlots)
            {
                slot.gameObject.SetActive(true);
            }
        }
        else
        {
            // 특정 타입만 활성화
            if (slotsUIsByType.TryGetValue(arcType, out var slots))
            {
                Debug.Log($"[BuildUI] 타입 {arcType}: {slots.Count}개 슬롯 활성화");
                foreach (var slot in slots)
                {
                    slot.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogWarning($"[BuildUI] 타입 {arcType}에 슬롯 없음!");
            }
        }

        UpdateTabVisuals(arcType);
    }

    /// <summary>
    /// 선택된 탭 버튼 하이라이트
    /// </summary>
    private void UpdateTabVisuals(int selectedType)
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int typeIndex = i + 1;
            var colors = tabButtons[i].colors;
            colors.normalColor = (typeIndex == selectedType) ? activeTabColor : inactiveTabColor;
            tabButtons[i].colors = colors;

            // 또는 Image 컴포넌트 직접 변경
            // tabButtons[i].GetComponent<Image>().color = 
            //     (typeIndex == selectedType) ? activeTabColor : inactiveTabColor;
        }
    }

    private void UpdateResearchPointDisplay(float p)
    {
        //researchPoint.text = $"{p:F0}"; // 반올림
        researchPoint.text = Mathf.FloorToInt(p).ToString(); // 버림
        RefreshUnlockStates();
    }

    private void RefreshUnlockStates()
    {
        if (arcDB == null) return;

        foreach (var pair in slotUIsByArcId)
        {
            int arcId = pair.Key;
            BuildSlotUI slotUI = pair.Value;
            if (slotUI == null) continue;

            if (!arcDB.GetAll().TryGetValue(arcId, out var arc))
                continue;

            bool unlocked = ResearchManager.I.IsUnlocked(arc);

            slotUI.UpdateUnlockState(unlocked);
        }
    }

    private void BuildAll()
    {
        Debug.Log($"[BuildUI] BuildAll 시작");

        // 타입별 리스트 초기화 (1~4)
        for (int i = 1; i < arcTypeNames.Length; i++)
        {
            slotsUIsByType[i] = new List<BuildSlotUI>();
        }

        int count = 0;
        foreach (var pair in arcDB.GetAll())
        {
            int arcId = pair.Key;
            ArcData arc = pair.Value;

          //  Debug.Log($"[BuildUI] 슬롯 생성 중: {arc.name}, arcType={arc.arcType}");

            var slotUI = Instantiate(slotItemPrefab, slotContainer);
            if (slotUI == null)
            {
                Debug.LogError("[BuildUI] slotUI Instantiate 실패!");
                continue;
            }

            bool unlocked = ResearchManager.I.IsUnlocked(arc);
            arcRecipeDB.TryGetRecipe(arcId, out var recipe);
            slotUI.Set(arc, recipe, unlocked);
            slotUI.Init(toolTip);

            // 타입별 리스트에 추가
            if (slotsUIsByType.ContainsKey(arc.arcType))
            {
                slotsUIsByType[arc.arcType].Add(slotUI);
            }

            // 전체 리스트에도 추가
            allSlots.Add(slotUI);
            slotUIsByArcId[arcId] = slotUI;

            slotUI.gameObject.SetActive(false);
            count++;
        }

       // Debug.Log($"[BuildUI] BuildAll 완료 - 총 {count}개 슬롯 생성");
    }


    private string GetArcTypeName(int arcType)
    {
        int index = Mathf.Clamp(arcType - 1, 0, arcTypeNames.Length - 1);
        return arcTypeNames[index];
    }

    public void Toggle()
    {
        //bool next = !gameObject.activeSelf;
        //gameObject.SetActive(next);

        //Cursor.visible = next;
        //Cursor.lockState = next ? CursorLockMode.None : CursorLockMode.Locked;
        //Camera.main.GetComponent<ThirdPersonCamera>().isZoomLocked = next;
        if (UIManager.Instance != null && UIManager.Instance.IsBlockedInput)
            return;
        UIManager.Instance?.ToggleUI(Type);
    }

    protected override void OnShow()
    {
        BuildPlacementController.I?.CancelPlacement();
        SoundManager.I?.PlayOpenInventory();
    }

    protected override void OnHide()
    {
        base.OnHide();
        SoundManager.I?.PlayCloseInventory();
    }
}