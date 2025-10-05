using UnityEngine;
using Mirror;

namespace MMO
{
    public class GameState : MonoBehaviour
    {
        private static GameState _instance;
        public static GameState Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameState");
                    _instance = go.AddComponent<GameState>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Authentication")]
        private string authToken;
        private int userId;
        private int selectedCharacterId;
        private MMO.Networking.CharacterData currentCharacterData;

        [Header("Network")]
        private MMO.Networking.NetworkPlayer localPlayer;
        private bool isConnectedToServer = false;
        private bool isAuthenticated = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ===== Métodos de Autenticação =====

        public void SetAuthToken(string token)
        {
            authToken = token;
            // NOTA: PlayerPrefs é apenas para desenvolvimento
            // Em produção, usar método mais seguro (Keychain, etc)
            PlayerPrefs.SetString("AuthToken", token);
            PlayerPrefs.Save();
        }

        public string GetAuthToken()
        {
            if (string.IsNullOrEmpty(authToken))
            {
                authToken = PlayerPrefs.GetString("AuthToken", "");
            }
            return authToken;
        }

        public void SetUserId(int id)
        {
            userId = id;
            PlayerPrefs.SetInt("UserId", id);
            PlayerPrefs.Save();
        }

        public int GetUserId()
        {
            if (userId == 0)
            {
                userId = PlayerPrefs.GetInt("UserId", 0);
            }
            return userId;
        }

        public void SetSelectedCharacterId(int id)
        {
            selectedCharacterId = id;
        }

        public int GetSelectedCharacterId()
        {
            return selectedCharacterId;
        }

        public void SetCharacterData(MMO.Networking.CharacterData data)
        {
            currentCharacterData = data;
        }

        public MMO.Networking.CharacterData GetCharacterData()
        {
            return currentCharacterData;
        }

        public void ClearAuthData()
        {
            authToken = "";
            userId = 0;
            selectedCharacterId = 0;
            currentCharacterData = null;
            isAuthenticated = false;

            PlayerPrefs.DeleteKey("AuthToken");
            PlayerPrefs.DeleteKey("UserId");
            PlayerPrefs.Save();
        }

        // ===== Métodos de Rede =====

        public void SetLocalPlayer(MMO.Networking.NetworkPlayer player)
        {
            localPlayer = player;
            Debug.Log($"Player local definido: {player.characterName}");
        }

        public MMO.Networking.NetworkPlayer GetLocalPlayer()
        {
            return localPlayer;
        }

        public void ClearLocalPlayer()
        {
            localPlayer = null;
        }

        public void OnAuthenticationSuccess()
        {
            isAuthenticated = true;
            Debug.Log("Autenticação com servidor bem-sucedida!");
        }

        public void OnAuthenticationFailed(string reason)
        {
            isAuthenticated = false;
            Debug.LogError($"Autenticação falhou: {reason}");
            
            // TODO: Mostrar UI de erro e desconectar
        }

        public bool IsAuthenticated()
        {
            return isAuthenticated;
        }

        public void SetConnectedToServer(bool connected)
        {
            isConnectedToServer = connected;
        }

        public bool IsConnectedToServer()
        {
            return isConnectedToServer;
        }
    }
}