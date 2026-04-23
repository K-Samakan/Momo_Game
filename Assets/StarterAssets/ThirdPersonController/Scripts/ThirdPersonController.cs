using UnityEngine;
using System.Collections;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Sokoban Input Values")]
        public bool pushMode;
        public bool pushAction;

        [Header("Combat Settings")]
        [Tooltip("Attack cooldown in seconds")]
        public float attackCooldown = 1.0f;

        [Tooltip("Attack animation duration in seconds")]
        public float attackDuration = 0.8f;

        [Tooltip("Can player move while attacking")]
        public bool canMoveWhileAttacking = false;

        [Tooltip("Movement speed multiplier during attack")]
        [Range(0f, 1f)]
        public float attackMoveSpeedMultiplier = 0.3f;

        // Private combat variables
        private bool _isAttacking = false;
        private float _lastAttackTime = 0f;

        // =============== JUMP PAD VARIABLES ===============
        private bool _isLaunched = false;
        private float _launchedVelocity = 0f;

        public void OnPushMode(InputValue value)   => PushModeInput(value.isPressed);
        public void OnPushAction(InputValue value) => PushActionInput(value.isPressed);
        public void PushModeInput(bool v)  => pushMode  = v;
        public void PushActionInput(bool v) => pushAction = v;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        // ── Smart Attack (แทน PlayerAttackDetection เดิม) ──
        private SmartAttackSystem _smartAttack;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        [HideInInspector] public Animator _animator;
        [HideInInspector] public CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;
        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator  = TryGetComponent(out _animator);
            _controller   = GetComponent<CharacterController>();
            _input        = GetComponent<StarterAssetsInputs>();

            // ── ใช้ SmartAttackSystem แทน PlayerAttackDetection ──
            _smartAttack = GetComponent<SmartAttackSystem>();
            if (_smartAttack == null)
                Debug.LogWarning("[TPC] SmartAttackSystem not found on this GameObject!");

