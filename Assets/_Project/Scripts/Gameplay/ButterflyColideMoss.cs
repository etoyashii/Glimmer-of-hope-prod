using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay
{
    public class NewMonoBehaviourScript : MonoBehaviour
    {
        
        [SerializeField] private GameObject Butterfly;
        [SerializeField] PlayableDirector _director;
        [SerializeField] UnityEvent _onReactPreDirector;
  
        
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
                
                
                if (_director == null) return;
               
                _director.Play();
                
               
            }
        }
        void PostReact(PlayableDirector director)
        {
            //_onReactPostDirector?.Invoke();
            _director.stopped -= PostReact;
        }
    }
}
