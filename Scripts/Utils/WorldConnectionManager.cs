using UnityEngine;
using Mirror;
using MMO.Networking;
using System.Collections;

namespace MMO
{
    public class WorldConnectionManager : MonoBehaviour
    {
        [Header("Network Settings")]
        [SerializeField] private bool autoStartAsHost = true; // NOVO: para testes locais
        [SerializeField] private string serverAddress = "localhost";
        [SerializeField] private ushort serverPort = 7777;

        [Header("UI References")]
        [SerializeField] private GameObject connectingPanel;
        [SerializeField] private UnityEngine.UI.Text statusText;

        private NetworkManager networkManager;
        private bool isConnecting = false;

        private void Start()
        {
            networkManager = NetworkManager.singleton;

            if (networkManager == null)
            {
                Debug.LogError("NetworkManager não encontrado na cena!");
                return;
            }

            // Se auto-start como host está ativado, iniciar como servidor+cliente
            if (autoStartAsHost && !NetworkServer.active && !NetworkClient.isConnected)
            {
                Debug.Log("Iniciando como HOST (servidor + cliente)...");
                StartCoroutine(StartAsHost());
            }
            else
            {
                // Conectar como cliente normal
                StartCoroutine(ConnectToServer());
            }
        }

        private IEnumerator StartAsHost()
        {
            UpdateStatus("Iniciando servidor local...");
            
            // Iniciar como host (servidor + cliente)
            networkManager.StartHost();
            
            yield return new WaitForSeconds(0.5f);

            if (NetworkServer.active && NetworkClient.isConnected)
            {
                UpdateStatus("Servidor ativo! Autenticando...");
                yield return new WaitForSeconds(0.5f);
                SendAuthenticationRequest();
            }
            else
            {
                UpdateStatus("Erro ao iniciar servidor!");
                yield return new WaitForSeconds(2f);
                ReturnToCharacterSelect();
            }
        }

        private IEnumerator ConnectToServer()
        {
            if (isConnecting) yield break;
            isConnecting = true;

            UpdateStatus("Conectando ao servidor...");

            networkManager.networkAddress = serverAddress;
            networkManager.StartClient();

            float timeout = 10f;
            float elapsed = 0f;

            while (!NetworkClient.isConnected && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (NetworkClient.isConnected)
            {
                UpdateStatus("Conectado! Autenticando...");
                yield return new WaitForSeconds(0.5f);
                SendAuthenticationRequest();
            }
            else
            {
                UpdateStatus("Falha ao conectar ao servidor!");
                Debug.LogError("Timeout ao conectar ao servidor");
                yield return new WaitForSeconds(3f);
                ReturnToCharacterSelect();
            }

            isConnecting = false;
        }

private void SendAuthenticationRequest()
{
    string token = GameState.Instance.GetAuthToken();
    int characterId = GameState.Instance.GetSelectedCharacterId();

    // DEBUG: Mostrar dados completos
    Debug.Log($"=== DADOS DE AUTENTICAÇÃO ===");
    Debug.Log($"Token: {token}");
    Debug.Log($"Character ID: {characterId}");
    Debug.Log($"Token Length: {token?.Length ?? 0}");
    Debug.Log($"=============================");

    if (string.IsNullOrEmpty(token) || characterId == 0)
    {
        Debug.LogError("Token ou characterId inválido!");
        UpdateStatus("Erro de autenticação!");
        ReturnToCharacterSelect();
        return;
    }

    var authRequest = new AuthenticationRequest
    {
        token = token,
        characterId = characterId
    };

    NetworkClient.Send(authRequest);
    UpdateStatus("Aguardando resposta...");

    StartCoroutine(WaitForAuthentication());
}


        private IEnumerator WaitForAuthentication()
        {
            float timeout = 10f;
            float elapsed = 0f;

            while (!GameState.Instance.IsAuthenticated() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (GameState.Instance.IsAuthenticated())
            {
                UpdateStatus("Autenticado! Entrando no mundo...");
                yield return new WaitForSeconds(1f);

                if (connectingPanel != null)
                    connectingPanel.SetActive(false);
            }
            else
            {
                UpdateStatus("Falha na autenticação!");
                Debug.LogError("Timeout na autenticação");
                
                yield return new WaitForSeconds(2f);
                ReturnToCharacterSelect();
            }
        }

        private void UpdateStatus(string message)
        {
            Debug.Log($"[WorldConnection] {message}");

            if (statusText != null)
                statusText.text = message;
        }

        private void ReturnToCharacterSelect()
        {
            if (NetworkClient.isConnected)
                networkManager.StopClient();
                
            if (NetworkServer.active)
                networkManager.StopHost();

            UnityEngine.SceneManagement.SceneManager.LoadScene("01_CharacterSelect");
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
