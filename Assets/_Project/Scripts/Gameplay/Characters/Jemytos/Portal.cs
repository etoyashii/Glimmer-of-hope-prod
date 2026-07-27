using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class Portal : MonoBehaviour
    {
        [SerializeField] private GameObject _jemytos;
        [SerializeField] private float _spawnCooldown = 1f;

        private float _spawnProgress = 0f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

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

        void GenerateJemytos()
        {
            GameObject newJemytos = Instantiate(_jemytos);
            newJemytos.transform.position = transform.position;
        }
    }
}
