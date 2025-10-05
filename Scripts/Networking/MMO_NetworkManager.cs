using Mirror;
using UnityEngine;
using System.Collections.Generic;

namespace MMO.Networking
{
    public class MMO_NetworkManager : NetworkManager
    {
        // Removido: playerPrefab já existe na classe base NetworkManager
        // Use o campo "Player Prefab" no Inspector do NetworkManager

        // Dicionário para armazenar dados de autenticação pendentes
        private Dictionary<int, PendingAuthData> pendingAuth = new Dictionary<int, PendingAuthData>();

        public override void OnStartServer()
        {
            base.OnStartServer();
            Debug.Log("MMO Server iniciado!");
            
            // Registrar handler de mensagens customizadas
            NetworkServer.RegisterHandler<AuthenticationRequest>(OnAuthenticationRequest);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Debug.Log("Cliente MMO conectado ao servidor");
            
            // Registrar handler de respostas
            NetworkClient.RegisterHandler<AuthenticationResponse>(OnAuthenticationResponse);
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            // NÃO chamar base.OnServerConnect - vamos controlar a conexão manualmente
            Debug.Log($"Cliente conectado: {conn.connectionId}. Aguardando autenticação...");
            
            // Adicionar timeout para autenticação (30 segundos)
            StartCoroutine(AuthenticationTimeout(conn));
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            // Limpar dados de autenticação pendentes
            if (pendingAuth.ContainsKey(conn.connectionId))
            {
                pendingAuth.Remove(conn.connectionId);
            }
            
            base.OnServerDisconnect(conn);
            Debug.Log($"Cliente desconectado: {conn.connectionId}");
        }

        private void OnAuthenticationRequest(NetworkConnectionToClient conn, AuthenticationRequest msg)
        {
            Debug.Log($"Recebida requisição de autenticação do cliente {conn.connectionId}");
            
            // Validar token via API REST
            StartCoroutine(ServerAuthHandler.ValidateTokenAndCharacter(
                msg.token, 
                msg.characterId,
                (success, characterData) =>
                {
                    if (success)
                    {
                        Debug.Log($"Autenticação bem-sucedida para personagem: {characterData.name}");
                        
                        // Autenticação bem-sucedida - spawnar player
                        SpawnPlayerForConnection(conn, characterData);
                        
                        // Enviar resposta de sucesso
                        conn.Send(new AuthenticationResponse
                        {
                            success = true,
                            message = "Autenticação bem-sucedida"
                        });
                    }
                    else
                    {
                        Debug.LogWarning($"Falha na autenticação do cliente {conn.connectionId}");
                        
                        // Enviar resposta de falha
                        conn.Send(new AuthenticationResponse
                        {
                            success = false,
                            message = "Token ou personagem inválido"
                        });
                        
                        // Desconectar cliente após 1 segundo
                        Invoke(nameof(DisconnectClient), 1f);
                        void DisconnectClient() => conn.Disconnect();
                    }
                }
            ));
        }

        private void OnAuthenticationResponse(AuthenticationResponse msg)
        {
            if (msg.success)
            {
                Debug.Log("Autenticação aceita pelo servidor!");
                GameState.Instance.OnAuthenticationSuccess();
            }
            else
            {
                Debug.LogError($"Autenticação rejeitada: {msg.message}");
                GameState.Instance.OnAuthenticationFailed(msg.message);
            }
        }

        private void SpawnPlayerForConnection(NetworkConnectionToClient conn, CharacterData characterData)
        {
            // Usar posição de spawn padrão ou última posição salva
            Vector3 spawnPosition = new Vector3(
                characterData.positionX,
                characterData.positionY,
                characterData.positionZ
            );
            
            // Se posição é zero, usar spawn point padrão
            if (spawnPosition == Vector3.zero)
            {
                spawnPosition = GetStartPosition()?.position ?? Vector3.zero;
            }

            // Instanciar player usando o prefab configurado no NetworkManager
            GameObject playerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            
            // Configurar dados do personagem
            NetworkPlayer player = playerObject.GetComponent<NetworkPlayer>();
            if (player != null)
            {
                player.SetCharacterData(characterData);
            }
            
            // Spawnar para todos os clientes
            NetworkServer.AddPlayerForConnection(conn, playerObject);
            
            Debug.Log($"Player {characterData.name} spawnado na posição {spawnPosition}");
        }

        private System.Collections.IEnumerator AuthenticationTimeout(NetworkConnectionToClient conn)
        {
            yield return new WaitForSeconds(30f);
            
            // Se após 30 segundos não houver player spawnado, desconectar
            if (conn.identity == null)
            {
                Debug.LogWarning($"Timeout de autenticação para cliente {conn.connectionId}");
                conn.Disconnect();
            }
        }

        // Override para prevenir spawn automático
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            // Não fazer nada - spawn é controlado pela autenticação
        }

        private class PendingAuthData
        {
            public string token;
            public int characterId;
            public float timestamp;
        }
    }
}