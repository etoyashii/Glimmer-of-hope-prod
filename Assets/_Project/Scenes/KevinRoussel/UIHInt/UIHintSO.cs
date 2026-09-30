using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "UI/Hint")]
public class UIHintSO : ScriptableObject
{
    [Serializable]
    class StringImageAssociation
    {
        public string Name;
        public Sprite Image;
    }
    
    [SerializeField] StringImageAssociation[] _data;
    
    public Sprite GetSpriteForInput(string n) => _data.FirstOrDefault(i=>i.Name == n)?.Image;
    
}
