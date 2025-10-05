using UnityEngine;
using Mirror;
using MMO.Networking;
using System.Collections;

namespace MMO
{
    /// <summary>
    /// Gerencia a conexão com o servidor de jogo ao entrar na cena World
    /// </summary>
    public class WorldConnectionManager : MonoBehaviour
    {
        [Header("Network Settings")]
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

            // Conectar automaticamente ao servidor
            StartCoroutine(ConnectToServer());
        }

        private IEnumerator ConnectToServer()
        {
            if (isConnecting) yield break;
            isConnecting = true;

            UpdateStatus("Conectando ao servidor...");

            // Configurar endereço do servidor
            networkManager.networkAddress = serverAddress;

            // Tentar conectar
            networkManager.StartClient();

            // Aguardar conexão (timeout de 10 segundos)
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

                // Enviar requisição de autenticação
                SendAuthenticationRequest();
            }
            else
            {
                UpdateStatus("Falha ao conectar ao servidor!");
                Debug.LogError("Timeout ao conectar ao servidor");
                
                // TODO: Mostrar UI de erro e opção de reconectar
                yield return new WaitForSeconds(3f);
                ReturnToCharacterSelect();
            }

            isConnecting = false;
        }

        private void SendAuthenticationRequest()
        {
            string token = GameState.Instance.GetAuthToken();
            int characterId = GameState.Instance.GetSelectedCharacterId();

            if (string.IsNullOrEmpty(token) || characterId == 0)
            {
                Debug.LogError("Token ou characterId inválido!");
                UpdateStatus("Erro de autenticação!");
                ReturnToCharacterSelect();
                return;
            }

            // Enviar mensagem de autenticação para o servidor
            var authRequest = new AuthenticationRequest
            {
                token = token,
                characterId = characterId
            };

            NetworkClient.Send(authRequest);
            UpdateStatus("Aguardando resposta do servidor...");

            // Aguardar resposta (será tratada pelo MMO_NetworkManager)
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

                // Esconder painel de conexão
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
            // Desconectar do servidor
            if (NetworkClient.isConnected)
                networkManager.StopClient();

            // Voltar para seleção de personagem
            UnityEngine.SceneManagement.SceneManager.LoadScene("01_CharacterSelect");
        }

        private void OnDestroy()
        {
            // Cleanup ao sair da cena
            StopAllCoroutines();
        }
    }
}