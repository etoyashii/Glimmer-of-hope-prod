using GlimmerOfHope.Gameplay.Dialogue;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine;
namespace GlimmerOfHope.Gameplay.NewDialogue
{
    public class TestActionDialogue : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            DialogueActions.Register("takecube", takecube);

            
        }
        // Update is called once per frame
        void Update()
        {
        }

        void takecube()
        {
            DialogueFlags.Set("HasCube", true);
            Debug.Log("Cube Taken");
            gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
    } 
}

