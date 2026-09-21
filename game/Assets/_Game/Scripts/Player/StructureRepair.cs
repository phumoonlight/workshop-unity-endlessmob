using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Hold F (or the gamepad's left face button) next to a damaged structure
// (the castle, a building, or a ruin) to repair it.
public class StructureRepair : MonoBehaviour
{
    [SerializeField] PlayerHealth health;

    [Tooltip("Text that tells the player they can repair. Hidden when nothing is in reach.")]
    [SerializeField] TMP_Text prompt;

    [Tooltip("How close to the wall (in meters) you must stand.")]
    [SerializeField] float repairRange = 2.5f;

    [Tooltip("HP restored per second while holding the button.")]
    [SerializeField] float repairPerSecond = 40f;

    public bool IsRepairing { get; private set; }

    void Update()
    {
        IsRepairing = false;

        Structure target = null;
        if (!health.IsDead && !GameStats.IsGameOver && Time.timeScale > 0f)
            target = FindRepairTarget();

        if (target != null && !target.IsRepairLocked && IsHoldingRepair())
        {
            target.Repair(repairPerSecond * Time.deltaTime);
            IsRepairing = true;
        }

        if (prompt == null)
            return;

        prompt.enabled = target != null;
        if (target != null)
        {
            string hp = $"{Mathf.CeilToInt(target.Health)} / {target.MaxHealth}";
            if (target.IsRepairLocked)
                prompt.text = $"<color=#ff7070>Under attack!</color> Repair {target.DisplayName} in {Mathf.CeilToInt(target.RepairLockTimeLeft)}s  <size=70%>({hp})</size>";
            else if (IsRepairing)
                prompt.text = $"Repairing {target.DisplayName}...  {hp}";
            else
                prompt.text = $"Hold <color=#ffd24a>F</color> to repair {target.DisplayName}  <size=70%>({hp})</size>";
        }
    }

    // The closest structure in reach that needs repair, or null.
    Structure FindRepairTarget()
    {
        Structure best = null;
        float bestDistance = repairRange;
        foreach (Structure s in Structure.All)
        {
            if (!s.CanBeRepaired)
                continue;

            Vector3 toWall = s.ClosestPoint(transform.position) - transform.position;
            toWall.y = 0f;
            float d = toWall.magnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = s;
            }
        }
        return best;
    }

    static bool IsHoldingRepair()
    {
        bool key = Keyboard.current != null && Keyboard.current.fKey.isPressed;
        bool pad = Gamepad.current != null && Gamepad.current.buttonWest.isPressed;
        return key || pad;
    }
}
