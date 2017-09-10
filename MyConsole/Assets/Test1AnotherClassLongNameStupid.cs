using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class Test1AnotherClassLongNameStupid : MonoBehaviour {
	
	[MenuItem("Test1/All Log")]
	public static void AllLog () {
		AnotherClass.SayHello();
		log1();
		Log();
		Warning();
		Error();
		Assertion();
		Exception();
	}

	[MenuItem("Test1/Log")]
	public static void Log () {
		log1();
	}

	public static void log1 () {
		log2();
	} 

	public static void log2 () {
		Debug.Log("Alog <color=red>RED</color> This is a log " + (int)Time.realtimeSinceStartup);
	}

	[MenuItem("Test1/Warning")]
	public static void Warning () {
		Debug.LogWarning("This is a log warning");
	}

	[MenuItem("Test1/Error")]
	public static void Error () {
		Debug.LogError("This is a log error");
	}

	[MenuItem("Test1/Assertion")]
	public static void Assertion () {
		Debug.LogAssertion("This is a log assertion");
	}

	[MenuItem("Test1/Exception")]
	public static void Exception () {
		Debug.LogException(new System.Exception("This is a log exception"));
	}

	[MenuItem("Test1/Log Big Text")]
	public static void LogBig () {
		Debug.Log("And seriously, unless you are familiar with GUIScripting, just ignore trying to figure out that code for now! I will be writing a GUIScript tutorial in the future to help with that :D\n\nOk so attach the new javascript to your MainCamera, and drag the Menu GUISkin into the MenuGUISkin variable of the Main Camera.\n\nClick play now and you'll see that the skin is the same as the default skin right now. Obviously we don't want that so, starting from the top we'll create our very own GUISkin!\n\nFont\nThis is the global font for the entire Skin. Each component has their own font element as well, but if the majority of your skin uses a particular theme, it is best to set the Font up top as that Font! The individual Font options will override the global one. Think of it this way, if the button/box/etc has not got an individually defined font, then use the globaly defined font. If it has an individually defined font, use that font for this element.\n\nImporting fonts is pretty straightforward! You just need to put in any .ttf OR .otf formatted fonts into your project tab in Unity!\n\nYou can actually drag these straight from your fonts file of your computer:");
	}

}