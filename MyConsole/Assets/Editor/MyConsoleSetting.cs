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
		//logAsset.columnFile = GUILayout.Toggle(logAsset.columnFile, "Show File Column");
	}
}
