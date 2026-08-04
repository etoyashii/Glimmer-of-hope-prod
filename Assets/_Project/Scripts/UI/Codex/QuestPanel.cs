using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GlimmerOfHope.UI.BookMenu;
using GlimmerOfHope.UI.BookMenu.Data;

namespace GlimmerOfHope.UI.BookMenu.Panels
{
    public class QuestsPanel : MonoBehaviour, IBookPage
    {
        #region Private Fields

        [Header("Left Page: Zone Progress")]
        [SerializeField] private GameObject _zoneRowPrefab;
        [SerializeField] private Transform _zonesContainer;
        [SerializeField] private List<ZoneProgressData> _zones = new List<ZoneProgressData>();

        [Header("Right Page: Quests")]
        [SerializeField] private TMP_Text _mainQuestText;
        [SerializeField] private Transform _sideQuestsContainer;
        [SerializeField] private GameObject _sideQuestRowPrefab;
        [SerializeField] private List<string> _mainQuest = new List<string>();
        [SerializeField] private List<string> _sideQuests = new List<string>();

        #endregion

        #region Public Methods

        public void OnPageShown() => Refresh();

        public void Refresh()
        {
            for (int i = _zonesContainer.childCount - 1; i >= 0; i--)
                Destroy(_zonesContainer.GetChild(i).gameObject);

            foreach (var zone in _zones)
            {
                var zoneInstance = Instantiate(_zoneRowPrefab, _zonesContainer);
                zoneInstance.GetComponent<ZoneRow>()?.Setup(zone.ZoneName, zone.CompletionPercent);
            }

            if (_mainQuestText != null)
                _mainQuestText.text = _mainQuest.Count > 0
                    ? string.Join("\n", _mainQuest)
                    : "No active main quest";

            for (int i = _sideQuestsContainer.childCount - 1; i >= 0; i--)
                Destroy(_sideQuestsContainer.GetChild(i).gameObject);

            foreach (var quest in _sideQuests)
            {
                var questInstance = Instantiate(_sideQuestRowPrefab, _sideQuestsContainer);
                var text = questInstance.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = "• " + quest;
            }
        }

        #endregion
    }
}