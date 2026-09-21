using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Stand by the castle and hold E (gamepad: top face button) to spend coins on a
// one-time upgrade: double castle HP and 4 arrow towers on the corners.
public class CastleUpgrade : MonoBehaviour
{
    [SerializeField] Castle castle;
    [SerializeField] PlayerHealth player;
    [SerializeField] PlayerWallet wallet;
    [SerializeField] TMP_Text prompt;
    [SerializeField] LevelUpPopup messages;

    [SerializeField] int cost = 200;

    [Tooltip("Seconds you must hold the button.")]
    [SerializeField] float holdSeconds = 1.5f;

    [Tooltip("How close to the castle wall you must stand.")]
    [SerializeField] float range = 3f;

    [SerializeField] float healthMultiplier = 2f;

    [Tooltip("Arrow towers that switch on when upgraded.")]
    [SerializeField] GameObject[] towers;

    public bool IsUpgraded { get; private set; }

    float holdProgress; // 0 to 1

    void Awake()
    {
        foreach (GameObject tower in towers)
            tower.SetActive(false);
    }

    void Update()
    {
        bool inReach = !IsUpgraded && !player.IsDead && !castle.IsDestroyed
                       && Time.timeScale > 0f && IsNearCastle();

        prompt.enabled = inReach;
        if (!inReach)
        {
            holdProgress = 0f;
            return;
        }

        bool canAfford = wallet.Coins >= cost;
        if (canAfford && IsHolding())
        {
            holdProgress += Time.deltaTime / holdSeconds;
            if (holdProgress >= 1f)
            {
                Upgrade();
                return;
            }
        }
        else
        {
            holdProgress = 0f;
        }

        if (!canAfford)
            prompt.text = $"<color=#a0a0a0>Castle upgrade: {cost} coins  (you have {wallet.Coins})</color>";
        else if (holdProgress > 0f)
            prompt.text = $"Upgrading castle...  {Mathf.FloorToInt(holdProgress * 100f)}%";
        else
            prompt.text = $"Hold <color=#ffd24a>E</color> to upgrade the castle  (<color=#ffd24a>{cost} coins</color>)\n" +
                          "<size=70%>Double castle HP + 4 arrow towers</size>";
    }

    void Upgrade()
    {
        if (!wallet.TrySpend(cost))
            return;

        IsUpgraded = true;
        prompt.enabled = false;
        castle.MultiplyHealth(healthMultiplier);
        foreach (GameObject tower in towers)
            tower.SetActive(true);

        if (messages != null)
            messages.ShowMessage("<color=#80ff80>CASTLE UPGRADED!</color>", "Castle HP doubled, 4 arrow towers ready");
    }

    bool IsNearCastle()
    {
        Vector3 toWall = castle.ClosestPoint(player.transform.position) - player.transform.position;
        toWall.y = 0f;
        return toWall.sqrMagnitude < range * range;
    }

    static bool IsHolding()
    {
        bool key = Keyboard.current != null && Keyboard.current.eKey.isPressed;
        bool pad = Gamepad.current != null && Gamepad.current.buttonNorth.isPressed;
        return key || pad;
    }
}
