using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Ajoute Create -> OdinLike -> Enum Section Script
    /// pour créer un script préconfiguré avec enum et balises de sections.
    /// </summary>
    public class EnumSectionCreator
    {
        private const string MENU_PATH = "Assets/Create/OdinLike/Enum Section Script";
        private const string TEMPLATE_NAME = "EnumSectionScript.cs.txt";
        private const string DEFAULT_NAME = "NewEnumSectionScript.cs";

        [MenuItem(MENU_PATH, priority = 80)]
        private static void CreateEnumSectionScript()
        {
            string templatePath = FindTemplatePath();
            if (templatePath == null) return;
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, DEFAULT_NAME);
        }

        private static string FindTemplatePath()
        {
            string[] guids = AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(TEMPLATE_NAME));
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(TEMPLATE_NAME))
                    return path;
            }

            Debug.LogError($"[EnumSection] Template introuvable : {TEMPLATE_NAME}\n"
                + "Placer le fichier dans un dossier Assets accessible.");
            return null;
        }
    }
}
