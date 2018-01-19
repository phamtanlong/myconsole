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

	static void ClearLog()
	{
		Assembly assembly = Assembly.GetAssembly(typeof(SceneView));
		Type logEntries = assembly.GetType("UnityEditorInternal.LogEntries");
		logEntries.GetMethod("Clear").Invoke (new object (), null);

//		//Debug.Log(Encoder.Encode(logEntries));
//		var mems = logEntries.GetMembers(BindingFlags.NonPublic);
//		foreach (var mem in mems.Where(x => x.MemberType == MemberTypes.Method)) {
//			Debug.Log(mem.MemberType + ": " + mem.Name + " => " + ((MethodInfo)mem).ReturnType);
//		}
//
//		Debug.Log("--------");
//
//		var method = logEntries.GetMethod("GetEntryCount");
//		var paras = method.GetParameters();
//		foreach (var para in paras) {
//			Debug.Log(para.ParameterType + ": " + para.Name);
//		}
//
//		Debug.Log("return " + method.ReturnType);

		int count = (int)logEntries.GetMethod("GetCount").Invoke(new object (), null);

		if (count > 0) {
			Debug.Log("Error = " + count);

			//start getting entries
			var startGettingEntries = logEntries.GetMethod("StartGettingEntries");
			var totalRow = startGettingEntries.Invoke(new object(), null);
			Debug.Log("TotalRow: " + totalRow);



//			//get 2 first line
//			var getFirst2Line = logEntries.GetMethod("GetFirstTwoLinesEntryTextAndModeInternal");
//
//			int row = 0;
//			int mask = 0;
//			string outString = (string) null;
//			object[] arguments = new object[] { row, mask, outString };
//
//			getFirst2Line.Invoke(new object(), arguments);
//
//			Debug.Log("row: " + arguments[0]);
//			Debug.Log("mask: " + arguments[1]);
//			Debug.Log("string: " + arguments[2]);



			//get intry enternal
			var getEntryInternal = logEntries.GetMethod("GetEntryInternal");

			Type logEntry = assembly.GetType("UnityEditorInternal.LogEntry");

			object entry = Activator.CreateInstance(logEntry);

			object[] arguments = new object[] { 0, entry };
			getEntryInternal.Invoke(new object(), arguments);

			string json = Encoder.Encode(entry);
			LogEntry myEntry = Decoder.Decode(json).Make<LogEntry>();
			Debug.Log(myEntry.condition);




			//finish getting entries
			var endGettingEntries = logEntries.GetMethod("EndGettingEntries");
			endGettingEntries.Invoke(new object(), null);
		}
	}

}

public class AnotherClass {

	public static void SayHello () {
		Debug.Log("Hello from another");
	}
}
