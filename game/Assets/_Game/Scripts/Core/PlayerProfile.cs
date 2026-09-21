using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Everything that carries over from one run to the next: banked coins, the item
// stash, and each class's experience.
//
// "static" means there is one copy for the whole game, not attached to any
// scene -- so it survives the jump from a run back to the main menu. It is also
// written to a file every time it changes, and read back when the game starts,
// so closing the game loses nothing. There's no save button: saving just happens.
//
// The file is plain JSON, profile.json, in:
//   built game:  C:\Users\<you>\AppData\LocalLow\<Company>\<Product>\
//   editor:      C:\Users\<you>\AppData\LocalLow\<Company>\<Product>Dev\
// Two folders so testing in the editor never touches real progress (yours
// included, when you play the built game). Delete a file to start over.
//
// Items and classes are saved by their number id (ItemData.id,
// PlayerClassData.id), never by name, so both can be renamed at will.
public static class PlayerProfile
{
    public const int StashSlots = 10;

    // Bump this when the file's layout changes. A file with any other version
    // is not read at all: the game starts fresh and overwrites it on the next
    // save. (Version 1 stored items and classes by name.)
    const int SaveVersion = 2;

    // Every item the save file can mention lives in this Resources folder, so it
    // can be found by id when the game starts.
    const string ItemsFolder = "Items";
    const string FileName = "profile.json";

    public static int Coins;

    // Items waiting for the next run. The run's Inventory starts as a copy of
    // this, and is copied back when the run ends.
    public static readonly ItemBag Stash = new ItemBag(StashSlots);

    // Total class experience ever earned, by class id.
    static readonly Dictionary<int, int> classXp = new Dictionary<int, int>();

    static string FilePath => Path.Combine(SaveFolder, FileName);

    // persistentDataPath is ...\<Company>\<Product>. In the editor we use a
    // sibling folder with "Dev" on the end instead. "#if UNITY_EDITOR" code only
    // exists inside the editor -- it is left out of a built game entirely, so the
    // .exe can never pick the Dev folder by mistake.
    static string SaveFolder
    {
        get
        {
#if UNITY_EDITOR
            string parent = Path.GetDirectoryName(Application.persistentDataPath);
            return Path.Combine(parent, Application.productName + "Dev");
#else
            return Application.persistentDataPath;
#endif
        }
    }

    // ---- Class XP ------------------------------------------------------------

    public static int ClassXp(PlayerClassData hero)
    {
        return hero != null && classXp.TryGetValue(hero.id, out int xp) ? xp : 0;
    }

    public static void AddClassXp(PlayerClassData hero, int amount)
    {
        if (hero == null || amount <= 0)
            return;
        if (hero.id == 0)
            Debug.LogError($"Class '{hero.name}' has no id (0). Give it a unique number, or its XP gets mixed up with other classes.");
        classXp[hero.id] = ClassXp(hero) + amount;
    }

    // Walks up the class's XP curve with the total earned. Also reports how far
    // into the current level it is, for progress bars.
    public static int ClassLevel(PlayerClassData hero, out int xpIntoLevel, out int xpForNextLevel)
    {
        int level = 1;
        int left = ClassXp(hero);
        xpForNextLevel = hero != null ? hero.ClassXpToNextLevel(level) : 1;
        while (hero != null && left >= xpForNextLevel)
        {
            left -= xpForNextLevel;
            level++;
            xpForNextLevel = hero.ClassXpToNextLevel(level);
        }
        xpIntoLevel = left;
        return level;
    }

    public static int ClassLevel(PlayerClassData hero) => ClassLevel(hero, out _, out _);

    // ---- The save file -------------------------------------------------------

    // JsonUtility only understands plain fields, arrays and [Serializable]
    // classes -- no dictionaries, no asset references -- so the profile is
    // copied into these simple shapes to be written, and back out when read.
    [Serializable] class SaveData
    {
        public int version = SaveVersion;
        public int coins;
        public SlotData[] stash;
        public ClassXpData[] classes;
    }

