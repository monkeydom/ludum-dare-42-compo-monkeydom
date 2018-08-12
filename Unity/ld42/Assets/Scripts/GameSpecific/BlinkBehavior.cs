using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkBehavior : MonoBehaviour {
	Material material;

	IEnumerator ShowClickCoroutine() {
		if (!material) {
			foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true)) {
				material = renderer.sharedMaterial;
				break;
			}
		}

		Color sourceColor = material.color;
		Color targetColor = Color.white;
		float startTime = Time.realtimeSinceStartup;

		float now = Time.realtimeSinceStartup;
		float duration = 0.10f;
		while (now < startTime + duration) {
			foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true)) {
				renderer.material.color = Color.Lerp(sourceColor, targetColor, (now - startTime) / (duration * 1.1f));
			}
			yield return null;
			now = Time.realtimeSinceStartup;
		}
		float targetTime = startTime + duration + duration * 2.0f;
		while (now < targetTime) {
			foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true)) {
				renderer.material.color = Color.Lerp(sourceColor, targetColor, (targetTime - now) / (duration * 2.0f));
			}
			yield return null;
			now = Time.realtimeSinceStartup;
		}

		while (now < targetTime) {
			foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>(true)) {
				Material byebye = renderer.material;
				renderer.sharedMaterial = material;
				Destroy(byebye);
			}
		}
	}

	public void Blink() {
		StartCoroutine(ShowClickCoroutine());
	}
}
