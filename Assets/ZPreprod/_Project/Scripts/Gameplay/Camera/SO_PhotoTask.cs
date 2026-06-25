/*using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.GCamera
{
    [CreateAssetMenu(fileName = "SO_PhotoTask", menuName = "Scriptable Objects/SO_PhotoTask")]

    ///<summary>
    ///SO for camera task
    ///Give a list of object name that will react to the camera
    /// </summary>
    public class SO_PhotoTask : ScriptableObject
    {
        #region Public Properties

        public string[] nameObject;

        #endregion

        #region Private Fields

        private bool _done = false;

        private Canvas _canvas;

        private List<TextMeshProUGUI> _txts;

        private Object _textOBJ;

        #endregion

        #region Public Methods
        public void Init(Canvas canva)
        {
            _txts = new List<TextMeshProUGUI>();

            _canvas = canva;

            _textOBJ = AssetDatabase.LoadAssetAtPath("Assets/_Project/Prefabs/UI/Camera/Text.prefab", typeof(GameObject));

            foreach (string _name in nameObject)
            {
                GameObject _text = Instantiate(_textOBJ, _canvas.transform) as GameObject;
                _text.name = _name;
                TextMeshProUGUI txt = _text.GetComponent<TextMeshProUGUI>();
                txt.color = Color.black;
                txt.text = _name;
                _txts.Add(txt);
            }
        }

        public void CheckName(string name, bool isVisible = true)
        {
            if (nameObject.Contains(name))
            {
                foreach (TextMeshProUGUI txt in _txts)
                {
                    if (txt.text == name)
                    {
                        if (isVisible)
                            txt.color = Color.green;
                        else
                            txt.color = Color.black;
                    }
                }
            }
        }

        public void SetTextVisibility(bool visible)
        {
            foreach (TextMeshProUGUI text in _txts)
            {
                text.gameObject.SetActive(visible);
            }
        }

        #endregion
    }
}*/
