using UnityEngine;

namespace MMO.Gameplay
{
    /// <summary>
    /// Gerencia estados visuais e animações do player (cliente-side)
    /// </summary>
    public class PlayerState : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private MMO.Networking.NetworkPlayer networkPlayer;

        [Header("Animation Parameters")]
        private readonly int speedParam = Animator.StringToHash("Speed");
        private readonly int isMovingParam = Animator.StringToHash("IsMoving");

        private MMO.Networking.PlayerStateType currentState;

        private void Start()
        {
            if (networkPlayer == null)
                networkPlayer = GetComponent<MMO.Networking.NetworkPlayer>();

            // TODO: Configurar animator quando tiver modelo 3D com animações
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (networkPlayer == null) return;

            MMO.Networking.PlayerStateType newState = networkPlayer.GetState();

            if (newState != currentState)
            {
                currentState = newState;
                ApplyStateVisuals(currentState);
            }
        }

        private void ApplyStateVisuals(MMO.Networking.PlayerStateType state)
        {
            if (animator == null) return;

            switch (state)
            {
                case MMO.Networking.PlayerStateType.Idle:
                    animator.SetFloat(speedParam, 0f);
                    animator.SetBool(isMovingParam, false);
                    break;

                case MMO.Networking.PlayerStateType.Moving:
                    animator.SetFloat(speedParam, 1f);
                    animator.SetBool(isMovingParam, true);
                    break;

                case MMO.Networking.PlayerStateType.Dead:
                    animator.SetFloat(speedParam, 0f);
                    animator.SetBool(isMovingParam, false);
                    // TODO: Trigger death animation
                    break;
            }
        }

        // Método para forçar estado específico (para debugging)
        public void ForceState(MMO.Networking.PlayerStateType state)
        {
            currentState = state;
            ApplyStateVisuals(state);
        }
    }
}