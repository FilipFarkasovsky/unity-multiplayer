using UnityEngine;

namespace Multiplayer
{
    public partial class Interpolation
    {
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
        private float lerpAlpha;

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
    }
}
