using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour {
	
	float last = 0;

	void Update () {
		if (Time.realtimeSinceStartup - last > 1) {
			last = Time.realtimeSinceStartup;

			Debug.Log("New Log " + Time.realtimeSinceStartup);
		}
	}
}
