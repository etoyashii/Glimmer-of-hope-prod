using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Gameplay.Characters;

namespace GlimmerOfHope.UI.Character
{
    [RequireComponent(typeof(Button))]
    public class CharacterConfirmButton : MonoBehaviour
    {
        [SerializeField] private string _targetScene;

        private Button _button;
        private CharacterCreatorController _controller;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<CharacterCreatorController>();
            _button.onClick.AddListener(OnConfirm);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnConfirm);
        }

        private void OnConfirm()
        {
            _controller?.SaveCurrentSelections();

            if (!string.IsNullOrEmpty(_targetScene))
                SceneManager.LoadScene(_targetScene);
            else
                Debug.LogWarning("[CharacterConfirmButton] Aucune scene cible assignee.", this);
        }
    }
}
