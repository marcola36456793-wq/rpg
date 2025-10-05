using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using MMO.Networking;

namespace MMO
{
    public class ApiClient
    {
        private const string API_BASE_URL = "http://localhost:5000/api";

        // ===== Authentication Endpoints =====

        public IEnumerator Register(string username, string password, Action<bool, string, string> callback)
        {
            string url = $"{API_BASE_URL}/auth/register";

            var requestData = new RegisterRequest
            {
                username = username,
                password = password
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            yield return PostRequest(url, jsonBody, null, (success, response, error) =>
            {
                if (success)
                {
                    var data = JsonUtility.FromJson<MessageResponse>(response);
                    callback(true, data.message, null);
                }
                else
                {
                    callback(false, null, error);
                }
            });
        }

        public IEnumerator Login(string username, string password, Action<bool, LoginResponse, string> callback)
        {
            string url = $"{API_BASE_URL}/auth/login";

            var requestData = new LoginRequest
            {
                username = username,
                password = password
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            yield return PostRequest(url, jsonBody, null, (success, response, error) =>
            {
                if (success)
                {
                    var data = JsonUtility.FromJson<LoginResponse>(response);
                    callback(true, data, null);
                }
                else
                {
                    callback(false, null, error);
                }
            });
        }

        // ===== Character Endpoints =====

        public IEnumerator GetCharacters(string token, Action<bool, List<CharacterData>, string> callback)
        {
            string url = $"{API_BASE_URL}/characters";

            yield return GetRequest(url, token, (success, response, error) =>
            {
                if (success)
                {
                    try
                    {
                        // Converter array JSON para lista
                        var wrapper = JsonUtility.FromJson<CharacterListWrapper>("{\"characters\":" + response + "}");
                        callback(true, new List<CharacterData>(wrapper.characters), null);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Erro ao parsear personagens: {e.Message}");
                        callback(false, null, "Erro ao processar dados dos personagens");
                    }
                }
                else
                {
                    callback(false, null, error);
                }
            });
        }

        public IEnumerator GetCharacter(string token, int characterId, Action<bool, CharacterData, string> callback)
        {
            string url = $"{API_BASE_URL}/characters/{characterId}";

            yield return GetRequest(url, token, (success, response, error) =>
            {
                if (success)
                {
                    var data = JsonUtility.FromJson<CharacterData>(response);
                    callback(true, data, null);
                }
                else
                {
                    callback(false, null, error);
                }
            });
        }

        public IEnumerator CreateCharacter(string token, string name, string race, string characterClass, 
            Action<bool, string, string> callback)
        {
            string url = $"{API_BASE_URL}/characters";

            var requestData = new CreateCharacterRequest
            {
                name = name,
                race = race,
                characterClass = characterClass
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            yield return PostRequest(url, jsonBody, token, (success, response, error) =>
            {
                if (success)
                {
                    var data = JsonUtility.FromJson<MessageResponse>(response);
                    callback(true, data.message, null);
                }
                else
                {
                    callback(false, null, error);
                }
            });
        }

        public IEnumerator UpdateCharacterPosition(string token, int characterId, Vector3 position,
            Action<bool, string, string> callback)
        {
            string url = $"{API_BASE_URL}/characters/{characterId}/position";

            var requestData = new UpdatePositionRequest
            {
                x = position.x,
                y = position.y,
                z = position.z
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            yield return PutRequest(url, jsonBody, token, (success, response, error) =>
            {
                if (success)
                {
                    var data = JsonUtility.FromJson<MessageResponse>(response);
                    callback(true, data.message, null);
                }
                else
                {
                    callback(false, null, error);
                }
            });
        }

        // ===== HTTP Helpers =====

        private IEnumerator GetRequest(string url, string token, Action<bool, string, string> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Adicionar token de autenticação se fornecido
                if (!string.IsNullOrEmpty(token))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {token}");
                }

                yield return request.SendWebRequest();

                HandleResponse(request, callback);
            }
        }

        private IEnumerator PostRequest(string url, string jsonBody, string token, Action<bool, string, string> callback)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Adicionar token de autenticação se fornecido
                if (!string.IsNullOrEmpty(token))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {token}");
                }

                yield return request.SendWebRequest();

                HandleResponse(request, callback);
            }
        }

        private IEnumerator PutRequest(string url, string jsonBody, string token, Action<bool, string, string> callback)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Adicionar token de autenticação se fornecido
                if (!string.IsNullOrEmpty(token))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {token}");
                }

                yield return request.SendWebRequest();

                HandleResponse(request, callback);
            }
        }

        private void HandleResponse(UnityWebRequest request, Action<bool, string, string> callback)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                callback(true, request.downloadHandler.text, null);
            }
            else
            {
                string errorMessage = request.error;

                // Tentar extrair mensagem de erro do corpo da resposta
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    try
                    {
                        var errorResponse = JsonUtility.FromJson<ErrorResponse>(request.downloadHandler.text);
                        if (!string.IsNullOrEmpty(errorResponse.message))
                        {
                            errorMessage = errorResponse.message;
                        }
                    }
                    catch
                    {
                        // Se não conseguir parsear, usar erro padrão
                    }
                }

                callback(false, null, errorMessage);
            }
        }

        // ===== Data Classes =====

        [Serializable]
        private class RegisterRequest
        {
            public string username;
            public string password;
        }

        [Serializable]
        private class LoginRequest
        {
            public string username;
            public string password;
        }

        [Serializable]
        public class LoginResponse
        {
            public string token;
            public int userId;
            public string username;
        }

        [Serializable]
        private class CreateCharacterRequest
        {
            public string name;
            public string race;
            [SerializeField] public string characterClass;
            
            // Unity JsonUtility não suporta "class" como nome de campo
            // Backend usa "Class", precisamos serializar como "characterClass" e mapear
        }

        [Serializable]
        private class UpdatePositionRequest
        {
            public float x;
            public float y;
            public float z;
        }

        [Serializable]
        private class MessageResponse
        {
            public string message;
        }

        [Serializable]
        private class ErrorResponse
        {
            public string message;
        }

        [Serializable]
        private class CharacterListWrapper
        {
            public CharacterData[] characters;
        }
    }
}