using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(SplineContainer))]
public class BranchMeshBuilder : MonoBehaviour
{
    [Header("Forme")]
    public int Segments = 14;
    public int Sides = 6;
    public float BaseRadius = 0.18f;
    public float TipRadius = 0.012f;

    [Header("Courbes")]
    [Tooltip("X = position le long de la branche (0=base, 1=pointe)  Y = multiplicateur du radius")]
    public AnimationCurve TaperCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [Tooltip("X = GrowProgress  Y = scale global du radius")]
    public AnimationCurve GrowCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Aléatoire")]
    [Tooltip("Variation aléatoire du radius par branche (0 = aucune, 0.3 = ±30%)")]
    [Range(0f, 0.5f)]
    public float RadiusVariance = 0.15f;

    [Tooltip("Graine aléatoire (doit correspondre à celle du générateur pour cohérence)")]
    public int RandomSeed = 42;

    [Tooltip("Multiplicateur de radius au pic du flare de jonction sur le parent (1 = pas de flare)")]
    [Range(1f, 2f)]
    public float JunctionSphereScale = 1.3f;

    [Tooltip("Largeur du flare, en fraction de la longueur du spline parent")]
    [Range(0.01f, 0.3f)]
    public float JunctionFlareWidth = 0.08f;

    [Tooltip("Fraction de la longueur de la branche enfant sur laquelle le col de jonction s'élargit depuis 0")]
    [Range(0.05f, 0.5f)]
    public float JunctionNeckLength = 0.2f;

    [Range(0f, 1f)]
    public float GrowProgress = 1f;

    [HideInInspector] public Mesh BakedMesh;

    // ─────────────────────────────────────────────────────────────────
    //  Build
    // ─────────────────────────────────────────────────────────────────

    public void BuildMesh()
    {
        var container = GetComponent<SplineContainer>();
        var meshFilter = GetComponent<MeshFilter>();
        int count = container.Splines.Count;
        if (count == 0) return;

        var rng = new System.Random(RandomSeed);

        // -- 1. Graphe parent depuis KnotLinkCollection --
        // parentOf[i]        = index du spline parent (-1 = tronc)
        // parentKnotIndex[i] = index du knot sur le parent où cette branche commence
        int[] parentOf = new int[count];
        int[] parentKnotIndex = new int[count];
        for (int i = 0; i < count; i++) { parentOf[i] = -1; parentKnotIndex[i] = 0; }

        for (int i = 1; i < count; i++)
        {
            var childKnot = new SplineKnotIndex(i, 0);
            if (!container.KnotLinkCollection.TryGetKnotLinks(childKnot, out var links))
                continue;

            foreach (var ki in links)
            {
                if (ki.Spline == i) continue;
                parentOf[i] = ki.Spline;
                parentKnotIndex[i] = ki.Knot;
                break;
            }
        }

        // -- 2 & 3. Radius basé sur la distance depuis la racine --
        //
        // distFromRoot[i]    = distance accumulée de la racine jusqu'au début de cette branche
        // farthestThrough[i] = distance jusqu'à la feuille la plus lointaine qui passe PAR cette branche
        //
        // Le gradient BaseRadius→TipRadius est normalisé par farthestThrough[i]
        // -> chaque branche mappe son gradient sur son propre sous-arbre,
        //   en restant cohérent avec la taille de ses descendants.

        float[] distFromRoot = new float[count];
        distFromRoot[0] = 0f;

        for (int i = 1; i < count; i++)
        {
            int p = parentOf[i];
            int knotIdx = parentKnotIndex[i];
            float knotCount = container.Splines[p].Count - 1;
            float tOnParent = knotCount > 0 ? (float)knotIdx / knotCount : 0f;
            float parentLen = container.Splines[p].GetLength();
            distFromRoot[i] = distFromRoot[p] + parentLen * tOnParent;
        }

        float[] farthestThrough = new float[count];
        for (int i = 0; i < count; i++)
            farthestThrough[i] = distFromRoot[i] + container.Splines[i].GetLength();

        for (int i = count - 1; i >= 1; i--)
        {
            int p = parentOf[i];
            if (p >= 0 && farthestThrough[i] > farthestThrough[p])
                farthestThrough[p] = farthestThrough[i];
        }

        float[] effBase = new float[count];
        float[] effTip = new float[count];

        for (int i = 0; i < count; i++)
        {
            float splineLen = container.Splines[i].GetLength();
            float norm = farthestThrough[i];
            if (norm <= 0f) norm = 1f;

            float tBase = distFromRoot[i] / norm;
            float tTip = (distFromRoot[i] + splineLen) / norm;

            float variance = 1f - Mathf.Abs((float)(rng.NextDouble() * 2 - 1)) * RadiusVariance;

            effBase[i] = Mathf.Lerp(BaseRadius, TipRadius, tBase) * variance;
            effTip[i] = Mathf.Lerp(BaseRadius, TipRadius, tTip) * variance;
        }

        // - 4. Table de jonctions par spline parent (pour le flare) -
        // Pour chaque spline, liste des (tOnSpline, childBaseRadius) de ses enfants directs.
        var junctionsPerSpline = new List<(float t, float childR)>[count];
        for (int i = 0; i < count; i++)
            junctionsPerSpline[i] = new List<(float, float)>();

        for (int i = 1; i < count; i++)
        {
            int p = parentOf[i];
            if (p < 0) continue;
            float knotCount = container.Splines[p].Count - 1;
            float tOnParent = knotCount > 0 ? (float)parentKnotIndex[i] / knotCount : 0f;
            junctionsPerSpline[p].Add((tOnParent, effBase[i]));
        }

        // - 5. Bake -
        var allVerts = new List<Vector3>();
        var allUVs = new List<Vector2>();
        var allTris = new List<int>();

        for (int i = 0; i < count; i++)
        {
            BakeSpline(container.Splines[i], effBase[i], effTip[i],
                       parentOf[i] != -1, allVerts.Count, allVerts, allUVs, allTris,
                       junctionsPerSpline[i]);
        }

        if (BakedMesh == null)
            BakedMesh = new Mesh { name = "Branches_" + gameObject.name };
        else
            BakedMesh.Clear();

        BakedMesh.SetVertices(allVerts);
        BakedMesh.SetUVs(0, allUVs);
        BakedMesh.SetTriangles(allTris, 0);
        BakedMesh.RecalculateNormals();
        BakedMesh.RecalculateBounds();

        meshFilter.mesh = BakedMesh;
    }

