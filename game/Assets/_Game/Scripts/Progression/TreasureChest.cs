using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// A reward chest. Walk up to it and press E (gamepad: top face button) to open it:
// coins and XP gems burst out, the lid swings open, and the chest disappears.
public class TreasureChest : MonoBehaviour
{
    [SerializeField] Coin coinPrefab;
    [SerializeField] ExperienceGem gemPrefab;

    [Tooltip("Floating 'Press E' text, shown when the player is close.")]
    [SerializeField] TextMeshPro label;

    [Tooltip("The lid's hinge. It rotates backward when opened.")]
    [SerializeField] Transform lidHinge;

    [SerializeField] float interactRange = 2.5f;

    [Tooltip("How far the rewards scatter from the chest.")]
    [SerializeField] float scatterRadius = 2f;

    [Tooltip("Seconds the open chest stays before disappearing.")]
    [SerializeField] float vanishDelay = 2.5f;

    Transform player;
    PlayerHealth playerHealth;
    int coins;
    int gems;
    bool opened;
    float openedTime;
    Camera cam;

    // Called right after the chest is created.
    public void Setup(PlayerHealth playerHealthRef, int coinCount, int gemCount)
    {
        playerHealth = playerHealthRef;
        player = playerHealthRef.transform;
        coins = coinCount;
        gems = gemCount;
    }

    void Awake()
    {
        cam = Camera.main;
        label.gameObject.SetActive(false);
    }

    void Update()
    {
        if (opened)
        {
            // Swing the lid open over a moment, then vanish.
            openedTime += Time.deltaTime;
            float t = Mathf.Clamp01(openedTime / 0.4f);
            lidHinge.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -110f, t), 0f, 0f);
            if (openedTime > vanishDelay)
                Destroy(gameObject);
            return;
        }

        bool near = player != null && !playerHealth.IsDead && Time.timeScale > 0f && FlatDistance(player.position, transform.position) < interactRange;
        label.gameObject.SetActive(near);

        if (near && IsInteractPressed())
            Open();
    }

    void LateUpdate()
    {
        // Keep the label facing the camera.
        if (label.gameObject.activeSelf)
            label.transform.rotation = cam.transform.rotation;
    }

    void Open()
    {
        opened = true;
        label.gameObject.SetActive(false);

        for (int i = 0; i < coins; i++)
            Drop(coinPrefab.gameObject);
        for (int i = 0; i < gems; i++)
            Drop(gemPrefab.gameObject);
    }

    void Drop(GameObject prefab)
    {
        Vector2 offset = Random.insideUnitCircle * scatterRadius;
        Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);
        position.y = prefab.transform.position.y;
        Instantiate(prefab, position, Quaternion.identity);
    }

    static bool IsInteractPressed()
    {
        bool key = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool pad = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        return key || pad;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
