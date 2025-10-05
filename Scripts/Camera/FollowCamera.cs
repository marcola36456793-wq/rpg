using UnityEngine;

namespace MMO
{
    public class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Camera Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f);
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float currentZoom = 10f;

        [Header("Rotation Settings")]
        [SerializeField] private bool allowRotation = true;
        [SerializeField] private float rotationSensitivity = 3f;

        private Vector3 velocity = Vector3.zero;
        private float currentRotationY = 0f;

        private void Start()
        {
            // Inicializar zoom
            currentZoom = offset.magnitude;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleZoom();
            HandleRotation();
            FollowTarget();
        }

        private void HandleZoom()
        {
            // Zoom com scroll do mouse
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            
            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                currentZoom -= scrollInput * zoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }
        }

        private void HandleRotation()
        {
            if (!allowRotation) return;

            // Rotação com clique direito do mouse
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X");
                currentRotationY += mouseX * rotationSensitivity;
            }
        }

        private void FollowTarget()
        {
            // Calcular rotação baseada no input
            Quaternion rotation = Quaternion.Euler(0f, currentRotationY, 0f);

            // Normalizar offset e aplicar zoom
            Vector3 normalizedOffset = offset.normalized * currentZoom;

            // Aplicar rotação ao offset
            Vector3 rotatedOffset = rotation * normalizedOffset;

            // Calcular posição desejada
            Vector3 desiredPosition = target.position + rotatedOffset;

            // Suavizar movimento
            Vector3 smoothedPosition = Vector3.SmoothDamp(
                transform.position, 
                desiredPosition, 
                ref velocity, 
                1f / smoothSpeed
            );

            transform.position = smoothedPosition;

            // Fazer câmera olhar para o target
            transform.LookAt(target.position + Vector3.up * 1.5f); // Offset para olhar um pouco acima do chão
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            
            if (target != null)
            {
                // Posicionar câmera instantaneamente na primeira vez
                Vector3 initialPosition = target.position + offset;
                transform.position = initialPosition;
                transform.LookAt(target.position);
            }
        }

        public Transform GetTarget()
        {
            return target;
        }

        // Métodos para configuração via script
        public void SetZoom(float zoom)
        {
            currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }

        public void SetRotation(float rotationY)
        {
            currentRotationY = rotationY;
        }
    }
} 
