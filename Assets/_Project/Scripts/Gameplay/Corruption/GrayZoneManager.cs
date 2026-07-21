using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// It manage the gray zones and the mask arrays then apply the propreties into the shaders. 
    /// if a terrian is ussing the custom mat this script is needed to shit something
    /// ExecuteAlways : Start/LateUpdate/OnEnable/OnDisable runs in Édition mode (otherwise oupsi doupsi on voit r)
    /// </summary>
    [ExecuteAlways]
    public class GrayZoneManager : MonoBehaviour
    {
        static List<GrayZone> zones = new List<GrayZone>();
        static bool dirty = true;

        ComputeBuffer zoneBuffer;
        public Texture2DArray maskArray;

        static readonly int ZonesID = Shader.PropertyToID("_GrayZones");
        static readonly int CountID = Shader.PropertyToID("_GrayZoneCount");
        static readonly int MasksID = Shader.PropertyToID("_GrayZoneMasks");

        void OnEnable()
        {
            dirty = true;
            Shader.SetGlobalTexture(MasksID, maskArray);
#if UNITY_EDITOR
            // make it reload
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseBuffer;
#endif
            UpdateBuffer();
        }

        void Start()
        {
            Shader.SetGlobalTexture(MasksID, maskArray);
            UpdateBuffer();
        }

        void LateUpdate()
        {
            if (dirty)
            {
                UpdateBuffer();
                dirty = false;
            }
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= ReleaseBuffer;
#endif
            ReleaseBuffer();
        }

        public static void MarkDirty()
        {
            dirty = true;
        }

        void UpdateBuffer()
        {
            ReleaseBuffer();

            int count = zones.Count;
            if (count == 0)
            {
                Shader.SetGlobalInt(CountID, 0);
                Shader.SetGlobalBuffer(ZonesID, (ComputeBuffer)null);
                return;
            }

            GrayZoneData[] data = new GrayZoneData[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = zones[i].GetData();
            }

            int stride = sizeof(float) * 16 + sizeof(float) * 2 + sizeof(float) + sizeof(int);
            zoneBuffer = new ComputeBuffer(count, stride);
            zoneBuffer.SetData(data);

            Shader.SetGlobalBuffer(ZonesID, zoneBuffer);
            Shader.SetGlobalInt(CountID, count);
        }

        public static void Register(GrayZone zone)
        {
            if (!zones.Contains(zone))
            {
                zones.Add(zone);
                dirty = true;
            }
        }

        public static void Unregister(GrayZone zone)
        {
            if (zones.Remove(zone))
            {
                dirty = true;
            }
        }

        void OnDestroy()
        {
            ReleaseBuffer();
        }

        void ReleaseBuffer()
        {
            if (zoneBuffer != null)
            {
                zoneBuffer.Release();
                zoneBuffer = null;
            }
        }
    }
}