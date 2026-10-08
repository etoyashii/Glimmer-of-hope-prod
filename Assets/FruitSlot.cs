using System;
using GlimmerOfHope.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class FruitSlot : MonoBehaviour
{
    public static int ValidatedSlot { get; private set; }
    
    [SerializeField] Interactable _interactable;

    [Header("Events")]
    [SerializeField] PlayableDirector _director;
    [SerializeField] UnityEvent _onReactPreDirector;
    [SerializeField] UnityEvent _onReactPostDirector;
    
    public bool IsValidated { get; private set; }
    
    void Start() => _interactable.OnInteracted.AddListener(React);

    void React()
    {
        if (!IsValidated && Fruit.LootedCount > 0)
        {
            Fruit.ConsumeFruit();
            Debug.Log("Slot valudated");
            // Early validated if no Director
            if(_director== null) IsValidated = true;
            
            if (_director == null) return;
            _director.Play();
            _director.stopped += PostReact;
            return;
        }
        else
        {
            Debug.Log("Slot NOT valudated");
        }
        
    }
    
    void PostReact(PlayableDirector director)
    {
        IsValidated = true;
        _onReactPostDirector?.Invoke();
        _director.stopped -= PostReact;
    }
    
    
}
