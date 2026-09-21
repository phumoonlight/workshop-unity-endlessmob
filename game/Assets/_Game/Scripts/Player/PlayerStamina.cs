using UnityEngine;
using UnityEngine.InputSystem;

// Hold Sprint (Shift / gamepad left stick click) while moving to run faster.
// Sprinting drains stamina; it refills after a short rest. Running it dry makes
// you "exhausted" until it has refilled a bit.
[RequireComponent(typeof(PlayerController))]
public class PlayerStamina : MonoBehaviour
{
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float drainPerSecond = 25f;
    [SerializeField] float regenPerSecond = 20f;

    [Tooltip("Seconds after you stop sprinting before stamina starts refilling.")]
    [SerializeField] float regenDelay = 0.8f;

    [Tooltip("Speed while sprinting, as a multiple of normal speed.")]
    [SerializeField] float sprintSpeedMultiplier = 1.6f;

    [Tooltip("After running out, you can sprint again once stamina is back to this fraction. 0.3 = 30%.")]
    [Range(0f, 1f)]
    [SerializeField] float recoverFraction = 0.3f;

    public float Stamina { get; private set; }
    public float MaxStamina => maxStamina;
    public bool IsSprinting { get; private set; }
    public bool IsExhausted { get; private set; }

    // PlayerController multiplies its speed by this.
    public float SpeedMultiplier => IsSprinting ? sprintSpeedMultiplier : 1f;

    PlayerController movement;
    InputAction sprintAction;
    float regenTimer;

    void Awake()
    {
        Stamina = maxStamina;
        movement = GetComponent<PlayerController>();
        sprintAction = InputSystem.actions.FindAction("Player/Sprint"); // Shift + left stick click
    }

    void Update()
    {
        bool wantsToSprint = IsSprintHeld() && movement.enabled && movement.IsMoving && !IsExhausted;

        if (wantsToSprint)
        {
            IsSprinting = true;
            Stamina -= drainPerSecond * Time.deltaTime;
            regenTimer = regenDelay;

            if (Stamina <= 0f)
            {
                Stamina = 0f;
                IsExhausted = true;
                IsSprinting = false;
            }
        }
        else
        {
            IsSprinting = false;
            regenTimer -= Time.deltaTime;
            if (regenTimer <= 0f)
                Stamina = Mathf.Min(maxStamina, Stamina + regenPerSecond * Time.deltaTime);

            if (IsExhausted && Stamina >= maxStamina * recoverFraction)
                IsExhausted = false;
        }
    }

    bool IsSprintHeld()
    {
        if (sprintAction != null)
            return sprintAction.IsPressed();
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed; // fallback
    }
}
