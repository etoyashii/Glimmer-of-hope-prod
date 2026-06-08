using UnityEngine;
using GlimmerOfHope.Editor;

public class Guide : MonoBehaviour
{
	public enum GuideType
	{
		TypeA,
		TypeB
	}

	public GuideType type;

	[EnumSectionBegin(nameof(type), GuideType.TypeA)]
	public int fieldTypeA;

	[EnumSectionBegin(nameof(type), GuideType.TypeB)]
	public int fieldTypeB;

	[EnumSectionEnd] //Utile seulement si variables shared a rajouter en dessous. Sinon supprimez.

	private int nonSharedField;
	public int sharedField;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}