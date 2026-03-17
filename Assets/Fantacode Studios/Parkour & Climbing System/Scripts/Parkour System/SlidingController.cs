using FS_Core;
using FS_ThirdPerson;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_ThirdPerson
{
    public static partial class AnimationNames
    {
        public static string Sliding = "Sliding";
        public static string SlideJump = "Slide Jump";
    }
}

namespace FC_ParkourSystem
{
    public class SlidingController : SystemBase
    {
        [Header("Slide Settings")]
        [SerializeField] private float slideSpeed = 12f;
        [SerializeField] private float slideAcceleration = 20f;
        [SerializeField] private float slideDeceleration = 8f;
        [SerializeField] private float minSlideSpeed = 4f;
        [SerializeField] private float slopeSlideBoost = 5f;
        [SerializeField] private float lateralControlStrength = 5f;
        [SerializeField] private float slideExitDuration = 0.4f;
        [SerializeField] private float raycastDistance = 1.5f;
        [SerializeField] private float minSlideAngle = 5f;

        [Header("Jump Settings")]
        [SerializeField] private float slideJumpHeight = 2f;
        [SerializeField] private float slideJumpForwardBoost = 1f;
        [SerializeField] private float airControlStrength = 2f;

        private Animator animator;
        private LocomotionInputManager inputManager;
        private PlayerController playerController;
        private CharacterController characterController;
        private LocomotionICharacter player;
        private BoneMapper boneMapper;

        private Vector3 currentSlideVelocity;
        private Vector3 slideDirection;
        private bool isExitingSlide;
        private bool isJumping;
        private bool isLanding;
        private float verticalVelocity;
        private Vector3 jumpStartPos;
        private Transform hips;
        private Transform modelTransform;

        float sampleRadius = 2f;         // how far from player to sample
        int sampleCount = 16;            // how many samples around a circle
        float sampleHeightOffset = 0.5f; // start raycast from this height above player's feet

        float slopeAngleThreshold = 10f; // min slope angle (degrees) to consider sliding
        float maxRaycastDistance = 5f;
        float gravity = 9.8f;

        public override SystemState State => SystemState.Sliding;

        public override float Priority => 20;

        private void Start()
        {
            animator = GetComponent<Animator>();
            inputManager = GetComponent<LocomotionInputManager>();
            playerController = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();
            player = GetComponent<LocomotionICharacter>();
            boneMapper = GetComponent<BoneMapper>();
            hips = boneMapper.GetBone(BoneType.Hips);
            modelTransform = hips.GetComponentsInParent<Animator>().ToList().First().transform;
        }

        private void Update()
        {
            if (isLanding) return;
            // Handle jump logic if in air
            if (isJumping)
            {
                HandleSlideJump();
                return;
            }

            // Don't update if we're exiting
            if (isExitingSlide) return;

            Vector3 rayOrigin = hips.position + hips.forward * .2f;
            RaycastHit slideHit;
            bool isSlideSurface = Physics.SphereCast(rayOrigin, 0.3f, Vector3.down, out slideHit, raycastDistance, FSSettings.i.GroundLayer);
            if (!isSlideSurface)
                isSlideSurface = Physics.Raycast(rayOrigin, Vector3.down, out slideHit, raycastDistance, FSSettings.i.GroundLayer);

            if (isSlideSurface && slideHit.transform.CompareTag("Slide"))
            {
                // Enter sliding state if not already
                if (!IsInFocus)
                {
                    player.OnStartSystem(this);
                    animator.CrossFade(AnimationNames.Sliding, .2f);
                    characterController.Move(transform.forward * .5f);
                }

                // Check for jump input while sliding
                if (inputManager.JumpKeyDown)
                {
                    InitiateSlideJump();
                    return;
                }

                PerformSlide();
            }
            else
            {
                StartCoroutine(OnEndSlide());
            }
        }

