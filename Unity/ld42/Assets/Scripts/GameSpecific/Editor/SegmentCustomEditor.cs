using UnityEngine;
using UnityEditor;

namespace MonkeydomSpecific {

	[CustomEditor(typeof(SegmentBehavior))]
	public class SegmentCustomEditor : Editor {

		SegmentBehavior typeCastedTarget {
			get {
				return (SegmentBehavior)target;
			}
		}

		public override void OnInspectorGUI() {
			base.OnInspectorGUI();

			if (GUILayout.Button("Update")) {
				typeCastedTarget.UpdateAppearanceForCurrentValues();
			}
		}

	}

}