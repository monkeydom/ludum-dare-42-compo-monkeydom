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

		private SegmentBehavior previousSegment;

		void Update() {
			if (Input.GetButtonDown("Jump")) {
				FindObjectOfType<LevelController>().Start();
			}

			Vector3 mousePosition = Input.mousePosition;
			Ray ray = mainCamera.ScreenPointToRay(mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit)) {
				Transform objectHit = hit.transform;
				DebugOutput($" {ray} hit {hit} {objectHit.name}");
				SegmentBehavior behavior = objectHit.parent.GetComponentInParent<SegmentBehavior>();
				if (behavior != previousSegment) {
					if (previousSegment) {
						previousSegment.highlighted = false;
					}
					if (behavior) {
						behavior.highlighted = true;
					}
					previousSegment = behavior;
				}
			}

			if (Input.GetButtonDown("Fire1")) {
				if (previousSegment) {
					previousSegment.selected = !previousSegment.selected;
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
	}
}