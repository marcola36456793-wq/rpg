using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MMO.Networking
{
    public static class ServerAuthHandler
    {
        private const string API_URL = "http://localhost:5000/api";

        public static IEnumerator ValidateTokenAndCharacter(
            string token, 
            int characterId, 
            Action<bool, CharacterData> callback)
        {
            yield return ValidateToken(token, (tokenValid, userId) =>
            {
                if (!tokenValid)
                {
                    Debug.LogError("Token validation failed");
                    callback(false, null);
                    return;
                }

                CoroutineRunner.Instance.StartCoroutine(
                    GetCharacterData(token, characterId, (success, characterData) =>
                    {
                        callback(success, characterData);
                    })
                );
            });
        }

        private static IEnumerator ValidateToken(string token, Action<bool, int> callback)
        {
            string url = $"{API_URL}/auth/validate";
            
            Debug.Log($"Validando token em: {url}");
            
            // CORREÇÃO: Criar objeto com a propriedade correta "Token" (maiúscula)
            string jsonBody = $"{{\"Token\":\"{token}\"}}";
            
            Debug.Log($"JSON Body: {jsonBody}");
            
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            
            var request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    Debug.Log($"Resposta do servidor: {request.downloadHandler.text}");
                    
                    var response = JsonUtility.FromJson<ValidateTokenResponse>(request.downloadHandler.text);
                    if (response.valid)
                    {
                        Debug.Log($"Token válido para userId: {response.userId}");
                        callback(true, response.userId);
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Erro ao parsear resposta de validação: {e.Message}");
                    Debug.LogError($"Response text: {request.downloadHandler.text}");
                }
            }
            else
            {
                Debug.LogError($"Erro ao validar token: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }

            callback(false, 0);
        }

        private static IEnumerator GetCharacterData(string token, int characterId, Action<bool, CharacterData> callback)
        {
            string url = $"{API_URL}/characters/{characterId}";
            
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    Debug.Log($"Character data response: {request.downloadHandler.text}");
                    var characterData = JsonUtility.FromJson<CharacterData>(request.downloadHandler.text);
                    Debug.Log($"Dados do personagem carregados: {characterData.name}");
                    callback(true, characterData);
                    yield break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Erro ao parsear dados do personagem: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Erro ao buscar personagem: {request.error}");
            }

            callback(false, null);
        }

        [Serializable]
        private class ValidateTokenResponse
        {
            public bool valid;
            public int userId;
            public string username;
        }
    }

    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CoroutineRunner");
                    _instance = go.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
    }
}
