using UnityEngine;
using UnityEngine.InputSystem;

// Keeps the camera at a fixed angle above the target and smoothly follows it.
// Mouse wheel (or + / - keys) zooms in and out.
public class CameraFollow : MonoBehaviour
{
    [Tooltip("What the camera follows (the player).")]
    [SerializeField] Transform target;

    [Tooltip("Where the camera sits relative to the target at normal zoom. Bigger Y = higher up, more negative Z = further back.")]
    [SerializeField] Vector3 offset = new Vector3(0f, 14f, -9f);

    [Tooltip("How long the camera takes to catch up. 0 = instantly.")]
    [SerializeField] float smoothTime = 0.15f;

    [Header("Zoom")]
    [Tooltip("Closest zoom, as a multiple of the offset. 0.5 = half as far.")]
    [SerializeField] float minZoom = 0.5f;

    [Tooltip("Farthest zoom, as a multiple of the offset. 2 = twice as far.")]
    [SerializeField] float maxZoom = 2.2f;

    [Tooltip("How much one mouse wheel notch zooms.")]
    [SerializeField] float zoomStep = 0.1f;

    [Tooltip("How much holding + or - zooms per second.")]
    [SerializeField] float keyZoomSpeed = 1f;

    [Tooltip("How quickly the zoom eases toward its new value.")]
    [SerializeField] float zoomSmoothing = 10f;

    [Header("Close-up view")]
    [Tooltip("Where the camera sits at full zoom-in. Low Y and a short Z give the near eye-level, over-the-shoulder look instead of the usual bird's eye.")]
    [SerializeField] Vector3 closeUpOffset = new Vector3(0f, 2.2f, -4.5f);

    [Tooltip("How high above the player's feet the close-up camera aims, in metres. Around chest height keeps the whole character in frame.")]
    [SerializeField] float closeUpLookHeight = 1.2f;

    Vector3 velocity;       // used internally by SmoothDamp
    float targetZoom = 1f;  // where the zoom is heading
    float zoom = 1f;        // current zoom, eases toward targetZoom

    // Where the camera would be with no shake. The shake is added on top, and
    // the follow keeps working from this clean position -- otherwise the camera
    // would chase its own shaking and the two would fight.
    Vector3 basePosition;

    static CameraFollow instance;
    float shakeStrength;
    float shakeTimeLeft;
    float shakeDuration;
    float shakeSeed;        // so two shakes in a row don't wobble identically

    [Tooltip("How fast the shake wobbles. Higher = a sharper rattle, lower = a slow sway.")]
    [SerializeField] float shakeFrequency = 22f;

    // Anywhere in the game: CameraFollow.Shake(0.3f, 0.2f)
    public static void Shake(float strength, float duration)
    {
        if (instance == null || strength <= 0f || duration <= 0f)
            return;

        // A bigger shake overrides a smaller one still fading out, instead of
        // the two adding up into something sickening.
        if (strength < instance.shakeStrength && instance.shakeTimeLeft > 0f)
            return;

        instance.shakeStrength = strength;
        instance.shakeDuration = duration;
        instance.shakeTimeLeft = duration;
        instance.shakeSeed = Random.Range(0f, 100f);
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Point the camera back along the offset, so it looks down at the target.
        transform.rotation = Quaternion.LookRotation(-offset);
        if (target != null)
            transform.position = target.position + CurrentOffset();
        basePosition = transform.position;
    }

    void Update()
    {
        ReadZoomInput();

        // Ease toward the target zoom. unscaledDeltaTime so zoom works even while paused.
        zoom = Mathf.Lerp(zoom, targetZoom, 1f - Mathf.Exp(-zoomSmoothing * Time.unscaledDeltaTime));
    }

    void ReadZoomInput()
    {
        // Mouse wheel: scroll up = zoom in. One notch is usually 120 on Windows, so we
        // only use the sign (+1 / -1) to behave the same on every mouse.
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0f)
                targetZoom -= Mathf.Sign(scroll) * zoomStep;
        }

        // + / - keys (main keyboard or number pad).
        if (Keyboard.current != null)
        {
            bool zoomIn = Keyboard.current.equalsKey.isPressed || Keyboard.current.numpadPlusKey.isPressed;
            bool zoomOut = Keyboard.current.minusKey.isPressed || Keyboard.current.numpadMinusKey.isPressed;
            if (zoomIn) targetZoom -= keyZoomSpeed * Time.unscaledDeltaTime;
            if (zoomOut) targetZoom += keyZoomSpeed * Time.unscaledDeltaTime;
        }

        targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
    }

    // How far we are into the close-up look: 0 at normal zoom (1) or zoomed out,
    // 1 when fully zoomed in. Everything about the close-up fades in with this.
    float CloseUpBlend()
    {
        return Mathf.InverseLerp(1f, minZoom, zoom);
    }

    // Where the camera wants to be, relative to the player.
    Vector3 CurrentOffset()
    {
        // Zooming out past 1 just pushes the normal bird's eye offset further away.
        Vector3 wide = offset * Mathf.Max(zoom, 1f);
        // Zooming in below 1 swings down toward the low close-up position instead.
        return Vector3.Lerp(wide, closeUpOffset, CloseUpBlend());
    }

    // The point the camera aims at: the player's feet normally, chest height up close.
    Vector3 CurrentFocus()
    {
        return target.position + Vector3.up * (closeUpLookHeight * CloseUpBlend());
    }

    // How far the camera is thrown off course by the current shake.
    Vector3 ShakeOffset()
    {
        if (shakeTimeLeft <= 0f)
            return Vector3.zero;

        // Unscaled time, so the shake keeps moving during a hit-stop freeze.
        // A shake that froze with the game would look like a stuck frame.
        shakeTimeLeft -= Time.unscaledDeltaTime;
        float left = Mathf.Clamp01(shakeTimeLeft / shakeDuration);
        float amount = shakeStrength * left * left; // fades out fast, then trails off

        // Perlin noise instead of Random: it wanders smoothly, so the camera
        // swims rather than teleporting to a new spot every frame.
        float time = Time.unscaledTime * shakeFrequency;
        float x = Mathf.PerlinNoise(shakeSeed, time) * 2f - 1f;
        float y = Mathf.PerlinNoise(shakeSeed + 37f, time) * 2f - 1f;

        // Shake across the screen, not through the world, so it always reads
        // the same however the camera is angled.
        return (transform.right * x + transform.up * y) * amount;
    }

    // LateUpdate runs after every Update, so the player has already moved this frame.
    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + CurrentOffset();
        basePosition = Vector3.SmoothDamp(basePosition, desiredPosition, ref velocity, smoothTime);
        transform.position = basePosition + ShakeOffset();

        // The close-up is a much flatter angle than the bird's eye, so the camera has to
        // re-aim every frame. Looking at the focus point keeps the hero framed all the way through.
        Vector3 toFocus = CurrentFocus() - transform.position;
        if (toFocus.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toFocus);
    }
}
