using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// A berry bush growing in the wild. Stand next to it and hold E (gamepad: top
// face button) for a moment to pick the berries into your inventory. The bush goes bare and
// regrows after a while, so the map doesn't run out.
public class BerryBush : MonoBehaviour
{
    [SerializeField] ItemData berryItem;

    [Tooltip("How many berries one bush gives (picked at random between the two).")]
    [SerializeField] int minBerries = 2;
    [SerializeField] int maxBerries = 5;

    [SerializeField] float interactRange = 2.5f;

    [Tooltip("How long E must be held to pick. Letting go or walking away starts it over.")]
    [SerializeField] float holdSeconds = 1f;

    [Tooltip("Seconds until a picked bush grows its berries back.")]
    [SerializeField] float regrowTime = 30f;

    [Tooltip("The berry blobs. Hidden while the bush is bare.")]
    [SerializeField] GameObject[] berries;

    [Tooltip("Floating 'Press E' text, shown when the player is close.")]
    [SerializeField] TextMeshPro label;

    // Shared by every bush, so we only search the scene once.
    static Inventory inventory;
    static PlayerHealth playerHealth;

    Camera cam;
    float regrowLeft;   // above 0 = picked and waiting to regrow
    string feedback;    // short message shown under the prompt ("Inventory full")
    float feedbackTimer;
    float held;         // seconds E has been held down so far

    void Awake()
    {
        cam = Camera.main;
        if (inventory == null)
            inventory = FindAnyObjectByType<Inventory>();
        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();

        label.gameObject.SetActive(false);
    }

    void Update()
    {
        feedbackTimer -= Time.deltaTime;

        // Bare bush: count down, then put the berries back.
        if (regrowLeft > 0f)
        {
            regrowLeft -= Time.deltaTime;
            if (regrowLeft <= 0f)
                ShowBerries(true);
            label.gameObject.SetActive(false);
            return;
        }

        bool near = playerHealth != null && !playerHealth.IsDead && Time.timeScale > 0f
                    && FlatDistance(playerHealth.transform.position, transform.position) < interactRange;

        label.gameObject.SetActive(near);
        if (!near)
        {
            held = 0f; // walking away cancels a half-finished pick
            return;
        }

        // Holding rather than tapping makes picking a small commitment: you stand
        // still for a second, which matters when enemies are closing in.
        if (IsInteractHeld())
        {
            held += Time.deltaTime;
            if (held >= holdSeconds)
            {
                held = 0f;
                Pick();
            }
        }
        else
            held = 0f;

        string line = held > 0f
            ? $"Picking... <color=#ffd24a>{Mathf.FloorToInt(held / holdSeconds * 100f)}%</color>"
            : "Hold <color=#ffd24a>E</color> to pick berries";
        label.text = feedbackTimer > 0f ? $"{line}\n<size=70%><color=#ffd24a>{feedback}</color></size>" : line;
    }

    void LateUpdate()
    {
        // Keep the label facing the camera, otherwise we'd read it edge-on.
        if (label.gameObject.activeSelf)
            label.transform.rotation = cam.transform.rotation;
    }

    void Pick()
    {
        int picked = Random.Range(minBerries, maxBerries + 1);
        int leftOver = inventory.Add(berryItem, picked);

        // Nothing fit: leave the berries on the bush so they aren't wasted.
        if (leftOver >= picked)
        {
            feedback = "Inventory full!";
            feedbackTimer = 2f;
            return;
        }

        GameAudio.Play(GameAudio.Sfx.Harvest);
        ShowBerries(false);
        regrowLeft = regrowTime;
    }

    void ShowBerries(bool visible)
    {
        foreach (GameObject berry in berries)
            if (berry != null)
                berry.SetActive(visible);
    }

    // "isPressed" is true every frame the button is down, unlike
    // "wasPressedThisFrame", which is only true on the frame it went down.
    static bool IsInteractHeld()
    {
        bool key = Keyboard.current != null && Keyboard.current.eKey.isPressed;
        bool pad = Gamepad.current != null && Gamepad.current.buttonNorth.isPressed;
        return key || pad;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
