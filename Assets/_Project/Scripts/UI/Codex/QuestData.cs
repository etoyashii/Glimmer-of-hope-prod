using UnityEngine;

namespace GlimmerOfHope.UI.BookMenu.Data
{
    [System.Serializable]
    public class ZoneProgressData
    {
        public string ZoneName;
        [Range(0, 100)] public int CompletionPercent;
    }
}