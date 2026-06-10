using UnityEngine;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Début d'une section conditionnelle via Enum.
    /// Ferme automatiquement la section précédente si encore ouverte.
    /// Usage : [EnumSectionBegin(nameof(MyEnum), MyEnum.TypeA)]
    /// </summary>
    public class EnumSectionBeginAttribute : PropertyAttribute
    {
        public string fieldName {  get; }
        public object value { get; }

        public EnumSectionBeginAttribute(string name, object val)
        {
            fieldName = name;
            value = val;
        }
    }
}
