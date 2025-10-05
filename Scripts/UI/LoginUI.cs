using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace MMO.UI
{
    public class LoginUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingPanel;

        [Header("Settings")]
        [SerializeField] private string characterSelectScene = "01_CharacterSelect";

        private ApiClient apiClient;

        private void Start()
        {
            apiClient = new ApiClient();

            // Configurar listeners
            loginButton.onClick.AddListener(OnLoginClicked);
            registerButton.onClick.AddListener(OnRegisterClicked);

            // Esconder loading panel
            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            // Limpar status
            SetStatus("");

            // Auto-preencher para testes (REMOVER EM PRODUÇÃO)
            #if UNITY_EDITOR
            if (string.IsNullOrEmpty(usernameInput.text))
            {
                usernameInput.text = "testuser";
                passwordInput.text = "test123";
            }
            #endif
        }

        private void OnLoginClicked()
        {
            string username = usernameInput.text.Trim();
            string password = passwordInput.text;

            // Validação básica
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetStatus("Preencha todos os campos!", true);
                return;
            }

            StartCoroutine(LoginCoroutine(username, password));
        }

        private void OnRegisterClicked()
        {
            string username = usernameInput.text.Trim();
            string password = passwordInput.text;

            // Validação básica
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetStatus("Preencha todos os campos!", true);
                return;
            }

            if (username.Length < 3)
            {
                SetStatus("Username deve ter no mínimo 3 caracteres!", true);
                return;
            }

            if (password.Length < 6)
            {
                SetStatus("Senha deve ter no mínimo 6 caracteres!", true);
                return;
            }

            StartCoroutine(RegisterCoroutine(username, password));
        }

        private IEnumerator LoginCoroutine(string username, string password)
        {
            SetLoading(true);
            SetStatus("Autenticando...");

            yield return apiClient.Login(username, password, (success, response, error) =>
            {
                SetLoading(false);

                if (success)
                {
                    // Salvar token e userId
                    GameState.Instance.SetAuthToken(response.token);
                    GameState.Instance.SetUserId(response.userId);

                    SetStatus($"Bem-vindo, {response.username}!");

                    // Ir para seleção de personagem
                    StartCoroutine(LoadCharacterSelectScene());
                }
                else
                {
                    SetStatus($"Erro: {error}", true);
                }
            });
        }

        private IEnumerator RegisterCoroutine(string username, string password)
        {
            SetLoading(true);
            SetStatus("Criando conta...");

            yield return apiClient.Register(username, password, (success, message, error) =>
            {
                SetLoading(false);

                if (success)
                {
                    SetStatus("Conta criada! Faça login.");
                    
                    // Limpar senha por segurança
                    passwordInput.text = "";
                }
                else
                {
                    SetStatus($"Erro: {error}", true);
                }
            });
        }

        private IEnumerator LoadCharacterSelectScene()
        {
            yield return new WaitForSeconds(1f);
            SceneManager.LoadScene(characterSelectScene);
        }

        private void SetStatus(string message, bool isError = false)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = isError ? Color.red : Color.white;
            }
        }

        private void SetLoading(bool loading)
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(loading);

            loginButton.interactable = !loading;
            registerButton.interactable = !loading;
            usernameInput.interactable = !loading;
            passwordInput.interactable = !loading;
        }
    }
} 
