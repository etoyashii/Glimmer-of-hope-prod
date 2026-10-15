using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core.Events;
using Unity.Cinemachine;
using System;

namespace GlimmerOfHope.UI
{
    public class CharacterCreatorCameraManager : MonoBehaviour
    {
        [SerializeField] private CharacterCreatorCamera _defaultCharacterCreatorCamera;
        [SerializeField] private StringEventChannel _onCharacterCategoryChanged;
        [SerializeField] private CinemachineBrain _brain;

        private CharacterCreatorCamera[] _cameras;
        private Dictionary<string, CharacterCreatorCamera> _camerasByCategory;

        void Awake()
        {
            _onCharacterCategoryChanged.Subscribe(OnCategoryChanged);
        }

        private void OnDestroy()
        {
            _onCharacterCategoryChanged.Unsubscribe(OnCategoryChanged);
        }

        private void OnCategoryChanged(string category)
        {
            if (_camerasByCategory == null)
                FillCamerasByCategory();
            
            CharacterCreatorCamera selectedCamera = _camerasByCategory.GetValueOrDefault(category, _defaultCharacterCreatorCamera);
            Debug.LogFormat("Set camers {0}", selectedCamera.name);
            foreach (CharacterCreatorCamera camera in _cameras)
                camera.SetActive(camera == selectedCamera);
        }

        private void FillCamerasByCategory()
        {
            if (_cameras == null)
                _cameras = GetComponentsInChildren<CharacterCreatorCamera>();
            _camerasByCategory = new Dictionary<string, CharacterCreatorCamera>();

            foreach (CharacterCreatorCamera camera in _cameras)
            {
                foreach (string category in camera.Categories)
                {
                    if (!_camerasByCategory.ContainsKey(category))
                        _camerasByCategory.Add(category, camera);
                }
            }
        }
    }
}