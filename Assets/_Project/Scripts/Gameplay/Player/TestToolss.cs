using System;
using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core;
using GlimmerOfHope.Editor;

namespace GlimmerOfHope.Gameplay
{

    /// <summary>
    /// 
    /// </summary>
    public class TestToolss : MonoBehaviour
    {

        #region Constants
        #endregion

        #region Serialized Fields
        [Slider(0.0f, 10.0f, SliderColor.Red)]
        [SerializeField] private float _quoicoubeh;

        [Slider(0, 100, SliderColor.Green)]
        [SerializeField] private int HP;

        [TexturePreview]
        public Sprite icon;

        [TexturePreview]
        public Texture2D tex;

        [TexturePreview]
        public Material mat;

        [AudioPlayer]
        public AudioClip clip;

        public enum GuideType
        {
            TypeA,
            TypeB
        }

        public GuideType type;

        [EnumSectionBegin(nameof(type), GuideType.TypeA)]
        public int fieldTypeA;

        [EnumSectionBegin(nameof(type), GuideType.TypeB)]
        public int fieldTypeB;

        [EnumSectionEnd] //Utile seulement si variables shared a rajouter en dessous. Sinon supprimez.

        private int privateSharedField;
        public int sharedField;
        #endregion

        #region Public Properties
        #endregion

        #region Events
        #endregion

        #region Private Fields
        #endregion

        #region Unity Lifecycle

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        #endregion

        #region Public Methods
        #endregion

        #region Private Methods
        #endregion
    }
}