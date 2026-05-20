using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class CameraIgnoreTrigger : MonoBehaviour
    {
        #region Unity Lifecycle

        void Start()
        {
            int triggersLayer = LayerMask.NameToLayer("Triggers");
            if (triggersLayer != -1)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    cam.cullingMask |= (1 << triggersLayer);
                    cam.eventMask &= ~(1 << triggersLayer);
                }
            }
        }

        #endregion
    }
}
