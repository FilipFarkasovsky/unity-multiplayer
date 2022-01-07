using RiptideNetworking;
using RiptideNetworking.Transports.RudpTransport;
using RiptideNetworking.Utils;
using System;
using UnityEngine;
using Multiplayer;
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

/// <summary> Main core of the networking - conection handling, tick counting, spawning local player, disconnecting</summary>
public class NetworkManager : MonoBehaviour
{
    private static NetworkManager singleton;
    public static NetworkManager Singleton
    {
        get => singleton;
        private set
        {
            if (singleton == null)
                singleton = value;
            else if (singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying object!");
                Destroy(value);
            }
        }
    }

    public string ip;
    public ushort port;

    public Client Client { get; private set; }

    private LogicTimer logicTimer;

    public Convar tickrate = new Convar("sv_tickrate", 32, "Ticks per second", Flags.NETWORK, 1, 128);

    public Dictionary<ushort, Player> playerList { get; private set; } = new Dictionary<ushort, Player>();
    public Dictionary<ushort, ClientNetworkedEntity> entitiesList { get; private set; } = new Dictionary<ushort, ClientNetworkedEntity>();

    public GameObject LocalPlayerPrefab => localPlayerPrefab;
    [SerializeField] private GameObject localPlayerPrefab;
    public GameObject PlayerPrefab => playerPrefab;
    [SerializeField] private GameObject playerPrefab;
    public GameObject EnemyPrefab => enemyPrefab;
    [SerializeField] private GameObject enemyPrefab;
    public GameObject ProjectilePrefab => projectilePrefab;
    [SerializeField] private GameObject projectilePrefab;

    public Dictionary<byte, GameObject> entityPrefabs;


    private void Awake()
    {
        Application.runInBackground = true;
        Singleton = this;

        entityPrefabs = new Dictionary<byte, GameObject>()
        {
            { (byte)NetworkedObjectType.localPlayer, Singleton.LocalPlayerPrefab },
            { (byte)NetworkedObjectType.player, Singleton.PlayerPrefab },
            { (byte)NetworkedObjectType.enemy, Singleton.EnemyPrefab },
            { (byte)NetworkedObjectType.projectile, Singleton.ProjectilePrefab },
        };
    }

    private void Start()
    {
        logicTimer = new Multiplayer.LogicTimer(FixedTime);
        logicTimer.Start();

        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Client = new Client();

        Client.Connected += DidConnect;
        Client.ConnectionFailed += FailedToConnect;
        Client.ClientDisconnected += PlayerLeft;
        Client.Disconnected += DidDisconnect;
    }

    private void Update()
    {
        logicTimer.Update();
        Multiplayer.LerpManager.Update();
    }

    private void FixedTime()
    {
        // Execute networking operations (handled messages etc.)
        Client.Tick();
    }

    private void OnApplicationQuit()
    {
        Client.Disconnect();

        Client.Connected -= DidConnect;
        Client.ConnectionFailed -= FailedToConnect;
        Client.ClientDisconnected -= PlayerLeft;
        Client.Disconnected -= DidDisconnect;
    }

    public void Connect()
    {
        Client.Connect($"{ip}:{port}");
    }

    private void DidConnect(object sender, EventArgs e)
    {
        UIManager.Singleton.SendName();
    }

    private void FailedToConnect(object sender, EventArgs e)
    {
        UIManager.Singleton.BackToMain();
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        Destroy(playerList[e.Id].gameObject);
    }

    private void DidDisconnect(object sender, EventArgs e)
    {
        Destroy(playerList[Client.Id].gameObject);
        UIManager.Singleton.BackToMain();
    }
}
