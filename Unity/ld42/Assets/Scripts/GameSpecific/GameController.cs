﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using MonkeydomGeneral;

namespace MonkeydomSpecific {
	public class GameController : Singleton<GameController> {

		[Header("Outlets")]
		public TextMeshPro debugOutput;
		public TextMeshPro statusText;
		public Camera mainCamera;


		public override void Awake() {
			base.Awake();
			if (gameObject) { // take care of the variant where the singleton already existed, so only do this if we aren't destroyed
				InitGame();
			}
		}

		void InitGame() {
			Debug.Log($"Here goes nothing {Camera.main}");
		}

		void Update() {
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


		#region Statics

		public static int LayerMaskSegments {
			get {
				return 1 << 10;
			}
		}

		#endregion
	}
}