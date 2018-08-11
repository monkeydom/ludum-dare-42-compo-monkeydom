using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using MonkeydomGeneral;

namespace MonkeydomSpecific {
	public class GameController : Singleton<GameController> {

		[Header("Outlets")]
		public TextMeshPro debugOutput;

		public override void Awake() {
			base.Awake();
			if (gameObject) { // take care of the variant where the singleton already existed, so only do this if we aren't destroyed
				InitGame();
			}
		}

		void InitGame() {
			Debug.Log("Here goes nothing");
		}


		private void Update() {
			if (Input.anyKeyDown) {
				FindObjectOfType<LevelController>().Start();
			}
		}

		public void DebugOutput(string str) {
			debugOutput.text = str;
		}

	}
}