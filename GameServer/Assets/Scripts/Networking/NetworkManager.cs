using RiptideNetworking;
using RiptideNetworking.Transports.RudpTransport;
using UnityEngine;
using Multiplayer;
using RiptideNetworking.Utils;
using System.Collections.Generic;

public enum ServerToClientId : ushort
{
    spawnPlayer = 1,
    spawnEntity,
    playerSnapshot,
    entitySnapshot,
    serverSimulationState,
    serverConvar,
    serverTick,
}

public enum ClientToServerId : ushort
{
    playerName = 1,
    playerInput,
    playerConvar,
}
public enum NetworkedObjectType : byte
{
    localPlayer = 0,
    player = 1,
    enemy,
    projectile,
}

/// <summary> Main core of the networking - conection handling, tick counting, spawning, disconnecting</summary>
/// <remarks> Conects Riptide, Multiplayer and Console namespaces</remarks>
public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _singleton;
    public static NetworkManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                Destroy(value);
            }
        }
    }
    public Server Server { get; private set; }

    private LogicTimer logicTimer;

    public Convar tickrate = new Convar("sv_tickrate", 32, "Ticks per second", Flags.NETWORK, 1, 128);
    
    public int tick = 0;
    [SerializeField] private ushort port;
    public ushort maxClientCount;

    public GameObject PlayerPrefab => playerPrefab;
    [SerializeField] private GameObject playerPrefab;
    public GameObject EnemyPrefab => enemyPrefab;
    [SerializeField] private GameObject enemyPrefab;
    public GameObject ProjectilePrefab => projectilePrefab;
    [SerializeField] private GameObject projectilePrefab;
    public Dictionary<ushort, Player> playerList { get; private set; } = new Dictionary<ushort, Player>();
    public Dictionary<ushort, ServerNetworkedEntity> entitiesList { get; private set; } = new Dictionary<ushort, ServerNetworkedEntity>();

    public Dictionary<byte, GameObject> entityPrefabs;


    private void Awake()
    {
        Singleton = this;
        entityPrefabs = new Dictionary<byte, GameObject>()
        {
            { (byte)NetworkedObjectType.localPlayer, null },
            { (byte)NetworkedObjectType.player, _singleton.PlayerPrefab },
            { (byte)NetworkedObjectType.enemy, _singleton.EnemyPrefab },
            { (byte)NetworkedObjectType.projectile, _singleton.ProjectilePrefab },
        };
    }

    private void Start()
    {
        logicTimer = new LogicTimer(FixedTime);
        logicTimer.Start();
        Physics.autoSimulation = false;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;


#if UNITY_EDITOR
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
#else
        System.Console.Title = "Server";
        System.Console.Clear();
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        RiptideLogger.Initialize(Debug.Log, true);
#endif

        Server = new Server(new RudpServer());
        Server.ClientConnected += NewPlayerConnected;
        Server.ClientDisconnected += PlayerLeft;

        LagCompensation.Start(maxClientCount);
        Server.Start(port, maxClientCount);
        //StartCoroutine(EnemySpawning.Singleton.StartSpawning());
    }

    private void FixedTime()
    {
        tick++;
        ServerTime();
        LagCompensation.UpdatePlayerRecords();
        Server.Tick();
    }

    private void Update()
    {
        logicTimer.Update();
    }


    private void ServerTime()
    {
        for (ushort i = 1; i <= maxClientCount; i++)
        {
            if (playerList.TryGetValue(i, out Player player))
                SendMessages.PlayerSnapshot(player);
        }

        for (ushort i = 1; i <= maxClientCount; i++)
        {
            if (entitiesList.TryGetValue(i, out ServerNetworkedEntity entity))
                SendMessages.EntitySnapshot(entity);
        }

        SendMessages.ServerTick();
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
        LagCompensation.Stop();

        Server.ClientConnected -= NewPlayerConnected;
        Server.ClientDisconnected -= PlayerLeft;
    }

    private void NewPlayerConnected(object sender, ServerClientConnectedEventArgs e)
    {
        // Sending positions of all players to new player
        foreach (Player player in playerList.Values)
        {
            if (player.id != e.Client.Id)
                player.SendSpawn(e.Client.Id);
        }

        // Sending positions of all other entities to new player
        foreach (ServerNetworkedEntity entity in NetworkManager.Singleton.entitiesList.Values)
        {
            entity.SendSpawn(e.Client.Id);
        }
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        if(playerList.TryGetValue(e.Id, out Player player))
            Destroy(player.gameObject);
    }
}
