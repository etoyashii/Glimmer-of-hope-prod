using System;
using System.Collections.Generic;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.UI.Widgets;
using UnityEngine;

namespace GlimmerOfHope.UI
{
    public class CharacterCategoryGroupsManager : MonoBehaviour
    {
        [SerializeField] private CharacterCategoryGroups _defaultCategoryGroup;
        [SerializeField] private string _defaultCategoryId;
        [SerializeField] private StringEventChannel _onCategorySelected;

        private CharacterCategoryGroupButton[] _characterCategoryGroupButtons;

        private void Awake()
        {
            _characterCategoryGroupButtons = gameObject.GetComponentsInChildren<CharacterCategoryGroupButton>();

            foreach (CharacterCategoryGroupButton characterCategoryButton in _characterCategoryGroupButtons)
                characterCategoryButton.OnSelect += SelectCategoryGroup;
            _onCategorySelected.Raise(_defaultCategoryId);
        }

        void Start()
        {
            SelectCategoryGroup(_defaultCategoryGroup, false);
        }

        private void SelectCategoryGroup(CharacterCategoryGroups groups)
        {
            SelectCategoryGroup(groups, true);
        }

        private void SelectCategoryGroup(CharacterCategoryGroups characterCategoryGroup, bool animate)
        {
            foreach (CharacterCategoryGroupButton characterCategoryGroupButton in _characterCategoryGroupButtons)
            {
                if (characterCategoryGroup == characterCategoryGroupButton.CategoryGroup)
                    characterCategoryGroupButton.ShowSubMenu(animate);
                else
                    characterCategoryGroupButton.HideSubMenu(animate);
            }
        }
    }
}
