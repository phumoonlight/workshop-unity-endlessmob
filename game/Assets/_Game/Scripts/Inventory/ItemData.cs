using UnityEngine;

// Describes one kind of item that can sit in the inventory.
// One asset per item: every berry in the game shares the same Berry asset, and
// the inventory only counts how many of it you have.
// Create more with: right-click in the Project window > Create > Endless Mob > Item
[CreateAssetMenu(fileName = "Item", menuName = "Endless Mob/Item")]
public class ItemData : ScriptableObject
{
    [Tooltip("Permanent number the save file uses for this item. Rename the item freely, but never change this once players have saves, and never reuse a number. 0 = not set.")]
    public int id;

    public string displayName = "Item";

    [TextArea(2, 3)]
    public string description = "What it's for.";

    [Tooltip("How many of this item fit in one slot before a new slot is used.")]
    public int maxStack = 99;

    [Tooltip("Colour of the square drawn in the inventory slot (we have no icon art yet).")]
    public Color color = Color.white;

    [Header("Using the item")]
    [Tooltip("Can the player use this from the inventory (number keys 1-0)?")]
    public bool usable = false;

    [Tooltip("Health restored when used. 0 means it heals nothing.")]
    public float healAmount = 0f;

    [Header("Carried (works just by being in the inventory)")]
    [Tooltip("When you would die, one of these is used up instead and you get back up.")]
    public bool revivesOnDeath = false;

    [Tooltip("Extra max HP for every one of these you carry into a run.")]
    public float maxHealthBonus = 0f;

    [Header("Shop")]
    [Tooltip("Coins to buy one in the main menu shop. 0 = not for sale.")]
    public int buyPrice = 0;

    [Tooltip("Coins you get for selling one.")]
    public int sellPrice = 0;
}
