using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIDragManager : MonoBehaviour
{
    public static UIDragManager I { get; private set; }

    [SerializeField] private RectTransform dragLayer; // Canvas/DragLayer 연결

    private DragContext current;
    private RectTransform ghostRT;

    private void Awake()
    {
        I = this;
    }

    public bool IsDragging => current != null;

    public void BeginDrag(DragContext ctx, GameObject sourceGO, Vector2 screenPos)
    {
        current = ctx;

        // 1) 고스트 만들기 (원본 슬롯을 그대로 복제)
        var ghost = Instantiate(sourceGO, dragLayer);
        ghost.name = $"[DragGhost]{sourceGO.name}";
        ghostRT = ghost.transform as RectTransform;
        ghostRT.SetAsLastSibling();

        var srcRT = (RectTransform)sourceGO.transform;
        ghostRT.anchorMin = new Vector2(0.5f, 0.5f);
        ghostRT.anchorMax = new Vector2(0.5f, 0.5f);
        ghostRT.pivot = new Vector2(0.5f, 0.5f);
        ghostRT.sizeDelta = srcRT.rect.size;

        MakeGhostNonBlocking(ghost);

        // 2) 시작 위치
        ghostRT.position = screenPos;

        // 컨텍스트에 보관해두고 싶으면
        current.Ghost = ghost;
    }

    public void Drag(Vector2 screenPos)
    {
        if (ghostRT == null) return;
        ghostRT.position = screenPos; // Overlay면 이걸로 충분
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (current == null) return;

        // 1) 드롭 타겟 찾기
        var dropTarget = FindDropTarget(eventData);

        bool dropped = false;
        if (dropTarget != null && dropTarget.CanAcceptDrop(current))
        {
            dropTarget.HandleDrop(current);
            dropped = true;
        }

        // 2) 고스트 제거
        if (current.Ghost != null)
            Destroy(current.Ghost);

        // 3) 소스 콜백
        if (dropped) current.Source?.OnDragSuccess();
        else current.Source?.OnDragCancelled();

        current = null;
        ghostRT = null;
    }

    private IDropTarget FindDropTarget(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            // interface는 GetComponent로 바로 못잡는 경우가 많아서 MonoBehaviour들 중 캐스팅으로 찾기
            var monos = r.gameObject.GetComponentsInParent<MonoBehaviour>(true);
            foreach (var m in monos)
            {
                if (m is IDropTarget dt)
                    return dt;
            }
        }
        return null;
    }

    private void MakeGhostNonBlocking(GameObject ghost)
    {
        // 고스트가 레이캐스트를 막으면 드롭이 안됨 -> 전부 raycastTarget 끄기
        foreach (var g in ghost.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        // 버튼/셀렉터 꺼서 하이라이트/클릭 반응 제거
        foreach (var s in ghost.GetComponentsInChildren<Selectable>(true))
            s.enabled = false;

        // 드래그 소스 스크립트가 고스트에서도 또 동작하면 꼬임 -> IDragSource 구현 컴포넌트 꺼주기
        foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is IDragSource) mb.enabled = false;
        }

        // CanvasGroup으로도 한 번 더 보장
        var cg = ghost.GetComponent<CanvasGroup>();
        if (cg == null) cg = ghost.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;
        cg.ignoreParentGroups = true;
    }
}
