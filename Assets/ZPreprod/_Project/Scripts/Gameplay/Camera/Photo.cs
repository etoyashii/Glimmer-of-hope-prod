using UnityEngine;

namespace GlimmerOfHope.Gameplay.GCamera
{
    #region Dependencies

    [RequireComponent(typeof(CameraManager))]

    #endregion

    /// <summary>
    /// The camera use to take photo
    /// Set auto of the resolution according to the size of the current window
    /// Need a camera Manager
    /// </summary>
    public class Photo : MonoBehaviour
    {
        #region Public Properties

        public int resWidth;
        public int resHeight;

        #endregion

        #region Private Properties

        private Camera _camera;
        private bool _takeShot = false;

        private CameraManager _cameraManager;

        #endregion

        #region Unity LifeCycle

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _camera = Camera.main;

            resWidth = _camera.pixelWidth;
            resHeight = _camera.pixelHeight;

            _cameraManager = GetComponent<CameraManager>();
        }

        private void LateUpdate()
        {
            if (_takeShot)
            {
                RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
                _camera.targetTexture = rt;
                Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
                _camera.Render();
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
                _camera.targetTexture = null;
                RenderTexture.active = null; // JC: added to avoid errors
                Destroy(rt);
                byte[] bytes = screenShot.EncodeToPNG();
                string filename = ScreenShotName();
                using (var fs = new System.IO.FileStream(filename, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
                Debug.Log(string.Format("Took screenshot to: {0}", filename));
                _takeShot = false;
   //             _cameraManager.ChangeEffectVisibility(true);
            }
        }

        #endregion

        #region Public Methods
        public string ScreenShotName()
        {
            return string.Format("{0}/_Project/Screenshots/screenshot_{1}.png",
                Application.dataPath,
                System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        }

        public void TakeShot()
        {
            _takeShot = true;
        }

        #endregion
    }
}
