using System;

namespace GlimmerOfHope.Core.Save
{
    [Serializable]
    public class PreferencesData
    {
        public string language = "fr";
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public bool vibrationEnabled = true;
        public int qualityLevel = 2;
    }
}
