using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using MonkeydomGeneral;

namespace MonkeydomSpecific {
	public class GameController : Singleton<GameController> {

		[Header("Outlets")]
		public TextMeshPro debugOutput;
		public TextMeshPro statusText;
		public TextMeshPro levelStatusText;
		public TextMeshPro topRightText;
		public Camera mainCamera;
		public GameObject startScreen;
		public LevelController levelController;


		public override void Awake() {
			base.Awake();
			if (gameObject) { // take care of the variant where the singleton already existed, so only do this if we aren't destroyed
				InitGame();
			}
		}

		void InitGame() {
		}

		void Update() {
			if (startScreen.activeInHierarchy) {
				if (Input.anyKeyDown) {
					levelController.gameObject.SetActive(true);
					startScreen.SetActive(false);
				}
			}
		}

		public void DebugOutput(string str) {
			if (debugOutput) {
				debugOutput.text = str;
			} else {
				Debug.Log($"DebugOutput: {str}");
			}
		}

		public void SetStatusText(string str) {
			if (statusText) {
				statusText.text = str;
			} else {
				Debug.Log($"Status Text: {str}");
			}
		}

		public void SetLevelStatusText(string str) {
			if (levelStatusText) {
				levelStatusText.text = str;
			} else {
				Debug.Log($"Level Status Text: {str}");
			}
		}

		public void SetTopRightText(string str) {
			if (topRightText) {
				topRightText.text = str;
			} else {
				Debug.Log($"Top right Text: {str}");
			}
		}

		#region Statics

		public static int LayerMaskSegments {
			get {
				return 1 << 10;
			}
		}

		#endregion
	}
}