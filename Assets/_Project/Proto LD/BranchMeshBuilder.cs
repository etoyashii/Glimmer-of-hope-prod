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

    [Header("Courbe d'effilement")]
    [Tooltip("X = position le long de la branche (0=base, 1=pointe)  Y = multiplicateur du radius (0=fin, 1=plein)")]
    public AnimationCurve TaperCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Croissance")]
    [Range(0f, 1f)]
    public float GrowProgress = 1f;

    [Tooltip("Comment le radius grandit au fur et à mesure que la branche pousse (X=GrowProgress, Y=scale)")]
    public AnimationCurve GrowCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [HideInInspector] public Mesh BakedMesh;

    public void BuildMesh()
    {
        var container = GetComponent<SplineContainer>();
        var meshFilter = GetComponent<MeshFilter>();

        var allVerts = new List<Vector3>();
        var allUVs = new List<Vector2>();
        var allTris = new List<int>();

        var trunk = container.Splines.Count > 0 ? container.Splines[0] : null;

        for (int i = 0; i < container.Splines.Count; i++)
        {
            var spline = container.Splines[i];

            float effectiveBaseRadius = BaseRadius;
            if (i > 0 && trunk != null)
                effectiveBaseRadius = RadiusOnTrunkAtBranchStart(spline, trunk);

            BakeSpline(spline, allVerts.Count, effectiveBaseRadius, allVerts, allUVs, allTris);
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

    float RadiusOnTrunkAtBranchStart(Spline branch, Spline trunk)
    {
        branch.Evaluate(0f, out float3 branchStart, out _, out _);
        SplineUtility.GetNearestPoint(trunk, branchStart, out _, out float tOnTrunk);
        return Mathf.Lerp(BaseRadius, TipRadius, tOnTrunk);
    }

    void BakeSpline(Spline spline, int vertexOffset, float baseRadius,
                    List<Vector3> verts, List<Vector2> uvs, List<int> tris)
    {
        int segsVisible = Mathf.Max(1, Mathf.RoundToInt(Segments * GrowProgress));

        // Facteur global de scale lié au GrowProgress (ex: branche encore petite = tout réduit)
        float growScale = GrowCurve.Evaluate(GrowProgress);

        for (int s = 0; s <= segsVisible; s++)
        {
            float tFull = (float)s / Segments;
            float tLocal = (float)s / segsVisible;

            // TaperCurve pilote la forme sur toute la longueur
            float taper = TaperCurve.Evaluate(tFull);
            float radius = Mathf.Lerp(TipRadius, baseRadius, taper) * growScale;

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