using System.Collections.Generic;
using UnityEngine;
using RiptideNetworking;
using System.Collections;

namespace Multiplayer
{
    /// <summary> Responsible for spawning, tracking and making history of players </summary>
    public class Player : ServerNetworkedEntity
    {
        public PlayerMovement playerMovement;
        public PlayerAnimation playerAnimation;
        public AnimationData animationData = new AnimationData();
        private LogicTimer logicTimer;

        public GameObject head;

        public string username;
        public float health = 100;

        private int lastFrame = 0;
        private Queue<ClientInputState> clientInputs = new Queue<ClientInputState>();

        private float currentLayerWeightVelocity;

        private void Start()
        {
            NetworkManager.Singleton.OnMovement += ProcessInputs;
            NetworkManager.Singleton.OnSendMessages += OnSendMessages;
            logicTimer = new LogicTimer(FixedTime);
            logicTimer.Start();
        }
        new private void OnDestroy()
        {
            NetworkManager.Singleton.OnMovement -= ProcessInputs;
            NetworkManager.Singleton.playerList.Remove(id);
        }

        private void OnSendMessages()
        {
            SendMessages.PlayerSnapshot(this);
        }

        public static void Spawn(ushort id, string username)
        {
            // If player with given id exists dont instantiate him 
            // This can sometimes happen, but i have not found out why or when
            if (NetworkManager.Singleton.playerList.ContainsKey(id))
                return;

            Player player = Instantiate(NetworkManager.Singleton.entityPrefabs[(byte)NetworkedObjectType.player], new Vector3(0f, 5f, 0f), Quaternion.identity).GetComponent<Player>();
            player.name = $"Player {id} ({(username == "" ? "Guest" : username)})";
            player.id = id;
            player.username = username;

            NetworkManager.Singleton.playerList.Add(player.id, player);
            player.SendSpawn();
        }

        /// <summary>Sends a player's info to all clients.</summary>
        public override void SendSpawn()
        {
            Message message = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.spawnPlayer);
            message.Add(id);
            message.Add(username);
            message.Add(transform.position);
            NetworkManager.Singleton.Server.SendToAll(message);
        }

        public IEnumerator PlayerDied()
        {
            yield return new WaitForSeconds(0.01f);

            Vector3 position = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
            health = 100f;
            if (Physics.Raycast(position + new Vector3(0, 100.0f, 0), Vector3.down, out RaycastHit hit, 400.0f))
            {
                transform.position = hit.point + Vector3.up * 1;
            }
        }

        public void OnDeath()
        {
            StartCoroutine(PlayerDied());
        }

        /// <summary>Sends a player's info to the given client.</summary>
        /// <param name="toClient">The client to send the message to.</param>
        public override void SendSpawn(ushort toClient)
        {
            Message message = Message.Create(MessageSendMode.reliable, (ushort)ServerToClientId.spawnPlayer);
            message.Add(id);
            message.Add(username);
            message.Add(transform.position);
            NetworkManager.Singleton.Server.Send(message, toClient);
        }

        private void FixedTime()
        {
            ProcessInputs();
        }

        private void Update()
        {
            // logicTimer.Update();
        }

        private void ObtainAnimationData(bool _isFiring)
        {
            //Obtain animation data
            Vector3 localVelocity = Quaternion.Euler(0, -head.transform.rotation.eulerAngles.y, 0) * new Vector3(playerMovement.velocity.x, 0, playerMovement.velocity.z);
            var lateralSpeed = localVelocity.x / PlayerMovement.moveSpeed.GetValue();
            var forwardSpeed = localVelocity.z / PlayerMovement.moveSpeed.GetValue();
            var normalizedTime = Time.time;
            var rifleAmount = 1f;
            var isFiring = _isFiring;
            var jumpLayerWeight = Mathf.SmoothDamp(animationData.jumpLayerWeight, playerMovement.isGrounded ? 0 : 1, ref currentLayerWeightVelocity, 0.1f, Mathf.Infinity, Time.fixedDeltaTime);
            animationData = new AnimationData(lateralSpeed, forwardSpeed, jumpLayerWeight, rifleAmount, normalizedTime, isFiring);

        }


        public void ProcessInputs()
        {
            // Declare the ClientInputState that we're going to be using.
            ClientInputState inputState = null;

            // Obtain CharacterInputState's from the queue. 
            while (clientInputs.Count > 0 && (inputState = clientInputs.Dequeue()) != null)
            {

                // Player is sending simulation frames that are in the past, dont process them
                if (inputState.simulationFrame <= lastFrame)
                    continue;

                if (lastFrame - inputState.simulationFrame > 1)
                    Debug.LogError("Missing client input");

                lastFrame = inputState.simulationFrame;

                // Obtain animation data
                ObtainAnimationData((inputState.buttons & Button.Fire1) == Button.Fire1);
                
                // Process the input.
                playerMovement.ProcessInput(inputState);

                //Compensate for lag - hit detection
                if ((inputState.buttons & Button.Fire1) == Button.Fire1)
                {
                    LagCompensation.Backtrack(id, inputState.tick, inputState.lerpAmount);
                }

                // Obtain the current SimulationState.
                SimulationState state = SimulationState.CurrentSimulationState(inputState, this);

                // Send the state back to the client.
                SendMessages.SendSimulationState(id, state);
               
            }
        }

        public void AddInput(ClientInputState _inputState)
        {
            clientInputs.Enqueue(_inputState);
        }
    }
}
