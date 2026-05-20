using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GlimmerOfHope.UI.Utils
{
    public class UIManager : MonoBehaviour
    {
        #region SerializeFields

        [Header("References")]
        [SerializeField] private List<GameObject> _panelList;
        [SerializeField] private TextMeshProUGUI _spellModeText;

        #endregion

        #region Private Fields

        private bool _isMenuPanelActive = false;
        private bool _isSpellMode1 = true;

        #endregion

        #region Public Methods

        public void ToggleUI()
        {
            foreach (GameObject panel in _panelList)
            {
                panel.SetActive(!panel.activeSelf);
            }
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("LV_Main");
        }

        public void UpdateSpellModeText()
        {
            if (_isSpellMode1)
            {
                _spellModeText.text = "Spell Mode: 2";
                _isSpellMode1 = false;
            }
            else
            {
                _spellModeText.text = "Spell Mode: 1";
                _isSpellMode1 = true;
            }
        }
        #endregion
    }
}
