using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 마우스 클릭 위치의 UI 요소를 확인하는 디버깅 유틸리티
/// New Input System 사용 버전
/// </summary>
public class UIRaycastDebugger : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private bool showInHierarchy = true; // 계층 구조까지 표시
    [SerializeField] private bool detectOnLeftClick = true; // 왼쪽 클릭 시 감지
    [SerializeField] private bool detectOnRightClick = false; // 우클릭 시 감지

    private PointerEventData pointerData;
    private EventSystem eventSystem;
    private Mouse mouse;

    void Start()
    {
        eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("EventSystem이 씬에 없습니다!");
        }

        mouse = Mouse.current;
        if (mouse == null)
        {
            Debug.LogError("Mouse를 찾을 수 없습니다!");
        }

        pointerData = new PointerEventData(eventSystem);
    }

    void Update()
    {
        if (mouse == null) return;

        // 왼쪽 클릭 감지
        if (detectOnLeftClick && mouse.leftButton.wasPressedThisFrame)
        {
            CheckUIUnderMouse();
        }

        // 우클릭 감지
        if (detectOnRightClick && mouse.rightButton.wasPressedThisFrame)
        {
            CheckUIUnderMouse();
        }
    }

    /// <summary>
    /// 마우스 위치의 UI 요소들을 체크
    /// </summary>
    void CheckUIUnderMouse()
    {
        if (eventSystem == null || mouse == null) return;

        // 마우스 위치 설정
        Vector2 mousePosition = mouse.position.ReadValue();
        pointerData.position = mousePosition;

        // Raycast 결과를 담을 리스트
        List<RaycastResult> results = new List<RaycastResult>();

        // UI Raycast 실행
        EventSystem.current.RaycastAll(pointerData, results);

        Debug.Log($"========== 마우스 위치: {mousePosition} ==========");

        if (results.Count == 0)
        {
            Debug.Log("마우스 위치에 UI 요소가 없습니다.");
            return;
        }

        Debug.Log($"발견된 UI 요소: {results.Count}개\n");

        // 모든 결과를 순서대로 출력 (앞에 있는 것부터)
        for (int i = 0; i < results.Count; i++)
        {
            RaycastResult result = results[i];
            GameObject hitObject = result.gameObject;

            string prefix = i == 0 ? "[최상위]" : $"   [{i}]";

            Debug.Log($"{prefix} {hitObject.name}", hitObject);

            // 상세 정보 출력
            LogDetailedInfo(hitObject, i == 0);

            // 계층 구조 출력
            if (showInHierarchy)
            {
                LogHierarchy(hitObject);
            }

            Debug.Log(""); // 구분선
        }
    }

    /// <summary>
    /// UI 요소의 상세 정보 로그
    /// </summary>
    void LogDetailedInfo(GameObject obj, bool isTop)
    {
        // Canvas 정보
        Canvas canvas = obj.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"Canvas: {canvas.name} (SortOrder: {canvas.sortingOrder})");
        }

        // Graphic 컴포넌트 확인 (Image, Text 등)
        Graphic graphic = obj.GetComponent<Graphic>();
        if (graphic != null)
        {
            Debug.Log($"Type: {graphic.GetType().Name}");
            Debug.Log($"Raycast Target: {graphic.raycastTarget}");

            if (!graphic.raycastTarget && isTop)
            {
                Debug.LogWarning($"이 요소는 raycastTarget이 꺼져있습니다!");
            }
        }

        // Button 컴포넌트 확인
        Button button = obj.GetComponent<Button>();
        if (button != null)
        {
            Debug.Log($"Button 발견!");
            Debug.Log($"Interactable: {button.interactable}");

            if (!button.interactable)
            {
                Debug.LogWarning($"Button이 비활성화 상태입니다!");
            }
        }

        // CanvasGroup 확인 (클릭 차단 가능)
        CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            Debug.Log($"CanvasGroup:");
            Debug.Log($"Block Raycasts: {canvasGroup.blocksRaycasts}");
            Debug.Log($"Interactable: {canvasGroup.interactable}");

            if (!canvasGroup.blocksRaycasts || !canvasGroup.interactable)
            {
                Debug.LogWarning($"CanvasGroup 설정이 클릭을 막고 있습니다!");
            }
        }

        // Active 상태 확인
        if (!obj.activeInHierarchy)
        {
            Debug.LogWarning($"이 오브젝트는 비활성화 상태입니다!");
        }
    }

    /// <summary>
    /// 계층 구조 로그
    /// </summary>
    void LogHierarchy(GameObject obj)
    {
        string hierarchy = GetHierarchyPath(obj);
        Debug.Log($"경로: {hierarchy}");
    }

    /// <summary>
    /// 오브젝트의 전체 경로 가져오기
    /// </summary>
    string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    /// <summary>
    /// 즉시 체크 (외부에서 호출 가능)
    /// </summary>
    public void CheckNow()
    {
        CheckUIUnderMouse();
    }
}