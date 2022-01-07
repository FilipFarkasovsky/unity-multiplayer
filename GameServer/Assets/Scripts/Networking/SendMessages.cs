﻿using RiptideNetworking;
using Multiplayer;
using UnityEngine;

public static class SendMessages
{
    #region Messages
    /// <summary>Sends a player's animation properties to all clients except to the client himself (to avoid overwriting the player's simulation state).</summary>
    /// <param name="_player">The player whose position, rotation and animation properties to update.</param>
    public static void PlayerSnapshot(Player player)
    {
        if (!player)
            return;

        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ServerToClientId.playerSnapshot);

        message.Add(player.id);
        message.Add(player.transform.position);
        message.Add(player.transform.rotation);
        message.Add(NetworkManager.Singleton.tick);
        message.Add(Time.unscaledTime);

        message.Add(player.animationData.lateralSpeed);
        message.Add(player.animationData.forwardSpeed);
        message.Add(player.animationData.jumpLayerWeight);
        message.Add(player.animationData.isFiring);

        NetworkManager.Singleton.Server.SendToAll(message, player.id);
    }

    /// <summary>Sends snapshot of certain object depending on their type</summary>
    public static void EntitySnapshot(ServerNetworkedEntity entity)
    {
        if (entity == null)
            return;

        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ServerToClientId.entitySnapshot);

        message.Add(entity.id);
        message.Add(entity.transform.position);
        message.Add(entity.transform.rotation);
        message.Add(NetworkManager.Singleton.tick);
        message.Add(Time.unscaledTime);

        NetworkManager.Singleton.Server.SendToAll(message);
    }

    /// <summary>Sends a player's simulation state.</summary>
    /// <param name="_toClient">The client that should receive the simulation state.</param>
    /// <param name="_simulationState">The simulation state to send.</param>
    public static void SendSimulationState(ushort _toClient, SimulationState _simulationState)
    {
        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ServerToClientId.serverSimulationState);

            message.Add(_simulationState.simulationFrame);
            message.Add(_simulationState.position);
            message.Add(_simulationState.velocity);

        NetworkManager.Singleton.Server.Send(message, _toClient);
    }

    /// <summary>Sends a convar state.</summary>
    /// <param name="i">The convar to send.</param>
    public static void SendConvar(Convar i)
    {
        Message message = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.serverConvar);

        message.Add(i.name);
        message.Add(i.value);
        message.Add(i.helpString);

        NetworkManager.Singleton.Server.SendToAll(message);
    }

    /// <summary>Sends current server tick.</summary>
    public static void ServerTick()
    {
        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ServerToClientId.serverTick);

            message.Add(NetworkManager.Singleton.tick);

        NetworkManager.Singleton.Server.SendToAll(message);
    }

    #endregion
}
