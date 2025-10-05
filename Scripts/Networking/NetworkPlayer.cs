using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace MMO.Networking
{
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("Character Data")]
        [SyncVar] public string characterName;
        [SyncVar] public string race;
        [SyncVar] public string characterClass;
        [SyncVar] public int characterId;

        [Header("Components")]
        private NavMeshAgent navAgent;
        private MMO.Gameplay.PlayerMovementServer movementServer;

        [Header("State")]
        [SyncVar] private PlayerStateType currentState = PlayerStateType.Idle;

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            movementServer = GetComponent<MMO.Gameplay.PlayerMovementServer>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log($"NetworkPlayer iniciado no servidor: {characterName}");
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            // Configurar nome visual do GameObject
            gameObject.name = $"Player_{characterName}_{netId}";
            
            Debug.Log($"NetworkPlayer iniciado no cliente: {characterName}");
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            // Notificar GameState que o player local está pronto
            GameState.Instance.SetLocalPlayer(this);
            
            // Configurar câmera para seguir este player
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                FollowCamera followCam = mainCamera.GetComponent<FollowCamera>();
                if (followCam != null)
                {
                    followCam.SetTarget(transform);
                }
            }

            Debug.Log($"Player local iniciado: {characterName}");
        }

        public void SetCharacterData(CharacterData data)
        {
            characterId = data.id;
            characterName = data.name;
            race = data.race;
            characterClass = data.characterClass;
            
            Debug.Log($"Dados do personagem definidos: {characterName} ({race} {characterClass})");
        }

        [Command]
        public void CmdRequestMove(Vector3 targetPosition)
        {
            // Comando do cliente para servidor: solicitar movimento
            if (movementServer != null)
            {
                movementServer.MoveToPosition(targetPosition);
            }
        }

        [Server]
        public void SetState(PlayerStateType newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                RpcUpdateState(newState);
            }
        }

        [ClientRpc]
        private void RpcUpdateState(PlayerStateType newState)
        {
            currentState = newState;
            // TODO: Atualizar animações baseado no estado
            Debug.Log($"{characterName} mudou para estado: {newState}");
        }

        public PlayerStateType GetState()
        {
            return currentState;
        }

        private void OnDestroy()
        {
            // Se for o player local, notificar GameState
            if (isLocalPlayer)
            {
                if (GameState.Instance != null)
                {
                    GameState.Instance.ClearLocalPlayer();
                }
            }
        }
    }

    [System.Serializable]
    public class CharacterData
    {
        public int id;
        public string name;
        public string race;
        public string characterClass;
        public float positionX;
        public float positionY;
        public float positionZ;
    }

    public enum PlayerStateType
    {
        Idle,
        Moving,
        Dead
    }
}	