    // ─────────────────────────────────────────────────────────────────
    //  Bake un spline
    // ─────────────────────────────────────────────────────────────────

    void BakeSpline(Spline spline, float baseR, float tipR,
                    bool isChild, int vertexOffset,
                    List<Vector3> verts, List<Vector2> uvs, List<int> tris,
                    List<(float t, float childR)> junctions)
    {
        int segsVisible = Mathf.Max(1, Mathf.RoundToInt(Segments * GrowProgress));
        // Nombre d'anneaux du col : le radius part de ~0 et s'élargit sur cette distance.
        // Remplace le système de "sink ring" qui générait des étoiles et des becs.
        int neckLen = isChild ? Mathf.Max(1, Mathf.RoundToInt(segsVisible * JunctionNeckLength)) : 0;

        for (int s = 0; s <= segsVisible; s++)
        {
            float tFull = (float)s / segsVisible;
            float taper = TaperCurve.Evaluate(tFull);
            float radius = Mathf.Lerp(tipR, baseR, taper);

            // Col de jonction : ease-in quadratique de ~0 -> 1 sur neckLen anneaux.
            // Le radius quasi-nul à la base est caché par le flare du parent.
            if (s < neckLen)
            {
                float neckT = (float)s / neckLen;
                radius *= Mathf.Max(neckT * neckT, 0.01f);
            }

            // Flare gaussien centré sur chaque point d'attache d'un enfant.
            float flare = 0f;
            foreach (var (jt, _) in junctions)
            {
                float dist = Mathf.Abs(tFull - jt);
                float bell = Mathf.Exp(-(dist * dist) / (2f * JunctionFlareWidth * JunctionFlareWidth));
                flare = Mathf.Max(flare, (JunctionSphereScale - 1f) * bell);
            }
            radius *= (1f + flare);

            spline.Evaluate(tFull, out float3 pos, out float3 tangent, out float3 up);

            if (math.lengthsq(tangent) < 1e-6f) tangent = math.forward();
            tangent = math.normalize(tangent);

            float3 safeUp = math.abs(math.dot(up, tangent)) > 0.99f
                ? (math.abs(tangent.y) < 0.9f ? math.up() : math.right())
                : up;

            float3 right = math.normalize(math.cross(tangent, safeUp));
            float3 trueUp = math.cross(right, tangent);

            for (int v = 0; v <= Sides; v++)
            {
                float angle = (float)v / Sides * math.PI * 2f;
                float3 offset = (math.cos(angle) * right + math.sin(angle) * trueUp) * radius;
                verts.Add((Vector3)(pos + offset));
                uvs.Add(new Vector2((float)v / Sides, tFull));
            }
        }

        for (int s = 0; s < segsVisible; s++)
            for (int v = 0; v < Sides; v++)
            {
                int a = vertexOffset + s * (Sides + 1) + v;
                int b = a + (Sides + 1);
                tris.Add(a); tris.Add(b); tris.Add(a + 1);
                tris.Add(b); tris.Add(b + 1); tris.Add(a + 1);
            }

        // Cone tip : ferme la pointe de chaque branche avec un vertex central.
        int lastRingStart = vertexOffset + segsVisible * (Sides + 1);
        spline.Evaluate(1f, out float3 tipPos, out _, out _);
        int apexIdx = verts.Count;
        verts.Add((Vector3)tipPos);
        uvs.Add(new Vector2(0.5f, 1f));
        for (int v = 0; v < Sides; v++)
        {
            tris.Add(lastRingStart + v);
            tris.Add(apexIdx);
            tris.Add(lastRingStart + v + 1);
        }
    }
}
