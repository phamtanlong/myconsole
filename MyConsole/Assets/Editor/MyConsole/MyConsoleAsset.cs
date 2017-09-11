using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

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

	[SerializeField]
	public List<Log> compileErrorLogs = new List<Log>();

	public int countLog { get; private set; }
	public int countWarn { get; private set; }
	public int countError { get; private set; }

	private void updateCount () {
		countLog = logs.Count(x => x.type == LogType.Log);
		countWarn = logs.Count(x => x.type == LogType.Warning);
		countError = logs.Count - countLog - countWarn;
	}

	public bool containsCompileErrorLog (string condition) {
		return compileErrorLogs.Any(x => x.condition.Equals(condition));
	}

	public void addLog (Log log) {
		logs.Add(log);

		if (log.type == LogType.Log)
			countLog++;
		else if (log.type == LogType.Warning)
			countWarn++;
		else
			countError++;

		if (log.isCompileError)
			compileErrorLogs.Add(log);
	}

	public void removeAll () {
		logs.Clear();
		compileErrorLogs.Clear();

		countLog = 0;
		countWarn = 0;
		countError = 0;
	}

	public void removeAllCompileError () {
		int count = logs.RemoveAll(x => x.isCompileError);
		compileErrorLogs.Clear();

		countError -= count;
	}
}

[System.Serializable]
public class CallStack {
	public string path;
	public int lineNumber;
}

[System.Serializable]
public class Log {

	//origin data
	public string condition = string.Empty;
	public string stackTrace = string.Empty;
	public LogType type;

	//calculated data
	//public bool selected;
	public string content; //text to show in list
	public int number;

	public CallStack callstack;
	public string file = string.Empty;
	public float time;
	public long frame;

	public bool isCompileError = false;
}