        private void PerformSlide()
        {

            var slopeSample = GetSlideDirection();
            slideDirection = slopeSample.downhill;
            float slopeAngle = slopeSample.slopeAngle;
            Vector3 normal = slopeSample.normal;
            Vector3 hitPoint = slopeSample.position;

            float slopeBoost = (slopeAngle / 90f) * slopeSlideBoost;
            currentSlideVelocity += slideDirection * slopeBoost * Time.deltaTime;

            // Apply lateral control based on player input
            //float inputX = inputManager.DirectionInput.x;
            //Vector3 lateralControl = playerController.CameraPlanarRotation * Vector3.right * inputX;
            //lateralControl = Vector3.ProjectOnPlane(lateralControl, normal);
            //currentSlideVelocity += lateralControl * lateralControlStrength * Time.deltaTime;

            float inputX = inputManager.DirectionInput.x;
            if (Mathf.Abs(inputX) > 0)
            {
                // lateral axis tangent to the slope and perpendicular to downhill:
                // cross(normal, downhill) gives a vector orthogonal to both normal and downhill.
                // normalize to get a unit direction for consistent input scaling.
                Vector3 lateralAxis = Vector3.Cross(normal, slideDirection);
                if (lateralAxis.sqrMagnitude < 1e-8f)
                {
                    // fallback: if downhill is nearly parallel to normal (flat or degenerate),
                    // use camera/right projected to the slope plane
                    lateralAxis = Vector3.ProjectOnPlane(playerController.CameraPlanarRotation * Vector3.right, normal);
                    if (lateralAxis.sqrMagnitude < 1e-8f)
                        lateralAxis = playerController.transform.right; // last resort
                }
                lateralAxis.Normalize();

                // Build lateral control from input
                Vector3 lateralControl = lateralAxis * inputX * lateralControlStrength;

                // Remove any uphill component from lateralControl just in case (safety)
                Vector3 upAlongSlope = Vector3.ProjectOnPlane(Vector3.up, normal);
                if (upAlongSlope.sqrMagnitude > 1e-6f) upAlongSlope.Normalize();
                float uphill = Vector3.Dot(lateralControl, upAlongSlope);
                if (uphill > 0f)
                {
                    lateralControl -= upAlongSlope * uphill;
                }

                float maxLateralContribution = lateralControlStrength; // add this float to your script (tweakable)
                float lateralComponent = Vector3.Dot(currentSlideVelocity, lateralAxis);
                float desiredLateral = lateralComponent + lateralControl.magnitude * Mathf.Sign(inputX) * Time.deltaTime;
                desiredLateral = Mathf.Clamp(desiredLateral, -maxLateralContribution, maxLateralContribution);
                Vector3 lateralPart = lateralAxis * desiredLateral;
                Vector3 otherPart = currentSlideVelocity - lateralAxis * lateralComponent;
                currentSlideVelocity = otherPart + lateralPart;
            }



            // Accelerate or decelerate based on speed
            float currentSpeed = currentSlideVelocity.magnitude;
            if (currentSpeed < slideSpeed)
            {
                currentSlideVelocity += slideDirection * slideAcceleration * Time.deltaTime;
            }
            else if (currentSpeed > slideSpeed)
            {
                currentSlideVelocity = Vector3.Lerp(currentSlideVelocity, currentSlideVelocity.normalized * slideSpeed, slideDeceleration * Time.deltaTime);
            }

            // Clamp minimum speed
            if (currentSlideVelocity.magnitude < minSlideSpeed)
            {
                currentSlideVelocity = currentSlideVelocity.normalized * minSlideSpeed;
            }

            // Apply gravity and stick to surface
            Vector3 gravityForce = Vector3.down * gravity * Time.deltaTime;
            Vector3 moveVector = currentSlideVelocity * Time.deltaTime + gravityForce;

            // Rotate player to face slide direction
            if (slideDirection != Vector3.zero)
            {
                var dir = moveVector;
                dir.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 20 * Time.deltaTime);
                //transform.localEulerAngles = new Vector3(slopeAngle, transform.localEulerAngles.y, transform.localEulerAngles.z);
                //Debug.DrawRay(transform.position, transform.position + moveVector.normalized, Color.green);

                // cast from a forward offset and down (keep your debug draw)
                var rayOrigin = transform.position + transform.forward + Vector3.up * 0.5f;
                var forwardHit = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 3f, FSSettings.i.GroundLayer);
                //Debug.DrawRay(rayOrigin, Vector3.down * 3f, Color.green);

