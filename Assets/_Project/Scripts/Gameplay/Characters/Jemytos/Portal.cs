using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay
{
    public class Portal : MonoBehaviour
    {
        #region Serialize Field

        [SerializeField] private GameObject _jemytos;
        [SerializeField] private float _spawnCooldown = 1f;

        [Tooltip("Event called when a jemytos reach the finish line")]
        [SerializeField] private UnityEvent _finishJemytos;

        #endregion

        #region Private Properties

        private float _spawnProgress = 0f;

        #endregion

        #region Unity Lifecycle

        // Update is called once per frame
        void Update()
        {
            _spawnProgress -= Time.deltaTime;

            if (_spawnProgress <= 0f)
            {
                _spawnProgress = _spawnCooldown;

                GenerateJemytos();
            }
        }

        #endregion

        #region Private Methodes

        private void GenerateJemytos()
        {
            GameObject newJemytos = Instantiate(_jemytos);
            newJemytos.transform.position = transform.position;

            newJemytos.GetComponent<Jemytos>().portal = this;
        }

        #endregion

        #region Public Methodes

        public void FinishJemytos()
        {
            _finishJemytos?.Invoke();
        }

        #endregion
    }
}
