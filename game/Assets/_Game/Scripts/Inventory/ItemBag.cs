using System;
using UnityEngine;

// A fixed number of slots, each holding a stack of one kind of item.
//
// Plain C#, not a MonoBehaviour, so it can live anywhere: the Player's
// Inventory holds one during a run, and PlayerProfile holds one between runs
// (the stash the shop buys into and sells from). Both use exactly the same
// stacking rules because they are the same code.
public class ItemBag
{
    // One slot. A class (not a struct) so the HUD can hold on to a slot and
    // keep seeing its changes, instead of getting a frozen copy.
    public class Slot
    {
        public ItemData Item;
        public int Count;
        public bool IsEmpty => Item == null || Count <= 0;
    }

    readonly Slot[] slots;

    // Fired whenever anything changes, so a HUD only redraws when it needs to.
    public event Action Changed;

    public ItemBag(int slotCount)
    {
        slots = new Slot[slotCount];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = new Slot();
    }

    public int SlotCount => slots.Length;
    public Slot GetSlot(int index) => slots[index];

    // Puts items in and returns how many did NOT fit (0 means all of them went in).
    public int Add(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
            return 0;

        int left = amount;

        // First top up stacks of this item that aren't full yet, so we don't
        // waste an empty slot while a half-full stack of the same thing exists.
        foreach (Slot slot in slots)
        {
            if (left <= 0)
                break;
            if (slot.Item != item)
                continue;

            int moved = Mathf.Min(item.maxStack - slot.Count, left);
            slot.Count += moved;
            left -= moved;
        }

        // Then start new stacks in empty slots.
        foreach (Slot slot in slots)
        {
            if (left <= 0)
                break;
            if (!slot.IsEmpty)
                continue;

            slot.Item = item;
            slot.Count = Mathf.Min(item.maxStack, left);
            left -= slot.Count;
        }

        if (left != amount)
            Changed?.Invoke(); // something actually went in

        return left;
    }

    // Would this many fit? The shop asks before taking your coins.
    public bool CanFit(ItemData item, int amount)
    {
        int space = 0;
        foreach (Slot slot in slots)
        {
            if (slot.IsEmpty)
                space += item.maxStack;
            else if (slot.Item == item)
                space += item.maxStack - slot.Count;
            if (space >= amount)
                return true;
        }
        return false;
    }

    // How many of this item there are, across every slot.
    public int CountOf(ItemData item)
    {
        int total = 0;
        foreach (Slot slot in slots)
            if (slot.Item == item)
                total += slot.Count;
        return total;
    }

    // Takes items out. Returns false and changes nothing if there aren't enough.
    public bool Remove(ItemData item, int amount)
    {
        if (item == null || amount <= 0 || CountOf(item) < amount)
            return false;

        int left = amount;
        foreach (Slot slot in slots)
        {
            if (left <= 0)
                break;
            if (slot.Item != item)
                continue;

            int taken = Mathf.Min(slot.Count, left);
            slot.Count -= taken;
            left -= taken;
            if (slot.Count <= 0)
                slot.Item = null; // stack used up: the slot is free again
        }

        Changed?.Invoke();
        return true;
    }

    // Takes items out of one particular slot. Used when the player presses the
    // number key for that slot: pressing "3" should drain slot 3, not some other
    // stack of the same item.
    public bool RemoveFromSlot(int index, int amount)
    {
        if (index < 0 || index >= slots.Length || amount <= 0)
            return false;

        Slot slot = slots[index];
        if (slot.IsEmpty || slot.Count < amount)
            return false;

        slot.Count -= amount;
        if (slot.Count <= 0)
            slot.Item = null;

        Changed?.Invoke();
        return true;
    }

    // Uses up one of the first item that passes the test, e.g. "anything that
    // revives you". Returns the item used, or null if there was none.
    public ItemData RemoveOneWhere(Func<ItemData, bool> test)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty && test(slots[i].Item))
            {
                ItemData item = slots[i].Item;
                RemoveFromSlot(i, 1);
                return item;
            }
        }
        return null;
    }

    // Puts exactly this in one slot. Used when reading the save file, so every
    // item comes back in the slot it was saved from.
    public void SetSlot(int index, ItemData item, int count)
    {
        bool empty = item == null || count <= 0;
        slots[index].Item = empty ? null : item;
        slots[index].Count = empty ? 0 : count;
        Changed?.Invoke();
    }

    // Makes this bag an exact copy of another, slot for slot. How items move
    // between the stash and a run.
    public void CopyFrom(ItemBag other)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            bool has = i < other.slots.Length;
            slots[i].Item = has ? other.slots[i].Item : null;
            slots[i].Count = has ? other.slots[i].Count : 0;
        }
        Changed?.Invoke();
    }
}
