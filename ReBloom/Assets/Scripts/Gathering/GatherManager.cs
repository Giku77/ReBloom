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

    public DropResult GetDropResult(int gatherObjectId)
    {
        Debug.Log($"[GatherManager] 요청된 ID: {gatherObjectId}, DB에 있는 키들: {string.Join(", ", gatherObjectDB.GetAll().Keys)}");

        if (!gatherObjectDB.TryGet(gatherObjectId, out GatherObjectData objectData))
        {
            Debug.Log("[GatherManager] gatherObjectId 없음");
            return null;
        }

        if (!gatherDB.TryGet(objectData.gatherId, out GatherData data))
        {
            Debug.Log("[GatherManager] gatherId 없음");
            return null;
        }

        if (Random.Range(0, 100) < data.item1Probability)
        {
            int amount = Random.Range(data.item1MinAmount, data.item1MaxAmount + 1);
            return new DropResult
            {
                item = ItemDatabase.I.GetItem(data.getItem1),
                amount = amount
            };
        }
        else if (data.getItem2 != 0 && Random.Range(0, 100) < data.item2Probability)
        {
            int amount = Random.Range(data.item2MinAmount, data.item2MaxAmount + 1);
            return new DropResult
            {
                item = ItemDatabase.I.GetItem(data.getItem2),
                amount = amount
            };
        }

        return null;
    }
}
