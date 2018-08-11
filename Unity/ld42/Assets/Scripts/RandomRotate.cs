using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomRotate : MonoBehaviour {

	// Use this for initialization
	void Start() {

	}

	void FixedUpdate() {
		transform.rotation = transform.rotation * Quaternion.Euler(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));
	}
}
