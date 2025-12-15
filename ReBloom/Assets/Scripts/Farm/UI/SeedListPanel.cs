using System;
using System.Collections.Generic;
using UnityEngine;

public class SeedListPanel : MonoBehaviour
{
    [SerializeField] private SeedSlotUI slotPrefab;
    [SerializeField] private Transform root;

    private readonly List<SeedSlotUI> pool = new();

    public void Bind(List<SeedStack> stacks, Action<int> onSeedClicked)
    {
        EnsurePool(stacks.Count);

        for (int i = 0; i < pool.Count; i++)
        {
            bool on = i < stacks.Count;
            var ui = pool[i];
            ui.gameObject.SetActive(on);

            if (!on) continue;

            ui.SetData(stacks[i], onSeedClicked);
        }
    }

    private void EnsurePool(int count)
    {
        while (pool.Count < count)
            pool.Add(Instantiate(slotPrefab, root));
    }
}
