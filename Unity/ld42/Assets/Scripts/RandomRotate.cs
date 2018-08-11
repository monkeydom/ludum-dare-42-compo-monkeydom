using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRotate : MonoBehaviour {

	// Use this for initialization
	void Start() {

	}

	void FixedUpdate() {
		transform.rotation = transform.rotation * Quaternion.EulerAngles(Random.Range(0, 0.01f), Random.Range(0, 0.01f), Random.Range(0, 0.01f));
	}
}
