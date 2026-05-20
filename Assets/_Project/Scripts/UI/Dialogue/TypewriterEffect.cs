using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace GlimmerOfHope.UI.Dialogue
{
    [RequireComponent(typeof(TMP_Text))]
    public class TypewriterEffect : MonoBehaviour
    {
        #region Constants

        public const float DEFAULT_CHARACTER_DELAY = 0.03f;

        #endregion

        #region Serialized Fields

        [SerializeField] private float _defaultCharacterDelay = DEFAULT_CHARACTER_DELAY;

        #endregion

        #region Private Fields

        private TMP_Text _text;
        private Coroutine _typewriteRoutine;
        private bool _isPlaying;
        private bool _skipRequested;

        #endregion

        #region Properties

        public bool IsPlaying => _isPlaying;

        #endregion

        #region Events

        public event Action OnComplete;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnDisable()
        {
            Stop();
        }

        #endregion

        #region Public Methods

        public void Play(string content)
        {
            Play(content, _defaultCharacterDelay);
        }

        public void Play(string content, float characterDelay)
        {
            Stop();
            _text.text = content;
            _text.maxVisibleCharacters = 0;
            _skipRequested = false;
            _typewriteRoutine = StartCoroutine(TypewriteRoutine(characterDelay));
        }

        public void Skip()
        {
            if (!_isPlaying) return;
            _skipRequested = true;
        }

        public void Stop()
        {
            if (_typewriteRoutine != null)
            {
                StopCoroutine(_typewriteRoutine);
                _typewriteRoutine = null;
            }
            _isPlaying = false;
            _skipRequested = false;
        }

        public void ShowAll()
        {
            Stop();
            _text.maxVisibleCharacters = _text.textInfo.characterCount;
            OnComplete?.Invoke();
        }

        #endregion

        #region Private Methods

        private IEnumerator TypewriteRoutine(float delay)
        {
            _isPlaying = true;
            _text.ForceMeshUpdate();

            int total = _text.textInfo.characterCount;
            int visible = 0;

            while (visible < total)
            {
                if (_skipRequested)
                {
                    _text.maxVisibleCharacters = total;
                    break;
                }

                visible++;
                _text.maxVisibleCharacters = visible;

                yield return new WaitForSeconds(delay);
            }

            _isPlaying = false;
            _typewriteRoutine = null;
            OnComplete?.Invoke();
        }

        #endregion
    }
}
