using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using Encoder = TinyJSON.Encoder;
using Decoder = TinyJSON.Decoder;

public class Test : MonoBehaviour {

	[MenuItem("Test/Log 50")]
	public static void Log50 () {
		for (int i = 0; i < 50; i++) {
			AllLog();
		}
	}

	[MenuItem("Test/Log 100")]
	public static void Log100 () {
		for (int i = 0; i < 100; i++) {
			AllLog();
		}
	}

	[MenuItem("Test/Log 200")]
	public static void Log200 () {
		for (int i = 0; i < 200; i++) {
			AllLog();
		}
	}

	[MenuItem("Test/All Log")]
	public static void AllLog () {
		AnotherClass.SayHello();
		log1();
		Log();
		Warning();
		Error();
		Assertion();
		Exception();
	}

	[MenuItem("Test/Log")]
	public static void Log () {
		log1();
	}

	public static void log1 () {
		log2();
	} 

	public static void log2 () {
		log3();
	} 

	public static void log3 () {
		log4();
	} 

	public static void log4 () {
		log5();
	} 

	public static void log5 () {
		log6();
	} 

	public static void log6 () {
		log7();
	} 

	public static void log7 () {
		log8();
	} 

	public static void log8 () {
		log9();
	} 

	public static void log9 () {
		log10();
	} 

	public static void log10 () {
		Debug.Log("Alog <color=red>RED</color> This is a log " + (int)Time.realtimeSinceStartup);
	}

	[MenuItem("Test/Warning")]
	public static void Warning () {
		Debug.LogWarning("This is a log warning with a And seriously, unless you are familiar with GUIScripting, just ignore trying to figure out that code for now! I will be writin");
	}

	[MenuItem("Test/Error")]
	public static void Error () {
		Debug.LogError("This is a log error");
	}

	[MenuItem("Test/Assertion")]
	public static void Assertion () {
		Debug.LogAssertion("This is a log assertion");
	}

	[MenuItem("Test/Exception")]
	public static void Exception () {
		Debug.LogException(new System.Exception("This is a log exception"));
	}

	[MenuItem("Test/Log Big Text")]
	public static void LogBig () {
		Debug.Log("And seriously, unless you are familiar with GUIScripting, just ignore trying to figure out that code for now! I will be writing a GUIScript tutorial in the future to help with that :D\n\nOk so attach the new javascript to your MainCamera, and drag the Menu GUISkin into the MenuGUISkin variable of the Main Camera.\n\nClick play now and you'll see that the skin is the same as the default skin right now. Obviously we don't want that so, starting from the top we'll create our very own GUISkin!\n\nFont\nThis is the global font for the entire Skin. Each component has their own font element as well, but if the majority of your skin uses a particular theme, it is best to set the Font up top as that Font! The individual Font options will override the global one. Think of it this way, if the button/box/etc has not got an individually defined font, then use the globaly defined font. If it has an individually defined font, use that font for this element.\n\nImporting fonts is pretty straightforward! You just need to put in any .ttf OR .otf formatted fonts into your project tab in Unity!\n\nYou can actually drag these straight from your fonts file of your computer:");
	}

	[MenuItem("Test/Clear Logs")]
	public static void TestClear () {
		ClearLog();
	}

	static List<MyLogEntry> ClearLog()
	{
		List<MyLogEntry> result = new List<MyLogEntry>();

		MethodClear.Invoke(new object(), null);

		int count = (int)MethodGetCount.Invoke(new object(), null);
		if (count > 0) {
			//start getting entries
			var totalRow = (int)MethodStartGettingEntries.Invoke(new object(), null);
			//Debug.Log("TotalRow: " + totalRow);

			for (int i = 0; i < totalRow; ++i) {
				//get intry enternal
				object entry = Activator.CreateInstance(LogEntry);

				object[] arguments = new object[] { i, entry };
				MethodGetEntryInternal.Invoke(new object(), arguments);

				string json = Encoder.Encode(entry);
				MyLogEntry myEntry = Decoder.Decode(json).Make<MyLogEntry>();
				//Debug.Log(myEntry.condition);
				result.Add(myEntry);
			}

			Debug.Log("TotalRow: " + totalRow);
			foreach (var item in result) {
				Debug.Log(item.condition);
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

	#endregion

}

public class AnotherClass {

	public static void SayHello () {
		Debug.Log("Hello from another");
	}
}
