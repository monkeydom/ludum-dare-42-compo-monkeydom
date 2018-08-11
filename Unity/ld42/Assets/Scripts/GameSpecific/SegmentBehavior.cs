using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MonkeydomSpecific {

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

		public override string ToString() {
			return $"{base.ToString()} {fileNumber}, {segmentNumber}, {segmentLength}";
		}
#if UNITY_EDITOR
		protected virtual void OnValidate() {
			UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
		}

		public void NotifyOfUpdatedValues() {
			UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
			if (validationListener != null) {
				validationListener();
			}
		}
#endif

	}

	public class SegmentBehavior : MonoBehaviour {

		[SerializeField]
		public SegmentData segmentData;

		[Header("Outlets")]
		public Transform segmentTransform;
		public TextMeshPro textScript;

		// Use this for initialization
		void Start() {
			EnsureSegmentData();
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

		// Update is called once per frame
		void Update() {
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
			AdjustLength();
		}

		void AdjustLength() {
			segmentTransform.localScale = new Vector3(segmentData.segmentLength, 1, 1);
		}

		public override string ToString() {
			return $"{base.ToString()} {segmentData}";
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