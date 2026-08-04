using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.UI.BookMenu;
using GlimmerOfHope.UI.BookMenu.Data;

namespace GlimmerOfHope.UI.BookMenu.Panels
{
    public class PartitionsPanel : MonoBehaviour, IBookPage
    {
        #region Private Fields

        [Header("Row Prefabs")]
        [Tooltip("Left page row: thumbnail + title + description")]
        [SerializeField] private GameObject _infoRowPrefab;
        [Tooltip("Right page row: row of colored dots")]
        [SerializeField] private GameObject _sequenceRowPrefab;

        [Header("Containers (with a Vertical Layout Group)")]
        [SerializeField] private Transform _leftListContainer;
        [SerializeField] private Transform _rightListContainer;

        [Header("Data")]
        [SerializeField] private List<PartitionData> _partitions = new List<PartitionData>();

        #endregion

        #region Public Methods

        public void OnPageShown() => Refresh();

        public void Refresh()
        {
            ClearContainer(_leftListContainer);
            ClearContainer(_rightListContainer);

            foreach (var partition in _partitions)
            {
                var infoInstance = Instantiate(_infoRowPrefab, _leftListContainer);
                infoInstance.GetComponent<PartitionInfoRow>()?.Setup(partition);

                var sequenceInstance = Instantiate(_sequenceRowPrefab, _rightListContainer);
                sequenceInstance.GetComponent<PartitionSequenceRow>()?.Setup(partition.Sequence);
            }
        }

        #endregion

        #region Private Methods

        private void ClearContainer(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        #endregion
    }
}