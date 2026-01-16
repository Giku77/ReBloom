using UnityEngine;

public class BuildMessageResolver : MonoBehaviour, IBuildMessageResolver
{
    [SerializeField] private BuildMessageTable table;

    public string Resolve(BuildError error)
        => table != null ? table.Resolve(error) : $"[No Table] {error}";
}
