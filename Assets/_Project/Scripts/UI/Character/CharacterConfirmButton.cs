using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Gameplay.Characters;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GlimmerOfHope.UI.Character
{
    [RequireComponent(typeof(Button))]
    public class CharacterConfirmButton : MonoBehaviour
    {
        [Header("Event")]
        [Tooltip("Raise apres confirmation.")]
        [SerializeField] private VoidEventChannel _onCharacterConfirmed;

        [Header("Scene Transition")]
        [Tooltip("Nom exact de la scene a charger apres save. Laisser vide pour ne pas changer de scene.")]
        [SerializeField] private string _targetSceneName;

        private Button _button;
        private CharacterCreatorController _controller;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<CharacterCreatorController>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (_controller == null)
            {
                Debug.LogWarning("[CharacterConfirmButton] CharacterCreatorController introuvable.");
                return;
            }

            _controller.SaveCurrentSelections();
            _onCharacterConfirmed?.Raise();

            if (!string.IsNullOrEmpty(_targetSceneName))
                SceneManager.LoadScene(_targetSceneName);
        }
    }
}
