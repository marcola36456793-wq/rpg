using Mirror;
using UnityEngine;

namespace MMO.Gameplay
{
    [RequireComponent(typeof(MMO.Networking.NetworkPlayer))]
    public class PlayerInput : NetworkBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private LayerMask groundLayer = -1; // Aceita todos os layers por padrão

        private MMO.Networking.NetworkPlayer networkPlayer;
        private Camera mainCamera;

        private void Start()
        {
            networkPlayer = GetComponent<MMO.Networking.NetworkPlayer>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            // Apenas processar input para o player local
            if (!isLocalPlayer) return;

            HandleMovementInput();
        }

        private void HandleMovementInput()
        {
            // Detectar clique esquerdo do mouse
            if (Input.GetMouseButtonDown(0))
            {
                // Raycast do mouse para o mundo
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
                {
                    // Enviar comando de movimento para o servidor
                    networkPlayer.CmdRequestMove(hit.point);
                    
                    // Feedback visual (opcional)
                    Debug.DrawLine(transform.position, hit.point, Color.green, 1f);
                    Debug.Log($"Movimento solicitado para: {hit.point}");
                }
            }
        }

        // Método alternativo usando UI (para mobile/touch)
        public void RequestMoveTo(Vector3 worldPosition)
        {
            if (isLocalPlayer)
            {
                networkPlayer.CmdRequestMove(worldPosition);
            }
        }
    }
}