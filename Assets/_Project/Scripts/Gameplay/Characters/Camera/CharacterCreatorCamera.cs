using System;
using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core;
using GlimmerOfHope.Gameplay;
using Unity.Cinemachine;
using NaughtyAttributes;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.UI
{

    public class CharacterCreatorCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CinemachineCamera _cinemachineCamera;
        [Header("Settings")]
        [SerializeField] private List<string> _categories;
        [Header("Rotate")]
        [SerializeField] private bool _rotateCamera = true;
        [ShowIf(nameof(_rotateCamera))]
        [SerializeField] float _rotationSpeed = 10;

        public List<string> Categories => _categories;
        private void Awake()
        {
            if (_rotateCamera)
                RotateCameraAsync(_cinemachineCamera.GetComponent<CinemachineOrbitalFollow>());
        }

        async void RotateCameraAsync(CinemachineOrbitalFollow orbitalFollow)
        {
            try
            {
                while (true)
                {
                    orbitalFollow.HorizontalAxis.Value += Time.deltaTime * _rotationSpeed;
                    await Awaitable.NextFrameAsync(cancellationToken: this.destroyCancellationToken);
                }
            }
            catch (OperationCanceledException) { }
        }

        public void SetActive(bool active)
        {
            _cinemachineCamera.enabled = active;
        }

    }
}