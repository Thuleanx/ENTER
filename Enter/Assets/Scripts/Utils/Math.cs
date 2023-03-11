using UnityEngine;

namespace Enter.Utils {
	public class Math {
		// See https://www.rorydriscoll.com/2016/03/07/frame-rate-independent-damping-using-lerp/ for how framerate independent lerp works

		// C# templates is bad so this is how we do it
		public static Vector2 Damp(Vector2 source, Vector2 target, float smoothing, float dt) {
			float t = Mathf.Exp(-smoothing * dt); 
			return source * t + target * (1 - t);
		}

		public static float Damp(float source, float target, float smoothing, float dt) {
			float t = Mathf.Exp(-smoothing * dt); 
			return source * t + target * (1 - t);
		}
	}
}