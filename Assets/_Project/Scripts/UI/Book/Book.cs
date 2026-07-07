using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace GlimmerOfHope.UI
{
    /// <summary>
    /// Script to manage the book
    /// </summary>
    public class Book : MonoBehaviour
    {
        #region SerializeField

        [Header("Reference")]
        [SerializeField] private GameObject _pagesParent;
        [SerializeField] private GameObject _buttonLeft;
        [SerializeField] private GameObject _buttonRight;

        [Header("Parametre")]
        [SerializeField] private float _pageSpeed = 0.5f;        

        #endregion

        #region Private value

        private int _index = -1;
        private List<Transform> _pages;
        private bool _isPerforming = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _pages = new List<Transform>();

            for (int i = _pagesParent.transform.childCount - 1; i >= 0; i--) //get all the pages. They need to be in descending order (3, 2, 1)
            {
                _pages.Add(_pagesParent.transform.GetChild(i).transform);
            }
        }
        private void Start()
        {
            _buttonLeft.SetActive(false);
        }

        #endregion

        #region Public Methods

        public void GoToPage(int pageId)
        {
            if (pageId-2 == _index || _isPerforming) return; //the -2 is for the diference between our number and the index number
            if (pageId-2 > _index)
            {
                RightAction(pageId - 2);

                StartCoroutine(Rotates(-180, true, _index + 1, pageId - 1));
            }
            else
            {
                LeftAction(pageId - 1);

                StartCoroutine(Rotates(0, false, pageId - 1, _index + 1));
            }
            _index = pageId-2;
        }

        public void RotateLeft()
        {
            if (_index == -1 || _isPerforming) return;

            LeftAction(_index);
            
            StartCoroutine(Rotate(0, false));
        }

        public void RotateRight()
        {
            if (_index == _pages.Count - 1 || _isPerforming) return;

            _index++;

            RightAction(_index);

            StartCoroutine(Rotate(-180, true));
        }

        #endregion

        #region Private Methods

        private void LeftAction(int index) //function to update the button
        {
            if (_buttonRight.activeInHierarchy == false)
                _buttonRight.SetActive(true);

            if (index <= 0)
                _buttonLeft.SetActive(false);
        }

        private void RightAction(int index) //function to update the button
        {
            if (_buttonLeft.activeInHierarchy == false)
                _buttonLeft.SetActive(true);

            if (index == _pages.Count - 1)
                _buttonRight.SetActive(false);
        }

        private IEnumerator Rotate(float angle, bool isForward) //rotate one page
        {
            _isPerforming = true;
            _pages[_index].SetAsLastSibling();
            float value = 0f;

            while (true)
            {
                Quaternion targetRotation = Quaternion.Euler(0f, angle, 0f);
                value += Time.deltaTime * _pageSpeed;
                _pages[_index].rotation = Quaternion.Slerp(_pages[_index].rotation, targetRotation, value);
                float angle1 = Quaternion.Angle(_pages[_index].rotation, targetRotation);
                if (angle1 < 0.1f)
                {
                    _pages[_index].rotation = targetRotation;

                    if (isForward == false)
                    {
                        _index--;
                    }
                    break;
                }
                yield return null;
            }
            _isPerforming = false;
        }

        private IEnumerator Rotates(float angle,bool forward, int startId, int lastId) //rotate a group of pages
        {
            _isPerforming = true;
            float value = 0f;
            bool switchSibling = true;

            while (true)
            {
                Quaternion targetRotation = Quaternion.Euler(0f, angle, 0f);
                value += Time.deltaTime * _pageSpeed;

                for (int i = startId; i < lastId; i++)
                    _pages[i].rotation = Quaternion.Slerp(_pages[i].rotation, targetRotation, value);

                float angle1 = Quaternion.Angle(_pages[startId].rotation, targetRotation);

                if (angle1 <= 90f && switchSibling)
                {
                    switchSibling = false;
                    if (forward)
                    {
                        for (int i = startId; i < lastId; i++)
                        {
                            _pages[i].SetAsLastSibling();
                        }
                    }
                    else
                    {
                        for (int i = lastId; i >= startId; i--)
                        {
                            if (i < _pages.Count)
                                _pages[i].SetAsLastSibling();
                        }
                    }                    
                }

                if (angle1 < 0.1f)
                {
                    for (int i = startId; i < lastId; i++)
                        _pages[i].rotation = targetRotation;

                    break;
                }
                yield return null;
            }
            _isPerforming = false;
        }

        #endregion
    }
}
