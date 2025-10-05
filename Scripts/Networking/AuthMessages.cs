using Mirror;
using UnityEngine;

namespace MMO.Networking
{
    // Mensagem enviada pelo cliente para autenticar
    public struct AuthenticationRequest : NetworkMessage
    {
        public string token;
        public int characterId;
    }

    // Resposta do servidor
    public struct AuthenticationResponse : NetworkMessage
    {
        public bool success;
        public string message;
    }

    // Mensagem de requisição de movimento
    public struct MoveRequest : NetworkMessage
    {
        public Vector3 targetPosition;
    }

    // Mensagem de atualização de estado
    public struct StateUpdate : NetworkMessage
    {
        public PlayerStateType state;
        public Vector3 position;
    }

    // ENUM PlayerStateType agora está apenas em NetworkPlayer.cs
    // Removido daqui para evitar duplicação
}