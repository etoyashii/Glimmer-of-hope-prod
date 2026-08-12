using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine;
namespace GlimmerOfHope.Gameplay.NewDialogue
{
    public class TestActionDialogue : MonoBehaviour
    {
        
        void Awake()
        {
            DialogueActions.Register("takecube", takecube);

            
        }

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

