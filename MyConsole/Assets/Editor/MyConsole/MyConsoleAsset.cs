using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "ConsoleAsset", menuName = "Tools/Create Console Asset", order = 1)]
[System.Serializable]
public class MyConsoleAsset : ScriptableObject {

	public bool searchRegex = false;

	public bool columnFile = false;
	public bool columnTime = false;
	public bool columnFrame = false;

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
	public string condition = string.Empty;
	public string stackTrace = string.Empty;
	public LogType type;

	public CallStack callstack;
	public string fileName = string.Empty;
	public float time;
	public long frame;
}
