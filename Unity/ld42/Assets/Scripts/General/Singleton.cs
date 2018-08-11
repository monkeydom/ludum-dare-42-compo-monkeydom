using UnityEngine;

namespace MonkeydomGeneral {

	public abstract class Singleton<T> : Singleton where T : Singleton<T> {
		public static T Instance {
			get {
				return _Instance;
			}
		}

		private static T _Instance;

		virtual public void Awake() {
			if (_Instance) {
				Destroy(gameObject);
			} else {
				DontDestroyOnLoad(gameObject);
				_Instance = (T)this;
			}
		}

	}

	public class Singleton : MonoBehaviour {
	}

}