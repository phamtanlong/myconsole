using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MyConsole : EditorWindow
{
	[MenuItem("Tools/My Console")]
	static void ShowMe()
	{
		MyConsole window = (MyConsole)EditorWindow.GetWindow(typeof(MyConsole));
		window.ShowTab();
	}

	const string assetPath = "Assets/ConsoleAsset.asset";
	static readonly GUILayoutOption[] opts = new GUILayoutOption[]{};

	static bool isInited = false;

	ConsoleAsset _logAsset;
	ConsoleAsset logAsset {
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

	#region Lifecycle

	void Awake () {
		ClearData();
		Init();
	}

	void OnDestroy () {
		Application.logMessageReceived -= LogHandler;
		ClearData();
	}

	void OnProjectChange () {
		Repaint();
	}

	void OnFocus () {
		Repaint();
	}

	void OnLostFocus () {
		Repaint();
	}

	void Update () {
		if (logAsset == null || isInited == false) {
			Init();
		}
	}

	#endregion //Lifecycle

	#region Process

	void ClearData () {
		logAsset.logs.Clear();
	}

	ConsoleAsset LoadOrCreateAsset () {
		var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ConsoleAsset)) as ConsoleAsset;

		if (asset == null) {
			asset = ScriptableObject.CreateInstance<ConsoleAsset>();
			AssetDatabase.CreateAsset(asset, assetPath);
			AssetDatabase.SaveAssets();
		}

		return asset;
	}

	void Init () {
		LoadOrCreateAsset();
		Application.logMessageReceived += LogHandler;
		isInited = true;
	}

	void LogHandler(string condition, string stackTrace, LogType type)
	{
		logAsset.logs.Add(new ConsoleAsset.Log {
			condition = condition,
			stackTrace = stackTrace,
			type = type
		});
		Repaint();
	}

	#endregion //Process

	#region Draw

	Vector2 scrollList;
	Vector2 scrollDetail;
	int selectedLog = 0;

	List<ConsoleAsset.Log> visiableLogs = new List<ConsoleAsset.Log>();

	void OnGUI()
	{
		visiableLogs = new List<ConsoleAsset.Log>();
		foreach (var log in logAsset.logs) {
			if ((log.type == LogType.Log && logAsset.showLog)
				|| (log.type == LogType.Warning && logAsset.showWarn)
				|| (log.type != LogType.Log && log.type != LogType.Warning && logAsset.showError)) {

				visiableLogs.Add(log);
			}
		}

		DrawToolbar();
		GUILayout.Space(2);
		DrawLogList(visiableLogs);
		DrawDetail(visiableLogs);
	}

	void DrawToolbar () {
		GUI.Box(new Rect(0, 0, position.width, 22), string.Empty);
		GUILayout.BeginHorizontal();
		{
			if (GUILayout.Button("Clear", opts)) {
				logAsset.logs.Clear();
			}

			Texture logIcon = Resources.Load("log") as Texture;
			Texture warnIcon = Resources.Load("warn") as Texture;
			Texture errorIcon = Resources.Load("error") as Texture;

			GUILayout.FlexibleSpace();

			GUIContent logcontent = new GUIContent("" + logAsset.logs.Count(x => x.type == LogType.Log), logIcon);
			GUIContent warncontent = new GUIContent("" + logAsset.logs.Count(x => x.type == LogType.Warning), warnIcon);
			GUIContent errorcontent = new GUIContent("" + logAsset.logs.Count(x => x.type != LogType.Log && x.type != LogType.Warning), errorIcon);

			GUIStyle styleLog = new GUIStyle(GUI.skin.button);
			styleLog.normal.textColor = Color.black;
			styleLog.fixedWidth = 35;
			logAsset.showLog = GUILayout.Toggle(logAsset.showLog, logcontent, styleLog);

			GUIStyle styleWarn = new GUIStyle(GUI.skin.button);
			styleWarn.normal.textColor = new Color32(201, 97, 0, 255);
			styleWarn.fixedWidth = 35;
			logAsset.showWarn = GUILayout.Toggle(logAsset.showWarn, warncontent, styleWarn);

			GUIStyle styleError = new GUIStyle(GUI.skin.button);
			styleError.normal.textColor = Color.red;
			styleError.fixedWidth = 35;
			logAsset.showError = GUILayout.Toggle(logAsset.showError, errorcontent, styleError);
		}
		GUILayout.EndHorizontal();
	}

	void DrawLogList (List<ConsoleAsset.Log> list) {
		var arr = list.Select(x => x.condition + "\n" + x.stackTrace).ToArray();

		GUIStyle styleLog = new GUIStyle(GUI.skin.button);
		styleLog.alignment = TextAnchor.UpperLeft;
		styleLog.fixedHeight = 33;
		styleLog.fixedWidth = position.width;
		styleLog.padding = new RectOffset(10, 0, 3, 3);
		styleLog.margin = new RectOffset(0, 0, 0, 0);

		styleLog.active.textColor = Color.white;
		styleLog.active.background = MakeTex(2, 2, new Color32(61, 128, 223, 255));

		styleLog.onActive.textColor = Color.white;
		styleLog.onActive.background = MakeTex(2, 2, new Color32(61, 128, 223, 255));

		styleLog.wordWrap = false;

		scrollList = GUILayout.BeginScrollView(scrollList, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(position.width), GUILayout.Height(position.height - 300));
		for (int i = 0; i < arr.Length; ++i) {
			
			if (list[i].selected) {
				styleLog.normal.textColor = Color.white;
				styleLog.normal.background = MakeTex(2, 2, new Color32(61, 128, 223, 255));

				styleLog.onNormal.textColor = Color.white;
				styleLog.onNormal.background = MakeTex(2, 2, new Color32(61, 128, 223, 255));
			} else {
				styleLog.normal.textColor = Color.black;
				if (i % 2 == 0)
					styleLog.normal.background = MakeTex(2, 2, new Color32(216, 216, 216, 255));
				else
					styleLog.normal.background = MakeTex(2, 2, new Color32(200, 200, 200, 255));
			}

			EditorGUI.BeginChangeCheck();
			GUILayout.Toggle(list[i].selected, arr[i], styleLog); //list[i].selected
			bool changed = EditorGUI.EndChangeCheck();
			if (changed)
				list[i].selected = true;

			if (list[i].selected) {
				selectedLog = i;
				foreach (var item in list) {
					item.selected = item == list[i];
				}
			}
		}
		GUILayout.EndScrollView();
	}

	void DrawDetail (List<ConsoleAsset.Log> list) {
		string detail = string.Empty;
		ConsoleAsset.Log log = null;

		if (selectedLog >= 0 && selectedLog < list.Count) {
			log = list[selectedLog];
		} else {
			if (list.Count > 0){
				log = list.Last<ConsoleAsset.Log>();
			}
		}

		if (log != null)
			detail = "[" + log.type + "] " + log.condition + "\n" + log.stackTrace + "\n";

		scrollDetail = GUILayout.BeginScrollView(scrollDetail, GUILayout.Width(position.width), GUILayout.Height(300 - 5));
		GUIStyle style = new GUIStyle(GUI.skin.box);
		style.focused = GUI.skin.box.focused;
		style.fixedWidth = position.width;
		style.alignment = TextAnchor.UpperLeft;
		style.fixedHeight = 300 - 5;
		GUILayout.TextArea(detail, style);
		GUILayout.EndScrollView();
	}

	private Texture2D MakeTex( int width, int height, Color col )
	{
		Color[] pix = new Color[width * height];
		for( int i = 0; i < pix.Length; ++i )
		{
			pix[ i ] = col;
		}
		Texture2D result = new Texture2D( width, height );
		result.SetPixels( pix );
		result.Apply();
		return result;
	}

	#endregion //Draw

}