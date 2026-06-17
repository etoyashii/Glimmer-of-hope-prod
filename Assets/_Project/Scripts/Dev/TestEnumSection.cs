using UnityEngine;
using GlimmerOfHope.Editor;

public class TestEnumSection : MonoBehaviour
{
	public enum TestEnumSectionType
	{
		TypeA,
		TypeB
	}

	public enum TestEnumSectionType2
    {
		Type1, 
		Type2
	}

    [Header("--Section 1--")]
    public TestEnumSectionType type;

	[EnumSectionBegin(nameof(type), TestEnumSectionType.TypeA)]
	public int fieldTypeA;
	public float fieldTypefloatA;

	[EnumSectionBegin(nameof(type), TestEnumSectionType.TypeB)]
	public int fieldTypeB;

	[EnumSectionEnd]

	[Header("--Shared--")]
    public int sharedField;
    private int nonSharedField;

	[Header("--Section 2--")]
    public TestEnumSectionType2 type2;

    [EnumSectionBegin(nameof(type2), TestEnumSectionType2.Type1)]
    public int fieldType1;

    [EnumSectionBegin(nameof(type2), TestEnumSectionType2.Type2)]
    public int fieldType2;
    public float fieldTypefloat2;

}
