using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "ConsoleAsset", menuName = "Tools/Create Console Asset", order = 1)]
[System.Serializable]
public class ConsoleAsset : ScriptableObject {

	public bool searchRegex = false;
	public bool showFile = false;

	public bool collapse = false;
	public bool clearOnPlay = true;
	public bool errorPause = false;
	public bool showLog = true;
	public bool showWarn = true;
	public bool showError = true;

	[SerializeField]
	public List<Log> logs = new List<Log>();
}

[System.Serializable]
public class CallStack {
	public string path;
	public int lineNumber;
}

[System.Serializable]
public class Log {
	public int number;
	public bool selected;
	public string condition;
	public string stackTrace;
	public LogType type;

	public CallStack callstack;
}
