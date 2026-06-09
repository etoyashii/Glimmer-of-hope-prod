using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Jai pas toucher ca seul dieux sait comment generer des mesh 
/// </summary>
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

    [Tooltip("Multiplicateur de taille de la sphère de jonction (1 = radius du parent à cet endroit)")]
    [Range(0.5f, 2f)]
    public float JunctionSphereScale = 1.1f;

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

        // ── 1. Construire le graphe parent depuis KnotLinkCollection ──
        // parentOf[i]        = index du spline parent (-1 = tronc)
        // parentKnotIndex[i] = index du knot sur le parent où cette branche commence
        int[] parentOf = new int[count];
        int[] parentKnotIndex = new int[count];
        for (int i = 0; i < count; i++) { parentOf[i] = -1; parentKnotIndex[i] = 0; }

        // Pour chaque branche enfant (i > 0), on interroge son knot[0]
        // pour trouver a quel knot du parent il est lie via KnotLinkCollection
        for (int i = 1; i < count; i++)
        {
            var childKnot = new SplineKnotIndex(i, 0);

            if (!container.KnotLinkCollection.TryGetKnotLinks(childKnot, out var links))
                continue;

            foreach (var ki in links)
            {
                if (ki.Spline == i) continue; // ignore le knot de l'enfant lui-meme

                parentOf[i] = ki.Spline;
                parentKnotIndex[i] = ki.Knot;
                break;
            }
        }

        // ── 2 & 3. Radius basé sur la distance depuis la racine ─────────
        //
        // distFromRoot[i]    = distance accumulée de la racine jusqu'au début de cette branche
        // farthestThrough[i] = distance jusqu'à la feuille la plus lointaine qui passe PAR cette branche
        //
        // Le gradient BaseRadius→TipRadius est normalisé par farthestThrough[i]
        // → chaque branche mappe son gradient sur son propre sous-arbre,
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

        // Initialise farthestThrough à la longueur propre de chaque spline
        float[] farthestThrough = new float[count];
        for (int i = 0; i < count; i++)
            farthestThrough[i] = distFromRoot[i] + container.Splines[i].GetLength();

        // Propagation bottom-up : chaque parent hérite du max de ses enfants
        // (tableau trié par depth → enfants après parents → on itère en sens inverse)
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
            float norm = farthestThrough[i]; // normalisation par rapport au sous-arbre
            if (norm <= 0f) norm = 1f;

            float tBase = distFromRoot[i] / norm;
            float tTip = (distFromRoot[i] + splineLen) / norm;

            float variance = 1f - Mathf.Abs((float)(rng.NextDouble() * 2 - 1)) * RadiusVariance;

            effBase[i] = Mathf.Lerp(BaseRadius, TipRadius, tBase) * variance;
            effTip[i] = Mathf.Lerp(BaseRadius, TipRadius, tTip) * variance;
        }

        // ── 4. Bake ───────────────────────────────────────────────────
        var allVerts = new List<Vector3>();
        var allUVs = new List<Vector2>();
        var allTris = new List<int>();

        for (int i = 0; i < count; i++)
        {
            // Pour les branches enfants : on recule le premier anneau dans le parent
            // pour couvrir le trou à la jonction (sink = enfoncement dans le parent)
            float sinkDistance = 0f;
            if (parentOf[i] != -1)
                sinkDistance = effBase[i]; // on recule d'un radius

            BakeSpline(container.Splines[i], effBase[i], effTip[i],
                       sinkDistance, allVerts.Count, allVerts, allUVs, allTris);
        }

        // ── 5. Sphères de jonction ───────────────────────────────────────
        // Pour chaque knot parent qui a au moins un enfant, on ajoute une sphère
        // qui remplit le vide entre le cylindre parent et les cylindres enfants.
        var junctionsDone = new System.Collections.Generic.HashSet<(int, int)>();

        for (int i = 1; i < count; i++)
        {
            int p = parentOf[i];
            int knot = parentKnotIndex[i];
            if (p < 0) continue;

            var key = (p, knot);
            if (junctionsDone.Contains(key)) continue;
            junctionsDone.Add(key);

            float knotCount = container.Splines[p].Count - 1;
            float t = knotCount > 0 ? (float)knot / knotCount : 0f;
            container.Splines[p].Evaluate(t, out Unity.Mathematics.float3 jPos, out _, out _);

            float jRadius = Mathf.Lerp(effBase[p], effTip[p], t) * JunctionSphereScale;
            BakeJunctionSphere((Vector3)jPos, jRadius, allVerts.Count, allVerts, allUVs, allTris);
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
                    float sinkDistance, int vertexOffset,
                    List<Vector3> verts, List<Vector2> uvs, List<int> tris)
    {
        int segsVisible = Mathf.Max(1, Mathf.RoundToInt(Segments * GrowProgress));
        float growScale = GrowCurve.Evaluate(GrowProgress);

        // Longueur totale de la spline pour convertir sinkDistance en t
        float splineLength = spline.GetLength();
        float sinkT = splineLength > 0f ? sinkDistance / splineLength : 0f;

        // +1 anneau supplémentaire au début si on a un sink (branche enfant)
        int extraRing = sinkT > 0f ? 1 : 0;
        int totalRings = segsVisible + extraRing;

        for (int s = 0; s <= totalRings; s++)
        {
            // s=0 avec extraRing : anneau "enfoncé" à -sinkT (dans le parent)
            // s=extraRing..totalRings : anneaux normaux de 0 à 1
            float tFull = extraRing > 0
                ? Mathf.Clamp01((float)(s - extraRing) / Segments + (s == 0 ? -sinkT : 0f))
                : (float)s / Segments;
            float tLocal = (float)s / totalRings;
            float taper = TaperCurve.Evaluate(tFull);
            float radius = Mathf.Lerp(tipR, baseR, taper) * growScale;

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
                uvs.Add(new Vector2((float)v / Sides, tLocal));
            }
        }

        for (int s = 0; s < totalRings; s++)
            for (int v = 0; v < Sides; v++)
            {
                int a = vertexOffset + s * (Sides + 1) + v;
                int b = a + (Sides + 1);
                tris.Add(a); tris.Add(b); tris.Add(a + 1);
                tris.Add(b); tris.Add(b + 1); tris.Add(a + 1);
            }
    }
    // ─────────────────────────────────────────────────────────────────
    //  Sphère de jonction — remplit le vide à chaque fork
    // ─────────────────────────────────────────────────────────────────

    void BakeJunctionSphere(Vector3 center, float radius, int vertexOffset,
                             List<Vector3> verts, List<Vector2> uvs, List<int> tris)
    {
        int rings = Sides;
        int slices = Sides;

        for (int r = 0; r <= rings; r++)
        {
            float phi = Mathf.PI * r / rings;
            for (int s = 0; s <= slices; s++)
            {
                float theta = 2f * Mathf.PI * s / slices;
                Vector3 p = center + new Vector3(
                    Mathf.Sin(phi) * Mathf.Cos(theta),
                    Mathf.Cos(phi),
                    Mathf.Sin(phi) * Mathf.Sin(theta)
                ) * radius;
                verts.Add(p);
                uvs.Add(new Vector2((float)s / slices, (float)r / rings));
            }
        }

        for (int r = 0; r < rings; r++)
            for (int s = 0; s < slices; s++)
            {
                int a = vertexOffset + r * (slices + 1) + s;
                int b = a + (slices + 1);
                tris.Add(a); tris.Add(a + 1); tris.Add(b);
                tris.Add(b); tris.Add(a + 1); tris.Add(b + 1);
            }
    }
}