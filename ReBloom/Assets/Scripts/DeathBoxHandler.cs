using UnityEngine;

/// <summary>
/// �÷��̾� ��� ó�� �� ��ü�ڽ� ���� ����
/// PlayerStats���� �ڵ����� �̺�Ʈ ��ϵ�
/// </summary>
public class DeathBoxHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryItemData playerInventory;
    [SerializeField] private DeathBoxData deathBoxData;

    [Header("Death Box Settings")]
    [SerializeField] private GameObject deathBoxPrefab;
    [SerializeField] private Vector3 dropOffset = Vector3.zero;

    [Header("Options")]
    [SerializeField] private bool autoSpawnDeathBox = true;
    [SerializeField] private bool clearInventoryOnDeath = true;

    private GameObject currentDeathBox;

    /// <summary>
    /// �÷��̾� ��� ó�� (PlayerStats���� �ڵ� ȣ��)
    /// </summary>
    public void OnCreateDeathBox()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("[DeathBoxHandler] �÷��̾ ã�� �� �����ϴ�!");
            return;
        }

        Transform playerTransform = player.transform;

        if (playerInventory == null || deathBoxData == null)
        {
            Debug.LogError("[DeathBoxHandler] �ʼ� ������ �����ϴ�!");
            return;
        }

        // 1. �κ��丮 �������� ��ü�ڽ��� �̵�
        Vector3 deathPosition = playerTransform.position + dropOffset;
        deathBoxData.StoreItemsFromInventory(playerInventory, deathPosition);

        // 2. �κ��丮 Ŭ����
        if (clearInventoryOnDeath)
        {
            playerInventory.Clear();
            Debug.Log("[DeathBoxHandler] �κ��丮�� ������ϴ�.");
        }

        // 3. ��ü�ڽ� ������Ʈ ����
        if (autoSpawnDeathBox && deathBoxPrefab != null)
        {
            SpawnDeathBox(deathPosition);
        }

        Debug.Log($"[DeathBoxHandler] �÷��̾� ��� ó�� �Ϸ�. ��ġ: {deathPosition}");
    }

    private void SpawnDeathBox(Vector3 position)
    {
        if (currentDeathBox != null)
        {
            Destroy(currentDeathBox);
        }

        currentDeathBox = Instantiate(deathBoxPrefab, position, Quaternion.identity);

        var deathBoxInteract = currentDeathBox.GetComponent<WorldDeathBox>();
        if (deathBoxInteract != null)
        {
            deathBoxInteract.Initialize(deathBoxData, playerInventory);
        }

        Debug.Log($"[DeathBoxHandler] ��ü�ڽ� ����: {position}");
    }
}