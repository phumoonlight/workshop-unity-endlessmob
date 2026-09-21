using UnityEngine;

// Shows the right character model for the hero you picked, and keeps its
// stand/walk/run animation in step with how fast the player is really moving.
//
// The models are only what you see. All the gameplay (moving, attacking, being
// hit) still runs on the Player object itself, exactly as before.
[RequireComponent(typeof(CharacterController))]
public class PlayerModel : MonoBehaviour
{
    [Tooltip("The character shown for the Swordsman.")]
    [SerializeField] GameObject swordModel;

    [Tooltip("The character shown for the Archer.")]
    [SerializeField] GameObject bowModel;

    [Tooltip("How quickly the animation eases between standing, walking and running.")]
    [SerializeField] float blendSmoothing = 0.12f;

    [Header("Attacks")]
    [Tooltip("Seconds a swing stays at full strength before easing back into walking.")]
    [SerializeField] float swingTime = 0.45f;

    [Tooltip("How fast a swing blends in and out on top of the walk/run.")]
    [SerializeField] float swingBlendSpeed = 8f;

    // The attack animations live on layer 1 of the animator controller, which
    // is masked to the upper body only, so the legs keep walking underneath.
    const int AttackLayer = 1;

    CharacterController controller;
    Animator active;
    float swingLeft;   // seconds of swing still to play
    float swingWeight; // 0 = just walking, 1 = full swing

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        HideAll(); // nothing is shown until a class is chosen
    }

    public void HideAll()
    {
        swordModel.SetActive(false);
        bowModel.SetActive(false);
        active = null;
        swingLeft = 0f;
        swingWeight = 0f;
    }

    // Called by a weapon each time it attacks. "step" is which strike of the
    // combo it was (0, 1, 2), so the character swings a different way each time.
    public void Attack(int step)
    {
        if (active == null)
            return;

        active.SetInteger("AttackStep", Mathf.Clamp(step, 0, 2));
        active.SetTrigger("Attack");
        swingLeft = swingTime;
    }

    // Called by PlayerClassSelector once a hero is picked.
    public void Show(bool isSword)
    {
        HideAll();

        GameObject model = isSword ? swordModel : bowModel;
        model.SetActive(true);
        active = model.GetComponent<Animator>();
    }

    void Update()
    {
        if (active == null)
            return;

        // Ask the CharacterController how fast we actually moved, rather than
        // what we asked for: that already accounts for sprinting and for walking
        // into a wall, so the feet never slide.
        Vector3 velocity = controller.velocity;
        velocity.y = 0f; // ignore falling

        // The last two arguments ease the value instead of snapping it, so the
        // character eases from standing into a walk rather than popping.
        active.SetFloat("Speed", velocity.magnitude, blendSmoothing, Time.deltaTime);

        // Fade the attack layer in while swinging and back out afterwards.
        // Leaving it switched on would freeze the arms mid-swing between attacks.
        swingLeft -= Time.deltaTime;
        float target = swingLeft > 0f ? 1f : 0f;
        swingWeight = Mathf.MoveTowards(swingWeight, target, swingBlendSpeed * Time.deltaTime);
        active.SetLayerWeight(AttackLayer, swingWeight);
    }
}
