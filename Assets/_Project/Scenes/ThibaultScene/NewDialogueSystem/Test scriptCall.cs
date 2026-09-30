using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine;

public class TestscriptCall : MonoBehaviour
{

    public DialogueGraph dialogueGraph;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnCollisionEnter() 
    { 
        DialogueManager.Instance.StartDialogue(dialogueGraph);

    }
}
