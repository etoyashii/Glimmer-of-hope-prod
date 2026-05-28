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

        // ── 2. Passe 1 : effBase — radius à la base de chaque branche ──
        // Propagé depuis la racine, tronc = BaseRadius global
        float[] effBase = new float[count];
        float[] branchRnd = new float[count]; // multiplicateur random par branche

        effBase[0] = BaseRadius;
        branchRnd[0] = 1f;

        // On itère en ordre croissant d'index (le tri par depth dans le générateur
        // garantit que le parent a toujours un index < enfant)
        for (int i = 1; i < count; i++)
        {
            int p = parentOf[i];
            int knotIdx = parentKnotIndex[i];
            float knotCount = container.Splines[p].Count - 1;
            float tOnParent = knotCount > 0 ? (float)knotIdx / knotCount : 0f;

            // Radius du parent à ce t (avec sa TaperCurve)
            float parentRadiusAtT = Mathf.Lerp(effBase[p], TipRadius, TaperCurve.Evaluate(tOnParent));

            // Variation aléatoire : chaque branche est légèrement différente
            float variance = 1f + ((float)(rng.NextDouble() * 2 - 1)) * RadiusVariance;
            branchRnd[i] = variance;
            effBase[i] = parentRadiusAtT * variance;
        }

        // ── 3. Passe 2 : effTip — radius à la pointe de chaque branche ──
        // Règle : tip = base du fils le plus lointain (knot le plus grand)
        //         tip = TipRadius si feuille (aucun enfant)
        float[] effTip = new float[count];
        int[] farthestKnot = new int[count];
        int[] farthestChild = new int[count];

        for (int i = 0; i < count; i++) { effTip[i] = TipRadius; farthestKnot[i] = -1; farthestChild[i] = -1; }

        for (int i = 1; i < count; i++)
        {
            int p = parentOf[i];
            int knot = parentKnotIndex[i];

            if (knot > farthestKnot[p])
            {
                farthestKnot[p] = knot;
                farthestChild[p] = i;
            }
        }

        for (int i = 0; i < count; i++)
            if (farthestChild[i] != -1)
                effTip[i] = effBase[farthestChild[i]];

        // ── 4. Bake ───────────────────────────────────────────────────
        var allVerts = new List<Vector3>();
        var allUVs = new List<Vector2>();
        var allTris = new List<int>();

        for (int i = 0; i < count; i++)
            BakeSpline(container.Splines[i], effBase[i], effTip[i],
                       allVerts.Count, allVerts, allUVs, allTris);

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

    void BakeSpline(Spline spline, float baseR, float tipR, int vertexOffset,
                    List<Vector3> verts, List<Vector2> uvs, List<int> tris)
    {
        int segsVisible = Mathf.Max(1, Mathf.RoundToInt(Segments * GrowProgress));
        float growScale = GrowCurve.Evaluate(GrowProgress);

        for (int s = 0; s <= segsVisible; s++)
        {
            float tFull = (float)s / Segments;
            float tLocal = (float)s / segsVisible;
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

        for (int s = 0; s < segsVisible; s++)
            for (int v = 0; v < Sides; v++)
            {
                int a = vertexOffset + s * (Sides + 1) + v;
                int b = a + (Sides + 1);
                tris.Add(a); tris.Add(b); tris.Add(a + 1);
                tris.Add(b); tris.Add(b + 1); tris.Add(a + 1);
            }
    }
}