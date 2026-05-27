using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Génération procédurale de splines en forme de branches arborescentes.
/// Nécessite le package com.unity.splines (Unity Splines).
/// Clic droit sur le composant > "Generate Branches" pour générer.
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(SplineContainer))]
public class ProceduralBranchSplines : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // BRANCHE PRINCIPALE
    // ─────────────────────────────────────────────
    [Header("Branche principale")]
    [Tooltip("Nombre de points (knots) par branche.")]
    public int pointsPerBranch = 12;

    [Tooltip("Distance entre chaque point consécutif.")]
    public float distanceBetweenPoints = 0.4f;

    [Tooltip("Direction initiale du tronc.")]
    public Vector3 initialDirection = Vector3.up;

    // ─────────────────────────────────────────────
    // SOUS-BRANCHES
    // ─────────────────────────────────────────────
    [Header("Sous-branches")]
    [Tooltip("Nombre de sous-branches qui partent à chaque point de séparation.")]
    public int subBranchCount = 3;

    [Tooltip("Position normalisée [0-1] le long de la branche où commence la séparation.")]
    [Range(0f, 1f)]
    public float branchSplitPosition = 0.5f;

    [Tooltip("Angle d'ouverture des sous-branches par rapport à la direction parent (degrés).")]
    [Range(5f, 90f)]
    public float branchAngleSpread = 35f;

    [Tooltip("Variation aléatoire de l'angle de séparation (± degrés).")]
    [Range(0f, 30f)]
    public float branchAngleVariance = 10f;

    [Tooltip("Profondeur de récursion (0 = tronc seulement, 1 = 1 niveau de branches, etc.).")]
    [Range(0, 4)]
    public int recursionDepth = 2;

    // ─────────────────────────────────────────────
    // TORSION & BRUIT
    // ─────────────────────────────────────────────
    [Header("Torsion & Bruit")]
    [Tooltip("Déviation angulaire maximale par étape (slope max, degrés).")]
    [Range(0f, 90f)]
    public float maxSlopeDeg = 20f;

    [Tooltip("Intensité globale du bruit de Perlin appliqué à la direction.")]
    [Range(0f, 2f)]
    public float noiseForce = 0.4f;

    [Tooltip("Fréquence spatiale du bruit (valeur haute = changements plus rapides).")]
    [Range(0.1f, 5f)]
    public float noiseFrequency = 1.2f;

    [Tooltip("Vitesse de torsion axiale (rotation de la branche sur elle-même à chaque pas).")]
    [Range(0f, 90f)]
    public float twistDegreesPerStep = 15f;

    [Tooltip("Force de la gravité appliquée à chaque branche (0 = droit, 1 = très courbé vers le bas).")]
    [Range(0f, 1f)]
    public float gravityInfluence = 0.08f;

    // ─────────────────────────────────────────────
    // ATTÉNUATION PAR GÉNÉRATION
    // ─────────────────────────────────────────────
    [Header("Atténuation par génération")]
    [Tooltip("Multiplicateur de longueur entre chaque niveau de récursion (< 1 = branches plus courtes).")]
    [Range(0.3f, 1f)]
    public float lengthMultiplierPerDepth = 0.65f;

    [Tooltip("Multiplicateur du nombre de points par niveau de récursion.")]
    [Range(0.3f, 1f)]
    public float pointCountMultiplierPerDepth = 0.75f;

    // ─────────────────────────────────────────────
    // PARAMÈTRES AVANCÉS
    // ─────────────────────────────────────────────
    [Header("Paramètres avancés")]
    [Tooltip("Graine aléatoire pour la reproductibilité.")]
    public int seed = 42;

    [Tooltip("Régénère automatiquement quand un paramètre change (peut être lent avec beaucoup de branches).")]
    public bool autoRegenerate = false;

    [Tooltip("Tension des tangentes de la spline (0 = linéaire, 1 = très courbe).")]
    [Range(0f, 1f)]
    public float splineTension = 0.4f;

    // ─────────────────────────────────────────────
    // ÉTAT INTERNE
    // ─────────────────────────────────────────────
    private SplineContainer _container;
    private System.Random _rng;

    // ─────────────────────────────────────────────
    // UNITY
    // ─────────────────────────────────────────────
    private void OnValidate()
    {
        if (autoRegenerate)
            // Délai d'une frame pour éviter les appels pendant la sérialisation
            UnityEditor.EditorApplication.delayCall += Generate;
    }

    [ContextMenu("Generate Branches")]
    public void Generate()
    {
        _container = GetComponent<SplineContainer>();
        if (_container == null)
            _container = gameObject.AddComponent<SplineContainer>();

        _rng = new System.Random(seed);

        // Supprimer toutes les splines existantes
        int count = _container.Splines.Count;
        for (int i = count - 1; i >= 0; i--)
            _container.RemoveSplineAt(i);

        // Lancer la génération récursive depuis la racine
        GenerateBranch(
            origin: Vector3.zero,
            direction: initialDirection.normalized,
            depth: 0,
            numPoints: pointsPerBranch,
            stepDist: distanceBetweenPoints
        );
    }

    // ─────────────────────────────────────────────
    // GÉNÉRATION RÉCURSIVE D'UNE BRANCHE
    // ─────────────────────────────────────────────
    private void GenerateBranch(Vector3 origin, Vector3 direction, int depth, int numPoints, float stepDist)
    {
        if (depth > recursionDepth) return;

        var knots = new List<BezierKnot>();

        Vector3 pos = origin;
        Vector3 dir = direction.normalized;

        // Décalage aléatoire dans le bruit de Perlin pour éviter les branches identiques
        float noiseOffsetX = (float)(_rng.NextDouble() * 1000f);
        float noiseOffsetZ = (float)(_rng.NextDouble() * 1000f);

        // Torsion initiale aléatoire
        float twistAccum = (float)(_rng.NextDouble() * 360f);

        int splitIndex = Mathf.RoundToInt(branchSplitPosition * (numPoints - 1));
        splitIndex = Mathf.Clamp(splitIndex, 1, numPoints - 2);

        for (int i = 0; i < numPoints; i++)
        {
            float t = (float)i / Mathf.Max(numPoints - 1, 1);

            // ── Bruit de Perlin pour dévier la direction ──
            float nx = (Mathf.PerlinNoise(t * noiseFrequency + noiseOffsetX, 0.3f) - 0.5f) * 2f;
            float nz = (Mathf.PerlinNoise(0.7f, t * noiseFrequency + noiseOffsetZ) - 0.5f) * 2f;
            Vector3 noiseVec = new Vector3(nx, 0f, nz) * noiseForce;

            // ── Gravité ──
            noiseVec += Vector3.down * gravityInfluence;

            // ── Torsion axiale : on fait tourner le vecteur bruit autour de la direction ──
            twistAccum += twistDegreesPerStep * (float)(_rng.NextDouble() * 2 - 1);
            Quaternion twist = Quaternion.AngleAxis(twistAccum, dir);
            noiseVec = twist * noiseVec;

            // ── Application et clamp du slope ──
            Vector3 newDir = (dir + noiseVec).normalized;
            float angle = Vector3.Angle(dir, newDir);
            if (angle > maxSlopeDeg)
                newDir = Vector3.RotateTowards(dir, newDir, maxSlopeDeg * Mathf.Deg2Rad, 0f).normalized;

            dir = newDir;

            // ── Ajout du knot ──
            float3 knotPos = (float3)(Vector3)pos;
            float tangentLen = stepDist * splineTension;
            float3 tangentFwd = (float3)(dir * tangentLen);

            knots.Add(new BezierKnot(knotPos, -tangentFwd, tangentFwd, quaternion.identity));

            // ── Point de séparation : générer les sous-branches ──
            if (i == splitIndex && depth < recursionDepth)
                SpawnSubBranches(pos, dir, depth + 1, stepDist);

            pos += dir * stepDist;
        }

        // Ajouter la spline au container
        var spline = new Spline(knots, false);
        _container.AddSpline(spline);
    }

    // ─────────────────────────────────────────────
    // CRÉATION DES SOUS-BRANCHES
    // ─────────────────────────────────────────────
    private void SpawnSubBranches(Vector3 origin, Vector3 parentDir, int depth, float parentStepDist)
    {
        // Vecteur perpendiculaire de référence pour la distribution angulaire
        Vector3 perp = Vector3.Cross(parentDir, Vector3.up).normalized;
        if (perp.sqrMagnitude < 0.001f)
            perp = Vector3.Cross(parentDir, Vector3.right).normalized;

        float angleStep = 360f / subBranchCount;

        for (int b = 0; b < subBranchCount; b++)
        {
            // Distribution uniforme + jitter aléatoire
            float jitter = (float)((_rng.NextDouble() - 0.5) * angleStep * 0.4f);
            float rotAngle = angleStep * b + jitter;

            // Rotation autour de l'axe parent pour obtenir le vecteur de déviation
            Quaternion rot = Quaternion.AngleAxis(rotAngle, parentDir);
            Vector3 spreadDir = rot * perp;

            // Mélange direction parent + direction spread selon l'angle de séparation
            float spread = branchAngleSpread + (float)((_rng.NextDouble() - 0.5) * branchAngleVariance * 2f);
            spread = Mathf.Clamp(spread, 5f, 175f);
            Vector3 branchDir = Vector3.Slerp(parentDir, spreadDir, Mathf.Sin(spread * Mathf.Deg2Rad)).normalized;

            // Paramètres atténués
            int childPoints = Mathf.Max(3, Mathf.RoundToInt(pointsPerBranch
                * Mathf.Pow(pointCountMultiplierPerDepth, depth)));
            float childStep = parentStepDist * Mathf.Pow(lengthMultiplierPerDepth, depth);

            GenerateBranch(origin, branchDir, depth, childPoints, childStep);
        }
    }
}
