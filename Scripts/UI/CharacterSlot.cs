using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MMO.Networking;
using System;

namespace MMO.UI
{
    public class CharacterSlot : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI characterNameText;
        [SerializeField] private TextMeshProUGUI raceClassText;
        [SerializeField] private Button selectButton;

        private CharacterData characterData;
        private Action<CharacterData> onSelectCallback;

        public void Setup(CharacterData data, Action<CharacterData> onSelect)
        {
            characterData = data;
            onSelectCallback = onSelect;

            // Atualizar UI
            if (characterNameText != null)
                characterNameText.text = data.name;

            if (raceClassText != null)
                raceClassText.text = $"{data.race} - {data.characterClass}";

            // Configurar botão
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectClicked);
        }

        private void OnSelectClicked()
        {
            onSelectCallback?.Invoke(characterData);
        }
    }
} 
