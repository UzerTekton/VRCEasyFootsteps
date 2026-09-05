// VRC Easy Footsteps 1.0.0
// Uzer Tekton
// MIT License

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;

namespace UzerTekton.VRCEFS
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VRCEasyFootsteps : UdonSharpBehaviour
    {
        private AudioSource _footstepAudioSource;

        private AudioClip _footstepAudioClip;

        [SerializeField]
        [Tooltip("Default 0.125")]
        private float minVolumeScale = 0.125f;

        [SerializeField]
        [Tooltip("Default 1")]
        private float maxVolumeScale = 1f;

        private VRCPlayerApi _localPlayer;
        private VRCPlayerApi _owner;

        private Vector3 _lastStepPosition;
        private Vector3 _currentPosition;

        private Vector3 _lastCheckVelocity;
        private Vector3 _currentVelocity;

        private Quaternion _lastStepRotation;
        private Quaternion _currentRotation;

        private bool _lastCheckIsGrounded;
        private bool _currentIsGrounded;

        private float _avatarEyeHeight;

        private double _lastStepTime;

        private const float CheckingInterval = 1f / 24f; // 24 Hz instead of every frame for performance reasons.
        private const float CheckingIntervalFuzzAmount = CheckingInterval / 4f; // Fuzzing within plus minus this amount to prevent spike patterns.

        private float _hearingDistance = 25f; // For far distance culling. Default 25.

        private float _avatarSizeRatio = 1f; // Compared to default height 1.65. Affects volume and range, and distance and velocity and acceleration thresholds.
        private const float StandardAvatarHeight = 1.65f; // In game the local player capsule is always this height.

        private const float VelocityAccelThreshold = 0.5f; // Accelerations (and decelerations) above this level will make a sound.

        private const float AngleChangeThreshold = 45f; // Rotating more than this angle will make shuffling sound.

        private const double AudioCooldownTime = 0.125; // Prevent steps in too quick succession.

        private const float RaycastDistance = 0.01f; // An idle player is normally 0.005 sunken into the ground hence we are checking for double this distance.

        private readonly RaycastHit[] _raycastResults = new RaycastHit[1]; // Physics.RaycastNonAlloc generates no garbage.

        private LayerMask _raycastLayerMask; // Set in Start().


        private void Start()
        {
            _footstepAudioSource = GetComponent<AudioSource>();
            _footstepAudioClip = _footstepAudioSource.clip;

            _localPlayer = Networking.LocalPlayer;
            _owner = Networking.GetOwner(gameObject);

            if (!_owner.IsValid()) return;

            Reset();

            _raycastLayerMask = LayerMask.GetMask("Default", "Water", "Interactive", "Environment"); // Any layer a floor could be in normally.

            SendCustomEventDelayedSeconds(nameof(CheckForFootstep), GetFuzzedCheckingInterval(), EventTiming.FixedUpdate);
        }

        public void CheckForFootstep()
        {
            if (!_owner.IsValid()) return;

            _lastCheckVelocity = _currentVelocity;
            _currentVelocity = _owner.GetVelocity();

            _currentPosition = _owner.GetPosition();
            _currentRotation = _owner.GetRotation();

            _lastCheckIsGrounded = _currentIsGrounded;

            // IsPlayerGrounded is only possible for local player
            // Cannot check if avatar flying using collider, so let's use raycast for everyone
            // _currentIsGrounded = _owner.IsPlayerGrounded();
            // Check if player is grounded

            _currentIsGrounded = Physics.RaycastNonAlloc(_currentPosition + Vector3.up * RaycastDistance, Vector3.down, _raycastResults, RaycastDistance, _raycastLayerMask) > 0;


            // Play sound in any of these situations:
            // Within hearing distance and...
            //   Jumping or landing, or...
            //   Grounded,
            //     and faster than crawling speed,
            //     and meeting velocity acceleration or step distance or rotation thresholds.
            if (CheckIsWithinHearingDistance() && (_currentIsGrounded != _lastCheckIsGrounded || _currentIsGrounded && CheckIsNotProne() && (Vector3.Distance(_lastCheckVelocity, _currentVelocity) >= VelocityAccelThreshold * _avatarSizeRatio || Vector3.Distance(_lastStepPosition, _currentPosition) >= GetStepDistanceThreshold(_currentVelocity.magnitude) || Quaternion.Angle(_lastStepRotation, _currentRotation) >= AngleChangeThreshold)))
            {
                PlayFootstep();
            }

            SendCustomEventDelayedSeconds(nameof(CheckForFootstep), GetFuzzedCheckingInterval(), EventTiming.FixedUpdate);
        }


        private bool CheckIsNotProne() => Vector3.Distance(_owner.GetPosition(), _owner.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position) >= _avatarEyeHeight * 0.5f; // If head is below half the normal height we can be fairly certain it is in prone.

        private bool CheckIsWithinHearingDistance() => Vector3.Distance(_owner.GetPosition(), _localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position) < _hearingDistance;

        private float GetStepDistanceThreshold(float velocity) => Mathf.LerpUnclamped(0.5f, 1.25f, InverseLerpUnclamped(1f, 4f, velocity)) * _avatarSizeRatio; // Linearly scaling from assumed step sizes of 0.5 m at 1 m/s, and 1.25 m at 4 m/s.

        private static float InverseLerpUnclamped(float a, float b, float value) => (value - a) / (b - a);

        private void PlayFootstep()
        {
            if (Time.timeAsDouble - _lastStepTime < AudioCooldownTime) return;

            // Volume is determined by:
            // Velocity, scaling from 1 m/s to 4 m/s, weighted 75%
            // Acceleration, scaling from 1 to 3 delta m/s, weighted 25%
            // Then scaled by avatar size
            transform.position = _currentPosition;
            _footstepAudioSource.PlayOneShot(_footstepAudioClip, Mathf.Clamp((Mathf.InverseLerp(1f, 4f, _currentVelocity.magnitude) * 0.75f + Mathf.InverseLerp(1f, 3f, Vector3.Distance(_lastCheckVelocity, _currentVelocity) * 0.25f)) * _avatarSizeRatio, minVolumeScale, maxVolumeScale));

            _lastStepPosition = _currentPosition;
            _lastStepRotation = _currentRotation;

            _lastStepTime = Time.timeAsDouble;
        }

        private static float GetFuzzedCheckingInterval() => CheckingInterval + Random.Range(-CheckingIntervalFuzzAmount, CheckingIntervalFuzzAmount);

        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (player != _owner || !player.IsValid()) return;

            Reset();
        }

        private void Reset()
        {
            _currentPosition = _owner.GetPosition();
            _currentRotation = _owner.GetRotation();
            _currentVelocity = _owner.GetVelocity();
            _currentIsGrounded = _owner.IsPlayerGrounded();

            _lastStepPosition = _currentPosition;
            _lastStepRotation = _currentRotation;
            _lastCheckVelocity = _currentVelocity;
            _lastCheckIsGrounded = _currentIsGrounded;

            _lastStepTime = Time.timeAsDouble;

            _avatarSizeRatio = _owner.GetAvatarEyeHeightAsMeters() / StandardAvatarHeight;
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            if (player != _owner || !player.IsValid()) return;
            _avatarEyeHeight = _owner.GetAvatarEyeHeightAsMeters();
            _avatarSizeRatio = _avatarEyeHeight / StandardAvatarHeight;

            _footstepAudioSource.pitch = Mathf.Clamp(1f / _avatarSizeRatio, 0f, 3f); // Smaller avatar means higher pitch. 3f is the max allowed for an AudioSource.

            _hearingDistance = 25f * _avatarSizeRatio; // Bigger avatar means bigger perceived volume and further hearing range, scaled from default 25.
            _footstepAudioSource.maxDistance = _hearingDistance;
        }
    }
}