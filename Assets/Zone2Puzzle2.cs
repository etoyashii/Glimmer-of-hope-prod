using System;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class Zone2Puzzle2 : MonoBehaviour
{
    [field:SerializeField] FruitSlot[] FruitSlots { get; set; }

    [Header("Events")]
    [SerializeField] UnityEvent _onPuzzleCompleted;
    [SerializeField] PlayableDirector _playableDirector;
    [SerializeField] UnityEvent _onPuzzleCompletedPostTimeline;
    
    async void Start()
    {
        while (!FruitSlots.All(i => i.IsValidated))
        {
            await Awaitable.NextFrameAsync();
        }

        Win();
    }
    
    void Win()
    {
        Debug.Log("Zone2Puzzle2 Validated");
        _onPuzzleCompleted?.Invoke();
        
        if(_playableDirector == null) return;
        _playableDirector?.Play();
        _playableDirector.stopped += Post;
    }
        

    void Post(PlayableDirector obj)
    {
        _onPuzzleCompletedPostTimeline?.Invoke();
        _playableDirector.stopped -= Post;
    }

    [Button, ContextMenu("Win")]
    void EDITOR_Win() => Win();
}
