using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class GrassDebugPanelController : MonoBehaviour
    {
        public GameObject panelRoot;

        public void TogglePanel()
        {
            if (panelRoot == null) { return; }
            panelRoot.SetActive(!panelRoot.activeSelf);
        }
    }
}