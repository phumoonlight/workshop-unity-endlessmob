using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The gear button in the top-right corner of the run, and the settings window
// it opens. Built in code like the other menus, so the scene only holds this
// component. For now the window has one section: SOUND.
//
// Opening it pauses the game. The values themselves live in SoundSettings;
// this class is only the sliders that change them.
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] RectTransform canvasRoot;

    [Header("Gear button (top-right)")]
    [SerializeField] float buttonSize = 56f;
    [Tooltip("Gap between the button and the screen's top and right edges.")]
    [SerializeField] float buttonMargin = 16f;
    [SerializeField] Color buttonColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Window")]
    [SerializeField] Color panelColor = new Color(0.08f, 0.09f, 0.12f, 0.97f);
    [SerializeField] Color sliderColor = new Color(0.3f, 0.85f, 1f);
    [SerializeField] Color closeColor = new Color(0.35f, 0.35f, 0.40f);
    [SerializeField] Color resetColor = new Color(0.45f, 0.30f, 0.25f);

    [Tooltip("While dragging a slider, play its sound at most this often, so you hear the new volume.")]
    [SerializeField] float previewInterval = 0.25f;

    // Other scripts ask this: DebugHUD leaves Esc (and its PAUSED text) alone
    // while the window is open, so Esc only closes the window.
    public static bool IsOpen { get; private set; }

    GameObject panel;
    Slider masterSlider, slashSlider, shootSlider;
    TextMeshProUGUI masterValue, slashValue, shootValue;

    bool resumeOnClose;   // was the game running when we opened?
    bool dirty;           // a slider moved since the last save
    float lastPreviewTime;

    void Awake()
    {
        IsOpen = false; // statics survive between Play presses here
    }

    void Start()
    {
        BuildGearButton();
        BuildWindow();
        panel.SetActive(false);
    }

    void OnDestroy()
    {
        // Leaving the scene with the window open (M from... anywhere): don't
        // lose the change, and don't leave the flag set for the next scene.
        if (dirty)
            SoundSettings.Save();
        IsOpen = false;
    }

    // LateUpdate, not Update: DebugHUD reads Esc in its Update. Closing here,
    // after every Update has run, means it still sees the window as open this
    // frame and can't treat the same key press as "pause".
    void LateUpdate()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    void Open()
    {
        if (IsOpen)
            return;
        IsOpen = true;

        // Pause, remembering whether there was anything to pause. If a level-up
        // popup or the Esc pause already stopped time, closing must not restart it.
        // "> 0" rather than "== 1" because hit-stop briefly runs time at 5 %.
        resumeOnClose = Time.timeScale > 0f;
        Time.timeScale = 0f;

        ShowValues();
        panel.transform.SetAsLastSibling(); // drawn last = drawn on top
        panel.SetActive(true);
    }

    void Close()
    {
        if (!IsOpen)
            return;
        IsOpen = false;
        panel.SetActive(false);

        if (dirty)
        {
            SoundSettings.Save(); // once, on closing, not on every pixel of a drag
            dirty = false;
        }
        if (resumeOnClose)
            Time.timeScale = 1f;
    }

    // ---- Sliders ---------------------------------------------------------------

    // Put the saved values on the sliders. "WithoutNotify" so that showing a
    // value doesn't count as the player changing it (no preview sound, no save).
    void ShowValues()
    {
        masterSlider.SetValueWithoutNotify(SoundSettings.Master);
        slashSlider.SetValueWithoutNotify(SoundSettings.Slash);
        shootSlider.SetValueWithoutNotify(SoundSettings.Shoot);
        ShowPercents();
    }

    void ShowPercents()
    {
        masterValue.text = Percent(SoundSettings.Master);
        slashValue.text = Percent(SoundSettings.Slash);
        shootValue.text = Percent(SoundSettings.Shoot);
    }

    static string Percent(float value) => Mathf.RoundToInt(value * 100f) + "%";

    void OnMasterChanged(float value)
    {
        SoundSettings.Master = value;
        Changed(GameAudio.Sfx.Slash);
    }

    void OnSlashChanged(float value)
    {
        SoundSettings.Slash = value;
        Changed(GameAudio.Sfx.Slash);
    }

    void OnShootChanged(float value)
    {
        SoundSettings.Shoot = value;
        Changed(GameAudio.Sfx.Shoot);
    }

    // After any slider moved: update the numbers and let the player hear it.
    // Unscaled time, because the game is paused while this window is open.
    void Changed(GameAudio.Sfx preview)
    {
        dirty = true;
        ShowPercents();

        if (Time.unscaledTime - lastPreviewTime >= previewInterval)
        {
            lastPreviewTime = Time.unscaledTime;
            GameAudio.Play(preview);
        }
    }

    void ResetToDefaults()
    {
        SoundSettings.ResetToDefaults();
        dirty = true;
        ShowValues();
    }

    // ---- Building --------------------------------------------------------------

    void BuildGearButton()
    {
        Button button = UiFactory.Button("SettingsButton", canvasRoot, Vector2.zero,
                                         new Vector2(buttonSize, buttonSize), buttonColor, "", out _);

        // UiFactory anchors to the centre; this one belongs to the top-right
        // corner, so it stays there at any screen size.
        RectTransform rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-buttonMargin, -buttonMargin);

        Image icon = UiFactory.Box("Icon", rect, Vector2.zero, new Vector2(buttonSize - 16f, buttonSize - 16f), Color.white);
        icon.sprite = HudIcons.Gear;

        button.onClick.AddListener(Open);
    }

    void BuildWindow()
    {
        // Same dark veil as the other menus: blocks clicks on the HUD underneath.
        panel = UiFactory.Box("Settings", canvasRoot, Vector2.zero, new Vector2(4000f, 4000f),
                              new Color(0f, 0f, 0f, 0.7f), blocksClicks: true).gameObject;
        Transform box = UiFactory.Box("Panel", panel.transform, Vector2.zero, new Vector2(900f, 600f), panelColor).transform;

        UiFactory.Text("Title", box, new Vector2(0f, 245f), new Vector2(800f, 70f), 56f,
                       TextAlignmentOptions.Center, "SETTINGS");
        UiFactory.Text("SoundHeading", box, new Vector2(0f, 160f), new Vector2(780f, 50f), 34f,
                       TextAlignmentOptions.Left, "<color=#80c8ff>SOUND</color>");

        masterSlider = BuildRow(box, 90f, "Master volume", out masterValue);
        slashSlider = BuildRow(box, 10f, "Sword swing", out slashValue);
        shootSlider = BuildRow(box, -70f, "Dagger throw", out shootValue);

        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        slashSlider.onValueChanged.AddListener(OnSlashChanged);
        shootSlider.onValueChanged.AddListener(OnShootChanged);

        Button reset = UiFactory.Button("Reset", box, new Vector2(-160f, -230f), new Vector2(280f, 70f),
                                        resetColor, "Defaults", out _);
        reset.onClick.AddListener(ResetToDefaults);

        Button close = UiFactory.Button("Close", box, new Vector2(160f, -230f), new Vector2(280f, 70f),
                                        closeColor, "Close  [Esc]", out _);
        close.onClick.AddListener(Close);
    }

    // One line of the window: name on the left, slider in the middle, percent on the right.
    Slider BuildRow(Transform box, float y, string label, out TextMeshProUGUI value)
    {
        UiFactory.Text(label, box, new Vector2(-250f, y), new Vector2(280f, 50f), 30f,
                       TextAlignmentOptions.Left, label);
        Slider slider = UiFactory.Slider(label + " Slider", box, new Vector2(80f, y), new Vector2(360f, 40f), sliderColor);
        value = UiFactory.Text(label + " Value", box, new Vector2(340f, y), new Vector2(100f, 50f), 30f,
                               TextAlignmentOptions.Right);
        return slider;
    }
}
