using UnityEngine;
using System.Collections.Generic;

public class DropResult
{
    public ItemBase item;
    public int amount;
}

public class GatherManager : MonoBehaviour
{
    private GatherObjectDB gatherObjectDB;
    private GatherDB gatherDB;

    public GatherObjectDB GatherObjectDB => gatherObjectDB;

    private void Awake()
    {
        gatherObjectDB = new GatherObjectDB();
        gatherObjectDB.LoadFromBG();

        gatherDB = new GatherDB();
        gatherDB.LoadFromBG();
    }

    private void Start()
    {
        GatherObject[] gathers = FindObjectsByType<GatherObject>(FindObjectsSortMode.None);

        foreach (var gather in gathers)
        {
            gather.Initialize(gatherObjectDB);
        }
    }

    public DropResult GetDropResult(int gatherObjectId, bool isNight = false)
    {
        if (!gatherObjectDB.TryGet(gatherObjectId, out GatherObjectData objectData))
        {
            Debug.LogWarning($"[GatherManager] gatherObjectId 없음: {gatherObjectId}");
            return null;
        }

        if (!gatherDB.TryGet(objectData.gatherId, out GatherData data))
        {
            Debug.LogWarning($"[GatherManager] gatherId 없음: {objectData.gatherId}");
            return null;
        }

        float nightMultiplier = (isNight && data.nightMultiple > 0) ? data.nightMultiple : 1f;

        int randomValue = Random.Range(0, 100);

        if (randomValue < data.item1Probability)
        {
            int baseAmount = Random.Range(data.item1MinAmount, data.item1MaxAmount + 1);
            int finalAmount = Mathf.RoundToInt(baseAmount * nightMultiplier);
            return new DropResult
            {
                item = ItemDatabase.I.GetItem(data.getItem1),
                amount = finalAmount
            };
        }
        else if (data.getItem2 != 0 && randomValue < data.item1Probability + data.item2Probability)
        {
            int baseAmount = Random.Range(data.item2MinAmount, data.item2MaxAmount + 1);
            int finalAmount = Mathf.RoundToInt(baseAmount * nightMultiplier);
            return new DropResult
            {
                item = ItemDatabase.I.GetItem(data.getItem2),
                amount = finalAmount
            };
        }

        return null;
    }
}
