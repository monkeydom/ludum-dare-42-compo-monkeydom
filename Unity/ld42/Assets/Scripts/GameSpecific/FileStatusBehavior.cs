using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


namespace MonkeydomSpecific {
	public class FileStatusBehavior : MonoBehaviour {

		public TextMeshPro line1;
		public TextMeshPro line2;
		public TextMeshPro line3;

		public bool highlighted;

		Transform _tr;

		private void Start() {
			_tr = transform;
		}

		// Update is called once per frame
		void Update() {
			Vector3 targetScale = Vector3.one;
			if (highlighted) {
				targetScale = targetScale * 1.1f;
			}

			_tr.localScale = Vector3.Lerp(_tr.localScale, targetScale, Time.deltaTime * 30.0f);
		}


		public void SetTexts(string text1, string text2, string text3) {
			line1.text = text1;
			line2.text = text2;
			line3.text = text3;
		}
	}
}