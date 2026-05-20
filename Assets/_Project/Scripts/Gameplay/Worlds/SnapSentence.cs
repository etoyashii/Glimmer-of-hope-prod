using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Gameplay.ScriptableObjects;
using GlimmerOfHope.Gameplay.Character.SpecialActions;

namespace GlimmerOfHope.Gameplay.World
{
    public class SnapSentence : MonoBehaviour
    {
        [SerializeField] private List<SnapZone> snapZones;
        [SerializeField] private List<SO_Word> expectedWords;

        [Range(0.1f, 4.0f)]
        [SerializeField] private float _snapRadius = 1.0f;

        [Range(0.001f, 4.0f)]
        [SerializeField] private float _releaseRadius = 1.2f;

        [Range(10.0f, 4000.0f)]
        [SerializeField] private float _velocityLimitIn = 25.0f;

        [Range(0.001f, 4.0f)]
        [SerializeField] private float _velocityLimitOut = 0.01f;

        [SerializeField] private SkillManager _skillManager;
        private SO_Word[] currentWords;


        private void Awake()
        {
            if (snapZones.Count != expectedWords.Count)
                Debug.LogError("Le nombre de zones et de mots attendus doit correspondre !");

            currentWords = new SO_Word[snapZones.Count];
        }

        public void UpdateSnap(Word word)
        {
            if (word == null || word.SOWord == null) return;

            SnapZone snappedZone = null;
            foreach (var zone in snapZones)
            {
                if (currentWords[zone.Index] == word.SOWord)
                {
                    snappedZone = zone;
                    break;
                }
            }

            if (snappedZone != null)
            {
                float dist = Vector3.Distance(word.transform.position, snappedZone.transform.position);
                Rigidbody rb = word.GetComponent<Rigidbody>();

                if (dist > _releaseRadius)
                {
                    ReleaseWord(word, snappedZone);
                }
                else
                {
                    if (rb != null && rb.linearVelocity.magnitude < _velocityLimitOut)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;

                        word.transform.position = Vector3.Lerp(
                            word.transform.position,
                            snappedZone.transform.position,
                            0.2f
                        );
                    }
                }

                return;
            }

            float minDist = float.MaxValue;
            SnapZone closest = null;
            foreach (var zone in snapZones)
            {
                if (zone.IsOccupied) continue;

                float dist = Vector3.Distance(word.transform.position, zone.transform.position);
                if (dist < minDist && dist <= _snapRadius)
                {
                    minDist = dist;
                    closest = zone;
                }
            }

            if (closest != null)
            {
                Rigidbody rb = word.GetComponent<Rigidbody>();

                if (rb != null && rb.linearVelocity.magnitude < _velocityLimitIn)
                {
                    word.transform.position = Vector3.Lerp(
                        word.transform.position,
                        closest.transform.position,
                        0.2f
                    );

                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                closest.IsOccupied = true;
                currentWords[closest.Index] = word.SOWord;

                CheckSentence();
            }
        }

        private void OnDrawGizmos()
        {
            if (snapZones == null) return;

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            foreach (var zone in snapZones)
            {
                if (zone == null) continue;

                Gizmos.DrawSphere(zone.transform.position, _snapRadius);
            }
        }

        private void CheckSentence()
        {
            for (int i = 0; i < expectedWords.Count; i++)
            {
                if (currentWords[i] != expectedWords[i])
                    return;
            }

            Debug.Log("Phrase correcte !");

            _skillManager.UnlockSkill();
        }

        public void ReleaseWord(Word word, SnapZone zone)
        {
            if (word == null || zone == null) return;

            currentWords[zone.Index] = null;
            zone.IsOccupied = false;

            CheckSentence();
        }
    }
}