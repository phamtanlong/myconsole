using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using TinyJSON;
using System.Linq;

public class JundatCheat : MonoBehaviour {

	public class CallStack {
		public string path;
		public int lineNumber;
	}

	static CallStack LineToCallStack (string logLine) {
		try {
			var str = logLine.Trim();
			var index = str.IndexOf("Assets/");
			str = str.Substring(index);
			index = str.IndexOf(")");
			str = str.Substring(0, index);
			index = str.LastIndexOf(".cs");

			string line = str.Substring(index + 4);
			int lineNumber = StringToNumber(line);

			str = str.Substring(0, index + 3);
			return new CallStack { 
				path = str,
				lineNumber = lineNumber
			};
		} catch (Exception e) {
			//EditorUtility.DisplayDialog("Can not parse line", e.ToString(), "Ok");
		}

		return null;
	}

	private static int StringToNumber(string input)
	{
		bool findOut = false;
		string strNumber = string.Empty;
		for (int i = 0; i < input.Length; i++) {
			if (char.IsNumber(input[i])) {
				strNumber += input[i];
				findOut = true;
			} else if (findOut) {
				break;
			}
		}
		return int.Parse(strNumber);
	}

	[MenuItem("Jundat1/Test 1")]
	public static void Test1 () {
		string s1 = "Condition balah balah\nUnityEngine.Debug:Log(Object)\nJundatCheat:log2() (at Assets/JundatCheat.cs:29)\nJundatCheat:AllLog() (at Assets/JundatCheat.cs:10)";
		var line = s1.Split('\n').FirstOrDefault(x => x.Contains("Assets/") && x.Contains(".cs"));
		Debug.Log(line);
		CallStack callStack = LineToCallStack(line);
		Debug.Log(TinyJSON.Encoder.Encode(callStack));
	}

	[MenuItem("Jundat1/Test 2")]
	public static void Test2 () {
		string s1 = "Assets/Editor/MyConsole.cs(538,22): warning CS0168: The variable `e' is declared but never used";
		var line = s1.Split('\n').FirstOrDefault(x => x.Contains("Assets/") && x.Contains(".cs"));
		Debug.Log(line);
		CallStack callStack = LineToCallStack(line);
		Debug.Log(TinyJSON.Encoder.Encode(callStack));
	}

	[MenuItem("Jundat1/Test 3")]
	public static void Test3 () {
		string s1 = "ArgumentException: Getting control 4's position in a group with only 4 controls when doing KeyDown\nAborting\nUnityEngine.GUILayoutGroup.GetNext () (at /Users/builduser/buildslave/unity/build/Runtime/IMGUI/Managed/LayoutGroup.cs:114)\nUnityEngine.GUILayoutUtility.DoGetRect (UnityEngine.GUIContent content, UnityEngine.GUIStyle style, UnityEngine.GUILayoutOption[] options) (at /Users/builduser/buildslave/unity/build/Runtime/IMGUI/Managed/GUILayoutUtility.cs:379)\nUnityEngine.GUILayoutUtility.GetRect (UnityEngine.GUIContent content, UnityEngine.GUIStyle style, UnityEngine.GUILayoutOption[] options) (at /Users/builduser/buildslave/unity/build/Runtime/IMGUI/Managed/GUILayoutUtility.cs:339)\nUnityEngine.GUILayout.DoButton (UnityEngine.GUIContent content, UnityEngine.GUIStyle style, UnityEngine.GUILayoutOption[] options) (at /Users/builduser/buildslave/unity/build/Runtime/IMGUI/Managed/GUILayout.cs:53)\nUnityEngine.GUILayout.Button (System.String text, UnityEngine.GUIStyle style, UnityEngine.GUILayoutOption[] options) (at /Users/builduser/buildslave/unity/build/Runtime/IMGUI/Managed/GUILayout.cs:49)\nMyConsole.DrawDetail (System.Collections.Generic.List`1 list) (at Assets/Editor/MyConsole.cs:444)\nMyConsole.OnGUI () (at Assets/Editor/MyConsole.cs:233)\nSystem.Reflection.MonoMethod.Invoke (System.Object obj, BindingFlags invokeAttr, System.Reflection.Binder binder, System.Object[] parameters, System.Globalization.CultureInfo culture) (at /Users/builduser/buildslave/mono/build/mcs/class/corlib/System.Reflection/MonoMethod.cs:222)";
		var line = s1.Split('\n').FirstOrDefault(x => x.Contains("Assets/") && x.Contains(".cs"));
		Debug.Log(line);
		CallStack callStack = LineToCallStack(line);
		Debug.Log(TinyJSON.Encoder.Encode(callStack));
	}

	[MenuItem("Jundat/All Log")]
	public static void AllLog () {
		AnotherClass.SayHello();
		log1();
		Log();
		Warning();
		Error();
		Assertion();
		Exception();
	}

	[MenuItem("Jundat/Log")]
	public static void Log () {
		log1();
	}

	public static void log1 () {
		log2();
	} 

	public static void log2 () {
		Debug.Log("\nAlog <color=red>RED</color>\nThis is a log " + (int)Time.realtimeSinceStartup);
	}

	[MenuItem("Jundat/Warning")]
	public static void Warning () {
		Debug.LogWarning("This is a log warning");
	}

	[MenuItem("Jundat/Error")]
	public static void Error () {
		Debug.LogError("This is a log error");
	}

	[MenuItem("Jundat/Assertion")]
	public static void Assertion () {
		Debug.LogAssertion("This is a log assertion");
	}

	[MenuItem("Jundat/Exception")]
	public static void Exception () {
		Debug.LogException(new System.Exception("This is a log exception"));
	}

	[MenuItem("Jundat/Log Big")]
	public static void LogBig () {
		Debug.Log("And seriously, unless you are familiar with GUIScripting, just ignore trying to figure out that code for now! I will be writing a GUIScript tutorial in the future to help with that :D\n\nOk so attach the new javascript to your MainCamera, and drag the Menu GUISkin into the MenuGUISkin variable of the Main Camera.\n\nClick play now and you'll see that the skin is the same as the default skin right now. Obviously we don't want that so, starting from the top we'll create our very own GUISkin!\n\nFont\nThis is the global font for the entire Skin. Each component has their own font element as well, but if the majority of your skin uses a particular theme, it is best to set the Font up top as that Font! The individual Font options will override the global one. Think of it this way, if the button/box/etc has not got an individually defined font, then use the globaly defined font. If it has an individually defined font, use that font for this element.\n\nImporting fonts is pretty straightforward! You just need to put in any .ttf OR .otf formatted fonts into your project tab in Unity!\n\nYou can actually drag these straight from your fonts file of your computer:");
	}

}

public class AnotherClass {

	public static void SayHello () {
		Debug.Log("Hello from another");
	}
}
