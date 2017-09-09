using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "ConsoleAsset", menuName = "Tools/Create Console Asset", order = 1)]
[System.Serializable]
public class ConsoleAsset : ScriptableObject {

	[System.Serializable]
	public class Log {
		public bool selected;
		public string condition;
		public string stackTrace;
		public LogType type;
	}

	public bool showLog = true;
	public bool showWarn = true;
	public bool showError = true;

	[SerializeField]
	public List<Log> logs = new List<Log>();
}
