
using UnityEngine;

public class WeaponMotion : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera playerCamera;

    [Header("Weapon Weight & Inertia")]
    public float positionSmoothness = 10f;
    public float rotationSmoothness = 8f;
    public float swayAmount = 1.8f;
    public float maxSwayAngle = 4f;
    public float inertiaAmount = 0.035f;
    public float strafeTilt = 2f;

    [Header("Walking")]
    public float walkBobFrequency = 6.5f;
    public float walkBobAmount = 0.018f;
    public float walkSideAmount = 0.012f;

    [Header("Sprinting")]
    public float sprintBobFrequency = 9f;
    public float sprintBobAmount = 0.04f;
    public float sprintSideAmount = 0.022f;
    public float sprintDrop = 0.055f;
    public float sprintTilt = 5f;

    [Header("Idle Breathing")]
    public float breathingAmount = 0.004f;
    public float breathingFrequency = 1.1f;
    public float idleSwayAmount = 0.003f;

    [Header("Movement Transitions")]
    public float accelerationDip = 0.025f;
    public float stopDip = 0.035f;
    public float transitionSpeed = 7f;

    [Header("Landing")]
    public float landingDip = 0.06f;
    public float landingRecovery = 9f;

    [Header("Recoil")]
    public float recoilRecovery = 14f;

    private CharacterController controller;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Vector3 currentRecoil;
    private Vector3 targetRecoil;

    private Vector3 currentPositionOffset;
    private Vector3 positionVelocity;

    private Vector3 currentRotationOffset;
    private Vector3 rotationVelocity;

    private float bobTimer;
    private float breathingTimer;
    private float idleTimer;

    private float movementWeight;
    private float movementWeightVelocity;
    private float sprintWeight;
    private float sprintWeightVelocity;

    private float landingOffset;
    private float previousSpeed;
    private bool wasGrounded;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        if (player != null)
            controller = player.GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (controller != null)
            wasGrounded = controller.isGrounded;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        if (dt <= 0f || playerCamera == null)
            return;

        // Mouse input creates physical-looking weapon lag.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Vector3 velocity = controller != null
            ? controller.velocity
            : Vector3.zero;

        float horizontalSpeed = new Vector2(
            velocity.x, velocity.z).magnitude;

        bool grounded = controller == null || controller.isGrounded;
        bool moving = grounded && horizontalSpeed > 0.15f;
        bool sprinting = moving && Input.GetKey(KeyCode.LeftShift);

        float speedFactor = Mathf.Clamp01(horizontalSpeed / 4f);

        // Smooth transitions prevent sudden changes in animation.
        movementWeight = Mathf.SmoothDamp(
            movementWeight,
            moving ? 1f : 0f,
            ref movementWeightVelocity,
            1f / transitionSpeed,
            Mathf.Infinity,
            dt);

        sprintWeight = Mathf.SmoothDamp(
            sprintWeight,
            sprinting ? 1f : 0f,
            ref sprintWeightVelocity,
            0.22f,
            Mathf.Infinity,
            dt);

        // Weapon lags opposite to the direction of mouse movement.
        Vector3 sway = new Vector3(
            -mouseY * swayAmount,
            mouseX * swayAmount,
            -mouseX * swayAmount
        );

        sway.x = Mathf.Clamp(sway.x, -maxSwayAngle, maxSwayAngle);
        sway.y = Mathf.Clamp(sway.y, -maxSwayAngle, maxSwayAngle);
        sway.z = Mathf.Clamp(sway.z, -maxSwayAngle, maxSwayAngle);

        // Subtle follow-through from the weapon's physical weight.
        Vector3 inertiaPosition = new Vector3(
            -mouseX * inertiaAmount,
            -mouseY * inertiaAmount,
            0f
        );

        // Walking and sprinting have different movement rhythms.
        float frequency = Mathf.Lerp(
            walkBobFrequency, sprintBobFrequency, sprintWeight);

        float bobAmount = Mathf.Lerp(
            walkBobAmount, sprintBobAmount, sprintWeight);

        float sideAmount = Mathf.Lerp(
            walkSideAmount, sprintSideAmount, sprintWeight);

        bobTimer += dt * frequency * Mathf.Lerp(0.3f, 1f, speedFactor);

        Vector3 bob = Vector3.zero;

        if (moving)
        {
            bob.x = Mathf.Cos(bobTimer * 0.5f) * sideAmount;
            bob.y = Mathf.Sin(bobTimer) * bobAmount;

            // A little forward/back motion gives footsteps more weight.
            bob.z = Mathf.Cos(bobTimer) * bobAmount * 0.35f;
        }

        // Breathing continues even while standing still.
        breathingTimer += dt * breathingFrequency;

        Vector3 breathing = new Vector3(
            Mathf.Sin(breathingTimer * 0.5f) * breathingAmount * 0.5f,
            Mathf.Sin(breathingTimer) * breathingAmount,
            Mathf.Cos(breathingTimer) * breathingAmount * 0.25f
        );

        // Small irregular idle movement: avoid a perfectly static weapon.
        idleTimer += dt;

        Vector3 idleSway = new Vector3(
            Mathf.Sin(idleTimer * 0.73f) * idleSwayAmount,
            Mathf.Sin(idleTimer * 1.13f) * idleSwayAmount,
            Mathf.Sin(idleTimer * 0.51f) * idleSwayAmount * 0.5f
        );

        // Sprinting lowers the weapon and rotates it slightly.
        Vector3 sprintPosition = new Vector3(
            0f, -sprintDrop * sprintWeight, 0f);

        Vector3 sprintRotation = new Vector3(
            sprintTilt * sprintWeight,
            0f,
            -sprintTilt * sprintWeight);

        // Lean slightly in the direction of lateral movement.
        float localStrafe = player != null
            ? player.InverseTransformDirection(velocity).x
            : 0f;

        Vector3 movementRotation = new Vector3(
            0f,
            0f,
            -localStrafe * strafeTilt * 0.25f
        );

        // Dip slightly when accelerating or coming to a stop.
        float acceleration = (horizontalSpeed - previousSpeed) /
                             Mathf.Max(dt, 0.001f);

        float accelerationEffect = Mathf.Clamp(
            -acceleration * accelerationDip, -0.025f, 0.025f);

        previousSpeed = horizontalSpeed;

        Vector3 transitionPosition = new Vector3(
            0f,
            accelerationEffect * movementWeight,
            0f);

        // Detect landing and smoothly recover from the impact.
        if (controller != null)
        {
            if (!wasGrounded && grounded)
                landingOffset = -landingDip;

            wasGrounded = grounded;
        }

        landingOffset = Mathf.MoveTowards(
            landingOffset, 0f, landingRecovery * dt);

        Vector3 landingPosition = new Vector3(
            0f, landingOffset, 0f);

        // Recoil smoothly returns to the weapon's resting pose.
        currentRecoil = Vector3.Lerp(
            currentRecoil, targetRecoil,
            1f - Mathf.Exp(-recoilRecovery * dt));

        targetRecoil = Vector3.Lerp(
            targetRecoil, Vector3.zero,
            1f - Mathf.Exp(-recoilRecovery * dt));

        // Combine position effects.
        Vector3 targetPosition =
            initialPosition +
            bob +
            breathing * (1f - sprintWeight * 0.5f) +
            idleSway * (1f - movementWeight * 0.7f) +
            inertiaPosition +
            sprintPosition +
            transitionPosition +
            landingPosition;

        // Combine rotation effects.
        Quaternion targetRotation =
            initialRotation *
            Quaternion.Euler(
                sway +
                sprintRotation +
                movementRotation +
                currentRecoil
            );

        // SmoothDamp provides more natural settling than constant Lerp.
        Vector3 positionDifference = targetPosition - transform.localPosition;

        transform.localPosition = Vector3.SmoothDamp(
            transform.localPosition,
            targetPosition,
            ref positionVelocity,
            1f / Mathf.Max(positionSmoothness, 0.01f),
            Mathf.Infinity,
            dt);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            1f - Mathf.Exp(-rotationSmoothness * dt));
    }

    // Call this from your weapon firing script.
    public void AddRecoil(float kickX, float kickY)
    {
        targetRecoil += new Vector3(
            -kickX,
            Random.Range(-kickY, kickY),
            Random.Range(-kickX, kickX) * 0.15f
        );
    }

    public void ResetWeaponPosition()
    {
        currentRecoil = Vector3.zero;
        targetRecoil = Vector3.zero;
        currentPositionOffset = Vector3.zero;
        currentRotationOffset = Vector3.zero;
        positionVelocity = Vector3.zero;
        rotationVelocity = Vector3.zero;
        landingOffset = 0f;

        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
    }
}
