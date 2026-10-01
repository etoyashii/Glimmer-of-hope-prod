using UnityEngine;
using TMPro;

namespace UnityStandardAssets.Utility
{
    public class SimpleActivatorMenu : MonoBehaviour
    {
        public TMP_Text camSwitchButton;
        public GameObject[] objects;

        private int m_CurrentActiveObject = 0;

        private void OnEnable()
        {
            if (objects == null || objects.Length == 0)
            {
                Debug.LogWarning("SimpleActivatorMenu : aucun objet n'est assigné.", this);
                return;
            }

            m_CurrentActiveObject = 0;
            SetActiveObject(m_CurrentActiveObject);
        }

        public void NextCamera()
        {
            if (objects == null || objects.Length == 0)
                return;

            m_CurrentActiveObject++;

            if (m_CurrentActiveObject >= objects.Length)
                m_CurrentActiveObject = 0;

            SetActiveObject(m_CurrentActiveObject);
        }

        private void SetActiveObject(int index)
        {
            if (objects == null || objects.Length == 0)
                return;

            if (index < 0 || index >= objects.Length)
                return;

            // Active uniquement l'objet sélectionné
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(i == index);
                }
            }

            // Met à jour le texte
            if (camSwitchButton != null && objects[index] != null)
            {
                camSwitchButton.text = objects[index].name;
            }
        }
    }
}