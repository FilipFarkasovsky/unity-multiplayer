using System.Collections.Generic;
using UnityEngine;
using RiptideNetworking;

namespace Multiplayer
{
    /// <summary> Defines methods for server networked object</summary>
    public class ServerNetworkedEntity : MonoBehaviour
    {
        /// <summary> Networked type  </summary> 
        public NetworkedObjectType networkedObjectType;

        /// <summary> The id of the object in the list </summary> 
        public ushort id;

        /// <summary> Id of the last instantiated object </summary>
        private static ushort lastId = 0;

        public void OnDestroy()
        {
            NetworkManager.Singleton.entitiesList.Remove(id);
            NetworkManager.Singleton.OnSendMessages -= OnSendMessages;
        }

        /// <summary> Spawns object in the scene </summary>
        public static void Spawn(NetworkedObjectType networkedObjectType, Vector3 position)
        {
            ServerNetworkedEntity entity = Instantiate(NetworkManager.Singleton.entityPrefabs[(byte)networkedObjectType], position, Quaternion.identity).GetComponent<ServerNetworkedEntity>();
            NetworkManager.Singleton.OnSendMessages += entity.OnSendMessages;
            lastId++;
            entity.name = $"Entity {lastId}";
            entity.id = lastId;
            NetworkManager.Singleton.entitiesList.Add(lastId, entity);

            entity.SendSpawn();
        }

        private void OnSendMessages()
        {
            // SendMessages.EntitySnapshot(this);
        }

        /// <summary>  </summary>
        public virtual void SendSpawn()
        {
            Message message = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.spawnEntity);
            message.Add((byte)networkedObjectType);
            message.Add(id);
            message.Add(transform.position);
            NetworkManager.Singleton.Server.SendToAll(message);
        }

        /// <summary> Send message to certain client to spawn object </summary>
        public virtual void SendSpawn(ushort toClient)
        {
            Message message = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.spawnEntity);
            message.Add((byte)networkedObjectType);
            message.Add(id);
            message.Add(transform.position);
            NetworkManager.Singleton.Server.Send(message, toClient);
        }
    }
}