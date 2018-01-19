using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldToCanvas : MonoBehaviour {

	public Transform target;
	public RectTransform parentRectTransform;
	public Camera mainCamera;

	public void Update () {

		var screenPosition = mainCamera.WorldToScreenPoint(target.position);
		Vector2 vec;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRectTransform, screenPosition, mainCamera, out vec);

		transform.localPosition = vec;
	}

}
