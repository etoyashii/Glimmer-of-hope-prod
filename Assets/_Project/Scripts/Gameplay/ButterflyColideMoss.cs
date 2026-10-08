using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class NewMonoBehaviourScript : MonoBehaviour
    {
        
        [SerializeField] private GameObject Butterfly;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            
        }
        void OnCollisionEnter(Collision collision)
        {
            if(collision.gameObject.tag == "MossSphere")
            {
                Debug.Log("Collision with Moss");
                Butterfly.SetActive(false);
            }
        }
    }
}
