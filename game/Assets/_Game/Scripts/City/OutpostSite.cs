using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// The empty foundation left by a cleared fortified camp. Stand on it and press
// a number key to spend coins and build an outpost there.
public class OutpostSite : MonoBehaviour
{
    [Tooltip("Outposts offered here, in key order: first = [1], second = [2]...")]
    [SerializeField] BuildingData[] outposts;

    [Tooltip("Floating text with the choices, shown when the player is close.")]
    [SerializeField] TextMeshPro label;

    [SerializeField] float radius = 5f;
    [SerializeField] float interactRange = 7f;

    public float Radius => radius;

    PlayerWallet wallet;
    PlayerHealth player;
    Camera cam;
    string feedback;
    float feedbackTimer;

    void Awake()
    {
        // Sites are created during play, so find the player's parts here.
        player = FindAnyObjectByType<PlayerHealth>();
        wallet = player.GetComponent<PlayerWallet>();
        cam = Camera.main;
        label.gameObject.SetActive(false);
    }

    void Update()
    {
        feedbackTimer -= Time.deltaTime;

        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0f;
        bool near = !player.IsDead && Time.timeScale > 0f && !BuildManager.IsActive
                    && offset.sqrMagnitude < interactRange * interactRange;

        label.gameObject.SetActive(near);
        if (!near)
            return;

        var keyboard = Keyboard.current;
        if (keyboard != null)
            for (int i = 0; i < outposts.Length && i < 9; i++)
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                    TryBuild(outposts[i]);

        label.text = BuildText();
    }

    void LateUpdate()
    {
        if (label.gameObject.activeSelf)
            label.transform.rotation = cam.transform.rotation; // face the camera
    }

    void TryBuild(BuildingData outpost)
    {
        if (!wallet.TrySpend(outpost.cost))
        {
            feedback = "Not enough coins";
            feedbackTimer = 2f;
            return;
        }

        Instantiate(outpost.prefab, transform.position, transform.rotation, transform.parent);
        Destroy(gameObject);
    }

    string BuildText()
    {
        var text = new StringBuilder("BUILD AN OUTPOST\n");
        for (int i = 0; i < outposts.Length; i++)
        {
            BuildingData o = outposts[i];
            string color = wallet.Coins >= o.cost ? "#ffffff" : "#909090";
            text.Append($"<size=75%><color={color}>[{i + 1}] {o.displayName}  {o.cost}c</color>\n");
            text.Append($"<size=60%><color=#c0c0c0>{o.description}</color></size></size>\n");
        }
        if (feedbackTimer > 0f)
            text.Append($"<size=70%><color=#ff7070>{feedback}</color></size>");
        return text.ToString();
    }
}
