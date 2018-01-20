using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[InitializeOnLoad]
public static class MyConsoleService {

	public const string assetPath = "Assets/Editor/MyConsole/Resources/ConsoleAsset.asset";

	public static MyConsoleAsset _logAsset;
	public static MyConsoleAsset logAsset {
		get {
			if (_logAsset == null) {
				_logAsset = LoadOrCreateAsset();
			}
			return _logAsset;
		}
		set {
			_logAsset = value;
		}
	}

	public static MyConsoleAsset LoadOrCreateAsset () {
		var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(MyConsoleAsset)) as MyConsoleAsset;

		if (asset == null) {
			asset = ScriptableObject.CreateInstance<MyConsoleAsset>();
			AssetDatabase.CreateAsset(asset, assetPath);
			AssetDatabase.SaveAssets();
		}

		return asset;
	}

	[MenuItem("Window/MyConsole")]
	static void ShowMyConsole()
	{
		MyConsole window = EditorWindow.GetWindow<MyConsole>("MyConsole", true);
		window.Show();
	}

	static MyConsoleService () {
		RegisterHandlers();
	}

	[DidReloadScripts]
	public static void OnScriptsReloaded() {
		RegisterHandlers();
	}

	public static void RegisterHandlers () {
		EditorApplication.playmodeStateChanged -= PlayModeChange;
		EditorApplication.playmodeStateChanged += PlayModeChange;

		if (MyConsole.instance != null) {
			MyConsole.instance.RegisterHandlers();
		}
	}

	public static void PlayModeChange() {
		RegisterHandlers();
	}
}
