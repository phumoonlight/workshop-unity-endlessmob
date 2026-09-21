using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Uses your class skills when you press their keys (once you're high enough level)
// and drives the skill slots in the corner of the screen.
// Slot 0 = Q (gamepad right shoulder), slot 1 = R (gamepad left shoulder).
public class SkillController : MonoBehaviour
{
    [Serializable]
    class Slot
    {
        public string keyName = "Q";
        public Key key = Key.Q;
        public GameObject root;           // hidden if the class has no skill in this slot
        public TMP_Text label;
        public RectTransform cooldownFill; // bar that fills up as the cooldown finishes
        public Image cooldownFillImage;
    }

    [SerializeField] PlayerClassSelector classSelector;
    [SerializeField] PlayerExperience experience;
    [SerializeField] PlayerHealth health;

    [SerializeField] Slot[] slots;
    [SerializeField] Color readyColor = new Color(0.3f, 0.85f, 1f);
    [SerializeField] Color coolingColor = new Color(0.4f, 0.4f, 0.45f);

    void Update()
    {
        for (int i = 0; i < slots.Length; i++)
            UpdateSlot(slots[i], classSelector.GetSkill(i), i);
    }

    void UpdateSlot(Slot slot, ClassSkill skill, int index)
    {
        // Before a class is picked, only the first slot shows (as a hint).
        bool show = skill != null || (index == 0 && classSelector.SelectedClass == null);
        slot.root.SetActive(show);
        if (skill == null)
        {
            slot.label.text = "<size=80%>Choose a hero</size>";
            SetFill(slot, 0f, coolingColor);
            return;
        }

        bool unlocked = experience.Level >= skill.UnlockLevel;

        if (health.IsDead && skill.IsRunning)
            skill.Cancel(); // stop a running skill if the player dies

        if (unlocked && !health.IsDead && Time.timeScale > 0f && IsPressed(slot, index))
            skill.TryActivate();

        string name = $"<b>{skill.DisplayName}</b>  <color=#ffd24a>[{slot.keyName}]</color>";
        if (!unlocked)
        {
            slot.label.text = $"{name}\n<size=75%><color=#a0a0a0>Unlocks at LV {skill.UnlockLevel}</color></size>";
            SetFill(slot, 0f, coolingColor);
        }
        else if (skill.IsRunning)
        {
            slot.label.text = $"{name}\n<size=75%><color=#80ff80>ACTIVE</color></size>";
            SetFill(slot, 1f, readyColor);
        }
        else if (skill.CooldownLeft > 0f)
        {
            slot.label.text = $"{name}\n<size=75%>{skill.CooldownLeft:0.0}s</size>";
            SetFill(slot, 1f - skill.CooldownLeft / skill.Cooldown, coolingColor);
        }
        else
        {
            slot.label.text = $"{name}\n<size=75%><color=#80d8ff>READY</color></size>";
            SetFill(slot, 1f, readyColor);
        }
    }

    void SetFill(Slot slot, float fraction, Color color)
    {
        slot.cooldownFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        slot.cooldownFillImage.color = color;
    }

    static bool IsPressed(Slot slot, int index)
    {
        bool key = Keyboard.current != null && Keyboard.current[slot.key].wasPressedThisFrame;
        bool pad = false;
        if (Gamepad.current != null)
            pad = index == 0 ? Gamepad.current.rightShoulder.wasPressedThisFrame
                             : Gamepad.current.leftShoulder.wasPressedThisFrame;
        return key || pad;
    }
}
