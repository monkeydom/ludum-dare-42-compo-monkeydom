using UnityEngine;

namespace MonkeydomGeneral {

	public class Singleton : MonoBehaviour {

		private static Singleton Instance = null;

		virtual public void Awake() {
			if (Instance) {
				Destroy(gameObject);
			} else {
				DontDestroyOnLoad(gameObject);
			}
		}

	}

}