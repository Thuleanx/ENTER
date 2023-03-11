using UnityEngine;
using NaughtyAttributes;

namespace Enter {
	public class PhysicsPipe : MonoBehaviour {
		[SerializeField] float blocksPerMinute;
		[ShowAssetPreview, SerializeField] GameObject physicsObjectPrefab;
		float timeSinceLastSpawn = -1;

		// sadguh no object pool
		public void CreatePhysicsObject() {
			Instantiate(physicsObjectPrefab, transform.position, Quaternion.identity);
		}

		void Update() {
			if (timeSinceLastSpawn + 60f / blocksPerMinute < Time.time) {
				CreatePhysicsObject();
				timeSinceLastSpawn = Time.time;
			}
		}
	}
}