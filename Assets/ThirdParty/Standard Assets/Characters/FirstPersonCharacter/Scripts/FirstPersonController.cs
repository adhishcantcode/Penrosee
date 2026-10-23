using System;
using UnityEngine;
using UnityStandardAssets.Utility;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private bool m_IsWalking = true;
        [SerializeField] private float m_WalkSpeed = 5f;
        [SerializeField] private float m_RunSpeed = 10f;
        [SerializeField] private float m_JumpSpeed = 7f;
        [SerializeField] private float m_StickToGroundForce = 10f;
        [SerializeField] private float m_GravityMultiplier = 2f;

        [Header("Mobile Controls")]
        [SerializeField] private bool m_UseMobileControls;
        [SerializeField] private float m_MobileLookSensitivity = 0.12f;
        [SerializeField, Range(0f, 1f)]
        private float m_MobileSprintThreshold = 0.85f;

        [Header("Mouse Look")]
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick = true;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();

        [Header("Head Bob")]
        [SerializeField]
        private CurveControlledBob m_HeadBob =
            new CurveControlledBob();
        [SerializeField]
        private LerpControlledBob m_JumpBob =
            new LerpControlledBob();
        [SerializeField] private float m_StepInterval = 5f;
        [SerializeField] private float m_RunstepLenghten = 0.7f;

        [Header("Audio")]
        [SerializeField] private AudioClip[] m_FootstepSounds;
        [SerializeField] private AudioClip m_JumpSound;
        [SerializeField] private AudioClip m_LandSound;

        private Camera m_Camera;
        private CharacterController m_CharacterController;
        private AudioSource m_AudioSource;

        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;

        private bool m_Jump;
        private bool m_PreviouslyGrounded;
        private bool m_Jumping;

        private float m_StepCycle;
        private float m_NextStep;
        private float m_YRotation;

        private Vector2 m_MobileMoveInput;
        private Vector2 m_MobileLookInput;

        private bool m_MobileJumpRequested;
        private bool m_MobileSprinting;
        private bool m_WasWalking;

        private bool m_CursorIsLocked = true;

        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_AudioSource = GetComponent<AudioSource>();

            if (m_Camera == null)
            {
                Debug.LogError(
                    "FirstPersonController: No camera tagged MainCamera found."
                );
                enabled = false;
                return;
            }

            m_MouseLook.Init(transform, m_Camera.transform);
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);

            m_YRotation = transform.eulerAngles.y;
            m_StepCycle = 0f;
            m_NextStep = m_CharacterController.height * 0.5f;
        }

        private void Update()
        {
            if (m_UseMobileControls)
            {
                UpdateMobileLook();

                if (m_MobileJumpRequested)
                {
                    m_Jump = true;
                    m_MobileJumpRequested = false;
                }
            }
            else
            {
                RotateView();

                if (Input.GetButtonDown("Jump") && !m_Jump)
                    m_Jump = true;
            }

            if (!m_PreviouslyGrounded &&
                m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();

                m_MoveDir.y = 0f;
                m_Jumping = false;
            }

            if (!m_CharacterController.isGrounded &&
                !m_Jumping &&
                m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }

        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);

            Vector3 desiredMove =
                transform.forward * m_Input.y +
                transform.right * m_Input.x;

            RaycastHit hitInfo;

            bool foundGround = Physics.SphereCast(
                transform.position,
                m_CharacterController.radius,
                Vector3.down,
                out hitInfo,
                m_CharacterController.height / 2f,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            if (foundGround)
            {
                desiredMove = Vector3.ProjectOnPlane(
                    desiredMove,
                    hitInfo.normal
                ).normalized;
            }

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;

            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();

                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity *
                             m_GravityMultiplier *
                             Time.fixedDeltaTime;
            }

            m_CharacterController.Move(
                m_MoveDir * Time.fixedDeltaTime
            );

            ProgressStepCycle();
            UpdateCameraPosition();

            if (!m_UseMobileControls)
                m_MouseLook.UpdateCursorLock();
        }

        private void GetInput(out float speed)
        {
            if (m_UseMobileControls)
            {
                m_Input = m_MobileMoveInput;

                // Gradual acceleration as the joystick is pushed forward.
                float forwardAmount = Mathf.Clamp01(m_MobileMoveInput.y);

                speed = Mathf.Lerp(
                    m_WalkSpeed,
                    m_RunSpeed,
                    forwardAmount
                );

                m_MobileSprinting =
                    m_MobileMoveInput.y >= m_MobileSprintThreshold;

                m_IsWalking = !m_MobileSprinting;
            }
            else
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");

                m_Input = new Vector2(horizontal, vertical);

                if (m_Input.sqrMagnitude > 1f)
                    m_Input.Normalize();

                m_IsWalking = !Input.GetKey(KeyCode.LeftShift);

                speed = m_IsWalking
                    ? m_WalkSpeed
                    : m_RunSpeed;
            }

            if (m_Input.sqrMagnitude > 1f)
                m_Input.Normalize();

            if (m_IsWalking != m_WasWalking && m_UseFovKick)
            {
                StopAllCoroutines();

                if (m_IsWalking)
                    StartCoroutine(m_FovKick.FOVKickUp());
                else
                    StartCoroutine(m_FovKick.FOVKickDown());

                m_WasWalking = m_IsWalking;
            }
        }

        private void UpdateMobileLook()
        {
            if (m_MobileLookInput.sqrMagnitude <= 0f)
                return;

            float mouseX =
                m_MobileLookInput.x * m_MobileLookSensitivity;

            float mouseY =
                m_MobileLookInput.y * m_MobileLookSensitivity;

            m_YRotation += mouseX;

            float xRotation =
                m_Camera.transform.localEulerAngles.x - mouseY;

            if (xRotation > 180f)
                xRotation -= 360f;

            xRotation = Mathf.Clamp(xRotation, -80f, 80f);

            transform.localRotation =
                Quaternion.Euler(0f, m_YRotation, 0f);

            m_Camera.transform.localRotation =
                Quaternion.Euler(xRotation, 0f, 0f);

            m_MobileLookInput = Vector2.zero;
        }

        private void RotateView()
        {
            m_MouseLook.LookRotation(transform, m_Camera.transform);
        }

        private void ProgressStepCycle()
        {
            if (!m_CharacterController.isGrounded ||
                m_Input.sqrMagnitude <= 0f)
                return;

            float stepLength =
                m_CharacterController.velocity.magnitude +
                (m_IsWalking ? 0f : m_RunstepLenghten);

            m_StepCycle += stepLength * Time.fixedDeltaTime;

            if (m_StepCycle > m_NextStep)
            {
                m_NextStep = m_StepCycle + m_StepInterval;
                PlayFootStepAudio();
            }
        }

        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded ||
                m_FootstepSounds == null ||
                m_FootstepSounds.Length == 0)
                return;

            int index = UnityEngine.Random.Range(
                0,
                m_FootstepSounds.Length
            );

            AudioClip clip = m_FootstepSounds[index];

            if (clip == null)
                return;

            m_AudioSource.PlayOneShot(clip);

            m_FootstepSounds[index] = m_FootstepSounds[0];
            m_FootstepSounds[0] = clip;
        }

        private void PlayJumpSound()
        {
            if (m_JumpSound != null)
                m_AudioSource.PlayOneShot(m_JumpSound);
        }

        private void PlayLandingSound()
        {
            if (m_LandSound != null)
                m_AudioSource.PlayOneShot(m_LandSound);
        }

        private void UpdateCameraPosition()
        {
            if (m_CharacterController.velocity.magnitude > 0f &&
                m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(
                        m_CharacterController.velocity.magnitude +
                        (m_IsWalking ? 0f : m_RunstepLenghten)
                    );
            }
        }

        // Mobile joystick movement.
        public void SetMobileMoveInput(Vector2 input)
        {
            m_MobileMoveInput = Vector2.ClampMagnitude(input, 1f);

            m_MobileSprinting =
                m_MobileMoveInput.y >= m_MobileSprintThreshold;
        }

        // Mobile look-area drag.
        public void AddMobileLookInput(Vector2 delta)
        {
            if (m_UseMobileControls)
                m_MobileLookInput += delta;
        }

        // Connect the mobile Jump button to this method.
        public void MobileJump()
        {
            if (m_UseMobileControls)
                m_MobileJumpRequested = true;
        }

        // Kept for compatibility with MobileJoystick.
        public void SetMobileSprintAmount(float amount, bool sprinting)
        {
            m_MobileSprinting = sprinting;
        }

        public void SetMobileControls(bool enabled)
        {
            m_UseMobileControls = enabled;

            if (enabled)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void SetCursorLock(bool value)
        {
            m_CursorIsLocked = value;
        }

        public void ResetView()
        {
            if (m_Camera == null)
                return;

            m_MouseLook.Init(transform, m_Camera.transform);
            m_YRotation = transform.eulerAngles.y;
        }

        // Supports StaircaseLoopManager calls with one argument.
        public void TeleportTo(Vector3 position)
        {
            TeleportTo(position, transform.rotation);
        }

        // Supports calls that specify both position and rotation.
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            if (m_CharacterController == null)
                m_CharacterController = GetComponent<CharacterController>();

            m_CharacterController.enabled = false;

            transform.SetPositionAndRotation(position, rotation);

            m_MoveDir = Vector3.zero;
            m_Input = Vector2.zero;
            m_MobileMoveInput = Vector2.zero;
            m_MobileLookInput = Vector2.zero;

            m_Jump = false;
            m_Jumping = false;
            m_MobileJumpRequested = false;
            m_MobileSprinting = false;

            m_CharacterController.enabled = true;

            if (m_Camera != null)
            {
                m_MouseLook.Init(transform, m_Camera.transform);
                m_YRotation = transform.eulerAngles.y;
            }
        }
    }
}