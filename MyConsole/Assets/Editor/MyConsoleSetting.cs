using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MyConsoleSetting : EditorWindow {

	ConsoleAsset _logAsset;
	ConsoleAsset logAsset {
		get {
			if (_logAsset == null) {
				_logAsset = MyConsole.LoadOrCreateAsset();
			}
			return _logAsset;
		}
		set {
			_logAsset = value;
		}
	}

	void OnGUI()
	{
		logAsset.showFile = GUILayout.Toggle(logAsset.showFile, "Show File Column");
	}
}
