using System;
using UnityEngine;

// The player's backpack during a run. The slots and stacking rules are an
// ItemBag; this script just gives the Player one, and ties it to the stash:
// a run starts with whatever was stashed from the last run (and bought in the
// shop), and RunBank copies it back when the run ends.
public class Inventory : MonoBehaviour
{
    [Tooltip("How many slots the backpack has.")]
    [SerializeField] int slotCount = PlayerProfile.StashSlots;

    ItemBag bag;

    // Fired whenever anything changes, so the HUD only redraws when it needs to.
    public event Action Changed;

    public ItemBag Bag => bag;
    public int SlotCount => bag.SlotCount;
    public ItemBag.Slot GetSlot(int index) => bag.GetSlot(index);

    void Awake()
    {
        bag = new ItemBag(slotCount);
        bag.CopyFrom(PlayerProfile.Stash); // bring in last run's items
        bag.Changed += () => Changed?.Invoke();
    }

    public int Add(ItemData item, int amount) => bag.Add(item, amount);
    public int CountOf(ItemData item) => bag.CountOf(item);
    public bool Remove(ItemData item, int amount) => bag.Remove(item, amount);
    public bool RemoveFromSlot(int index, int amount) => bag.RemoveFromSlot(index, amount);
    public ItemData RemoveOneWhere(Func<ItemData, bool> test) => bag.RemoveOneWhere(test);
}
