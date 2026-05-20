using UnityEngine;
using System.Collections.Generic;
using GlimmerOfHope.Gameplay.ScriptableObjects;
using System;

namespace GlimmerOfHope.Gameplay.World
{
    public class Stele : MonoBehaviour
    {
        #region SerializeFields

        [SerializeField] private SO_Stele _stele;
        [SerializeField] private GameObject _glyphPanel;
        [SerializeField] private GameObject _glyphPrefab;
        [SerializeField] private List<GameObject> _obstacleList;
        [SerializeField] private float _offsetXEachTurn = 10f;
        [SerializeField] private Vector3 _offset = new Vector3(-1f, 0f, -1f);

        #endregion

        #region Private Field

        private List<GameObject> _glyphs;
        private List<GlyphInteraction> _glyphInteractions;
        private List<SO_Glyphe> _currentGlyphesRuntime;

        private int _currentLevel = 0;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _glyphs = new List<GameObject>();
            _currentGlyphesRuntime = new List<SO_Glyphe>(_stele.CurrentGlyphes);

            InstantiateAllGlyphAtSpawn();
        }

        #endregion

        #region Public Methods

        public void PlaceGlyphe(SO_Glyphe glyphe, int index)
        {
            if (glyphe == null || index < 0 || index >= _stele.CurrentGlyphes.Count) return;

            _currentGlyphesRuntime[index] = glyphe;

            CheckGlyphes();
        }

        public void CheckGlyphes()
        {
            for (int i = 0; i < _stele.ExpectedGlyphes.Count; i++)
            {
                if (_currentGlyphesRuntime[i] != _stele.ExpectedGlyphes[i])
                {
                    Debug.Log("Bad Glyphs !");
                    return;
                }
            }

            Debug.Log("Correct Glyphs !");
            
            UnlockLevel();

            //Do whatever
        }

        #endregion

        #region Private Methods

        private void UnlockLevel()
        {
            switch (_currentLevel)
            {
                case 0:
                    Destroy(_obstacleList[0]);
                    break;
            }

            _currentLevel++;
        }

        private void InstantiateAllGlyphAtSpawn()
        {
            _glyphs = new List<GameObject>();
            _glyphInteractions = new List<GlyphInteraction>();

            float offsetX = 0f;

            foreach (SO_Glyphe glyph in _currentGlyphesRuntime)
            {
                Vector3 position = new Vector3(transform.position.x + _offset.x + offsetX, transform.position.y + _offset.y, transform.position.z + _offset.z);

                GameObject newGlyph = Instantiate(_glyphPrefab, position, transform.rotation);
                newGlyph.transform.SetParent(transform);

                SpriteRenderer renderer = newGlyph.GetComponent<SpriteRenderer>();
                if (renderer != null)
                    renderer.sprite = glyph.Sprite;

                GlyphInteraction interaction = newGlyph.AddComponent<GlyphInteraction>();
                interaction.Glyph = glyph;
                interaction.OnGlyphClicked += ChangeGlyphClicked;

                _glyphs.Add(newGlyph);
                _glyphInteractions.Add(interaction);

                offsetX += _offsetXEachTurn;
            }
        }

        private void ChangeGlyphClicked(SO_Glyphe clickedGlyphe)
        {
            if (ExchangeGlyphManager.Instance.SelectedGlyph == null) return;

            for (int i = 0; i < _glyphInteractions.Count; i++)
            {
                if (_glyphInteractions[i].Glyph == clickedGlyphe)
                {
                    GlyphInteraction glyphInteraction = _glyphInteractions[i];

                    glyphInteraction.Glyph = ExchangeGlyphManager.Instance.SelectedGlyph;
                    SpriteRenderer sr = glyphInteraction.GetComponent<SpriteRenderer>();
                    if (sr != null)
                        sr.sprite = ExchangeGlyphManager.Instance.SelectedGlyph.Sprite;

                    _currentGlyphesRuntime[i] = ExchangeGlyphManager.Instance.SelectedGlyph;

                    ExchangeGlyphManager.Instance.SelectedGlyph = null;
                    print("exchange");

                    CheckGlyphes();

                    break;
                }
            }
        }

        #endregion
    }
}