#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies.");
#endif

            AssignAnimationIDs();
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);
            JumpAndGravity();
            GroundedCheck();
            HandleAttack();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed       = Animator.StringToHash("Speed");
            _animIDGrounded    = Animator.StringToHash("Grounded");
            _animIDJump        = Animator.StringToHash("Jump");
            _animIDFreeFall    = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            // หมายเหตุ: IsAttacking ถูกจัดการโดย SmartAttackSystem ผ่าน Trigger แล้ว
            // ไม่ต้อง hash ที่นี่อีก
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - GroundedOffset,
                transform.position.z);

            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius,
                GroundLayers, QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
                _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void CameraRotation()
        {
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                _cinemachineTargetYaw   += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            _cinemachineTargetYaw   = ClampAngle(_cinemachineTargetYaw,   float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch,  BottomClamp,   TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (_isAttacking && !canMoveWhileAttacking)
            {
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, 0f);
                    _animator.SetFloat(_animIDMotionSpeed, 0f);
                }
                return;
            }

            float speedMultiplier = _isAttacking ? attackMoveSpeedMultiplier : 1f;
            float targetSpeed     = (_input.sprint ? SprintSpeed : MoveSpeed) * speedMultiplier;
            if (_isAttacking) targetSpeed = MoveSpeed * speedMultiplier;
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(
                _controller.velocity.x, 0f, _controller.velocity.z).magnitude;

            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - 0.1f ||
                currentHorizontalSpeed > targetSpeed + 0.1f)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed,
                    targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                                + _mainCamera.transform.eulerAngles.y;

                float rotation = Mathf.SmoothDampAngle(
                    transform.eulerAngles.y, _targetRotation,
                    ref _rotationVelocity, RotationSmoothTime);

                // ── ไม่ override rotation ระหว่างที่ SmartAttack กำลัง snap ──
                if (!_isAttacking)
                    transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            }

            Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;

            if (_controller.enabled)
                _controller.Move(
                    targetDirection.normalized * (_speed * Time.deltaTime) +
                    new Vector3(0f, _verticalVelocity, 0f) * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0f && !_isLaunched)
                    _verticalVelocity = -2f;

                if (_isLaunched && _verticalVelocity < 0f)
                {
                    _isLaunched = false;
                    _launchedVelocity = 0f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    _isLaunched = false;
                    _input.jump = false;

                    if (_hasAnimator)
                        _animator.SetBool(_animIDJump, true);

                    HapticManager.Instance?.Vibrate(0.1f, 0.1f, 0.1f);
                }

                if (_jumpTimeoutDelta >= 0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0f)
                    _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator)
                    _animator.SetBool(_animIDFreeFall, true);

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;
        }

        // =============== COMBAT SYSTEM ===============

        private void HandleAttack()
        {
            if (_input.attack)
            {
                _input.attack = false; // consume input เสมอ

                if (!_isAttacking
                    && Time.time >= _lastAttackTime + attackCooldown
                    && Grounded)
                {
                    _isAttacking     = true;
                    _lastAttackTime  = Time.time;

                    // SmartAttackSystem จัดการ:
                    //   1) หาเป้า
                    //   2) หมุนหน้าหาเป้า (snap rotate)
                    //   3) เลือก Punch / Kick ตามขนาด monster
                    //   4) trigger animation + haptic
                    _smartAttack?.TriggerAttack();

                    StartCoroutine(ResetAttackState());
                }
            }
        }

        private IEnumerator ResetAttackState()
        {
            yield return new WaitForSeconds(attackDuration);
            _isAttacking = false;
            Debug.Log("[TPC] Attack state reset.");
        }

        /// <summary>
        /// เรียกจาก Animation Event ตอน animation โจมตีจบ
        /// ให้ reset เร็วกว่า timer (ถ้า animation สั้นกว่า attackDuration)
        /// </summary>
        public void OnAttackComplete()
        {
            StopCoroutine(ResetAttackState());
            _isAttacking = false;
            Debug.Log("[TPC] Attack complete via Animation Event.");
        }

        public bool IsAttacking() => _isAttacking;

        // =============== JUMP PAD SYSTEM ===============

        public void ApplyExternalForce(float jumpPower)
        {
            _jumpTimeoutDelta = 0f;
            _fallTimeoutDelta = FallTimeout;

            float velocity    = Mathf.Sqrt(jumpPower * -2f * Gravity);
            _verticalVelocity = velocity;
            _launchedVelocity = velocity;
            _isLaunched       = true;

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDJump, true);
                _animator.SetBool(_animIDFreeFall, false);
            }

            // ยกเลิก attack ถ้ากำลังโจมตีอยู่
            if (_isAttacking)
            {
                _isAttacking = false;
                StopAllCoroutines();
            }

            Debug.Log($"[JumpPad] Power: {jumpPower} → Velocity: {_verticalVelocity:F2} m/s");
        }

        // =============== UTILITY ===============

        public Vector2 GetMoveInput()            => _input.move;
        public bool    GetJumpInput()            => _input.jump;

        public Vector3 GetMoveInputDirection()
        {
            Vector3 inputDirection = new Vector3(_input.move.x, 0f, _input.move.y).normalized;
            if (_input.move != Vector2.zero)
            {
                float targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg
                                     + _mainCamera.transform.eulerAngles.y;
                return Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;
            }
            return Vector3.zero;
        }

        public void ForceJump(float jumpPower)
        {
            _verticalVelocity = Mathf.Sqrt(jumpPower * -2f * Gravity);
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            if (_hasAnimator)
                _animator.SetBool(_animIDJump, true);
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f)  lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0f, 1f, 0f, 0.35f);
            Color transparentRed   = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.color = Grounded ? transparentGreen : transparentRed;
            Gizmos.DrawSphere(
                new Vector3(transform.position.x,
                            transform.position.y - GroundedOffset,
                            transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(
                    FootstepAudioClips[index],
                    transform.TransformPoint(_controller.center),
                    FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
                AudioSource.PlayClipAtPoint(
                    LandingAudioClip,
                    transform.TransformPoint(_controller.center),
                    FootstepAudioVolume);
        }
    }
}