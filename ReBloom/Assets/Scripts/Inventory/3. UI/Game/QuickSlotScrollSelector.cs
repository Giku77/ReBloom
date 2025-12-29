using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QuickSlotScrollSelector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    [Header("Cell")]
    [SerializeField] private float cellHeight = 100f; // 슬롯 한 칸 높이(+spacing 포함)
    [SerializeField] private int slotCount = 6;

    [Header("Anim")]
    [SerializeField] private float snapDuration = 0.12f;

    private int index = 0;
    private Coroutine snapCo;

    private void Start()
    {
        SnapTo(index, immediate: true);
    }

    public void OnUp()
    {
        SetIndex(index - 1);
    }

    public void OnDown()
    {
        SetIndex(index + 1);
    }

    public void JumpToFirstFilled(ItemBase[] items)
    {
        if (items == null || items.Length == 0) return;

        int first = -1;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null) { first = i; break; }
        }

        index = Mathf.Max(0, first); // 없으면 0
        SnapTo(index, immediate: true);
    }


    private void SetIndex(int newIndex)
    {
        newIndex = Mathf.Clamp(newIndex, 0, slotCount - 1);
        if (newIndex == index) return;

        index = newIndex;
        SnapTo(index, immediate: false);
    }

    private void SnapTo(int i, bool immediate)
    {
        float targetY = i * cellHeight;
        Vector2 targetPos = new Vector2(content.anchoredPosition.x, targetY);

        if (snapCo != null) StopCoroutine(snapCo);

        if (immediate)
        {
            content.anchoredPosition = targetPos;
            scrollRect.velocity = Vector2.zero;
            return;
        }

        snapCo = StartCoroutine(SnapRoutine(targetPos));
    }

    private IEnumerator SnapRoutine(Vector2 target)
    {
        Vector2 start = content.anchoredPosition;
        float t = 0f;
        scrollRect.velocity = Vector2.zero;

        while (t < snapDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / snapDuration);
            content.anchoredPosition = Vector2.Lerp(start, target, a);
            yield return null;
        }

        content.anchoredPosition = target;
        snapCo = null;
    }
}
