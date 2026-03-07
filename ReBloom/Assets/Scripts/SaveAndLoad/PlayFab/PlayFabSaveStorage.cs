using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PlayFab;
using PlayFab.ClientModels;

public class PlayFabSaveStorage : ISaveStorage
{
    public const string WorldSlotIndexId = "__world_slot_index";

    private readonly string keyPrefix;

    public PlayFabSaveStorage(string keyPrefix = "save_")
    {
        this.keyPrefix = keyPrefix;
    }

    private string Key(string slotId) => $"{keyPrefix}{slotId}";

    public UniTask<bool> ExistsAsync(string slotId)
    {
        var tcs = new UniTaskCompletionSource<bool>();

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result => tcs.TrySetResult(result.Data != null && result.Data.ContainsKey(Key(slotId))),
            err => tcs.TrySetException(new Exception(err.GenerateErrorReport()))
        );

        return tcs.Task;
    }

    public UniTask SaveAsync(string slotId, byte[] bytes)
    {
        var tcs = new UniTaskCompletionSource();
        var payload = Convert.ToBase64String(bytes);

        var req = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { Key(slotId), payload }
            }
        };

        PlayFabClientAPI.UpdateUserData(req,
            _ => tcs.TrySetResult(),
            err => tcs.TrySetException(new Exception(err.GenerateErrorReport()))
        );

        return tcs.Task;
    }

    public UniTask<byte[]> LoadAsync(string slotId)
    {
        var tcs = new UniTaskCompletionSource<byte[]>();

        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data == null || !result.Data.TryGetValue(Key(slotId), out var record))
                {
                    tcs.TrySetResult(null);
                    return;
                }

                try
                {
                    var bytes = Convert.FromBase64String(record.Value);
                    tcs.TrySetResult(bytes);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            },
            err => tcs.TrySetException(new Exception(err.GenerateErrorReport()))
        );

        return tcs.Task;
    }

    public UniTask DeleteAsync(string slotId)
    {
        var tcs = new UniTaskCompletionSource();

        var req = new UpdateUserDataRequest
        {
            KeysToRemove = new List<string> { Key(slotId) }
        };

        PlayFabClientAPI.UpdateUserData(req,
            _ => tcs.TrySetResult(),
            err => tcs.TrySetException(new Exception(err.GenerateErrorReport()))
        );

        return tcs.Task;
    }

    public async UniTask<SaveSlotIndexDTO> LoadWorldSlotIndexAsync(bool compressGzip = true)
    {
        var bytes = await LoadAsync(WorldSlotIndexId);
        if (bytes == null || bytes.Length == 0)
            return new SaveSlotIndexDTO();

        var index = SaveSerializerNewtonsoft.FromBytes<SaveSlotIndexDTO>(bytes, compressGzip);
        return index ?? new SaveSlotIndexDTO();
    }

    public UniTask<IReadOnlyList<WorldSlotMetaDTO>> ListWorldSlotsAsync(bool compressGzip = true)
    {
        return ListWorldSlotsInternalAsync(compressGzip);
    }

    public UniTask SaveWorldSlotIndexAsync(SaveSlotIndexDTO index, bool compressGzip = true)
    {
        index ??= new SaveSlotIndexDTO();
        index.updatedAtUtcTicks = DateTime.UtcNow.Ticks;
        var bytes = SaveSerializerNewtonsoft.ToBytes(index, compressGzip);
        return SaveAsync(WorldSlotIndexId, bytes);
    }

    private async UniTask<IReadOnlyList<WorldSlotMetaDTO>> ListWorldSlotsInternalAsync(bool compressGzip)
    {
        var index = await LoadWorldSlotIndexAsync(compressGzip);
        return index.slots ?? (IReadOnlyList<WorldSlotMetaDTO>)Array.Empty<WorldSlotMetaDTO>();
    }
}
