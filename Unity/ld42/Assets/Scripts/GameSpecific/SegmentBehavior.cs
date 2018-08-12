using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MonkeydomSpecific {

	public enum SegmentDataPartType {
		Start,
		Middle,
		End,
	}

	public class SegmentData : ScriptableObject {
		[HideInInspector]
		public System.Action validationListener;

		[Range(1, 1024)]
		public int fileNumber = 1;

		[Space(5)]
		[Range(1, 99)]
		public int segmentNumber = 1;
		[Range(1, 128)]
		public int segmentLength = 1;

		public int location = 0;

		public int? temporaryTargetCopyPlacementLocation;

		public SegmentDataPartType partType;
		public Level level;

		public int PositionAfter {
			get {
				return location + segmentLength;
			}
		}

		public override string ToString() {
			return $"File{fileNumber}#{segmentNumber}-{segmentLength}";
		}
#if UNITY_EDITOR
		//protected virtual void OnValidate() {
		//	UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
		//}

		//public void NotifyOfUpdatedValues() {
		//	UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
		//	if (validationListener != null) {
		//		validationListener();
		//	}
		//}
#endif

	}

	public class SegmentBehavior : MonoBehaviour {

		public SegmentData segmentData;

		public bool highlighted;
		public bool selected;

		[Header("Outlets")]
		public Transform segmentTransform;
		public Transform segmentTransform2;
		public TextMeshPro textScript;

		public Material material;

		Animator selectionAnimator;

		// Use this for initialization
		void Start() {
			EnsureSegmentData();
			selectionAnimator = GetComponent<Animator>();
		}

		public void SetSegmentData(SegmentData data) {
			if (data != segmentData) {
				if (segmentData) {
					if (segmentData.validationListener == OnValidate) {
						segmentData.validationListener = null;
					}
				}
				segmentData = data;
				if (data) {
					data.validationListener = OnValidate;
					UpdateAppearanceForCurrentValues();
				}
			}
		}

		void EnsureSegmentData() {
			if (!segmentData) {
				SetSegmentData(new SegmentData());
			}
		}

		IEnumerator ShowClickCoroutine() {
			if (!material) {
				foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true)) {
					if (renderer.gameObject.tag == "SegmentObject") {
						material = renderer.sharedMaterial;
						break;
					}
				}
			}

			Color sourceColor = material.color;
			Color targetColor = Color.white;
			float startTime = Time.realtimeSinceStartup;

			float now = Time.realtimeSinceStartup;
			float duration = 0.10f;
			while (now < startTime + duration) {
				foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true)) {
					if (renderer.gameObject.tag == "SegmentObject") {
						renderer.material.color = Color.Lerp(sourceColor, targetColor, (now - startTime) / (duration * 1.1f));
					}
				}
				yield return null;
				now = Time.realtimeSinceStartup;
			}
			float targetTime = startTime + duration + duration * 2.0f;
			while (now < targetTime) {
				foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true)) {
					if (renderer.gameObject.tag == "SegmentObject") {
						renderer.material.color = Color.Lerp(sourceColor, targetColor, (targetTime - now) / (duration * 2.0f));
					}
				}
				yield return null;
				now = Time.realtimeSinceStartup;
			}

			while (now < targetTime) {
				foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true)) {
					if (renderer.gameObject.tag == "SegmentObject") {
						Material byebye = renderer.material;
						renderer.sharedMaterial = material;
						Destroy(byebye);
					}
				}
			}
		}

		public void ShowClick() {
			StartCoroutine(ShowClickCoroutine());
		}

		// Update is called once per frame
		void Update() {
			// Todo: update somwhere else to be less expensive
			selectionAnimator.SetBool("highlighted", highlighted);
			selectionAnimator.SetBool("selected", selected);
		}

		void SetupSegment(int fileNumber, int segmentNumber, int segmentLength) {
			segmentData.segmentNumber = segmentNumber;
			segmentData.fileNumber = fileNumber;
			segmentData.segmentLength = segmentLength;
			UpdateAppearanceForCurrentValues();
		}

		public void UpdateAppearanceForCurrentValues() {
			// Debug.Log($"Update to: {this}");
			textScript.text = $"{segmentData.segmentNumber}";
			AdjustLengthForIntRange(new IntRange(segmentData.location, segmentData.segmentLength));
		}

		public void AdjustLengthForIntRange(IntRange range) {
			if (segmentData.level == null) {
				return;
			}
			var extents = segmentData.level.ExtentsForIntRange(range);
			segmentTransform.localScale = new Vector3(extents[0].length, 1, 1);
			if (extents[1] != null) {
				segmentTransform2.gameObject.SetActive(true);
				segmentTransform2.localScale = new Vector3(extents[1].length, 1, 1);
				segmentTransform2.localPosition = segmentTransform.localPosition + ((float)segmentData.level.width - (float)extents[0].length) * Vector3.left + Vector3.down;
			} else {
				segmentTransform2.gameObject.SetActive(false);
			}
		}

		public override string ToString() {
			return $"#[{segmentData}]";
		}


		#region editor
		void OnValidate() {
			EnsureSegmentData();
			//			Debug.Log("Validate");
			UpdateAppearanceForCurrentValues();
		}
		#endregion

	}
}