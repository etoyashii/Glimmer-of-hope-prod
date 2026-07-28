using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace GlimmerOfHope.Editor
{
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
        [Range(0f, 0.5f)]
        public float RadiusVariance = 0.15f;
        public int RandomSeed = 42;

        [Header("Jonctions")]
        [Tooltip("Multiplicateur de radius au pic du flare de jonction sur le parent (1 = pas de flare)")]
        [Range(1f, 2f)]
        public float JunctionSphereScale = 1.15f;
        [Tooltip("Largeur du flare, en fraction de la longueur du spline parent")]
        [Range(0.01f, 0.3f)]
        public float JunctionFlareWidth = 0.05f;

        [Header("Face plate (marchable)")]
        [Tooltip("Si activé, une facette plane horizontale (alignée sur le monde, pas sur le twist du spline) est créée sur le dessus de chaque branche, pour pouvoir marcher dessus.")]
        public bool FlattenTop = true;
        [Tooltip("Demi-angle (en degrés) de la facette plate au sommet de la branche. 0 = pas de facette (cylindre normal). Plus grand = facette plus large.")]
        [Range(0f, 80f)]
        public float FlatTopAngle = 30f;

        [Range(0f, 1f)]
        public float GrowProgress = 1f;

        [HideInInspector] public Mesh BakedMesh;

        // ─────────────────────────────────────────────────────────────────

        public void BuildMesh()
        {
            var container = GetComponent<SplineContainer>();
            var meshFilter = GetComponent<MeshFilter>();
            int count = container.Splines.Count;
            if (count == 0) return;

            var rng = new System.Random(RandomSeed);

            // -- 1. Graphe parent --
            int[] parentOf = new int[count];
            int[] parentKnotIndex = new int[count];
            for (int i = 0; i < count; i++) { parentOf[i] = -1; parentKnotIndex[i] = 0; }

            for (int i = 1; i < count; i++)
            {
                if (!container.KnotLinkCollection.TryGetKnotLinks(new SplineKnotIndex(i, 0), out var links))
                    continue;
                foreach (var ki in links)
                {
                    if (ki.Spline == i) continue;
                    parentOf[i] = ki.Spline;
                    parentKnotIndex[i] = ki.Knot;
                    break;
                }
            }

            // -- 2. Radius gradient depuis la racine --
            float[] distFromRoot = new float[count];
            for (int i = 1; i < count; i++)
            {
                int p = parentOf[i];
                float knotCount = container.Splines[p].Count - 1;
                float tOnParent = knotCount > 0 ? (float)parentKnotIndex[i] / knotCount : 0f;
                distFromRoot[i] = distFromRoot[p] + container.Splines[p].GetLength() * tOnParent;
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
                float norm = Mathf.Max(farthestThrough[i], 1f);
                float tBase = distFromRoot[i] / norm;
                float tTip = (distFromRoot[i] + splineLen) / norm;
                float variance = 1f - Mathf.Abs((float)(rng.NextDouble() * 2 - 1)) * RadiusVariance;
                effBase[i] = Mathf.Lerp(BaseRadius, TipRadius, tBase) * variance;
                effTip[i] = Mathf.Lerp(BaseRadius, TipRadius, tTip) * variance;
            }

            // -- 3. Pour chaque branche enfant : rayon de départ = rayon du parent au point d'attache --
            // C'est la clé pour éviter les couteaux : l'enfant part à la surface du parent,
            // pas à un rayon proche de 0.
            float[] startRadius = new float[count];
            for (int i = 0; i < count; i++)
                startRadius[i] = effBase[i];

            for (int i = 1; i < count; i++)
            {
                int p = parentOf[i];
                if (p < 0) continue;
                float knotCount = container.Splines[p].Count - 1;
                float tOnParent = knotCount > 0 ? (float)parentKnotIndex[i] / knotCount : 0f;
                float taper = TaperCurve.Evaluate(tOnParent);
                startRadius[i] = Mathf.Lerp(effTip[p], effBase[p], taper);
            }

            // -- 4. Table de jonctions par spline parent (flare gaussien) --
            var junctionsPerSpline = new List<(float t, float childR)>[count];
            for (int i = 0; i < count; i++)
                junctionsPerSpline[i] = new List<(float, float)>();
            for (int i = 1; i < count; i++)
            {
                int p = parentOf[i];
                if (p < 0) continue;
                float knotCount = container.Splines[p].Count - 1;
                float tOnParent = knotCount > 0 ? (float)parentKnotIndex[i] / knotCount : 0f;
                junctionsPerSpline[p].Add((tOnParent, startRadius[i]));
            }

            // -- 5. Bake --
            var allVerts = new List<Vector3>();
            var allUVs = new List<Vector2>();
            var allTris = new List<int>();

            for (int i = 0; i < count; i++)
            {
                BakeSpline(container.Splines[i], startRadius[i], effTip[i],
                           allVerts.Count, allVerts, allUVs, allTris,
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

        void BakeSpline(Spline spline, float baseR, float tipR,
                        int vertexOffset,
                        List<Vector3> verts, List<Vector2> uvs, List<int> tris,
                        List<(float t, float childR)> junctions)
        {
            int segsVisible = Mathf.Max(1, Mathf.RoundToInt(Segments * GrowProgress));

            float flatHalfAngleRad = FlattenTop ? math.radians(FlatTopAngle) : 0f;
            float3 worldUp = new float3(0f, 1f, 0f);

            for (int s = 0; s <= segsVisible; s++)
            {
                float tFull = (float)s / segsVisible;
                float taper = TaperCurve.Evaluate(tFull);
                float radius = Mathf.Lerp(tipR, baseR, taper);

                // Flare gaussien au point d'attache des enfants (branch collar naturel).
                float flare = 0f;
                foreach (var (jt, _) in junctions)
                {
                    float dist = Mathf.Abs(tFull - jt);
                    float bell = Mathf.Exp(-(dist * dist) / (2f * JunctionFlareWidth * JunctionFlareWidth));
                    flare = Mathf.Max(flare, (JunctionSphereScale - 1f) * bell);
                }
                radius *= (1f + flare);

                // On garde le "up" natif du spline pour construire le repère de l'anneau :
                // c'est ce qui assure la continuité de phase (donc des jonctions propres)
                // entre une branche parente et ses enfants.
                spline.Evaluate(tFull, out float3 pos, out float3 tangent, out float3 up);

                if (math.lengthsq(tangent) < 1e-6f) tangent = math.forward();
                tangent = math.normalize(tangent);

                float3 safeUp = math.abs(math.dot(up, tangent)) > 0.99f
                    ? (math.abs(tangent.y) < 0.9f ? math.up() : math.right())
                    : up;

                float3 right = math.normalize(math.cross(tangent, safeUp));
                float3 trueUp = math.cross(right, tangent);

                // Axes indépendants, alignés sur le monde, utilisés UNIQUEMENT pour décider
                // quels vertices flatten et dans quelle direction. Ils ne remplacent pas
                // right/trueUp ci-dessus : la phase du cercle (et donc l'alignement aux
                // jonctions) reste intacte, seule une petite zone proche du "vrai" haut
                // est corrigée.
                float3 uAxis = worldUp - tangent * math.dot(worldUp, tangent);
                float uLen = math.length(uAxis);
                bool hasWorldFlattenAxis = uLen > 1e-4f;
                if (hasWorldFlattenAxis) uAxis /= uLen; else uAxis = trueUp;
                float3 wAxis = math.normalize(math.cross(tangent, uAxis));

                for (int v = 0; v <= Sides; v++)
                {
                    float angle = (float)v / Sides * math.PI * 2f;
                    float3 offset = (math.cos(angle) * right + math.sin(angle) * trueUp) * radius;

                    if (flatHalfAngleRad > 0f && radius > 1e-6f)
                    {
                        float h = math.dot(offset, uAxis);
                        float angFromTopWorld = math.acos(math.clamp(h / radius, -1f, 1f));
                        if (angFromTopWorld <= flatHalfAngleRad)
                        {
                            float s1 = math.dot(offset, wAxis);
                            float newH = radius * math.cos(flatHalfAngleRad);
                            offset = wAxis * s1 + uAxis * newH;
                        }
                    }

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

            // Cone tip : ferme la pointe avec un vertex central.
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
}