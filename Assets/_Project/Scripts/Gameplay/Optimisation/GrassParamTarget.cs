using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Composant "pont" léger destiné à RESTER dans la scène après suppression du
    /// générateur d'UI (PointGrassDebugPanelGenerator).
    ///
    /// Un UnityEvent persistant ne peut appeler qu'une vraie méthode publique sur un
    /// composant — pas une lambda / un champ directement. Ce composant expose donc,
    /// pour chaque paramètre réglable, une méthode à un seul argument float que les
    /// sliders peuvent cibler de façon persistante (visible et sérialisée dans
    /// l'Inspector, contrairement à un simple AddListener(lambda) qui disparaît à la
    /// fermeture du Play Mode).
    /// </summary>
    public class GrassParamTarget : MonoBehaviour
    {
        public PointGrassRenderer target;

        public void SetPointCount(float value) { if (target != null) { target.pointCount = value; } }
        public void SetPointLODFactor(float value) { if (target != null) { target.pointLODFactor = value; } }
        public void SetMaxRenderDistance(float value) { if (target != null) { target.maxRenderDistance = value; } }
        public void SetFadeStartDistance(float value) { if (target != null) { target.fadeStartDistance = value; } }
        public void SetDensityCutoff(float value) { if (target != null) { target.densityCutoff = value; } }
    }
}