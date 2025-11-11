using NUnit.Framework.Interfaces;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// 월드 맵에서 아이템을 드롭할 수 있는 영역
/// 카메라를 통해 스크린 좌표를 월드 좌표로 변환
/// </summary>
public class WorldDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ItemSpawner itemSpawner;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject dropIndicator; // 드롭 가능 영역 표시 (선택사항)
    [SerializeField] private LayerMask groundLayer; // 지면 레이어

    [Header("Placement Settings")]
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private float placementHeight = 0f; // 배치 높이 오프셋

    private bool isPointerOver = false;

    private void Awake()
    {
        // 자동으로 참조 찾기
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (itemSpawner == null)
        {
            itemSpawner = FindFirstObjectByType<ItemSpawner>();
        }

        // 드롭 인디케이터 초기에는 숨김
        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        // 드래그 중이고 포인터가 이 영역 위에 있으면 미리보기 표시
        if (isPointerOver && ItemIconDragHandler.CurrentDraggedItem != null)
        {
            UpdateDropIndicator();
        }
    }

    // 포인터가 들어왔을 때
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("포인터 들어옴");
        isPointerOver = true;

        if (dropIndicator != null && ItemIconDragHandler.CurrentDraggedItem != null)
        {
            dropIndicator.SetActive(true);
        }
    }

    // 포인터가 나갔을 때
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("포인터 나감");
        isPointerOver = false;

        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }
    }

    // 드롭 처리
    public async void OnDrop(PointerEventData eventData)
    {
        Debug.Log("드롭아이템");
        ItemBase draggedItem = ItemIconDragHandler.CurrentDraggedItem;

        if (draggedItem == null)
        {
            Debug.LogWarning("드롭된 아이템이 없습니다.");
            return;
        }

        // 마우스 위치를 월드 좌표로 변환
        Vector3 worldPosition = GetWorldPositionFromMouse(eventData.position);

        if (worldPosition != Vector3.zero)
        {
            // ItemSpawner를 통해 아이템 생성
            if (itemSpawner != null)
            {
                try
                {
                    await itemSpawner.DropItem(draggedItem, worldPosition, Vector3.zero);
                    Debug.Log($"{draggedItem.itemName}을(를) {worldPosition}에 배치했습니다.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"아이템 드롭 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                Debug.LogError("ItemSpawner를 찾을 수 없습니다!");
            }
        }

        // 인디케이터 숨김
        if (dropIndicator != null)
        {
            dropIndicator.SetActive(false);
        }
    }

    /// <summary>
    /// 마우스 스크린 좌표를 월드 3D 좌표로 변환
    /// </summary>
    private Vector3 GetWorldPositionFromMouse(Vector2 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, groundLayer))
        {
            return hit.point + Vector3.up * placementHeight;
        }

        // 지면이 없으면 기본 평면(Y=0)에 배치
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance) + Vector3.up * placementHeight;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 드롭 위치 미리보기 표시
    /// </summary>
    private void UpdateDropIndicator()
    {
        if (dropIndicator == null) return;

        Vector2 screenPos = GetMouseScreenPosition();
        if (screenPos == Vector2.zero) return;

        Vector3 worldPosition = GetWorldPositionFromMouse(screenPos);
        if (worldPosition != Vector3.zero)
        {
            dropIndicator.transform.position = worldPosition;
        }
    }

    private Vector2 GetMouseScreenPosition()
    {
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        else
        {
            return Vector2.zero;
        }
    }
}