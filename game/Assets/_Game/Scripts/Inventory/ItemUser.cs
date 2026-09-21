using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player use an item straight out of the backpack with the number keys:
// 1..9 for the first nine slots, 0 for the tenth.
//
// The rules of "what does this item do" live in the ItemData asset, not here, so
// adding a new edible item is a matter of making an asset and ticking "usable" —
// no new code.
[RequireComponent(typeof(Inventory))]
public class ItemUser : MonoBehaviour
{
    Inventory inventory;
    PlayerHealth health;

    // The HUD listens to this so it can flash the slot that was pressed.
    // The bool says whether the item was actually used.
    public event Action<int, bool> Used;

    // Keyboard keys for the slots, in order. Slot 10 is the "0" key, the way
    // most games lay the hotbar out.
    static readonly Key[] SlotKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
    };

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        health = GetComponent<PlayerHealth>();
    }

    void Update()
    {
        // No eating while paused, or while the hero-select screen is up.
        if (Keyboard.current == null || Time.timeScale == 0f)
            return;

        int slots = Mathf.Min(inventory.SlotCount, SlotKeys.Length);
        for (int i = 0; i < slots; i++)
        {
            if (Keyboard.current[SlotKeys[i]].wasPressedThisFrame)
                TryUse(i);
        }
    }

    // Returns true if one item was consumed.
    public bool TryUse(int slotIndex)
    {
        ItemBag.Slot slot = inventory.GetSlot(slotIndex);
        bool used = false;

        if (!slot.IsEmpty && slot.Item.usable && ApplyEffect(slot.Item))
            used = inventory.RemoveFromSlot(slotIndex, 1);

        Used?.Invoke(slotIndex, used);
        return used;
    }

    // Does what the item promises. Returns false if it would have done nothing,
    // so the item isn't wasted — eating a berry at full health should be refused,
    // not silently swallowed.
    bool ApplyEffect(ItemData item)
    {
        bool didSomething = false;

        if (item.healAmount > 0f && health != null && health.Heal(item.healAmount) > 0f)
            didSomething = true;

        if (didSomething)
            GameAudio.Play(GameAudio.Sfx.Eat);

        return didSomething;
    }
}
