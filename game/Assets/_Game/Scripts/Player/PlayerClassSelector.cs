using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Sets the player up for the chosen class.
//
// Normally the hero is picked in the PreStart scene (RunSettings.ChosenClass)
// and the run starts straight away. The "Choose your hero" panel here is the
// fallback for when nobody picked one -- pressing Play directly in the Game
// scene while testing in the editor.
public class PlayerClassSelector : MonoBehaviour
{
    [Header("Classes (one card each, in this order)")]
    [SerializeField] PlayerClassData[] classes;

    [Header("Player")]
    [SerializeField] PlayerController movement;
    [SerializeField] PlayerHealth health;
    [SerializeField] Renderer bodyRenderer;

    [Tooltip("The weapon component used by each weapon kind.")]
    [SerializeField] MeleeSlash swordWeapon;
    [SerializeField] AutoShooter bowWeapon;

    [Tooltip("The Q skill for each weapon kind.")]
    [SerializeField] ClassSkill swordSkill;
    [SerializeField] ClassSkill bowSkill;

    [Tooltip("The R skill for each weapon kind (leave empty if the class has none yet).")]
    [SerializeField] ClassSkill swordSkill2;
    [SerializeField] ClassSkill bowSkill2;

    [Tooltip("Props shown in the player's hand for each weapon kind. Leave empty when the character model carries its own weapon.")]
    [SerializeField] GameObject swordProp;
    [SerializeField] GameObject bowProp;

    [Tooltip("The character models. Leave empty to keep the plain capsule.")]
    [SerializeField] PlayerModel model;

    [Header("UI")]
    [SerializeField] GameObject panel;
    [SerializeField] UpgradeCard[] cards;

    public PlayerClassData SelectedClass { get; private set; }

    // Fired once the hero is set up. RunBank uses it to start the run at the
    // class level and apply carried items.
    public event Action<PlayerClassData> ClassChosen;
    public Weapon ActiveWeapon { get; private set; }
    public ClassSkill ActiveSkill { get; private set; }
    public ClassSkill ActiveSkill2 { get; private set; }

    // Skill in a slot: 0 = Q, 1 = R. Null if there's none.
    public ClassSkill GetSkill(int slot) => slot == 0 ? ActiveSkill : slot == 1 ? ActiveSkill2 : null;

    void Awake()
    {
        // No weapon until a class is picked.
        swordWeapon.enabled = false;
        bowWeapon.enabled = false;
        SetProp(swordProp, false);
        SetProp(bowProp, false);

        for (int i = 0; i < cards.Length; i++)
        {
            int index = i; // copy, so each button remembers its own number
            cards[i].Button.onClick.AddListener(() => Choose(index));
            cards[i].gameObject.SetActive(i < classes.Length);
            if (i < classes.Length)
                cards[i].Show(classes[i].displayName, classes[i].description,
                              $"[{i + 1}]    <color=#ffd24a>Class LV {PlayerProfile.ClassLevel(classes[i])}</color>");
        }
    }

    void Start()
    {
        panel.SetActive(true);
        Time.timeScale = 0f; // nothing moves until a hero is chosen

        // Already picked in PreStart? Start at once. This happens in the same
        // frame, before anything is drawn, so the panel never flashes up.
        int picked = System.Array.IndexOf(classes, RunSettings.ChosenClass);
        if (picked >= 0)
            Choose(picked);
    }

    void Update()
    {
        if (!panel.activeSelf || Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) Choose(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) Choose(1);
    }

    void Choose(int index)
    {
        if (!panel.activeSelf || index >= classes.Length)
            return;

        SelectedClass = classes[index];
        RunSettings.ChosenClass = SelectedClass; // so R after a defeat keeps this hero
        bool isSword = SelectedClass.weapon == WeaponKind.Sword;

        ActiveWeapon = isSword ? swordWeapon : bowWeapon;
        ActiveWeapon.enabled = true;
        ActiveSkill = isSword ? swordSkill : bowSkill;
        ActiveSkill2 = isSword ? swordSkill2 : bowSkill2;
        SetProp(swordProp, isSword);
        SetProp(bowProp, !isSword);
        if (model != null)
            model.Show(isSword);

        health.SetBaseMaxHealth(SelectedClass.maxHealth);
        movement.AddMoveSpeedPercent(SelectedClass.moveSpeedBonusPercent);
        bodyRenderer.material.SetColor("_BaseColor", SelectedClass.bodyColor);

        panel.SetActive(false);
        Time.timeScale = 1f;
        ClassChosen?.Invoke(SelectedClass);
    }

    // The props are the old primitive sword/bow. A character model brings its
    // own weapon, so the prop slots are then left empty and there's nothing to do.
    static void SetProp(GameObject prop, bool visible)
    {
        if (prop != null)
            prop.SetActive(visible);
    }
}
