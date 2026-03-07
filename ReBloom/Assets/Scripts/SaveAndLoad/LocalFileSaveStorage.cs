using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LocalFileSaveStorage : ISaveStorage
{
    private string PathForSlot(string slotId)
    {
        string accountNamespace = PlayFabAuth.CurrentStorageNamespace;
        string root = System.IO.Path.Combine(Application.persistentDataPath, "saves", accountNamespace);
        return System.IO.Path.Combine(root, $"{slotId}.sav");
    }

    public async UniTask SaveAsync(string slotId, byte[] bytes)
    {
        var path = PathForSlot(slotId);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        await File.WriteAllBytesAsync(path, bytes);
    }

    public async UniTask<byte[]> LoadAsync(string slotId)
    {
        var path = PathForSlot(slotId);
        if (!File.Exists(path)) return null;
        return await File.ReadAllBytesAsync(path);
    }

    public UniTask<bool> ExistsAsync(string slotId)
    {
        return UniTask.FromResult(File.Exists(PathForSlot(slotId)));
    }

    public UniTask DeleteAsync(string slotId)
    {
        var path = PathForSlot(slotId);
        if (File.Exists(path))
            File.Delete(path);
        return UniTask.CompletedTask;
    }
}
