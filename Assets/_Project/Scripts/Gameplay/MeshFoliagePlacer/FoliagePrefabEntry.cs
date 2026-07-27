using UnityEngine;
using System.Collections.Generic;
using static GlimmerOfHope.Gameplay.MassiveFoliageMeshPlacer;

namespace GlimmerOfHope.Gameplay
{

    [System.Serializable]
    public class FoliagePrefabEntry
    {
        #region public Properties
        public GameObject prefab;

        [Range(1, MassiveFoliageMeshPlacer.MAX_AMOUNT)]
        [Tooltip("Contrôle la probabilité de spawn de ce prefab quand il est choisi sur une cellule valide.")]
        public int density = 50;

        [Range(0, 1)]
        public float fallOff = 0.8f;

        [Tooltip("full = toute la zone couverte par la texture. sides = seulement les bords/transitions de la texture.")]
        public FillType fillType = FillType.full;

        [Tooltip("Échelle uniforme min/max appliquée aléatoirement à chaque instance.")]
        public Vector2 uniformScaleRange = new Vector2(0.85f, 1.15f);

        public bool randomRotationY = true;

        [Tooltip("Rayon utilisé pour la vérification de collision si le prefab n'a aucun Collider.")]
        public float collisionCheckRadius = 0.5f;

        [Header("Pente")]
        [Range(0, 90)]
        [Tooltip("Angle de pente minimum (degrés) requis pour autoriser le spawn ici.")]
        public float minSlope = 0f;

        [Range(0, 90)]
        [Tooltip("Angle de pente maximum (degrés) autorisé pour le spawn ici.")]
        public float maxSlope = 45f;
        #endregion
    }
}
