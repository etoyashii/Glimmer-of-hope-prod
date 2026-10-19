using UnityEngine;

namespace GlimmerOfHope.UI
{
    public class UrlOpen : MonoBehaviour
    {
        [SerializeField] private string url = "https://google.com";

        public void OpenURL()
        {
            Application.OpenURL(url);
        }
    }
}
