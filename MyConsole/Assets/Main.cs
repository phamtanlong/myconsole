using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour {

	public GameObject theParticle;
	public Vector3 position;
	public string nameOfMe;

	float last = 0;

	void Update () {
		if (Time.realtimeSinceStartup - last > 1) {
			last = Time.realtimeSinceStartup;

			Debug.Log("New Log " + Time.realtimeSinceStartup);
		}
	}

	[ContextMenu("Say")]
	public void Say (Job job, bool theBool, int theInt, float theFloat, double theDouble, string theString, GameObject theGameObject, Vector2 vec2, Vector3 vec3, Vector4 vec4) {
		Debug.LogError("Say: " + job
			+ ", theBool: " + theBool
			+ ", theInt: " + theInt
			+ ", theFloat: " + theFloat
			+ ", theDouble: " + theDouble
			+ ", theString: " + theString
			+ ", theGameObject: " + theGameObject);
	}

	[ContextMenu("Get High")]
	public void GetHigh () {
		
	}

	[ContextMenu("Run")]
	public void RunToKill (bool theBool, int theInt, float theFloat, double theDouble, string theString, GameObject theGameObject) {
		Debug.LogError("Say: " + theString);
	}
}


public enum Job {
	Teacher,
	Doctor,
	Student
}

