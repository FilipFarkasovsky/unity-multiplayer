using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer.Samples
{
    /// <summary> Controls interpolation on networked objects</summary>
    public class Interpolation : MonoBehaviour
    {
        #region properties
        [SerializeField] public InterpolationMode mode;
        [SerializeField] public InterpolationTarget target;
        [SerializeField] private PlayerAnimation playerAnimation;

        static public Convar interpolation = new Convar("cl_interp", 0.1f, "Visual delay for received updates", Flags.CLIENT, 0f, 0.5f);

        private List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();
        private TransformUpdate updateFrom, updateTo;

        private float lastTime;
        private int lastTick;
        private float lastLerpAmount;

        private float timeElapsed = 0f;
        private float timeToReachTarget = 0.1f;

        [SerializeField] bool Delay = false;
        [SerializeField] bool WaitForLerp = false;

        // ---------      ALEX

        float FixedStepAccumulator;
        public Vector3 PreviousPosition;
        public Vector3 NewPosition;
        public float PreviousTime;
        public float CurrentTime;

        #endregion

        [Header("Debug properties")]
        [SerializeField] float TimeLastSnapshotReceived;
        [SerializeField] float TimeSinceLastSnapshotReceived;

        [SerializeField] float DelayTarget;
        [SerializeField] float RealDelayTarget;

        [SerializeField] float MaxServerTimeReceived;
        [SerializeField] float ScaledInterpolationTime;
        private float NormalInterpolationTime;

        [Header("Interpolation properties")]
        [SerializeField] float InterpTimeScale = 1;
        [SerializeField] int SNAPSHOT_OFFSET_COUNT = 2;

        [SerializeField] float INTERP_NEGATIVE_THRESHOLD = SNAPSHOT_INTERVAL * 0.5f;
        [SerializeField] float INTERP_POSITIVE_THRESHOLD = SNAPSHOT_INTERVAL * 2f;


        private const int SNAPSHOT_RATE = 32;
        private const float SNAPSHOT_INTERVAL = 1.0f / SNAPSHOT_RATE;

        private StandardDeviation SnapshotDeliveryDeltaAvg;

        float lerpAlpha;
        public bool weHadReceivedInterpolationTime;

        private void Start()
        {

            if (target == InterpolationTarget.localPlayer)
            {
                Delay = false;
                WaitForLerp = false;
            }

            timeToReachTarget = Utils.TickInterval();

            // The localPlayer uses a different tick
            int currentTick = target == InterpolationTarget.localPlayer ? 0 : GlobalVariables.clientTick - Utils.timeToTicks(interpolation.GetValue());
            if (currentTick < 0)
                currentTick = 0;

            updateFrom = new TransformUpdate(currentTick, Time.time, transform.position, transform.rotation);
            updateTo = new TransformUpdate(currentTick, Time.time, transform.position, transform.rotation);

            lastTick = 0;
            lastLerpAmount = 0f;

            SnapshotDeliveryDeltaAvg.Initialize(SNAPSHOT_RATE);
        }

        private void Update()
        {
            switch (target)
            {
                case InterpolationTarget.localPlayer:
                    LocalPlayerUpdate();
                    break;
                case InterpolationTarget.localPlayerDeltaSnapshot:
                    LocalPlayerDeltaSnapshotUpdate();
                    break;
                case InterpolationTarget.syncedRemote:
                    SyncedUpdate();
                    break;
                case InterpolationTarget.nonSyncedRemote:
                    NonSyncedUpdate();
                    break;
                case InterpolationTarget.deviationRemote:
                    RemotePlayerDeltaSnapshot();
                    break;
            }
        }

        // NotAGoodUsername implementation
        // Used for syncing players - every player has same lerp amount
        // Sync is needed for entities that have lag compensation implemented
        private void SyncedUpdate()
        {
            // There is no updates to lerp from, return
            if (futureTransformUpdates.Count <= 0)
                return;

            // Set current tick
            updateFrom = futureTransformUpdates[0];
            if (futureTransformUpdates.Count >= 2)
            {
                updateTo = futureTransformUpdates[1];
            }
            else
            {
                updateTo = updateFrom;
            }

            // It is very new update so we dont interpolate
            if (Time.time - updateTo.time < Utils.roundTimeToTimeStep(interpolation.GetValue()) && Delay)
            {
                return;
            }

            // Lerp amount moved to the next loop but the current target didnt move to the next tick, so dont interpolate
            if (lastTick == updateTo.tick && GlobalVariables.lerpAmount < lastLerpAmount)
                return;

            Interpolate(GlobalVariables.lerpAmount);
            lastTick = updateTo.tick;
            lastLerpAmount = GlobalVariables.lerpAmount;

        }

        // NotAGoodUsername implementation
        // Used for entitities that don't require lag compensation
        private void NonSyncedUpdate()
        {
            // There is no updates to lerp from, return
            if (futureTransformUpdates.Count <= 1)
                return;

            while (futureTransformUpdates[1].tick < GlobalVariables.clientTick - Utils.timeToTicks(interpolation.GetValue()))
            {
                futureTransformUpdates.RemoveAt(0);

                // There is no updates to lerp from, return
                if (futureTransformUpdates.Count <= 1)
                    return;
            }


            // Set current tick
            updateFrom = futureTransformUpdates[0];
            updateTo = futureTransformUpdates[1];

            // If (time - time tick) <= interpolation amount, return
            if (Time.time - updateTo.time <= Utils.roundTimeToTimeStep(interpolation.GetValue()) && Delay)
                return;

            timeElapsed += Time.unscaledDeltaTime;

            Interpolate(timeElapsed / timeToReachTarget);

            // While we have reached the target, move to the next and repeat
            while (ReachedTarget(timeElapsed / timeToReachTarget))
            {
                timeElapsed -= timeToReachTarget;
                //timeToReachTarget = Mathf.Abs(current.time - current.lastTime);

                if (futureTransformUpdates.Count <= 1)
                    break;

                futureTransformUpdates.RemoveAt(0);
                if (futureTransformUpdates.Count <= 1)
                    break;

                // Set current tick
                updateFrom = futureTransformUpdates[0];
                updateTo = futureTransformUpdates[1];
            }
        }

        // Returns if it has reached the targe when interpolating
        // WaitForLerp waits for _lerpAmount to reach 1
        // If it is false it will return true if the target tick
        // is equal to the current interpolated tick
        private bool ReachedTarget(float lerpAmount)
        {
            if (lerpAmount <= 0)
                return false;
            switch (mode)
            {
                case InterpolationMode.both:
                    if (WaitForLerp)
                        return lerpAmount >= 1f;
                    else
                        return (transform.position == updateTo.position && transform.rotation == updateTo.rotation) || lerpAmount >= 1f;
                case InterpolationMode.position:
                    if (WaitForLerp)
                        return lerpAmount >= 1f;
                    else
                        return transform.position == updateTo.position || lerpAmount >= 1f;
                case InterpolationMode.rotation:
                    if (WaitForLerp)
                        return lerpAmount >= 1f;
                    else
                        return transform.rotation == updateTo.rotation || lerpAmount >= 1f;
            }
            return false;
        }

        // NotAGoodUsername implementation
        // Used for LocalPlayer
        private void LocalPlayerUpdate()
        {
            // There is no updates to lerp from, return
            if (futureTransformUpdates.Count <= 0 || futureTransformUpdates[0] == null)
                return;

            // Set current tick
            updateFrom = futureTransformUpdates[0];
            updateTo = futureTransformUpdates[1];

            // If (time - time tick) <= interpolation amount, return
            if (Time.time - futureTransformUpdates[1].time <= Utils.roundTimeToTimeStep(interpolation.GetValue()) && Delay)
                return;

            timeElapsed += Time.unscaledDeltaTime / Utils.TickInterval();

            Interpolate(timeElapsed);

            // While we have reached the target, move to the next and repeat
            while (ReachedTarget(timeElapsed))
            {
                timeElapsed = timeElapsed - 1;
                timeElapsed = Mathf.Max(0f, timeElapsed);

                if (futureTransformUpdates.Count <= 0)
                    break;

                futureTransformUpdates.RemoveAt(0);
                if (futureTransformUpdates.Count <= 0)
                    break;

                // Set current tick
                updateFrom = futureTransformUpdates[0];
                updateTo = futureTransformUpdates[1];
            }
        }

        // Alex implementation
        private void LocalPlayerDeltaSnapshotUpdate()
        {
            FixedStepAccumulator += Time.deltaTime;

            while (FixedStepAccumulator >= Time.fixedDeltaTime)
            {
                FixedStepAccumulator -= Time.fixedDeltaTime;
            }

            float _alpha = Mathf.Clamp01(Time.time - CurrentTime / Time.fixedDeltaTime);

            transform.position = Vector3.Lerp(PreviousPosition, NewPosition, _alpha);
        }

        // Alex implementation 
        private void RemotePlayerDeltaSnapshot()
        {
            ScaledInterpolationTime += (Time.unscaledDeltaTime * InterpTimeScale);
            NormalInterpolationTime += (Time.unscaledDeltaTime);
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
                    if (i + 1 == futureTransformUpdates.Count)
                    {
                        updateFrom.position = updateTo.position = futureTransformUpdates[i].position;
                        updateFrom.rotation = updateTo.rotation = futureTransformUpdates[i].rotation;
                        lerpAlpha = 0;
                    }
                    else
                    {
                        var f = i;
                        var t = i + 1;

                        if (futureTransformUpdates[f].time <= ScaledInterpolationTime && futureTransformUpdates[t].time >= ScaledInterpolationTime)
                        {
                            updateFrom.position = futureTransformUpdates[f].position;
                            updateTo.position = futureTransformUpdates[t].position;

                            updateFrom.rotation = futureTransformUpdates[f].rotation;
                            updateTo.rotation = futureTransformUpdates[t].rotation;

                            var current = ScaledInterpolationTime - futureTransformUpdates[f].time;
                            var range = futureTransformUpdates[t].time - futureTransformUpdates[f].time;

                            lerpAlpha = Mathf.Clamp01(current / range);

                            // Obtain current animation data
                            AnimationData animationData = futureTransformUpdates[t].animationData;
                            if (animationData != null && playerAnimation != null)
                            {
                                playerAnimation.UpdateAnimatorProperties(animationData.lateralSpeed, animationData.forwardSpeed, animationData.isFiring, animationData.jumpLayerWeight, ScaledInterpolationTime, animationData.rifleAmount);
                            }

                            break;
                        }
                    }
                }
                Interpolate(lerpAlpha);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(updateFrom.position, updateTo.position);
        }

        // Interpolates depending on the requested mode
        private void Interpolate(float lerpAmount)
        {
            switch (mode)
            {
                case InterpolationMode.both:
                    transform.position = Vector3.Lerp(updateFrom.position, updateTo.position, lerpAmount);
                    transform.rotation = Quaternion.Slerp(updateFrom.rotation, updateTo.rotation, lerpAmount);
                    break;
                case InterpolationMode.position:
                    transform.position = Vector3.Lerp(updateFrom.position, updateTo.position, lerpAmount);
                    break;
                case InterpolationMode.rotation:
                    transform.rotation = Quaternion.Slerp(updateFrom.rotation, updateTo.rotation, lerpAmount);
                    break;
            }

            // Update interpolation tick and lerp amount for proper hit detection
            GlobalVariables.interpolationTick = updateTo.tick;
            GlobalVariables.lerpAmount = lerpAmount;
        }

        // Updates are used to add a new tick to the list
        // the list is sorted and then set the last tick info to the respective variables
        #region Updates
        private int lastPes = 3;
        internal void NewUpdate(int tick, float time, Vector3 position, Quaternion rotation, AnimationData animationData)
        {
            if (!weHadReceivedInterpolationTime)
            {
                ScaledInterpolationTime = NormalInterpolationTime = time - (SNAPSHOT_INTERVAL * SNAPSHOT_OFFSET_COUNT);
                weHadReceivedInterpolationTime = true;
            }

            futureTransformUpdates.Add(new TransformUpdate(tick, time, position, rotation, animationData));

            MaxServerTimeReceived = Mathf.Max(MaxServerTimeReceived, time);

            SnapshotDeliveryDeltaAvg.Integrate(Time.time - TimeLastSnapshotReceived);
            TimeLastSnapshotReceived = Time.time;
            TimeSinceLastSnapshotReceived = 0f;
            DelayTarget = (SNAPSHOT_INTERVAL * SNAPSHOT_OFFSET_COUNT) + SnapshotDeliveryDeltaAvg.Mean + (SnapshotDeliveryDeltaAvg.Value * 2f);

            futureTransformUpdates.Sort(delegate (TransformUpdate x, TransformUpdate y)
            {
                return x.tick.CompareTo(y.tick);
            });

            AccountForPacketLoss();
        }
        internal void NewUpdate(int tick, Vector3 position, Quaternion rotation)
        {
            futureTransformUpdates.Add(new TransformUpdate(tick, Time.time, lastTime, position, rotation));

            futureTransformUpdates.Sort(delegate (TransformUpdate x, TransformUpdate y)
            {
                return x.tick.CompareTo(y.tick);
            });

            AccountForPacketLoss();
        }
        internal void NewUpdate(int tick, Quaternion rotation)
        {
            futureTransformUpdates.Add(new TransformUpdate(tick, Time.time, Vector3.zero, rotation));

            futureTransformUpdates.Sort(delegate (TransformUpdate x, TransformUpdate y)
            {
                return x.tick.CompareTo(y.tick);
            });

            AccountForPacketLoss();

        }
        #endregion

        // NotAGoodUsername implementation
        // Adds fake packets between real ones and remove very old updates
        private void AccountForPacketLoss()
        {
            // There is no updates to lerp from, return
            if (futureTransformUpdates.Count <= 0 || futureTransformUpdates[0] == null)
                return;

            // we remove incorrect updates and create new ones if needed
            TransformUpdate last = null;
            foreach (TransformUpdate update in futureTransformUpdates.ToArray())
            {
                // if tick < current client tick - interpolation, then remove
                if (update.tick < GlobalVariables.clientTick - Utils.timeToTicks(interpolation.maxValue))
                {

                    futureTransformUpdates.Remove(update);
                    continue;
                }

                // We want to get last tick
                if (update.tick <= last?.tick)
                {
                    futureTransformUpdates.Remove(update);
                    continue;
                }

                // Purpose: Add fake packets in between real ones, to account for packet loss
                if (last != null)
                {
                    // Get tick difference
                    int tickDifference = update.tick - last.tick;
                    if (tickDifference > 1)
                    {
                        // Loop through every tick till getting to the last tick,
                        // which we dont use since it is the current tick
                        TransformUpdate lastInForLoop = last;
                        for (int j = 1; j < tickDifference; j++)
                        {
                            // Create new update
                            TransformUpdate inBetween = new TransformUpdate();

                            // Calculate the fraction in between the ticks
                            float fraction = (float)j / (float)tickDifference;

                            // Lerp with the given fraction
                            inBetween.position = Vector3.Lerp(lastInForLoop.position, update.position, fraction);
                            inBetween.rotation = Quaternion.Slerp(lastInForLoop.rotation, update.rotation, fraction);
                            inBetween.time = Mathf.Lerp(lastInForLoop.time, update.time, fraction);

                            // Set new tick
                            inBetween.tick = lastInForLoop.tick + 1;

                            // Insert new update
                            futureTransformUpdates.Insert(futureTransformUpdates.IndexOf(lastInForLoop), inBetween);

                            // Last tick is now the inserted tick
                            lastInForLoop = inBetween;
                        }
                    }
                }

                last = update;
            }
        }

        // It is used for localPlayer interpolation, for smooth camera gameplay
        // the reason it is a separete function is to skip some unecessary calls
        internal void PlayerUpdate(int tick, Vector3 position)
        {
            futureTransformUpdates.Add(new TransformUpdate(tick, Time.time, position, Quaternion.identity));

            lastTime = Time.time;
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
            localPlayerDeltaSnapshot,
            syncedRemote,
            nonSyncedRemote,
            deviationRemote,
        }
    }
}
