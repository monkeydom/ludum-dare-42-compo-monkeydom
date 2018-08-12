using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadStorageBehavior : MonoBehaviour {
	public Transform IndicatorTransform;

	public void SetProgress(float progress) {
		// Debug.Log($"{gameObject.name} progress:{progress}");
		IndicatorTransform.localPosition = Vector3.Lerp(new Vector3(0.5f, 0.0f, 1.05f), new Vector3(0.5f, 0, -0.0f), progress);
		IndicatorTransform.localEulerAngles = Vector3.Lerp(new Vector3(0f, 45f, 0f), new Vector3(0.0f, 0f, 0.0f), progress);
		var renderer = IndicatorTransform.GetComponent<MeshRenderer>();
		Material material = renderer.material;
		material.color = Color.Lerp(Color.black, Color.white, progress);
	}
}