                if (forwardHit)
                {
                    float pitch = Vector3.SignedAngle(hit.normal, Vector3.up, -modelTransform.right);

                    Quaternion targetRot = Quaternion.Euler(pitch, modelTransform.eulerAngles.y, 0f);

                    modelTransform.rotation = Quaternion.Lerp(
                        modelTransform.rotation,
                        targetRot,
                        5 * Time.deltaTime
                    );
                    //Debug.DrawRay(hit.point, Vector3.up, Color.cyan);
                }
            }

            // Move the character
            characterController.Move(moveVector);
            Debug.DrawRay(transform.position, slideDirection, Color.blue);
        }

        IEnumerator ResetModelRotation(float speed = 10)
        {
            while (isLanding || isExitingSlide || isJumping)
            {
                modelTransform.localRotation = Quaternion.Lerp(
                    modelTransform.localRotation,
                    Quaternion.identity,
                    speed * Time.deltaTime
                );

                // Stop loop when rotation is close enough
                if (Quaternion.Angle(modelTransform.localRotation, Quaternion.identity) < 0.01f)
                    break;

                yield return null;
            }

            modelTransform.localRotation = Quaternion.identity;
        }

        public SlopeSample GetSlideDirection()
        {
            Vector3 origin = transform.position;
            Vector3 baseRayOrigin = origin + Vector3.up * sampleHeightOffset;

            var candidates = new List<SlopeSample>();

            float angleSum = 0f;

            // sample points around player in circle
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float ang = t * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * sampleRadius;
                Vector3 rayStart = baseRayOrigin + offset;

                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, maxRaycastDistance, FSSettings.i.GroundLayer, QueryTriggerInteraction.Ignore))
                {
                    // slope angle relative to vertical
                    float slopeAngle = Vector3.Angle(hit.normal, Vector3.up); // 0 = flat, larger = steeper

                    if (slopeAngle >= slopeAngleThreshold)
                    {
                        // downhill direction: projection of gravity onto surface tangent
                        // this gives the direction along which the object would slide (downhill)
                        Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, hit.normal);
                        if (downhill.sqrMagnitude > 0.0001f) downhill.Normalize();

                        candidates.Add(new SlopeSample
                        {
                            position = hit.point,
                            normal = hit.normal,
                            slopeAngle = slopeAngle,
                            downhill = downhill
                        });

                        angleSum += slopeAngle;
                    }

                    //Debug.DrawRay(rayStart, Vector3.down * maxRaycastDistance, Color.red);
                }
            }

            if (candidates.Count == 0)
                return new SlopeSample(); // no slopes found

            SlopeSample best = candidates[0];
            foreach (var s in candidates)
                if (s.position.y < best.position.y) best = s;
            return best;
        }

        private IEnumerator OnEndSlide()
        {
            
            if (IsInFocus && !isExitingSlide)
            {
                isExitingSlide = true;
                StartCoroutine(ResetModelRotation(2));

                float elapsed = 0f;
                Vector3 initialVelocity = currentSlideVelocity;

                while (elapsed < slideExitDuration)
                {
                    bool isGrounded = player.CheckIsGrounded();
                    animator.SetBool(AnimatorParameters.IsGrounded, isGrounded);
                    if (isLanding) yield break;

                    elapsed += Time.deltaTime;
                    float t = elapsed / slideExitDuration;

                    // Smoothly transition velocity to zero or walking speed
                    Vector3 exitVelocity = Vector3.Lerp(initialVelocity, Vector3.zero, t);

                    // Apply lateral control during exit
                    float inputX = inputManager.DirectionInput.x;
                    //float inputZ = inputManager.DirectionInput.y;
                    Vector3 inputDirection = new Vector3(inputX, 0, 0);
                    Vector3 lateralControl = playerController.CameraPlanarRotation * inputDirection * lateralControlStrength;

                    Vector3 moveVector = (exitVelocity + lateralControl) * Time.deltaTime;
                    moveVector.y = -gravity * Time.deltaTime; // Apply gravity

                    characterController.Move(moveVector);

                    yield return null;
                }
                isExitingSlide = false;
                if (!player.CheckIsGrounded())
                    animator.CrossFade(AnimationNames.FallTree, .2f);

                player.OnEndSystem(this);
            }
            currentSlideVelocity = Vector3.zero;
        }

        // Reset when entering sliding state
        private void OnEnable()
        {
            isExitingSlide = false;
        }

        private void InitiateSlideJump()
        {
            playerController.IsInAir = true;
            isJumping = true;
            //airTime = 0f;
            jumpStartPos = transform.position;
            // Calculate jump velocity using physics formula: v = sqrt(2 * g * h)
            // This ensures consistent jump height regardless of gravity value
            verticalVelocity = Mathf.Sqrt(2f * gravity * slideJumpHeight);

            // Boost forward momentum
            currentSlideVelocity += slideDirection * slideJumpForwardBoost;

            // Trigger animation
            animator.CrossFade(AnimationNames.SlideJump, .2f);
            //StartCoroutine(AsyncUtil.RunAfterDelay(.2f, () => animator.SetBool("IsSliding", false)));
            //animator.SetBool("IsSliding", false);
            StartCoroutine(ResetModelRotation());
        }

        private void HandleSlideJump()
        {
            animator.SetBool(AnimatorParameters.IsGrounded, false);

            // Apply gravity to vertical velocity
            verticalVelocity -= gravity * Time.deltaTime;

            // Get player input for air control
            float inputX = inputManager.DirectionInput.x;
            Vector3 inputDirection = new Vector3(inputX, 0, 0);

            // Apply air control
            if (inputDirection.magnitude > 0.1f)
            {
                Vector3 airControl = transform.rotation * inputDirection;
                airControl = new Vector3(airControl.x, 0, airControl.z).normalized;
                currentSlideVelocity += airControl * airControlStrength * Time.deltaTime;
            }

            // Gradually reduce horizontal velocity while in air
            currentSlideVelocity = Vector3.Lerp(currentSlideVelocity, currentSlideVelocity.normalized * (slideSpeed * 0.1f), Time.deltaTime);

            // Combine horizontal and vertical movement
            Vector3 moveVector = currentSlideVelocity * Time.deltaTime;
            moveVector.y = verticalVelocity * Time.deltaTime;

            // Move character
            characterController.Move(moveVector);

            // Check if grounded
            bool isGrounded = player.CheckIsGrounded();

            if (isGrounded && verticalVelocity <= 0)
            {
                // Landing
                isJumping = false;
                playerController.IsInAir = false;
                verticalVelocity = 0f;
                animator.SetBool(AnimatorParameters.IsGrounded, true);

                // Check if landing on slide surface
                Vector3 rayOrigin = animator.GetBoneTransform(HumanBodyBones.Hips).position;
                bool landedOnSlide = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit landHit, raycastDistance, FSSettings.i.GroundLayer);

                if (landedOnSlide && landHit.transform.CompareTag("Slide"))
                {
                    // Continue sliding
                    animator.CrossFade(AnimationNames.Sliding, .2f);
                    //Debug.Log("Landed back on slide surface");
                }
                else
                {
                    // Exit sliding system
                    StartCoroutine(OnEndSlide());
                    StartCoroutine(Landing());
                }
                
            }
            // Debug visualization
            Debug.DrawRay(transform.position, Vector3.up * verticalVelocity, Color.yellow);
            Debug.DrawRay(transform.position, currentSlideVelocity, Color.cyan);
        }

        IEnumerator Landing()
        {
            isLanding = true;
            var halfExtends = new Vector3(.3f, .9f, 0.01f);
            var hasSpaceForRoll = Physics.BoxCast(transform.position + Vector3.up, halfExtends, transform.forward, Quaternion.LookRotation(transform.forward), 2.5f, FSSettings.i.GroundLayer);

            halfExtends = new Vector3(.1f, .1f, 0.01f);
            var heightHiting = true;
            for (int i = 0; i < 6 && heightHiting; i++)
                heightHiting = Physics.BoxCast(transform.position + Vector3.up * 1.8f + transform.forward * (i * .5f + .5f), halfExtends, Vector3.down, Quaternion.LookRotation(Vector3.down), 2.2f + i * .2f, FSSettings.i.GroundLayer);
            if (!hasSpaceForRoll && heightHiting)
            {
                playerController.OnLand?.Invoke(Mathf.Clamp((jumpStartPos.y - transform.position.y) * 0.003f, 0f, 0.05f), 1.2f);
                yield return PlayAnimationCoroutine("FallingToRoll");
            }
            else
            {
                playerController.OnLand?.Invoke(Mathf.Clamp((jumpStartPos.y - transform.position.y) * 0.003f, 0f, 0.05f), .7f);
                animator.SetFloat(AnimatorParameters.fallAmount, 1);
                yield return PlayAnimationCoroutine("Landing");
            }
            isLanding = false;
            currentSlideVelocity = Vector3.zero;
            isExitingSlide = false;
            player.OnEndSystem(this);
        }

        /// <summary>
        /// Plays an animation state like Animator.CrossFade, but as a coroutine.
        /// </summary>
        /// <param name="stateName">The name of the state to crossfade into.</param>
        /// <param name="transitionDuration">Blend duration (seconds).</param>
        /// <param name="layer">Animator layer index.</param>
        /// <param name="normalizedStartTime">Start time (0 = beginning).</param>
        IEnumerator PlayAnimationCoroutine(string stateName, float transitionDuration = 0.2f, int layer = 0, float normalizedStartTime = 0f)
        {
            EnableRootMotion();
            // Get the target state info (to calculate length later)
            animator.CrossFadeInFixedTime(stateName, transitionDuration, layer, normalizedStartTime);

            // Wait for transition to start
            yield return null;

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);

            // Wait until the animation is actually playing the desired state
            while (!info.IsName(stateName))
            {
                yield return null;
                info = animator.GetCurrentAnimatorStateInfo(layer);
            }

            // Wait until animation finishes
            float time = 0f;
            float duration = info.length;

            while (time < duration)
            {
                time += Time.deltaTime;
                yield return null;

                if (animator.IsInTransition(0) && time > 0.5f)
                    break;
            }
            ResetRootMotion();
        }

        bool prevRootMotionVal;
        void EnableRootMotion()
        {
            prevRootMotionVal = player.UseRootMotion;
            player.UseRootMotion = true;
        }
        void ResetRootMotion()
        {
            player.UseRootMotion = prevRootMotionVal;
        }


        public override void ExitSystem()
        {
            isJumping = false;
            //animator.SetBool("IsSliding", false);
            currentSlideVelocity = Vector3.zero;
            isExitingSlide = false;
        }


        //void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.cyan;
        //    Gizmos.DrawWireSphere(transform.position + Vector3.up * sampleHeightOffset, sampleRadius);

        //    // Quick preview of detected slide if possible (perform a non-alloc scan)
        //    if (Application.isPlaying)
        //    {
        //        if (GetSlideDirection(out Vector3 dir, out float maxA, out Vector3 normal))
        //        {
        //            Gizmos.color = Color.red;
        //            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, slideDirection * (sampleRadius + 1f));
        //            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f + slideDirection * (sampleRadius + 0.5f), gizmoSize);
        //        }
        //    }
        //}
        public struct SlopeSample
        {
            public Vector3 position;
            public Vector3 normal;
            public float slopeAngle; // in degrees
            public Vector3 downhill; // normalized downhill vector
        }
    }
}