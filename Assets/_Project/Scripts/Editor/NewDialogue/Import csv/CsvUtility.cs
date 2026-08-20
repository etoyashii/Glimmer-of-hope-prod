using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Small CSV parser. Handles quoted fields containing commas or newlines (Google Sheets
    /// exports dialogue Text that way), which a naive line-split would break.
    /// </summary>
    public static class CsvUtility
    {
        public static List<string[]> Parse(string path)
        {
            var text = File.ReadAllText(path);
            var rows = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        field.Append(c);
                    }
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        row.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break; // handled on the following \n
                    case '\n':
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(row.ToArray());
                        row = new List<string>();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row.ToArray());
            }

            return rows;
        }
    }
}
