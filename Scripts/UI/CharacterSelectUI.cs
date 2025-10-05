using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using MMO.Networking;

namespace MMO.UI
{
    public class CharacterSelectUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform characterSlotContainer;
        [SerializeField] private GameObject characterSlotPrefab;
        [SerializeField] private Button createCharacterButton;
        [SerializeField] private Button logoutButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingPanel;

        [Header("Settings")]
        [SerializeField] private string createCharacterScene = "02_CharacterCreate";
        [SerializeField] private string worldScene = "10_World";
        [SerializeField] private string loginScene = "00_Login";

        private ApiClient apiClient;
        private List<CharacterData> characters = new List<CharacterData>();

        private void Start()
        {
            apiClient = new ApiClient();

            // Configurar listeners
            createCharacterButton.onClick.AddListener(OnCreateCharacterClicked);
            logoutButton.onClick.AddListener(OnLogoutClicked);

            // Esconder loading
            if (loadingPanel != null)
                loadingPanel.SetActive(false);

            // Carregar personagens
            StartCoroutine(LoadCharacters());
        }

        private IEnumerator LoadCharacters()
        {
            SetLoading(true);
            SetStatus("Carregando personagens...");

            string token = GameState.Instance.GetAuthToken();
            if (string.IsNullOrEmpty(token))
            {
                SetStatus("Token inválido! Faça login novamente.", true);
                yield return new WaitForSeconds(2f);
                SceneManager.LoadScene(loginScene);
                yield break;
            }

            yield return apiClient.GetCharacters(token, (success, characterList, error) =>
            {
                SetLoading(false);

                if (success)
                {
                    characters = characterList;
                    DisplayCharacters();

                    if (characters.Count == 0)
                    {
                        SetStatus("Nenhum personagem encontrado. Crie um novo!");
                    }
                    else
                    {
                        SetStatus($"{characters.Count} personagem(ns) encontrado(s)");
                    }
                }
                else
                {
                    SetStatus($"Erro ao carregar personagens: {error}", true);
                }
            });
        }

        private void DisplayCharacters()
        {
            // Limpar slots existentes
            foreach (Transform child in characterSlotContainer)
            {
                Destroy(child.gameObject);
            }

            // Criar slot para cada personagem
            foreach (var character in characters)
            {
                GameObject slotObj = Instantiate(characterSlotPrefab, characterSlotContainer);
                CharacterSlot slot = slotObj.GetComponent<CharacterSlot>();

                if (slot != null)
                {
                    slot.Setup(character, OnCharacterSelected);
                }
            }

            // Habilitar/desabilitar botão de criar baseado no limite
            createCharacterButton.interactable = characters.Count < 3;
        }

        private void OnCharacterSelected(CharacterData character)
        {
            Debug.Log($"Personagem selecionado: {character.name}");
            
            // Salvar ID do personagem selecionado
            GameState.Instance.SetSelectedCharacterId(character.id);
            GameState.Instance.SetCharacterData(character);

            // Carregar mundo
            StartCoroutine(LoadWorld());
        }

        private IEnumerator LoadWorld()
        {
            SetLoading(true);
            SetStatus("Entrando no mundo...");

            yield return new WaitForSeconds(0.5f);

            SceneManager.LoadScene(worldScene);
        }

        private void OnCreateCharacterClicked()
        {
            SceneManager.LoadScene(createCharacterScene);
        }

        private void OnLogoutClicked()
        {
            // Limpar dados de autenticação
            GameState.Instance.ClearAuthData();

            // Voltar para login
            SceneManager.LoadScene(loginScene);
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

            createCharacterButton.interactable = !loading;
            logoutButton.interactable = !loading;
        }
    }
} 
