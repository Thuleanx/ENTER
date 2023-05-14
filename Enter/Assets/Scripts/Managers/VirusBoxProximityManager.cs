using UnityEngine;
using System.Collections.Generic;

namespace Enter {
    [RequireComponent(typeof(CompositeCollider2D))]
    public class VirusBoxProximityManager : MonoBehaviour {
        public static VirusBoxProximityManager Instance;

        CompositeCollider2D compositeCollider;

        void Awake() {
            compositeCollider = GetComponent<CompositeCollider2D>();
            Instance = this;
        }

        public float GetClosestVirus(Vector2 pos) {
            float minDist = float.PositiveInfinity;
            for (int i = 0; i < compositeCollider.pathCount; i++) {
                List<Vector2> points = new List<Vector2>();
                int numPoints = compositeCollider.GetPath(i, points);
                for (int j = 0; j < numPoints; j++)
                    minDist = Mathf.Min(minDist, Vector2.Distance(pos, points[j]));
            }
            return minDist;
        }
    }
}
