using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MonkeydomGeneral;

namespace MonkeydomSpecific {
	public class GameController : Singleton {

		public override void Awake() {
			base.Awake();
			if (gameObject) { // take care of the variant where the singleton already existed, so only do this if we aren't destroyed
				InitGame();
			}
		}

		void InitGame() {
			Debug.Log("Here goes nothing");
		}
	}
}