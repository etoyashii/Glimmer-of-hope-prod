using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace GlimmerOfHope.Gameplay
{
    public sealed class GameEnvironment
    {
        private static GameEnvironment instance;
        private List<GameObject> checkpoints = new List<GameObject>();
        public List<GameObject> Checkpoints { get { return checkpoints; } }

        public static GameEnvironment Singleton
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameEnvironment();
                    instance.Checkpoints.AddRange(GameObject.FindGameObjectsWithTag("Checkpoint"));

                    instance.checkpoints = instance.checkpoints.OrderBy(waypoint => waypoint.name).ToList();
                }
                return instance;
            }
        }
    }
}
