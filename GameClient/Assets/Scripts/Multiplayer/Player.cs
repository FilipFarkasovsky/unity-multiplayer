using UnityEngine;
using Multiplayer;
using System.Collections.Generic;

/// <summary> Stores list of players, controls their interpolation and animation </summary>
public class Player : MonoBehaviour
{ 
    public string username;

    public Interpolation interpolation;

    public PlayerAnimation playerAnimation;

    public ushort id;

    private void OnDestroy()
    {
        NetworkManager.Singleton.playerList.Remove(id);
    }
    public void MoveTrans(Vector3 position, Quaternion rotation, int serverTick, float time, PlayerState playerState = null)
    {
        if (serverTick > GlobalVariables.serverTick)
            GlobalVariables.serverTick = serverTick;

        if (interpolation == null)
        {
            transform.position = position;
            transform.rotation = rotation;
            return;
        }

        InterpolationState state = new InterpolationState
        {
            tick = serverTick,
            time = time,
            position = position,
            rotation = rotation,
            playerState = playerState,

        };
        interpolation.OnInterpolationStateReceived(state);

        // if (cameraInterpolation) cameraInterpolation.NewUpdate(serverTick, rotation);
    }

    public static void Spawn(ushort id, string username, Vector3 position)
    {
        Player player;
        GameObject entityPrefab;

        if (id == NetworkManager.Singleton.Client.Id)
            entityPrefab = NetworkManager.Singleton.entityPrefabs[(byte)NetworkedObjectType.localPlayer];
        else
            entityPrefab = NetworkManager.Singleton.entityPrefabs[(byte)NetworkedObjectType.player];

        player = Instantiate(entityPrefab, position, Quaternion.identity).GetComponent<Player>();
        player.name = $"Player {id} ({username})";
        player.id = id;
        player.username = username;
        NetworkManager.Singleton.playerList.Add(player.id, player);
    }
}

