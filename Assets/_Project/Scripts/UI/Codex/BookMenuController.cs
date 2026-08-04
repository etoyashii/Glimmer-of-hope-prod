using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.UI.BookMenu
{
    [Serializable]
    public class BookPageEntry
    {
        [Tooltip("Internal identifier")]
        public string Id;

        [Tooltip("Label shown as the bookmark tooltip")]
        public string Label;

        [Tooltip("Icon displayed on the bookmark tab")]
        public Sprite BookmarkIcon;

        [Tooltip("GameObject holding the pre-built double-page (left + right content)")]
        public GameObject PageRoot;

        [Tooltip("Optional: page script implementing IBookPage, refreshed when the page is shown")]
        public MonoBehaviour PanelController;
    }

    public interface IBookPage
    {
        void OnPageShown();
    }

    public class BookMenuController : MonoBehaviour
    {
        #region Public Properties

        public int CurrentIndex => _currentIndex;

        #endregion

        #region Private Fields

        [Header("Pages")]
        [Tooltip("One entry per double-page, in display order")]
        [SerializeField] private List<BookPageEntry> _pages = new List<BookPageEntry>();

        [Header("UI References")]
        [Tooltip("Root GameObject of the whole book (enabled/disabled on open/close)")]
        [SerializeField] private GameObject _bookRoot;
        [Tooltip("Horizontal Layout Group container for the bookmark tabs")]
        [SerializeField] private RectTransform _bookmarksContainer;
        [Tooltip("Prefab used for each bookmark tab (must have a BookmarkTab component)")]
        [SerializeField] private GameObject _bookmarkTabPrefab;
        [SerializeField] private Button _arrowLeftButton;
        [SerializeField] private Button _arrowRightButton;
        [Tooltip("CanvasGroup on the pages container, used for the fade transition")]
        [SerializeField] private CanvasGroup _pagesCanvasGroup;

        [Header("Animation")]
        [SerializeField] private float _transitionDuration = 0.18f;

        private int _currentIndex = 0;
        private bool _isTransitioning = false;
        private readonly List<BookmarkTab> _bookmarkTabs = new List<BookmarkTab>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            BuildBookmarks();

            if (_arrowLeftButton != null)
                _arrowLeftButton.onClick.AddListener(PreviousPage);
            if (_arrowRightButton != null)
                _arrowRightButton.onClick.AddListener(NextPage);

            for (int i = 0; i < _pages.Count; i++)
                if (_pages[i].PageRoot != null)
                    _pages[i].PageRoot.SetActive(false);
        }

        #endregion

        #region Public Methods

        public void BuildBookmarks()
        {
            foreach (Transform child in _bookmarksContainer)
                Destroy(child.gameObject);
            _bookmarkTabs.Clear();

            for (int i = 0; i < _pages.Count; i++)
            {
                int capturedIndex = i;
                GameObject tabInstance = Instantiate(_bookmarkTabPrefab, _bookmarksContainer);
                BookmarkTab tab = tabInstance.GetComponent<BookmarkTab>();
                tab.Setup(_pages[i].BookmarkIcon, _pages[i].Label, () => GoToPage(capturedIndex));
                _bookmarkTabs.Add(tab);
            }
        }

        public void OpenBook()
        {
            _bookRoot.SetActive(true);
            ShowPageImmediate(_currentIndex);
        }

        public void CloseBook()
        {
            _bookRoot.SetActive(false);
        }

        public void NextPage() => GoToPage(_currentIndex + 1);
        public void PreviousPage() => GoToPage(_currentIndex - 1);

        public void GoToPage(int index)
        {
            if (_isTransitioning) return;
            if (index < 0 || index >= _pages.Count) return;
            if (index == _currentIndex) return;

            StartCoroutine(TransitionTo(index));
        }

        #endregion

        #region Private Methods

        private IEnumerator TransitionTo(int index)
        {
            _isTransitioning = true;

            yield return Fade(1f, 0f);

            if (_pages[_currentIndex].PageRoot != null)
                _pages[_currentIndex].PageRoot.SetActive(false);

            _currentIndex = index;
            ShowPageImmediate(_currentIndex);

            yield return Fade(0f, 1f);

            _isTransitioning = false;
        }

        private void ShowPageImmediate(int index)
        {
            for (int i = 0; i < _pages.Count; i++)
                if (_pages[i].PageRoot != null)
                    _pages[i].PageRoot.SetActive(i == index);

            if (_pages[index].PanelController is IBookPage bookPage)
                bookPage.OnPageShown();

            UpdateBookmarkHighlight();

            if (_arrowLeftButton != null) _arrowLeftButton.interactable = index > 0;
            if (_arrowRightButton != null) _arrowRightButton.interactable = index < _pages.Count - 1;
        }

        private void UpdateBookmarkHighlight()
        {
            for (int i = 0; i < _bookmarkTabs.Count; i++)
                _bookmarkTabs[i].SetActiveState(i == _currentIndex);
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_pagesCanvasGroup == null) yield break;

            float t = 0f;
            while (t < _transitionDuration)
            {
                t += Time.unscaledDeltaTime;
                _pagesCanvasGroup.alpha = Mathf.Lerp(from, to, t / _transitionDuration);
                yield return null;
            }
            _pagesCanvasGroup.alpha = to;
        }

        #endregion
    }
}