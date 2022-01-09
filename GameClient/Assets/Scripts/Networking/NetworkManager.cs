using RiptideNetworking;
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

    [SerializeField] private GameObject localPlayerPrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject projectilePrefab;

    public Dictionary<byte, GameObject> entityPrefabs;


    private void Awake()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;

        Singleton = this;

        entityPrefabs = new Dictionary<byte, GameObject>()
        {
            { (byte)NetworkedObjectType.localPlayer, Singleton.localPlayerPrefab },
            { (byte)NetworkedObjectType.player, Singleton.playerPrefab },
            { (byte)NetworkedObjectType.enemy, Singleton.enemyPrefab },
            { (byte)NetworkedObjectType.projectile, Singleton.projectilePrefab },
        };
    }

    private void Start()
    {
        logicTimer = new LogicTimer(FixedTime);
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
        LerpManager.Update();
        logicTimer.Update();
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
