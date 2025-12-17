using UnityEngine;

[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
    [SerializeField] private string persistentId;

    public string PersistentId => persistentId;

    // 런타임 스폰(설치 건축물) 시 호출해서 새 ID 발급
    public void AssignNewId()
    {
        persistentId = System.Guid.NewGuid().ToString("N");
    }

    public void ForceSetId(string id)
    {
        persistentId = id;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 씬에 원래 배치된 오브젝트도 ID가 비어있으면 생성
        if (string.IsNullOrEmpty(persistentId))
            persistentId = System.Guid.NewGuid().ToString("N");
    }
#endif
}