    [Serializable] class SlotData
    {
        public int item;  // ItemData.id; 0 = empty slot
        public int count;
    }

    [Serializable] class ClassXpData
    {
        public int hero;  // PlayerClassData.id
        public int xp;
    }

    // Just the version number. JsonUtility skips fields it doesn't know, so this
    // reads the version out of a file of any layout, before trusting the rest.
    [Serializable] class VersionOnly
    {
        public int version;
    }

    // Runs by itself before the first scene loads, every time the game starts
    // (and every time Play is pressed in the editor).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Load()
    {
        Coins = 0;
        classXp.Clear();
        for (int i = 0; i < Stash.SlotCount; i++)
            Stash.SetSlot(i, null, 0);

        if (!File.Exists(FilePath))
            return; // first time playing: start from nothing

        try
        {
            string json = File.ReadAllText(FilePath);

            int version = JsonUtility.FromJson<VersionOnly>(json).version;
            if (version != SaveVersion)
            {
                // An older layout: start over rather than guess what it meant.
                // The next save replaces the file.
                Debug.Log($"PlayerProfile: save file is version {version}, this game uses {SaveVersion}. Starting fresh.");
                return;
            }

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Coins = data.coins;

            Dictionary<int, ItemData> items = ItemsById();
            if (data.stash != null)
                for (int i = 0; i < data.stash.Length && i < Stash.SlotCount; i++)
                {
                    SlotData slot = data.stash[i];
                    // An id with no matching item (it was deleted) is just dropped.
                    if (slot.item != 0 && items.TryGetValue(slot.item, out ItemData item))
                        Stash.SetSlot(i, item, slot.count);
                }

            if (data.classes != null)
                foreach (ClassXpData entry in data.classes)
                    classXp[entry.hero] = entry.xp;
        }
        catch (Exception e)
        {
            // A broken file must never stop the game from starting. Keep it aside
            // (in case it's worth looking at) and start fresh.
            Debug.LogWarning($"PlayerProfile: couldn't read {FilePath}, starting fresh. {e.Message}");
            try { File.Copy(FilePath, FilePath + ".broken", true); } catch { }
        }
    }

    // Every item, by id. Also the place that catches the one mistake that would
    // quietly scramble saves: two items given the same id, or one left at 0.
    static Dictionary<int, ItemData> ItemsById()
    {
        Dictionary<int, ItemData> items = new Dictionary<int, ItemData>();
        foreach (ItemData item in Resources.LoadAll<ItemData>(ItemsFolder))
        {
            if (item.id == 0)
                Debug.LogError($"Item '{item.name}' has no id (0). Give it a unique number, or it can't be saved.");
            else if (items.TryGetValue(item.id, out ItemData other))
                Debug.LogError($"Items '{other.name}' and '{item.name}' share id {item.id}. Every item needs its own number.");
            else
                items[item.id] = item;
        }
        return items;
    }

    // Call after anything above changes. Cheap: the file is a few hundred bytes.
    public static void Save()
    {
        SaveData data = new SaveData { coins = Coins };

        data.stash = new SlotData[Stash.SlotCount];
        for (int i = 0; i < Stash.SlotCount; i++)
        {
            ItemBag.Slot slot = Stash.GetSlot(i);
            data.stash[i] = new SlotData
            {
                item = slot.IsEmpty ? 0 : slot.Item.id,
                count = slot.IsEmpty ? 0 : slot.Count,
            };
        }

        data.classes = new ClassXpData[classXp.Count];
        int n = 0;
        foreach (KeyValuePair<int, int> entry in classXp)
            data.classes[n++] = new ClassXpData { hero = entry.Key, xp = entry.Value };

        try
        {
            // Unity makes the normal folder by itself; the Dev one is ours to make.
            Directory.CreateDirectory(SaveFolder);

            // Write a temporary file first, then swap it in. If the game is
            // killed halfway through writing, the old save is still whole.
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(FilePath))
                File.Replace(temp, FilePath, null);
            else
                File.Move(temp, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PlayerProfile: couldn't save to {FilePath}. {e.Message}");
        }
    }
}
