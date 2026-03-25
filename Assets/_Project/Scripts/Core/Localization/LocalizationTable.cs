using System;
using System.Collections.Generic;

namespace GlimmerOfHope.Core.Localization
{
    [Serializable]
    public class LocalizationTable
    {
        public string tableName;
        public string languageCode;
        public List<LocalizationEntry> entries = new();

        public bool TryGetValue(string key, out string value)
        {
            foreach (var entry in entries)
            {
                if (entry.key == key)
                {
                    value = entry.value;
                    return true;
                }
            }
            value = null;
            return false;
        }
    }
}
