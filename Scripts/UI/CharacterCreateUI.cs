using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace MMO.UI
{
    public class CharacterCreateUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField characterNameInput;
        [SerializeField] private TMP_Dropdown raceDropdown;
        [SerializeField] private TMP_Dropdown classDropdown;
        [SerializeField] private Button createButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingPanel;

        [Header("Settings")]
        [SerializeField] private string characterSelectScene = "01_CharacterSelect";

        private ApiClient apiClient;

        // Opções de raça e classe (devem corresponder ao backend)
        private readonly string[] races = { "Humano", "Elfo", "Orc" };
        private readonly string[] classes = { "Guerreiro", "Mago", "Arqueiro" };

        private void Start()
        {
            apiClient = new ApiClient();

            // Configurar dropdowns
            SetupDropdowns();

            // Configurar listeners
            createButton.onClick.AddListener(OnCreateClicked);
            backButton.onClick.AddListener(OnBackClicked);

            // Esconder loading
            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            SetStatus("");
        }

        private void SetupDropdowns()
        {
            // Configurar dropdown de raça
            if (raceDropdown != null)
            {
                raceDropdown.ClearOptions();
                raceDropdown.AddOptions(new System.Collections.Generic.List<string>(races));
            }

            // Configurar dropdown de classe
            if (classDropdown != null)
            {
                classDropdown.ClearOptions();
                classDropdown.AddOptions(new System.Collections.Generic.List<string>(classes));
            }
        }

        private void OnCreateClicked()
        {
            string characterName = characterNameInput.text.Trim();
            string selectedRace = races[raceDropdown.value];
            string selectedClass = classes[classDropdown.value];

            // Validação
            if (string.IsNullOrEmpty(characterName))
            {
                SetStatus("Digite um nome para o personagem!", true);
                return;
            }

            if (characterName.Length < 3 || characterName.Length > 30)
            {
                SetStatus("Nome deve ter entre 3 e 30 caracteres!", true);
                return;
            }

            // Validar caracteres permitidos (apenas letras e números)
            if (!System.Text.RegularExpressions.Regex.IsMatch(characterName, @"^[a-zA-Z0-9]+$"))
            {
                SetStatus("Nome deve conter apenas letras e números!", true);
                return;
            }

            StartCoroutine(CreateCharacterCoroutine(characterName, selectedRace, selectedClass));
        }

        private IEnumerator CreateCharacterCoroutine(string name, string race, string characterClass)
        {
            SetLoading(true);
            SetStatus("Criando personagem...");

            string token = GameState.Instance.GetAuthToken();

            yield return apiClient.CreateCharacter(token, name, race, characterClass, 
                (success, message, error) =>
            {
                SetLoading(false);

                if (success)
                {
                    SetStatus("Personagem criado com sucesso!");
                    StartCoroutine(ReturnToCharacterSelect());
                }
                else
                {
                    SetStatus($"Erro: {error}", true);
                }
            });
        }

        private IEnumerator ReturnToCharacterSelect()
        {
            yield return new WaitForSeconds(1.5f);
            SceneManager.LoadScene(characterSelectScene);
        }

        private void OnBackClicked()
        {
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

            createButton.interactable = !loading;
            backButton.interactable = !loading;
            characterNameInput.interactable = !loading;
            raceDropdown.interactable = !loading;
            classDropdown.interactable = !loading;
        }
    }
} 
