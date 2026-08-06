using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Composant minimal qui reste dans la scène après suppression du générateur.
    /// Se contente d'ouvrir/fermer la box de réglages. Ciblé par un listener
    /// persistant sur le bouton d'ouverture.
    /// </summary>
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