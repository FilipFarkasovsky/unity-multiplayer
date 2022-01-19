using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer
{
    /// <summary> Represents networked entities - categorizes them, spawns them, moves them. </summary>
    public class ClientNetworkedEntity : MonoBehaviour
    {
        /// <summary> The id of the object in the list </summary> 
        public ushort id;

        /// <summary> Networked type  </summary> 
        public NetworkedObjectType networkedObjectType;

        public Interpolation interpolation;
        public Multiplayer.Samples.SnapshotStDev snapshotInterpolation;
        public Samples.PlayerInterpolation playerInterpolation;

        public void OnDestroy()
        {
            NetworkManager.Singleton.entitiesList.Remove(id);
        }

        /// <summary> Moves entity - parameters are in the message </summary> 
        public void MoveTrans(Vector3 position, Quaternion rotation, int serverTick, float time)
        {
            if (serverTick > GlobalVariables.serverTick)
                GlobalVariables.serverTick = serverTick;

            if (playerInterpolation.enabled) playerInterpolation.OnClientServerInterpolationStateReceived(new Samples.InterpolationState { position = position, rotation = rotation, tick = serverTick });
            if(snapshotInterpolation.enabled) snapshotInterpolation.OnReceivedSnapshot(position);
            if(interpolation.enabled) interpolation.OnInterpolationStateReceived(new InterpolationState { tick = serverTick, time = time, position = position, rotation = rotation, playerState = null });

        }

        public static void Spawn(byte networkedObjectType, ushort id, Vector3 position)
        {
            ClientNetworkedEntity entity = Instantiate(NetworkManager.Singleton.entityPrefabs[networkedObjectType], position, Quaternion.identity).GetComponent<ClientNetworkedEntity>();

            entity.name = $"Entity {id}";
            entity.id = id;
            NetworkManager.Singleton.entitiesList.Add(id, entity);
        }
    }
}
