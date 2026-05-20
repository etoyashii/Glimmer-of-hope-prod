using GlimmerOfHope.Gameplay.GCamera;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay.GCamera
{
    /// <summary>
    /// Manage the camera, the activation, take a picture and the visibility of the effect link to the camera
    /// </summary>

    public class CameraManager : MonoBehaviour
    {
        #region Public Properties

        public int maxDist = 20;
        public Canvas canvas;

        public SO_PhotoTask photoTask;

        public GameObject[] switchVisibility;

        #endregion

        #region Private Fields

        private Photo _photo;
        private bool _isInPhotoMode = false;
        private PicturabelObject[] _pictarblesObject;
        private GameObject _cameraObj;

        #endregion

        #region Unity Lifecycle

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _photo = GetComponent<Photo>();

            _pictarblesObject = FindObjectsByType<PicturabelObject>(FindObjectsSortMode.None);
            
            _cameraObj = Camera.main.gameObject;

            photoTask.Init(canvas);

            ChangeEffectVisibility(false);
        }

        // Update is called once per frame
        void Update()
        {
            //if(Keyboard.current.tabKey.wasPressedThisFrame)
            //{
            //    SwitchView();
            //}

            if(Keyboard.current.cKey.wasPressedThisFrame && _isInPhotoMode)
            {
                ChangeEffectVisibility(false);
                //_photo.TakeShot();
            }
        }

        private void FixedUpdate()
        {
            if (_isInPhotoMode)
            {
                foreach (PicturabelObject po in _pictarblesObject)
                {
                    if (po.gameObject.GetComponent<Renderer>().isVisible)
                    {
                        float dist = Vector3.Distance(po.gameObject.transform.position, _cameraObj.transform.position);

                        if (dist > maxDist)
                        {
                            po.EffectVisible(false);
                            photoTask.CheckName(po.gameObject.name, false);
                            continue;
                        }

                        RaycastHit hit;
                        if (Physics.Raycast(_cameraObj.transform.position, (po.gameObject.transform.position - _cameraObj.transform.position).normalized, out hit))
                        {
                            if (hit.collider.gameObject == po.gameObject)
                            {
                                po.EffectVisible(true);
                                photoTask.CheckName(po.gameObject.name);
                            }
                            else
                            {
                                po.EffectVisible(false);
                                photoTask.CheckName(po.gameObject.name, false);
                            }
                        }
                    }
                    else
                    {
                        po.EffectVisible(false);
                        photoTask.CheckName(po.gameObject.name, false);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public void ChangeEffectVisibility(bool visible)
        {
            foreach (PicturabelObject go in _pictarblesObject)
            {
                go.EffectVisible(visible);
            }

            //canvas.gameObject.SetActive(visible);
            photoTask.SetTextVisibility(visible);
        }

        public void SwitchView()
        {
            _isInPhotoMode = !_isInPhotoMode;

            //canvas.gameObject.SetActive(_isInPhotoMode);
            ChangeEffectVisibility(_isInPhotoMode);

            //disable other part of the canva when in photo mode
            foreach(GameObject go in switchVisibility)
            {
                go.SetActive(!go.activeInHierarchy);
            }
        }

        #endregion

    }
}
