using DG.Tweening.Plugins.Core.PathCore;
using System;
using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// The script is used forheritage logic that allow to call every LightReaction children (like FlowerLightReaction)
    /// </summary>
    public class LightReaction : MonoBehaviour
    {
        #region PublicVirtualMethods

        public virtual void PerformLight(){}

        public virtual void PerformUnlight(){}

        #endregion
    }
}
