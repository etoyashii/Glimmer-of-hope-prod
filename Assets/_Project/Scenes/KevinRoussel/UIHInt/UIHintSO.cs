using UnityEngine;

public class UIHintSO : ScriptableObject
{
    
    [Serialized]
    class StringImageAssociation
    {
        public string Name;
        public Sprite Image;
    }
    
    [SerializedField] StringImageAssociation[] _data;
    
    //public Image GetSpriteForInput(string n) => _da
    
    
}
