using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class GrowthByLight : MonoBehaviour
    {

        [SerializeField] private GameObject _bloomedVegetal;
        [SerializeField] private GameObject _littleSprout;
        //[SerializeField] private Animator _animator;

        public void Growth()
        {
            _littleSprout.SetActive(false);
            AnimateGrowth();
            _bloomedVegetal.SetActive(true);
        }

        private void AnimateGrowth()
        {
            //_animator.Play("Growing"); //example
        }
    }
}
