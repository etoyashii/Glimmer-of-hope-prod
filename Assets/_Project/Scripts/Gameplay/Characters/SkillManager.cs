using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    [Serializable]
    public class SkillUnlock
    {
        public string _skillName;
        public bool _isUnlocked;
    }

    public class SkillManager : MonoBehaviour
    {
        #region Enums
        
        public enum SkillType
        {
            IncreaseLight,
            DecreaseLight,
            Jump,
            Pull,
            Push,
            PlatformMaker,
            Swim,
            EmotionCheck,
            Climb,
            Warm,
            DestroyBlock,
            ShadowClone,
            Slide,
            Cold,
            Propulsion,
            Planer
        }
        #endregion

        #region SerializeFields

        [Header("Skills unlocked")]
        [SerializeField] private List<SkillUnlock> _learningSkillList;

        #endregion

        #region Events

        public event Action<int> _skillTypeUnlocked;

        #endregion

        #region Public Methods

        private void Start()
        {
            PlayerSignalManager.Instance.OnSkillLearn += UnlockSkill;
        }
        private void Awake()
        {
        }

        private void OnDisable()
        {
            PlayerSignalManager.Instance.OnSkillLearn -= UnlockSkill;
        }

        public void UnlockSkill(int skillType)
        {
            _learningSkillList[skillType]._isUnlocked = true;
            _skillTypeUnlocked?.Invoke(skillType);
        }

        #endregion

        #region Helpers

        public bool IsSkillUnlocked(int skillIndex)
        {
            return _learningSkillList[skillIndex]._isUnlocked;
        }

        #endregion
    }
}