using Mirror;
using UnityEngine;
using UnityEngine.AI;

namespace MMO.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(MMO.Networking.NetworkPlayer))]
    public class PlayerMovementServer : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float stoppingDistance = 0.5f;

        private NavMeshAgent navAgent;
        private MMO.Networking.NetworkPlayer networkPlayer;

        private Vector3 targetPosition;
        private bool isMoving = false;

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
            networkPlayer = GetComponent<MMO.Networking.NetworkPlayer>();
        }

        private void Start()
        {
            // Configurar NavMeshAgent
            navAgent.speed = moveSpeed;
            navAgent.angularSpeed = rotationSpeed * 60f; // Converter para graus/segundo
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.autoBraking = true;
            navAgent.autoRepath = true;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            // Apenas o servidor controla o movimento
            if (!isServer)
            {
                navAgent.enabled = false;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            // No cliente, desabilitar NavMeshAgent (movimento vem do servidor via NetworkTransform)
            if (!isServer)
            {
                navAgent.enabled = false;
            }
        }

        [Server]
        public void MoveToPosition(Vector3 destination)
        {
            // Validar que a posição está no NavMesh
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                targetPosition = hit.position;
                navAgent.SetDestination(targetPosition);
                isMoving = true;
                
                networkPlayer.SetState(MMO.Networking.PlayerStateType.Moving);
                
                Debug.Log($"Servidor: Movendo {networkPlayer.characterName} para {targetPosition}");
            }
            else
            {
                Debug.LogWarning($"Posição {destination} não está no NavMesh!");
            }
        }

        [Server]
        private void Update()
        {
            if (!isServer) return;

            if (isMoving)
            {
                // Verificar se chegou ao destino
                if (!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    if (!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f)
                    {
                        StopMovement();
                    }
                }
            }
        }

        [Server]
        private void StopMovement()
        {
            isMoving = false;
            navAgent.ResetPath();
            networkPlayer.SetState(MMO.Networking.PlayerStateType.Idle);
            
            Debug.Log($"Servidor: {networkPlayer.characterName} parou de se mover");
        }

        [Server]
        public void Teleport(Vector3 position)
        {
            // Teleportar instantaneamente (usado em spawn)
            StopMovement();
            
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                navAgent.Warp(hit.position);
                Debug.Log($"Servidor: {networkPlayer.characterName} teleportado para {hit.position}");
            }
        }

        // Getters para debugging
        public bool IsMoving() => isMoving;
        public Vector3 GetTargetPosition() => targetPosition;
    }
}