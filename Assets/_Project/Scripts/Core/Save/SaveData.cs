using System;

namespace GlimmerOfHope.Core.Save
{
    [Serializable]
    public class SaveData
    {
        public string version = "1.0.0";
        public long timestamp;
        public ProgressionData progression = new();
        public PreferencesData preferences = new();

        public SaveData()
        {
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
