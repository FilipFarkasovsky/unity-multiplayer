
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer.Samples
{
    public class SnapshotStDev : MonoBehaviour
    {
        struct Snapshot
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public float Time;
            public float DeliveryTime;
        }

        public GameObject Server;
        public GameObject Client;

        StandardDeviation _cSnapshotDeliveryDeltaAvg;
        float _lastSnapshot;

        float _cTimeLastSnapshotReceived;
        float _cTimeSinceLastSnapshotReceived;

        [SerializeField] float _cDelayTarget;
        [SerializeField] float _cRealDelayTarget;


        [SerializeField] float _cMaxServerTimeReceived;
        [SerializeField] float _cInterpolationTime;
        [SerializeField] float _cInterpTimeScale = 1;

        [SerializeField] int SNAPSHOT_OFFSET_COUNT = 2;

        Queue<Snapshot> _cNetworkSimQueue = new Queue<Snapshot>();
        List<Snapshot> _cSnapshots = new List<Snapshot>();

        const int SNAPSHOT_RATE = 32;
        const float SNAPSHOT_INTERVAL = 1.0f / SNAPSHOT_RATE;

        [SerializeField] float INTERP_NEGATIVE_THRESHOLD = 1;
        [SerializeField] float INTERP_POSITIVE_THRESHOLD = 2;
        void Start()
        {
            _cInterpTimeScale = 1;

            _cSnapshotDeliveryDeltaAvg.Initialize(SNAPSHOT_RATE);
        }

        void Update()
        {
            // ServerMovement();
            // ServerSnapshot();

            ClientUpdateInterpolationTime();
            ClientReceiveDataFromServer();
            ClientRenderLatestPostion();
        }

        void ClientReceiveDataFromServer()
        {
            var received = false;

            while (_cNetworkSimQueue.Count > 0)
            {
                if (_cSnapshots.Count == 0)
                    _cInterpolationTime = _cNetworkSimQueue.Peek().Time - (SNAPSHOT_INTERVAL * SNAPSHOT_OFFSET_COUNT);

                var snapshot = _cNetworkSimQueue.Dequeue();

                _cSnapshots.Add(snapshot);
                _cMaxServerTimeReceived = Math.Max(_cMaxServerTimeReceived, snapshot.Time);

                received = true;
            }

            if (received)
            {
                _cSnapshotDeliveryDeltaAvg.Integrate(Time.time - _cTimeLastSnapshotReceived);
                _cTimeLastSnapshotReceived = Time.time;
                _cTimeSinceLastSnapshotReceived = 0f;

                _cDelayTarget = (SNAPSHOT_INTERVAL * SNAPSHOT_OFFSET_COUNT) + _cSnapshotDeliveryDeltaAvg.Mean + (_cSnapshotDeliveryDeltaAvg.Value * 2f);
            }

            _cRealDelayTarget = (_cMaxServerTimeReceived + _cTimeSinceLastSnapshotReceived - _cInterpolationTime) - _cDelayTarget;

            if (_cRealDelayTarget > (SNAPSHOT_INTERVAL * INTERP_POSITIVE_THRESHOLD))
                _cInterpTimeScale = 1.05f;
            else if (_cRealDelayTarget < (SNAPSHOT_INTERVAL * -INTERP_NEGATIVE_THRESHOLD))
                _cInterpTimeScale = 0.95f;
            else _cInterpTimeScale = 1.0f;

            _cTimeSinceLastSnapshotReceived += Time.unscaledDeltaTime;
        }

        void ClientUpdateInterpolationTime()
        {
            _cInterpolationTime += (Time.unscaledDeltaTime * _cInterpTimeScale);
        }

        void ClientRenderLatestPostion()
        {
            if (_cSnapshots.Count > 0)
            {
                var interpFrom = default(Vector3);
                var interpTo = default(Vector3);
                var interpAlpha = default(float);

                for (int i = 0; i < _cSnapshots.Count; ++i)
                {
                    if (i + 1 == _cSnapshots.Count)
                    {
                        if (_cSnapshots[0].Time > _cInterpolationTime)
                        {
                            interpFrom = interpTo = _cSnapshots[0].Position;
                            interpAlpha = 0;
                        }
                        else
                        {
                            interpFrom = interpTo = _cSnapshots[i].Position;
                            interpAlpha = 0;
                        }
                    }
                    else
                    {

                        var f = i;
                        var t = i + 1;

                        if (_cSnapshots[f].Time <= _cInterpolationTime && _cSnapshots[t].Time >= _cInterpolationTime)
                        {
                            interpFrom = _cSnapshots[f].Position;
                            interpTo = _cSnapshots[t].Position;

                            var range = _cSnapshots[t].Time - _cSnapshots[f].Time;
                            var current = _cInterpolationTime - _cSnapshots[f].Time;

                            interpAlpha = Mathf.Clamp01(current / range);

                            break;
                        }
                    }
                }

                Client.transform.position = Vector3.Lerp(interpFrom, interpTo, interpAlpha);
            }
        }

        void ServerMovement()
        {
            Vector3 pos;
            pos = Server.transform.position;
            pos.x = Mathf.PingPong(Time.time * 5, 10f) - 5f;

            Server.transform.position = pos;
        }

        [SerializeField, Range(0, 0.4f)] float random;

        void ServerSnapshot()
        {
            if (_lastSnapshot + Time.fixedDeltaTime < Time.time)
            {
                _lastSnapshot = Time.time;
                _cNetworkSimQueue.Enqueue(new Snapshot
                {
                    Time = _lastSnapshot,
                    Position = Server.transform.position,
                    DeliveryTime = Time.time + random
                });
            }
        }

        public void  OnReceivedSnapshot(Vector3 position)
        {
            _cNetworkSimQueue.Enqueue(new Snapshot
            {
                Time = Time.time,
                Position = position,
                DeliveryTime = Time.time - 0.00001f
            });
        }
    }
}
