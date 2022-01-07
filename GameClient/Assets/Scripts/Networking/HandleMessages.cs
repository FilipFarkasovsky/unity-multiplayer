using UnityEngine;
using RiptideNetworking;
using Multiplayer;

public class HandleMessages : MonoBehaviour
{
    [MessageHandler((ushort)ServerToClientId.spawnPlayer)]
    public static void SpawnPlayer(Message message)
    {
        ushort id = message.GetUShort();
        string username = message.GetString();
        Vector3 position = message.GetVector3();

        Player.Spawn(id, username, position);
    }

    [MessageHandler((ushort)ServerToClientId.spawnEntity)]
    public static void SpawnEntity(Message message)
    {
        byte networkedObjectType = message.GetByte();
        ushort id = message.GetUShort();
        Vector3 position = message.GetVector3();

        ClientNetworkedEntity.Spawn(networkedObjectType, id, position);
    }

    [MessageHandler((ushort)ServerToClientId.playerSnapshot)]
    public static void PlayerSnapshot(Message message)
    {
        ushort id = message.GetUShort();
        Vector3 position = message.GetVector3();
        Quaternion rotation = message.GetQuaternion();
        int serverTick = message.GetInt();
        float time = message.GetFloat();


        float _lateralSpeed = message.GetFloat();
        float _forwardSpeed = message.GetFloat();
        float _jumpLayerWeight = message.GetFloat();
        bool _isFiring = message.GetBool();


        if (NetworkManager.Singleton.playerList.TryGetValue(id, out Player player))
        {
            player.MoveTrans(position, rotation, serverTick, time, new AnimationData(_lateralSpeed, _forwardSpeed, _jumpLayerWeight, 1f, _isFiring));
        }
    }

    [MessageHandler((ushort)ServerToClientId.entitySnapshot)]
    public static void EntitySnapshot(Message message)
    {
        ushort id = message.GetUShort();
        Vector3 position = message.GetVector3();
        Quaternion rotation = message.GetQuaternion();
        int serverTick = message.GetInt();
        float time = message.GetFloat();
        if (NetworkManager.Singleton.entitiesList.TryGetValue(id, out ClientNetworkedEntity entity))
        {
            entity.MoveTrans(position, rotation, serverTick, time);
        }
    }

    [MessageHandler((ushort)ServerToClientId.serverSimulationState)]
    public static void SimulationState(Message message)
    {
        SimulationState simulationState = new SimulationState();

        simulationState.simulationFrame = message.GetInt();
        simulationState.position = message.GetVector3();
        simulationState.velocity = message.GetVector3();
        //simulationState.rotation = message.GetQuaternion();
        //simulationState.angularVelocity = message.GetVector3();

        if (NetworkManager.Singleton.playerList.TryGetValue(NetworkManager.Singleton.Client.Id, out Player player))
            player.gameObject.GetComponentInChildren<PlayerInput>().OnServerSimulationStateReceived(simulationState);
    }

    [MessageHandler((ushort)ServerToClientId.serverConvar)]
    public static void ServerConvar(Message message)
    {
        string name =message.GetString();
        float value = message.GetFloat();
        string helpString = message.GetString();
        foreach(Convar i in Convars.list)
        {
            if(i.name == name)
            {
                i.ReceiveResponse(value);
                return;
            }
        }

        // We should have returned, but since the convar doesnt exist in the client
        // we need to create it although the client cant know what it is used for
        // Defaultvalue might be wrong, but it doesnt matter too much
        Convar newConvar = new Convar(name, value, helpString, Flags.NETWORK);
    }

    [MessageHandler((ushort)ServerToClientId.serverTick)]
    public static void ServerTick(Message message)
    {
        int _serverTick = message.GetInt();
        if(_serverTick > GlobalVariables.serverTick)
            GlobalVariables.serverTick = _serverTick;
    }
}
