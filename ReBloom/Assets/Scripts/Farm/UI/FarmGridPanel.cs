using System;
using System.Collections.Generic;
using UnityEngine;

public class FarmGridPanel : MonoBehaviour
{
    [SerializeField] private FarmCellSlotUI[] cells;
    [SerializeField] private Transform root;

    private FarmBed boundPlot;

    private Action<int> onCellClicked;
    private Action<int, int> onSeedDroppedToCell; 

    public void Bind(FarmBed plot, Action<int> onCellClicked, Action<int, int> onSeedDroppedToCell)
    {
        boundPlot = plot;
        this.onCellClicked = onCellClicked;
        this.onSeedDroppedToCell = onSeedDroppedToCell;

        //EnsurePool(plot.Slots.Length);

        Debug.Log($"[FarmGridPanel] Bind plot with {cells.Length} slots.");

        for (int i = 0; i < cells.Length; i++)
        {
            var ui = cells[i];
            bool on = i < plot.Slots.Length;
            Debug.Log($"[FarmGridPanel] Setting slot {i} active: {on}");
            ui.gameObject.SetActive(on);

            if (!on) continue;

            int idx = i;
            ui.Bind(
                index: idx,
                slot: plot.Slots[idx],              
                onClick: () => this.onCellClicked?.Invoke(idx),
                onSeedDrop: (seedId) => this.onSeedDroppedToCell?.Invoke(seedId, idx)
            );
        }
    }

    public void Refresh()
    {
        if (boundPlot == null) return;

        for (int i = 0; i < boundPlot.Slots.Length && i < cells.Length; i++)
            cells[i].Refresh(boundPlot.Slots[i]);     
    }

    // private void EnsurePool(int count)
    // {
    //     while (cells.Count < count)
    //         cells.Add(Instantiate(cellPrefab, root));
    // }
}
