using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay.GCamera
{

    /// <summary>
    /// Move the view according to the move of the mouse
    /// </summary>
    public class ViewController : MonoBehaviour
    {
        #region Public Properties

        public float mouseSensitivity = 1f;

        #endregion

        #region Private Properties

        private Vector2 _centerScreen;
        private bool _mouseIsLock = true;

        #endregion

        #region Unity LifeCycle

        void Start()
        {
            _centerScreen = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);

            Cursor.visible = false;
            Mouse.current.WarpCursorPosition(_centerScreen);
        }

        private void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.visible = _mouseIsLock;

                _mouseIsLock = !_mouseIsLock;
            }
        }

        private void FixedUpdate()
        {
            //handle rotate camera with mouse
            if (_mouseIsLock)
            {
                Vector2 mouseMove = new Vector2(_centerScreen.x - Mouse.current.position.x.value, _centerScreen.y - Mouse.current.position.y.value);
                Mouse.current.WarpCursorPosition(_centerScreen);

                transform.Rotate(transform.up, -mouseMove.x * mouseSensitivity);
            }

        }

        #endregion
    }
}
