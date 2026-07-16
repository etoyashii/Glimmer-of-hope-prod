using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Common
{
    /// <summary>
    /// Wraps AssetDatabase.FindAssets when a search is scoped to a folder.
    ///
    /// FindAssets(filter, searchInFolders) returns an EMPTY array when the folder does not exist.
    /// It throws nothing and logs nothing: a renamed or moved folder silently turns every tool that
    /// depends on it into a tool that finds nothing, with no red line in the console to say why.
    /// This class turns that silence into a console error naming the folder and the fix.
    ///
    /// A search that does not need a folder scope should call AssetDatabase.FindAssets directly:
    /// an unscoped search is immune to any move, and is always the better option when it is possible.
    /// </summary>
    public static class AssetSearch
    {
        private static readonly HashSet<string> ReportedFolders = new HashSet<string>();

        /// <summary>
        /// Same contract as AssetDatabase.FindAssets(filter, new[] { folder }), except that a missing
        /// folder is reported instead of being swallowed. Returns an empty array in that case, so the
        /// caller degrades instead of throwing inside an OnGUI pass.
        /// </summary>
        public static string[] FindInFolder(string filter, string folder)
        {
            if (!FolderExists(folder))
                return new string[0];

            return AssetDatabase.FindAssets(filter, new[] { folder });
        }

        /// <summary>Same, for several roots. Missing roots are reported and dropped.</summary>
        public static string[] FindInFolders(string filter, params string[] folders)
        {
            var valid = new List<string>(folders.Length);
            foreach (var folder in folders)
            {
                if (FolderExists(folder))
                    valid.Add(folder);
            }

            if (valid.Count == 0)
                return new string[0];

            return AssetDatabase.FindAssets(filter, valid.ToArray());
        }

        /// <summary>
        /// True if the folder exists. Logs an actionable error the first time a given folder is found
        /// missing, then stays quiet: this is called from OnGUI, and an error per repaint is noise.
        /// </summary>
        public static bool FolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return true;

            if (ReportedFolders.Add(folder))
            {
                Debug.LogError(
                    "[AssetSearch] Dossier introuvable : \"" + folder + "\".\n" +
                    "Toute recherche limitee a ce dossier renvoie zero resultat SANS erreur : " +
                    "l'outil qui en depend a l'air de fonctionner, mais il ne trouve plus rien.\n" +
                    "Le dossier a ete deplace, renomme ou supprime. Corrigez la constante qui le nomme.");
            }

            return false;
        }
    }
}
