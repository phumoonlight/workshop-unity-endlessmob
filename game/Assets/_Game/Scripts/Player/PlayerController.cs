using UnityEngine;
using UnityEngine.InputSystem;

// Moves the player with WASD / arrow keys / gamepad left stick.
// The input comes from the "Move" action in Assets/InputSystem_Actions.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Tooltip("How fast the player walks, in meters per second.")]
    [SerializeField] float moveSpeed = 6f;

    [Tooltip("How fast the player turns to face where they walk, in degrees per second.")]
    [SerializeField] float turnSpeed = 720f;

    CharacterController controller;
    InputAction moveAction;
    PlayerStamina stamina; // optional: makes us faster while sprinting
    float speedMultiplier = 1f; // raised by upgrades

    // True while the player is pushing a direction. Stamina uses it (no sprinting in place).
    public bool IsMoving { get; private set; }

    public void AddMoveSpeedPercent(float percent)
    {
        speedMultiplier += percent / 100f;
    }

    // Awake runs once, when the object is created.
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        moveAction = InputSystem.actions.FindAction("Player/Move");
        stamina = GetComponent<PlayerStamina>();
    }

    // When disabled (e.g. while dead) we're not moving.
    void OnDisable() => IsMoving = false;

    // Update runs once every frame.
    void Update()
    {
        // Input is a 2D value: x = left/right, y = up/down.
        Vector2 input = moveAction.ReadValue<Vector2>();

        // Turn it into a 3D direction on the ground (x = left/right, z = forward/back).
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        if (direction.sqrMagnitude > 1f)
            direction.Normalize(); // stops diagonal movement being faster

        IsMoving = direction.sqrMagnitude > 0.01f;
        float sprint = stamina != null ? stamina.SpeedMultiplier : 1f;

        // SimpleMove moves us, handles collisions and applies gravity.
        controller.SimpleMove(direction * moveSpeed * speedMultiplier * sprint);

        // Smoothly rotate to face the direction we're moving.
        if (IsMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}
