using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer
{
    /// <summary> Controls interpolation on networked objects</summary>
    public class Interpolation : MonoBehaviour
    {
        [SerializeField] public InterpolationMode mode;
        [SerializeField] public InterpolationTarget target;
        [SerializeField] private Player player;
        [SerializeField] private PlayerAnimation playerAnimation;

        static public Convar interpolation = new Convar("cl_interp", 0.1f, "Visual delay for received updates", Flags.CLIENT, 0f, 0.5f);

        private List<InterpolationState> futureTransformUpdates = new List<InterpolationState>(); // oldest first

        private float lerpAlpha = 0f;
        private InterpolationState to;
        private InterpolationState from;
        private InterpolationState previous;

        private int lastTick;

        [SerializeField] private float timeElapsed = 0f;
        [SerializeField] private float timeToReachTarget = 0.1f;

        [SerializeField] private int teleportDistance = 5;
        [SerializeField] private int SnapshotDifference;

        // ---------      ALEX

        [Header("Debug properties")]
        private StandardDeviation SnapshotDeliveryDeltaAvg;
        public bool weHadReceivedInterpolationTime;

        [SerializeField] float TimeLastSnapshotReceived;
        [SerializeField] float TimeSinceLastSnapshotReceived;

        [SerializeField] float DelayTarget;
        [SerializeField] float RealDelayTarget;

        [SerializeField] float MaxServerTimeReceived;
        [SerializeField] float ScaledInterpolationTime;

        [Header("Interpolation properties")]
        [SerializeField] float InterpTimeScale = 1;
        [SerializeField] int SNAPSHOT_OFFSET_COUNT = 2;

        [SerializeField] float INTERP_NEGATIVE_THRESHOLD = SNAPSHOT_INTERVAL * 0.5f;
        [SerializeField] float INTERP_POSITIVE_THRESHOLD = SNAPSHOT_INTERVAL * 2f;

        private const int SNAPSHOT_RATE = 32;
        private const float SNAPSHOT_INTERVAL = 1.0f / SNAPSHOT_RATE;

        private void Start()
        {
            timeToReachTarget = Utils.TickInterval();

            // The localPlayer uses a different tick
            int currentTick = target == InterpolationTarget.localPlayer ? 0 : GlobalVariables.clientTick - Utils.timeToTicks(interpolation.GetValue());
            if (currentTick < 0)
                currentTick = 0;

            to = CreateInterpolationState(currentTick, transform.position, transform.rotation, new PlayerState());
            from = CreateInterpolationState(currentTick, transform.position, transform.rotation, new PlayerState());
            previous = CreateInterpolationState(currentTick, transform.position, transform.rotation, new PlayerState());

            SnapshotDeliveryDeltaAvg.Initialize(SNAPSHOT_RATE);
        }

        private void Update()
        {
            switch (target)
            {
                case InterpolationTarget.unclampedRemote:
                    UnclampedRemoteUpdate();
                    break;
                case InterpolationTarget.deviationRemote:
                    DeviationRemoteUpdate();
                    break;
            }
        }

        private void DeviationRemoteUpdate()
        {
            ScaledInterpolationTime += (Time.unscaledDeltaTime * InterpTimeScale);
            TimeSinceLastSnapshotReceived += Time.unscaledDeltaTime;

            RealDelayTarget = (MaxServerTimeReceived + TimeSinceLastSnapshotReceived - ScaledInterpolationTime) - DelayTarget;

            if (RealDelayTarget > (SNAPSHOT_INTERVAL * INTERP_POSITIVE_THRESHOLD))
                InterpTimeScale = 1.05f;
            else if (RealDelayTarget < (SNAPSHOT_INTERVAL * -INTERP_NEGATIVE_THRESHOLD))
                InterpTimeScale = 0.95f;
            else InterpTimeScale = 1.0f;


            if (futureTransformUpdates.Count > 0)
            {
                for (int i = 0; i < futureTransformUpdates.Count; ++i)
                {
                    // remove very old update
                    if (futureTransformUpdates[i].time < TimeLastSnapshotReceived - 0.5f)
                        futureTransformUpdates.RemoveAt(i);

                    if (i + 1 == futureTransformUpdates.Count)
                    {
                        from.position = to.position = futureTransformUpdates[i].position;
                        from.rotation = to.rotation = futureTransformUpdates[i].rotation;
                    }
                    else
                    {
                        var f = i;
                        var t = i + 1;

                        if (futureTransformUpdates[f].time <= ScaledInterpolationTime && futureTransformUpdates[t].time >= ScaledInterpolationTime)
                        {
                            from.position = futureTransformUpdates[f].position;
                            to.position = futureTransformUpdates[t].position;

                            from.rotation = futureTransformUpdates[f].rotation;
                            to.rotation = futureTransformUpdates[t].rotation;

                            var current = ScaledInterpolationTime - futureTransformUpdates[f].time;
                            var range = futureTransformUpdates[t].time - futureTransformUpdates[f].time;

                            lerpAlpha = Mathf.Clamp01(current / range);

                            // Obtain current animation data
                            PlayerState playerState = futureTransformUpdates[t].playerState;
                            if (playerState != null && playerAnimation != null)
                            {
                                playerAnimation.UpdateAnimatorProperties(playerState.lateralSpeed, playerState.forwardSpeed, playerState.isFiring, playerState.jumpLayerWeight, ScaledInterpolationTime, playerState.rifleAmount);
                            }

                            break;
                        }
                    }
                }
                Interpolate(lerpAlpha);
            }
        }

        private void UnclampedRemoteUpdate()
        {
            for (int i = 0; i < futureTransformUpdates.Count; i++)
            {
                if (GlobalVariables.clientTick >= futureTransformUpdates[i].tick)
                {
                    previous = to;
                    to = futureTransformUpdates[i];
                    from = CreateInterpolationState(GlobalVariables.clientTick - Utils.timeToTicks(interpolation.GetValue()), transform.position, transform.rotation, null);
                    futureTransformUpdates.RemoveAt(i);
                    timeElapsed = 0;
                    timeToReachTarget = (to.tick - from.tick) * Utils.TickInterval();
                }
            }

            timeElapsed += Time.deltaTime;
            float lerpAmount = Mathf.Min(timeElapsed / timeToReachTarget, 1.8f);
            Interpolate(lerpAmount);
        }

        // Interpolates depending on the requested mode
        private void Interpolate(float lerpAmount)
        {
            switch (mode)
            {
                case InterpolationMode.both:
                    transform.position = Vector3.Lerp(from.position, to.position, lerpAmount);
                    transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, lerpAmount);
                    break;
                case InterpolationMode.position:
                    transform.position = Vector3.Lerp(from.position, to.position, lerpAmount);
                    break;
                case InterpolationMode.rotation:
                    transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, lerpAmount);
                    break;
            }

            // Update interpolation tick and lerp amount for proper hit detection
            GlobalVariables.interpolationTick = to.tick;
            GlobalVariables.lerpAmount = lerpAmount;
        }
        public void OnInterpolationStateReceived(InterpolationState interpolationState)
        {
            SnapshotDifference = interpolationState.tick - (GlobalVariables.clientTick - 4);
            if (interpolationState.tick <= GlobalVariables.clientTick - Utils.timeToTicks(interpolation.GetValue()))
            {
                return;
            }

            if (futureTransformUpdates.Count == 0)
            {
                futureTransformUpdates.Add(interpolationState);
                return;
            }

            for (int i = 0; i < futureTransformUpdates.Count; i++)
            {
                if (interpolationState.tick < futureTransformUpdates[i].tick)
                {
                    futureTransformUpdates.Insert(i, interpolationState);
                    break;
                }
            }

            // used for deviation snapshot implementaiton
            if (!weHadReceivedInterpolationTime)
            {
                weHadReceivedInterpolationTime = true;
            }

            MaxServerTimeReceived = Mathf.Max(MaxServerTimeReceived, interpolationState.time);

            SnapshotDeliveryDeltaAvg.Integrate(Time.time - TimeLastSnapshotReceived);
            TimeLastSnapshotReceived = Time.time;
            TimeSinceLastSnapshotReceived = 0f;
            DelayTarget = (SNAPSHOT_INTERVAL * SNAPSHOT_OFFSET_COUNT) + SnapshotDeliveryDeltaAvg.Mean + (SnapshotDeliveryDeltaAvg.Value * 2f);

        }
        public InterpolationState CreateInterpolationState(int tick, Vector3 position, Quaternion rotation, PlayerState playerState)
        {
            return new InterpolationState
            {
                tick = tick,
                position = position,
                rotation = rotation,
                playerState = playerState,
            };
        }
        public static InterpolationState CreateInterpolationState(int tick, float time, Vector3 position, Quaternion rotation, PlayerState playerState)
        {
            return new InterpolationState
            {
                tick = tick,
                time = time,
                position = position,
                rotation = rotation,
                playerState = playerState,
            };
        }

        public enum InterpolationMode
        {
            both,
            position,
            rotation,
        }
        public enum InterpolationTarget
        {
            localPlayer,
            deviationRemote,
            unclampedRemote,
        }
    }
}
