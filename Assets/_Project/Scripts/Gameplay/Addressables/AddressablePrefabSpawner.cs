using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace GlimmerOfHope.Gameplay
{
    public class AddressablePrefabSpawner : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Addressables")]
        [SerializeField] private string _address = "Prefabs/DemoSpawn";

        [Header("Spawn")]
        [SerializeField] private Transform _spawnPoint;
        #endregion

        #region Private Fields
        private AsyncOperationHandle<GameObject> _instanceHandle;
        private bool _hasInstance;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_spawnPoint == null)
            {
                _spawnPoint = transform;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Spawn();
            }

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                Release();
            }
        }

        private void OnDestroy()
        {
            if (_hasInstance && _instanceHandle.IsValid())
            {
                Addressables.ReleaseInstance(_instanceHandle);
                _hasInstance = false;
            }
        }
        #endregion

        #region Private Methods
        private void Spawn()
        {
            if (_hasInstance)
            {
                Debug.Log("Instance already spawned, press backspace to release it.");
                return;
            }
            _instanceHandle = Addressables.InstantiateAsync(_address, _spawnPoint.position, _spawnPoint.rotation);
            _instanceHandle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    _hasInstance = true;
                    Debug.Log($"Spawned Addressable Instance : {_address}");
                }
                else
                {
                    Debug.LogError($"Failded to instantiate addressable : {_address}");
                }
            };
        }

        private void Release()
        {
            if (!_hasInstance)
            {
                Debug.Log("no instance to release.");
                return;
            }

            if (_instanceHandle.IsValid())
            {
                Addressables.ReleaseInstance(_instanceHandle);
            }

            _hasInstance = false;
            Debug.Log("released Addressable instance");
        }
        #endregion
    }
}