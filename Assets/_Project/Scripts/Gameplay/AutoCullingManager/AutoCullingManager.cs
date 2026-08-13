using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace PerfectCulling
{

    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("Perfect Culling/Auto Culling Manager")]
    public class AutoCullingManager : MonoBehaviour
    {
        [Header("Cible")]
        [Tooltip("Layers des objets à culler automatiquement.")]
        public LayerMask cullableLayers = ~0;

        [Tooltip("Layers considérés comme occluders pour le raycast (doivent avoir un Collider : terrain, murs, bâtiments...).")]
        public LayerMask occluderLayers = ~0;

        [Header("Taille à l'écran")]
        [Tooltip("Distance à partir de laquelle le test de taille à l'écran commence à s'appliquer. En dessous, un objet visible reste visible peu importe sa taille à l'écran.")]
        public float screenSizeCheckDistance = 30f;
        [Tooltip("Fraction minimale de la hauteur d'écran que l'objet doit occuper pour rester visible au-delà de Screen Size Check Distance.")]
        [Range(0f, 0.5f)] public float minScreenSizePercent = 0.02f;
        [Tooltip("Marge relative avant bascule visible/invisible pour la taille à l'écran, évite le scintillement.")]
        [Range(0f, 0.5f)] public float screenSizeHysteresis = 0.15f;

        [Header("Fog")]
        [Tooltip("Si activé, un objet totalement noyé dans le brouillard natif d'Unity (RenderSettings.fog) n'est pas dessiné.")]
        public bool cullByFog = true;
        [Tooltip("Facteur de visibilité en dessous duquel l'objet est considéré comme invisible dans le brouillard (0 = totalement fondu dans le brouillard, 1 = aucun brouillard).")]
        [Range(0f, 0.5f)] public float fogVisibilityThreshold = 0.02f;
        [Tooltip("Marge relative avant bascule visible/invisible pour le brouillard, évite le scintillement.")]
        [Range(0f, 0.5f)] public float fogHysteresis = 0.15f;

        [Header("Occlusion par raycast batché")]
        public bool enableOcclusionRaycast = true;
        [Tooltip("Au-delà de cette distance, plus de raycast d'occlusion (0 = illimité).")]
        public float maxOcclusionCheckDistance = 100f;
        [Tooltip("Nombre de raycasts confiés à chaque thread worker. Unity recommande généralement entre 4 et 32 selon la charge par raycast.")]
        [Min(1)] public int minCommandsPerJob = 8;

        [Header("Performance")]
        [Tooltip("Si activé (recommandé), les bounds de chaque objet sont calculées UNE FOIS à l'enregistrement plutôt qu'à chaque frame. Correct pour les objets STATIQUES (herbe, rochers, arbres...). Désactive si des objets culled bougent, changent d'échelle ou de mesh en cours de partie.")]
        public bool assumeStaticBounds = true;
        [Tooltip("Distance sous laquelle les objets sont revérifiés à CHAQUE frame plutôt qu'en round-robin. C'est ce qui élimine le pop-in visible sur les objets proches.")]
        public float frequentCheckRadius = 25f;
        [Tooltip("Nombre d'objets LOINTAINS revérifiés par frame en round-robin. Les objets proches ne dépendent pas de ce réglage.")]
        [Min(1)] public int checksPerFrame = 40;
        [Tooltip("Même pour les objets proches, le raycast d'occlusion n'est refait que tous les N frames. Le frustum et la taille écran restent instantanés. Monte cette valeur si tu as beaucoup d'objets proches (forêt dense).")]
        [Min(1)] public int nearOcclusionCheckInterval = 4;
        public Camera referenceCamera;

        /// <summary>Un objet culled avec ses bounds mises en cache (voir assumeStaticBounds).</summary>
        struct Entry
        {
            public Renderer renderer;
            public Vector3 center;
            public Vector3 extents;
        }

        readonly List<Entry> nearEntries = new List<Entry>();
        readonly List<Entry> farEntries = new List<Entry>();
        int farCursor;

        // Buffers "en cours de construction" ce frame, deviennent le job actif à la fin de Update().
        // Buffers du job actuellement planifié, résolus au prochain Update().
        // (Pas readonly : les deux paires sont échangées par référence chaque frame.)
        List<Renderer> pendingRenderers = new List<Renderer>();
        List<Vector3> pendingDirs = new List<Vector3>();
        List<float> pendingDists = new List<float>();

        List<Renderer> activeJobRenderers = new List<Renderer>();
        List<Vector3> activeJobDirs = new List<Vector3>();
        List<float> activeJobDists = new List<float>();

        // Réutilisé chaque frame : GeometryUtility.CalculateFrustumPlanes(camera) sans argument de sortie
        // ALLOUE un nouveau Plane[6] à chaque appel. La variante avec tableau de sortie évite ça totalement.
        readonly Plane[] frustumPlanes = new Plane[6];

        NativeArray<RaycastCommand> commandBuffer;
        NativeArray<RaycastHit> resultBuffer;
        int bufferCapacity;
        JobHandle pendingHandle;
        bool hasPendingJob;

        void Start()
        {
            if (referenceCamera == null) referenceCamera = Camera.main;
            ScanScene();
        }

        void OnDisable()
        {
            if (hasPendingJob)
            {
                pendingHandle.Complete();
                hasPendingJob = false;
            }
        }

        void OnDestroy()
        {
            if (hasPendingJob) pendingHandle.Complete();
            if (commandBuffer.IsCreated) commandBuffer.Dispose();
            if (resultBuffer.IsCreated) resultBuffer.Dispose();
        }

        static Entry MakeEntry(Renderer r)
        {
            Bounds b = r.bounds;
            return new Entry { renderer = r, center = b.center, extents = b.extents };
        }

        /// <summary>Scanne tous les Renderer de la scène correspondant au layer mask et les répartit proches/lointains.</summary>
        [ContextMenu("Rescanner la scène")]
        public void ScanScene()
        {
            nearEntries.Clear();
            farEntries.Clear();

            Vector3 camPos = referenceCamera != null ? referenceCamera.transform.position : Vector3.zero;
            float hotSqr = frequentCheckRadius * frequentCheckRadius;

            var renderers = FindObjectsOfType<Renderer>(true);
            foreach (var r in renderers)
            {
                if (((1 << r.gameObject.layer) & cullableLayers) == 0) continue;
                Entry e = MakeEntry(r);
                float sqrDist = (e.center - camPos).sqrMagnitude;
                (sqrDist <= hotSqr ? nearEntries : farEntries).Add(e);
            }

            Debug.Log($"[Perfect Culling] {nearEntries.Count + farEntries.Count} renderer(s) enregistré(s) " +
                      $"({nearEntries.Count} proches, {farEntries.Count} lointains).");
        }

        /// <summary>Enregistre un renderer manuellement (utile pour du spawn dynamique en cours de partie).</summary>
        public void Register(Renderer r)
        {
            if (r == null) return;
            Entry e = MakeEntry(r);
            Vector3 camPos = referenceCamera != null ? referenceCamera.transform.position : Vector3.zero;
            float sqrDist = (e.center - camPos).sqrMagnitude;
            (sqrDist <= frequentCheckRadius * frequentCheckRadius ? nearEntries : farEntries).Add(e);
        }

        void Update()
        {
            if (referenceCamera == null)
            {
                referenceCamera = Camera.main;
                if (referenceCamera == null) return;
            }

            // 1) Résout le batch de raycasts planifié la frame précédente.
            ResolvePendingJob();

            if (nearEntries.Count == 0 && farEntries.Count == 0) return;

            Vector3 camPos = referenceCamera.transform.position;
            // Remplit le tableau existant au lieu d'en allouer un nouveau.
            GeometryUtility.CalculateFrustumPlanes(referenceCamera, frustumPlanes);
            float tanHalfFov = Mathf.Tan(referenceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            // Distances au carré précalculées une seule fois par frame plutôt qu'une fois par objet testé.
            float hotRadiusSqr = frequentCheckRadius * frequentCheckRadius;
            float screenCheckDistSqr = screenSizeCheckDistance * screenSizeCheckDistance;
            float maxOcclusionDistSqr = maxOcclusionCheckDistance > 0f ? maxOcclusionCheckDistance * maxOcclusionCheckDistance : -1f;
            int frameCount = Time.frameCount;

            pendingRenderers.Clear();
            pendingDirs.Clear();
            pendingDists.Clear();

            // Proches : frustum + taille écran instantanés chaque frame ; raycast d'occlusion étalé.
            for (int i = nearEntries.Count - 1; i >= 0; i--)
            {
                Entry e = nearEntries[i];
                if (e.renderer == null) { RemoveAtSwap(nearEntries, i); continue; }

                // Bounds recalculées seulement si l'objet n'est pas supposé statique.
                if (!assumeStaticBounds)
                {
                    Bounds b = e.renderer.bounds;
                    e.center = b.center;
                    e.extents = b.extents;
                }

                bool checkOcclusion = ((frameCount + i) % nearOcclusionCheckInterval) == 0;
                bool stillNear = EvaluateAndQueue(e, camPos, tanHalfFov, checkOcclusion, hotRadiusSqr, screenCheckDistSqr, maxOcclusionDistSqr);
                if (!stillNear)
                {
                    RemoveAtSwap(nearEntries, i);
                    farEntries.Add(e);
                }
                else
                {
                    nearEntries[i] = e; // persiste les bounds rafraîchies si assumeStaticBounds == false
                }
            }

            // Lointains : round-robin avec budget réglable, retard de mise à jour peu visible.
            int checks = Mathf.Min(checksPerFrame, farEntries.Count);
            for (int i = 0; i < checks; i++)
            {
                if (farEntries.Count == 0) break;
                farCursor %= farEntries.Count;
                Entry e = farEntries[farCursor];
                if (e.renderer == null) { RemoveAtSwap(farEntries, farCursor); continue; }

                if (!assumeStaticBounds)
                {
                    Bounds b = e.renderer.bounds;
                    e.center = b.center;
                    e.extents = b.extents;
                }

                bool isNear = EvaluateAndQueue(e, camPos, tanHalfFov, true, hotRadiusSqr, screenCheckDistSqr, maxOcclusionDistSqr);
                if (isNear)
                {
                    RemoveAtSwap(farEntries, farCursor);
                    nearEntries.Add(e);
                }
                else
                {
                    farEntries[farCursor] = e;
                    farCursor++;
                }
            }

            // 2) Planifie le nouveau batch (résolu au prochain Update()).
            ScheduleJob(camPos);
        }

        /// <summary>
        /// Calcule frustum + taille écran + fog à partir des bounds en cache de l'entrée (pas de r.bounds
        /// ici). Si un raycast d'occlusion est dû ce frame-ci, met l'objet en file pour le prochain batch
        /// (sans toucher r.enabled tout de suite). Sinon résout immédiatement avec le dernier résultat
        /// d'occlusion connu. Retourne true si l'objet est dans le rayon "proche".
        /// </summary>
        bool EvaluateAndQueue(in Entry e, Vector3 camPos, float tanHalfFov, bool checkOcclusion,
            float hotRadiusSqr, float screenCheckDistSqr, float maxOcclusionDistSqr)
        {
            Renderer r = e.renderer;
            bool currentlyVisible = r.enabled;
            float sqrDist = (e.center - camPos).sqrMagnitude;
            bool baseVisible;

            // Reconstruit une Bounds à partir du cache (struct, aucune allocation) pour le test de frustum.
            Bounds bounds = new Bounds(e.center, e.extents * 2f);

            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
            {
                baseVisible = false;
            }
            else
            {
                baseVisible = true;

                bool needsScreenCheck = minScreenSizePercent > 0f && sqrDist >= screenCheckDistSqr;
                bool needsFogCheck = cullByFog && RenderSettings.fog;
                // sqrt calculé au plus une fois, partagé entre les deux tests si les deux sont nécessaires.
                float dist = (needsScreenCheck || needsFogCheck) ? Mathf.Sqrt(sqrDist) : 0f;

                if (needsScreenCheck)
                {
                    float screenSize = e.extents.magnitude / (dist * tanHalfFov);
                    float sizeThreshold = currentlyVisible
                        ? minScreenSizePercent * (1f - screenSizeHysteresis)
                        : minScreenSizePercent * (1f + screenSizeHysteresis);
                    if (screenSize < sizeThreshold) baseVisible = false;
                }

                if (baseVisible && needsFogCheck)
                {
                    float fogFactor = ComputeFogFactor(dist);
                    float fogThreshold = currentlyVisible
                        ? fogVisibilityThreshold * (1f - fogHysteresis)
                        : fogVisibilityThreshold * (1f + fogHysteresis);
                    if (fogFactor < fogThreshold) baseVisible = false;
                }
            }

            bool needsOcclusionTest = baseVisible && enableOcclusionRaycast &&
                (maxOcclusionDistSqr < 0f || sqrDist <= maxOcclusionDistSqr);

            if (needsOcclusionTest && checkOcclusion)
            {
                Vector3 diff = e.center - camPos;
                float rayDist = diff.magnitude;
                if (rayDist > 0.01f)
                {
                    pendingRenderers.Add(r);
                    pendingDirs.Add(diff / rayDist);
                    pendingDists.Add(rayDist - 0.05f);
                    // Résultat appliqué au prochain Update(), on ne touche pas r.enabled maintenant.
                    return sqrDist <= hotRadiusSqr;
                }
            }

            // Pas de nouveau test d'occlusion ce tour-ci : garde le dernier résultat connu si un test
            // était nécessaire, sinon applique directement le résultat frustum/taille écran/fog.
            bool finalVisible = needsOcclusionTest ? currentlyVisible : baseVisible;
            if (r.enabled != finalVisible) r.enabled = finalVisible;

            return sqrDist <= hotRadiusSqr;
        }

        /// <summary>
        /// Calcule le facteur de visibilité du brouillard natif d'Unity à une distance donnée.
        /// 1 = aucun brouillard (pleinement visible), 0 = totalement fondu dans la couleur du brouillard.
        /// </summary>
        static float ComputeFogFactor(float dist)
        {
            switch (RenderSettings.fogMode)
            {
                case FogMode.Linear:
                    float start = RenderSettings.fogStartDistance;
                    float end = RenderSettings.fogEndDistance;
                    if (end <= start) return dist >= end ? 0f : 1f;
                    return Mathf.Clamp01((end - dist) / (end - start));

                case FogMode.Exponential:
                    return Mathf.Clamp01(Mathf.Exp(-RenderSettings.fogDensity * dist));

                case FogMode.ExponentialSquared:
                    float d = RenderSettings.fogDensity * dist;
                    return Mathf.Clamp01(Mathf.Exp(-(d * d)));

                default:
                    return 1f;
            }
        }

        void ScheduleJob(Vector3 camPos)
        {
            if (pendingRenderers.Count == 0)
            {
                hasPendingJob = false;
                activeJobRenderers.Clear();
                return;
            }

            EnsureCapacity(pendingRenderers.Count);

            var queryParams = new QueryParameters(occluderLayers, false, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < pendingRenderers.Count; i++)
            {
                commandBuffer[i] = new RaycastCommand(camPos, pendingDirs[i], queryParams, pendingDists[i]);
            }

            var commandsSlice = commandBuffer.GetSubArray(0, pendingRenderers.Count);
            var resultsSlice = resultBuffer.GetSubArray(0, pendingRenderers.Count);
            pendingHandle = RaycastCommand.ScheduleBatch(commandsSlice, resultsSlice, minCommandsPerJob);
            JobHandle.ScheduleBatchedJobs(); // force les workers à démarrer tout de suite plutôt qu'en fin de frame
            hasPendingJob = true;

            // Le buffer "pending" construit ce frame devient le buffer "actif" à résoudre au prochain Update().
            // Échange de références (O(1)) : pas de copie, pas d'allocation.
            (activeJobRenderers, pendingRenderers) = (pendingRenderers, activeJobRenderers);
            (activeJobDirs, pendingDirs) = (pendingDirs, activeJobDirs);
            (activeJobDists, pendingDists) = (pendingDists, activeJobDists);
        }

        void ResolvePendingJob()
        {
            if (!hasPendingJob) return;
            pendingHandle.Complete();
            hasPendingJob = false;

            for (int i = 0; i < activeJobRenderers.Count; i++)
            {
                var r = activeJobRenderers[i];
                if (r == null) continue;

                RaycastHit hit = resultBuffer[i];
                bool occluded = false;
                if (hit.colliderInstanceID != 0)
                {
                    var col = hit.collider;
                    // Compare les racines de hiérarchie pour éviter la self-occlusion
                    // (Collider et Renderer peuvent être sur des GameObjects différents du même prefab).
                    if (col != null && col.transform.root != r.transform.root)
                        occluded = true;
                }

                bool finalVisible = !occluded;
                if (r.enabled != finalVisible) r.enabled = finalVisible;
            }
        }

        void EnsureCapacity(int needed)
        {
            if (needed <= bufferCapacity) return;
            if (commandBuffer.IsCreated) commandBuffer.Dispose();
            if (resultBuffer.IsCreated) resultBuffer.Dispose();
            bufferCapacity = Mathf.NextPowerOfTwo(needed);
            commandBuffer = new NativeArray<RaycastCommand>(bufferCapacity, Allocator.Persistent);
            resultBuffer = new NativeArray<RaycastHit>(bufferCapacity, Allocator.Persistent);
        }

        static void RemoveAtSwap<T>(List<T> list, int index)
        {
            int last = list.Count - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }
    }
}