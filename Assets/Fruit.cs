using System;
using GlimmerOfHope.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class Fruit : MonoBehaviour
{
    static int _lootedCount=0;
        
    [SerializeField] Interactable _interactable;
    
    [Header("Events")]
    [SerializeField] PlayableDirector _director;
    [SerializeField] UnityEvent _onReactPreDirector;
    [SerializeField] UnityEvent _onReactPostDirector;
    
    public static int LootedCount => _lootedCount;

    void Start() => _interactable.OnInteracted.AddListener(React);
    void OnDestroy() => _interactable.OnInteracted.RemoveListener(React);

    public void React()
    {
        _lootedCount++;
        Debug.Log("FruitLooted");
        _onReactPreDirector?.Invoke();
        
        if (_director == null) return;
        _director.Play();
        _director.stopped += PostReact;
    }

    void PostReact(PlayableDirector director)
    {
        _onReactPostDirector?.Invoke();
        _director.stopped -= PostReact;
    }

    public static void ConsumeFruit()
    {
        _lootedCount--;
    }
}
