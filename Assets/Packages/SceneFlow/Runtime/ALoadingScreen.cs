using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kebab.SceneFlow
{
    public abstract class ALoadingScreen : MonoBehaviour
    {
        protected virtual void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnEnable()
        {
            UpdateProgress(0f);
        }

        public abstract void Show();
        public abstract void Hide();
        public virtual bool IsVisible => gameObject.activeSelf;
        public virtual void OnScenePreloaded() {}
        public virtual void UpdateProgress(float progress) {}

        private void OnApplicationQuit()
        {
            DestroyImmediate(gameObject);
        }
    }
}