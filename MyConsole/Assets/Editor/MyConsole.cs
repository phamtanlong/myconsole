using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
		currentScrollViewHeight = position.height / 2;
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

		if (logAsset.errorPause && type == LogType.Exception || type == LogType.Assert) {
			Debug.DebugBreak();
			Debug.Break();
		}
	}

	void DoubleClickLog(ConsoleAsset.Log log)
	{
		var str = log.stackTrace.Trim();
		var index = str.IndexOf(") (at Assets/");
		str = str.Substring(index + 6);
		index = str.IndexOf(")");
		str = str.Substring(0, index);
		index = str.LastIndexOf(".cs:");

		string line = str.Substring(index + 4);
		int lineNumber = int.Parse(line);

		str = str.Substring(0, index + 3);

		Object obj = AssetDatabase.LoadAssetAtPath<Object>(str);
		AssetDatabase.OpenAsset(obj, lineNumber);
	}

	#endregion //Process

	#region Draw

	Vector2 scrollDetail;
	int selectedLog = 0;
	string mylog = string.Empty;

	List<ConsoleAsset.Log> visiableLogs = new List<ConsoleAsset.Log>();

	void OnGUI()
	{
		DrawToolbar();
		GUILayout.Space(ToolbarSpaceScrollView);

		visiableLogs = new List<ConsoleAsset.Log>();
		foreach (var log in logAsset.logs) {
			if ((log.type == LogType.Log && logAsset.showLog)
				|| (log.type == LogType.Warning && logAsset.showWarn)
				|| (log.type != LogType.Log && log.type != LogType.Warning && logAsset.showError)) {

				visiableLogs.Add(log);
			}
		}

		GUILayout.BeginVertical();
		{
			DrawLogList(visiableLogs);

			ResizeScrollView();

			GUILayout.Space(ToolbarSpaceScrollView);

			DrawDetail(visiableLogs);
		}
		GUILayout.EndVertical();
		Repaint();
	}

	void DrawToolbar () {
		GUI.Box(new Rect(0, 0, position.width, ToolbarHeight), string.Empty);
		GUILayout.BeginHorizontal();
		{
			RectOffset margin = new RectOffset(0, 0, 0, 0);

			GUIStyle styleToolbarButton = new GUIStyle(GUI.skin.button);
			styleToolbarButton.normal.textColor = Color.black;
			styleToolbarButton.fontSize = ToolbarFontSize;
			styleToolbarButton.margin = margin;
			styleToolbarButton.fixedHeight = ToolbarHeight;

			GUILayout.Space(5);
			if (GUILayout.Button("Clear", styleToolbarButton)) {
				logAsset.logs.Clear();
			}

			GUILayout.Label(mylog);
			GUILayout.FlexibleSpace();

			logAsset.collapse = GUILayout.Toggle(logAsset.collapse, "Collapse", styleToolbarButton);

			logAsset.clearOnPlay = GUILayout.Toggle(logAsset.clearOnPlay, "Clear On Play", styleToolbarButton);

			logAsset.errorPause = GUILayout.Toggle(logAsset.errorPause, "Error Pause", styleToolbarButton);

			GUILayout.Space(5);

			GUIContent logcontent = new GUIContent("" + logAsset.logs.Count(x => x.type == LogType.Log), logIcon);
			GUIContent warncontent = new GUIContent("" + logAsset.logs.Count(x => x.type == LogType.Warning), warnIcon);
			GUIContent errorcontent = new GUIContent("" + logAsset.logs.Count(x => x.type != LogType.Log && x.type != LogType.Warning), errorIcon);

			styleToolbarButton.padding = new RectOffset(2, 5, 0, 0);
			styleToolbarButton.fixedWidth = ToolbarButtonWidth;

			logAsset.showLog = GUILayout.Toggle(logAsset.showLog, logcontent, styleToolbarButton);

			logAsset.showWarn = GUILayout.Toggle(logAsset.showWarn, warncontent, styleToolbarButton);

			logAsset.showError = GUILayout.Toggle(logAsset.showError, errorcontent, styleToolbarButton);
		}
		GUILayout.EndHorizontal();
		CheckInput();
	}

	void CheckInput () {
		if (selectedLog >= 0) {
			if (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown) {
				bool changed = false;
				if (Event.current.keyCode == KeyCode.UpArrow) {
					if (selectedLog > 0) {
						visiableLogs[selectedLog].selected = false;
						selectedLog = selectedLog - 1;
						visiableLogs[selectedLog].selected = true;
						changed = true;
					}
				}

				if (Event.current.keyCode == KeyCode.DownArrow) {
					if (selectedLog < visiableLogs.Count - 1) {
						visiableLogs[selectedLog].selected = false;
						selectedLog = selectedLog + 1;
						visiableLogs[selectedLog].selected = true;
						changed = true;
					}
				}

				if (changed) {
					scrollPos.y = selectedLog * LogHeight;
					mylog = scrollPos.x + ", " + scrollPos.y;
				}
			}
		}
	}

	float lastClickInLog = 0;

	void DrawLogList (List<ConsoleAsset.Log> list) {
		var arr = list.Select(x => x.condition + "\n" + x.stackTrace).ToArray();

		scrollPos = GUILayout.BeginScrollView(scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar, 
			GUILayout.Width(position.width), GUILayout.Height(currentScrollViewHeight - ToolbarHeight - ToolbarSpaceScrollView));
		for (int i = 0; i < arr.Length; ++i) {
			
			if (list[i].selected) {
				styleLog.normal.textColor = Color.white;
				styleLog.normal.background = texLogActive;

				styleLog.onNormal.textColor = Color.white;
				styleLog.onNormal.background = texLogActive;
			} else {
				styleLog.normal.textColor = Color.black;
				if (i % 2 == 0)
					styleLog.normal.background = texLogBlack;
				else
					styleLog.normal.background = texLogWhite;
			}

			GUIContent logContent = new GUIContent(arr[i], logIcon);

			bool clicked = GUILayout.Button(logContent, styleLog);

			if (clicked) {
				float deltaTime = Time.realtimeSinceStartup - lastClickInLog;
				if (deltaTime < DoubleClickTime && list[i].selected) {
					DoubleClickLog(list[i]);
				}
				list[i].selected = true;
				lastClickInLog = Time.realtimeSinceStartup;
			}

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
			detail = log.condition + "\n" + log.stackTrace + "\n"; //"[" + log.type + "] " + 

		float fixedHeight = position.height - currentScrollViewHeight;
		scrollDetail = GUILayout.BeginScrollView(scrollDetail, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.Width(position.width), GUILayout.Height(fixedHeight));
		GUIStyle style = new GUIStyle(GUI.skin.label);
		style.focused = GUI.skin.box.focused;
		style.fixedWidth = position.width - 5;
		style.alignment = TextAnchor.UpperLeft;
		//style.fixedHeight = fixedHeight - ToolbarSpaceScrollView;
		style.wordWrap = true;
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

	#region Resizable

	Vector2 scrollPos = Vector2.zero;
	float currentScrollViewHeight;
	bool resize = false;
	Rect cursorChangeRect;

	void ResizeScrollView(){
		cursorChangeRect = new Rect(0, currentScrollViewHeight, this.position.width, SplitHeight);

		GUI.DrawTexture(cursorChangeRect, EditorGUIUtility.whiteTexture);

		EditorGUIUtility.AddCursorRect(cursorChangeRect, MouseCursor.ResizeVertical);

		if (Event.current.type == EventType.mouseDown && cursorChangeRect.Contains(Event.current.mousePosition)) {
			resize = true;
		}

		if (resize) {
			currentScrollViewHeight = Event.current.mousePosition.y;
			cursorChangeRect.Set(cursorChangeRect.x, currentScrollViewHeight, cursorChangeRect.width, cursorChangeRect.height);
		}

		if (Event.current.type == EventType.MouseUp) {
			resize = false;
		}

		if (currentScrollViewHeight < ToolbarHeight + MinScrollHeight)
			currentScrollViewHeight = ToolbarHeight + MinScrollHeight;
		
		if (position.height - currentScrollViewHeight < MinDetailHeight)
			currentScrollViewHeight = position.height - MinDetailHeight;
	}

	#endregion

	#region Constants

	const int ToolbarFontSize = 9;
	const float DoubleClickTime = 0.3f;
	const float ToolbarButtonWidth = 35;
	const float LogHeight = 33;
	const float ToolbarSpaceScrollView = 2;
	const float SplitHeight = 2;
	const float ToolbarHeight = 17;
	const float MinScrollHeight = 70;
	const float MinDetailHeight = 80;

	#endregion //Constants

	#region Resources Cache

	Texture _logIcon;
	public Texture logIcon {
		get {
			if (_logIcon == null) {
				_logIcon = Resources.Load("log") as Texture;
			}
			return _logIcon;
		}
	}

	Texture _warnIcon;
	public Texture warnIcon {
		get {
			if (_warnIcon == null) {
				_warnIcon = Resources.Load("warn") as Texture;
			}
			return _warnIcon;
		}
	}

	Texture _errorIcon;
	public Texture errorIcon {
		get {
			if (_errorIcon == null) {
				_errorIcon = Resources.Load("error") as Texture;
			}
			return _errorIcon;
		}
	}

	GUIStyle _styleLog;
	public GUIStyle styleLog {
		get {
			if (_styleLog == null) {

				_styleLog = new GUIStyle(GUI.skin.button);
				_styleLog.alignment = TextAnchor.UpperLeft;
				_styleLog.fixedHeight = LogHeight;
				_styleLog.padding = new RectOffset(10, 0, 3, 3);
				_styleLog.margin = new RectOffset(0, 0, 0, 0);

				_styleLog.active.textColor = Color.white;
				_styleLog.active.background = MakeTex(2, 2, new Color32(61, 128, 223, 255));

				_styleLog.onActive.textColor = Color.white;
				_styleLog.onActive.background = MakeTex(2, 2, new Color32(61, 128, 223, 255));

				_styleLog.wordWrap = false;
			}
			return _styleLog;
		}
	}

	Texture2D _texLogActive;
	public Texture2D texLogActive {
		get {
			if (_texLogActive == null) {
				_texLogActive = MakeTex(2, 2, new Color32(61, 128, 223, 255));
			}
			return _texLogActive;
		}
	}

	Texture2D _texLogBlack;
	public Texture2D texLogBlack {
		get {
			if (_texLogBlack == null) {
				_texLogBlack = MakeTex(2, 2, new Color32(216, 216, 216, 255));
			}
			return _texLogBlack;
		}
	}

	Texture2D _texLogWhite;
	public Texture2D texLogWhite {
		get {
			if (_texLogWhite == null) {
				_texLogWhite = MakeTex(2, 2, new Color32(200, 200, 200, 255));
			}
			return _texLogWhite;
		}
	}
	#endregion
}