using Cysharp.Threading.Tasks;

public interface ISaveStorage
{
    UniTask<bool> ExistsAsync(string slotId);
    UniTask SaveAsync(string slotId, byte[] bytes);
    UniTask<byte[]> LoadAsync(string slotId);
    UniTask DeleteAsync(string slotId);
}
