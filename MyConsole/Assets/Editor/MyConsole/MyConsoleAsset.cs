using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System;
//using Encoder = TinyJSON.Encoder;
//using Decoder = TinyJSON.Decoder;

//[CreateAssetMenu(fileName = "ConsoleAsset", menuName = "MyConsole/Create Console Asset", order = 1)]
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

	public int compileErrorCount {
		get {
			return compileErrorLogs.Count;
		}
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
		
		var list = ClearLogEntries();

		List<Log> removeLogs = new List<Log>();
		foreach (var log in compileErrorLogs) {
			var contains = list.Any(x => log.callstack.path.Equals(x.file) && log.callstack.lineNumber == x.line);
			if (!contains)
				removeLogs.Add(log);
		}

		compileErrorLogs.RemoveAll(removeLogs.Contains);

		logs.Clear();
		logs.AddRange(compileErrorLogs);

		updateCount();
	}

	/// <summary>
	/// Clears the log and return list of compile error
	/// </summary>
	static List<MyLogEntry> ClearLogEntries()
	{
		List<MyLogEntry> result = new List<MyLogEntry>();

		MethodClear.Invoke(new object(), null);

		int count = (int)MethodGetCount.Invoke(new object(), null);
		if (count > 0) {
			//start getting entries
			var totalRow = (int)MethodStartGettingEntries.Invoke(new object(), null);

			for (int i = 0; i < totalRow; ++i) {
				//get intry enternal
				object entry = Activator.CreateInstance(LogEntry);

				object[] arguments = new object[] { i, entry };
				MethodGetEntryInternal.Invoke(new object(), arguments);

				string file = (string)LogEntryFile.GetValue(entry);
				int line = (int)LogEntryLine.GetValue(entry);

				result.Add(new MyLogEntry { file = file, line = line });
			}

			//finish getting entries
			MethodEndGettingEntries.Invoke(new object(), null);
		}

		return result;
	}

	#region Cache Reflection

	protected static MethodInfo _methodGetEntryInternal;
	public static MethodInfo MethodGetEntryInternal {
		get {
			if (_methodGetEntryInternal == null) {
				_methodGetEntryInternal = LogEntries.GetMethod("GetEntryInternal");
			}
			return _methodGetEntryInternal;
		}
	}

	protected static MethodInfo _methodStartGettingEntries;
	public static MethodInfo MethodStartGettingEntries {
		get {
			if (_methodStartGettingEntries == null) {
				_methodStartGettingEntries = LogEntries.GetMethod("StartGettingEntries");
			}
			return _methodStartGettingEntries;
		}
	}

	protected static MethodInfo _methodEndGettingEntries;
	public static MethodInfo MethodEndGettingEntries {
		get {
			if (_methodEndGettingEntries == null) {
				_methodEndGettingEntries = LogEntries.GetMethod("EndGettingEntries");
			}
			return _methodEndGettingEntries;
		}
	}

	protected static MethodInfo _methodGetCount;
	public static MethodInfo MethodGetCount {
		get {
			if (_methodGetCount == null) {
				_methodGetCount = LogEntries.GetMethod("GetCount");
			}
			return _methodGetCount;
		}
	}

	protected static MethodInfo _methodClear;
	public static MethodInfo MethodClear {
		get {
			if (_methodClear == null) {
				_methodClear = LogEntries.GetMethod("Clear");
			}
			return _methodClear;
		}
	}

	protected static Assembly _assemblyEditor;
	public static Assembly AssemblyEditor {
		get {
			if (_assemblyEditor == null) {
				_assemblyEditor = Assembly.GetAssembly(typeof(SceneView));
			}
			return _assemblyEditor;
		}
	}

	protected static Type _logEntries;
	public static Type LogEntries {
		get {
			if (_logEntries == null) {
				_logEntries = AssemblyEditor.GetType("UnityEditorInternal.LogEntries");
			}
			return _logEntries;
		}
	}

	protected static Type _logEntry;
	public static Type LogEntry {
		get {
			if (_logEntry == null) {
				_logEntry = AssemblyEditor.GetType("UnityEditorInternal.LogEntry");
			}
			return _logEntry;
		}
	}

	protected static FieldInfo _logEntryFile;
	public static FieldInfo LogEntryFile {
		get {
			if (_logEntryFile == null) {
				_logEntryFile = LogEntry.GetField("file");
			}
			return _logEntryFile;
		}
	}

	protected static FieldInfo _logEntryLine;
	public static FieldInfo LogEntryLine {
		get {
			if (_logEntryLine == null) {
				_logEntryLine = LogEntry.GetField("line");
			}
			return _logEntryLine;
		}
	}

	#endregion
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
