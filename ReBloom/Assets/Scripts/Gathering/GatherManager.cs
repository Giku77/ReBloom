using UnityEngine;
using System.Collections.Generic;


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

    public ItemBase GetDropResult(int gatherObjectId)
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
            return ItemDatabase.I.GetItem(data.getItem1);
        else if(data.getItem2 != 0 && Random.Range(0, 100) < data.item2Probability)
            return ItemDatabase.I.GetItem(data.getItem2);


        return null;
    }
}
