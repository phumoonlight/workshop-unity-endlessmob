using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Stand next to a barrack and press H to hire a soldier.
public class BarrackInteraction : MonoBehaviour
{
    [SerializeField] PlayerWallet wallet;
    [SerializeField] PlayerHealth health;
    [SerializeField] TMP_Text prompt;

    [Tooltip("How close to the barrack wall you must stand.")]
    [SerializeField] float range = 3f;

    string feedback;
    float feedbackTimer;

    void Update()
    {
        feedbackTimer -= Time.deltaTime;

        Barrack barrack = null;
        Building building = null;
        if (!health.IsDead && Time.timeScale > 0f && !BuildManager.IsActive)
            barrack = FindNearbyBarrack(out building);

        prompt.enabled = barrack != null;
        if (barrack == null)
            return;

        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            feedback = building.IsRuined ? "The barrack is in ruins. Repair it first (hold F)"
                                         : barrack.TryHire(wallet) ?? "Soldier hired!";
            feedbackTimer = 2f;
        }

        string line = building.IsRuined
            ? "<color=#ff7070>Barrack in ruins</color>"
            : $"Press <color=#ffd24a>H</color> to hire a soldier  (<color=#ffd24a>{barrack.SoldierCost}c</color>)  {barrack.SoldierCount} / {barrack.MaxSoldiers}";
        prompt.text = feedbackTimer > 0f ? $"{line}\n<size=70%><color=#ffd24a>{feedback}</color></size>" : line;
    }

    Barrack FindNearbyBarrack(out Building building)
    {
        building = null;
        foreach (Structure s in Structure.All)
        {
            if (!s.TryGetComponent(out Barrack barrack))
                continue;

            Vector3 toWall = s.ClosestPoint(transform.position) - transform.position;
            toWall.y = 0f;
            if (toWall.sqrMagnitude < range * range)
            {
                building = (Building)s;
                return barrack;
            }
        }
        return null;
    }
}
