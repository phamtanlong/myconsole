using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Object = UnityEngine.Object;
using System.Text.RegularExpressions;

public class MyConsole : EditorWindow, IHasCustomMenu
{
	#region IHasCustomMenu implementation

	public void AddItemsToMenu(GenericMenu menu)
	{
		menu.AddItem(new GUIContent("Setting"), false, () => {
			MyConsoleSetting window = (MyConsoleSetting)EditorWindow.GetWindow(typeof(MyConsoleSetting));
			window.Show();
		});

		menu.AddSeparator(string.Empty);

		menu.AddItem(new GUIContent("Columns/File"), logAsset.columnFile, () => {
			logAsset.columnFile = ! logAsset.columnFile;
		});

		menu.AddItem(new GUIContent("Columns/Time"), logAsset.columnTime, () => {
			logAsset.columnTime = ! logAsset.columnTime;
		});

		menu.AddItem(new GUIContent("Columns/Frame"), logAsset.columnFrame, () => {
			logAsset.columnFrame = ! logAsset.columnFrame;
		});
	}

	#endregion

	[MenuItem("Tools/My Console")]
	static void ShowMe()
	{
		MyConsole window = (MyConsole)EditorWindow.GetWindow(typeof(MyConsole));
		window.Show();
	}

	public const string assetPath = "Assets/ConsoleAsset.asset";
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
		RegisterHandlers();
		//Repaint();
	}

	void OnFocus () {
		RegisterHandlers();
		//Repaint();
	}

	void OnLostFocus () {
		RegisterHandlers();
		//Repaint();
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

	public static ConsoleAsset LoadOrCreateAsset () {
		var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(ConsoleAsset)) as ConsoleAsset;

		if (asset == null) {
			asset = ScriptableObject.CreateInstance<ConsoleAsset>();
			AssetDatabase.CreateAsset(asset, assetPath);
			AssetDatabase.SaveAssets();
		}

		return asset;
	}

	void RegisterHandlers () {
		Application.logMessageReceived -= LogHandler;
		Application.logMessageReceived += LogHandler;

		EditorApplication.playmodeStateChanged -= PlayModeChange;
		EditorApplication.playmodeStateChanged += PlayModeChange;
	}

	void Init () {
		LoadOrCreateAsset();
		RegisterHandlers();
		isInited = true;
	}

	void LogHandler(string condition, string stackTrace, LogType type)
	{
		Log log = new Log {
			condition = condition,
			stackTrace = stackTrace,
			type = type
		};

		CallStack callStack = GetCallStackInLog(log);
		log.callstack = callStack;

		if (callStack != null) {
			var index = callStack.path.LastIndexOf("/");
			string fileName = callStack.path.Substring(index + 1);
			log.fileName = fileName;
		}

		log.time = Time.realtimeSinceStartup;

		log.frame = Time.frameCount;

		logAsset.logs.Add(log);

		if (logAsset.errorPause && (type == LogType.Exception || type == LogType.Assert)) {
			if (EditorApplication.isPlaying) {
				needPause = true;
			}
		}
	}

	bool needPause = false;

	void PlayModeChange()
	{
		if (EditorApplication.isPlayingOrWillChangePlaymode) {
			if (!EditorApplication.isPlaying) {
				logAsset.logs.Clear();
			}
		}
	}

	void DoubleClickLog(Log log)
	{
		OpenCallStack(log.callstack);
	}

	void OpenCallStack (CallStack callStack) {
		if (callStack != null) {
			Object obj = AssetDatabase.LoadAssetAtPath<Object>(callStack.path);
			AssetDatabase.OpenAsset(obj, callStack.lineNumber);
		}
	}

	void HightLightFile(Log log)
	{
		if (log.callstack != null) {
			Object obj = AssetDatabase.LoadAssetAtPath<Object>(log.callstack.path);
			EditorGUIUtility.PingObject(obj);
			Selection.activeObject = obj;
		}
	}

	#endregion //Process

	#region Draw

	bool focusOnListLog = false;
	bool focusOnListDetail = false;

	void FocusOnListLog () {
		focusOnListLog = true;
		focusOnListDetail = false;
	}

	void FocusOnListDetail () {
		focusOnListLog = false;
		focusOnListDetail = true;
	}

	string keySearch = string.Empty;
	Vector2 scrollDetail;
	int selectedLog = 0;
	string mylog = string.Empty;

	List<Log> visiableLogs = new List<Log>();
	List<string> detailLines = new List<string>();

	void OnGUI()
	{
		DrawToolbar();
		GUILayout.Space(ToolbarSpaceScrollView);

		Dictionary<string, Log> countCollapse = new Dictionary<string, Log>();

		bool isSearching = !string.IsNullOrEmpty(keySearch);
		string keySearchLower = keySearch.ToLower();

		visiableLogs = new List<Log>();
		foreach (var log in logAsset.logs) {
			log.number = 1;

			if ((log.type == LogType.Log && logAsset.showLog)
				|| (log.type == LogType.Warning && logAsset.showWarn)
				|| (log.type != LogType.Log && log.type != LogType.Warning && logAsset.showError)) {

				if (isSearching) {
					string text = log.condition + "\n" + log.stackTrace;
					//if (logAsset.searchRegex) {
					//	try {
					//		Regex r = new Regex(keySearch, RegexOptions.IgnoreCase);
					//		Match m = r.Match(text);
					//		if (!m.Success)
					//			continue;
					//	} catch {
					//	}
					//} else 
					{
						if (!text.ToLower().Contains(keySearchLower)) {
							continue;
						}
					}
				}

				if (logAsset.collapse) {
					string key = log.condition + log.stackTrace;
					Log lastLog;
					if (countCollapse.TryGetValue(key, out lastLog)) {
						lastLog.number++;
					} else {
						countCollapse.Add(key, log);
						visiableLogs.Add(log);
					}
				} else {
					visiableLogs.Add(log);
				}
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

		//need pause
		if (needPause) {
			Debug.DebugBreak();
			Debug.Break();
			needPause = false;
		}

		CheckInput();
	}

	void DrawToolbar () {
		GUI.enabled = false;
		GUI.Button(new Rect(-5, 0, position.width + 5, ToolbarHeight), string.Empty);
		GUI.enabled = true;
		GUILayout.BeginHorizontal();
		{
			RectOffset margin = new RectOffset(0, 0, 1, 0);

			GUIStyle styleToolbarButton = new GUIStyle(GUI.skin.button);
			styleToolbarButton.normal.textColor = Color.black;
			styleToolbarButton.fontSize = ToolbarFontSize;
			styleToolbarButton.margin = margin;
			styleToolbarButton.fixedHeight = ToolbarHeight;

			GUILayout.Space(5);
			if (GUILayout.Button("Clear", styleToolbarButton)) {
				logAsset.logs.Clear();
			}

			//search
			var lastSearchKey = keySearch;
			keySearch = GUILayout.TextField(keySearch, GUILayout.Width(100), GUILayout.Height(ToolbarHeight - 2));

			if (lastSearchKey != keySearch) {
				if (keySearch.EndsWith("\n")) {
					keySearch = keySearch.TrimEnd('\n');
				}
			}

			bool showSearchOption = GUILayout.Button("...", styleToolbarButton);
			if (showSearchOption) {
				GenericMenu menu = new GenericMenu();
				menu.AddItem(new GUIContent("Regex"), logAsset.searchRegex, () => {
					logAsset.searchRegex = !logAsset.searchRegex;
				});
				menu.ShowAsContext();
			}

			bool clearSearch = GUILayout.Button("X", styleToolbarButton);
			if (clearSearch) {
				keySearch = string.Empty;
			}

			//GUILayout.Label(mylog);
			//GUILayout.Toggle(isInited, "Inited");
			GUILayout.FlexibleSpace();

			if (position.width >= MinWidthToShowCollapse) {
				logAsset.collapse = GUILayout.Toggle(logAsset.collapse, "Collapse", styleToolbarButton);
			}

			if (position.width >= MinWidthToShowClearOnPlay) {
				logAsset.clearOnPlay = GUILayout.Toggle(logAsset.clearOnPlay, "ClearOnPlay", styleToolbarButton);
			}

			if (position.width >= MinWidthToShowErrorPause) {
				logAsset.errorPause = GUILayout.Toggle(logAsset.errorPause, "ErrorPause", styleToolbarButton);
			}

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
	}

	void CheckInput () {
		if (focusOnListLog) {
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
						selectedTraceLine = 0;//reset current trace line
						scrollPos.y = selectedLog * LogHeight;
						mylog = scrollPos.x + ", " + scrollPos.y;
					}
				}
			}
		} else if (focusOnListDetail) {
			if (selectedTraceLine >= 0) {
				if (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown) {
					bool changed = false;
					if (Event.current.keyCode == KeyCode.UpArrow) {
						if (selectedTraceLine > 0) {
							selectedTraceLine--;
							changed = true;
						}
					}

					if (Event.current.keyCode == KeyCode.DownArrow) {
						if (selectedTraceLine < detailLines.Count - 1) {
							selectedTraceLine++;
							changed = true;
						}
					}

					if (changed) {
						scrollDetail.y = selectedTraceLine * DetailLineHeight;
						mylog = scrollDetail.x + ", " + scrollDetail.y;
					}
				}
			}
		}
	}

	float lastClickInLog = 0;

	void DrawLogList (List<Log> list) {
		var arr = list.Select(x => LogToString(x)).ToArray();

		float columnFileWidht = 0;
		if (logAsset.columnFile && list.Count > 0) {
			columnFileWidht = list.Max(x => x.fileName.Length) * 5.6f;
		}

		float columnTimeWidht = 0;
		if (logAsset.columnTime && list.Count > 0) {
			columnTimeWidht = (list.Max(x => x.time.ToString().Length) + 3) * 5.6f;
		}

		float columnFrameWidht = 0;
		if (logAsset.columnFrame && list.Count > 0) {
			columnFrameWidht = (list.Max(x => x.frame).ToString().Length + 3) * 5.6f;
		}

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
			GUIContent warnContent = new GUIContent(arr[i], warnIcon);
			GUIContent errorContent = new GUIContent(arr[i], errorIcon);
			GUIContent content = null;
			if (list[i].type == LogType.Log) {
				content = logContent;
			} else if (list[i].type == LogType.Warning) {
				content = warnContent;
			} else {
				content = errorContent;
			}

			GUILayout.BeginHorizontal();

			// Collapse -----------------------

			if (logAsset.collapse) {
				GUIStyle styleNumber = new GUIStyle(GUI.skin.box);
				styleNumber.normal.background = MakeTex(2, 2, new Color(1, 1, 1, 0));
				styleNumber.normal.textColor = Color.blue;
				styleNumber.margin = new RectOffset(0, 0, 0, 0);
				styleNumber.padding = new RectOffset(3, 3, 0, 0);
				styleNumber.alignment = TextAnchor.MiddleCenter;
				GUILayout.Box(list[i].number.ToString(), styleNumber, GUILayout.Width(22), GUILayout.Height(LogHeight));
			}

			bool clicked = false;

			// Column Frame -------------------

			if (logAsset.columnFrame) {
				styleLog.fixedWidth = columnFrameWidht;
				clicked |= GUILayout.Button(list[i].frame.ToString(), styleLog, GUILayout.Height(LogHeight));
			}

			// Column Time --------------------

			if (logAsset.columnTime) {
				styleLog.fixedWidth = columnTimeWidht;
				clicked |= GUILayout.Button(list[i].time.ToString(), styleLog, GUILayout.Height(LogHeight));
			}

			// Column File --------------------

			if (logAsset.columnFile) {
				styleLog.fixedWidth = columnFileWidht;
				clicked |= GUILayout.Button(list[i].fileName, styleLog, GUILayout.Height(LogHeight));
			}

			styleLog.fixedWidth = position.width;
			clicked |= GUILayout.Button(content, styleLog);

			GUILayout.EndHorizontal();

			if (clicked) {
				FocusOnListLog();
				GUI.FocusControl("balah"); //do not focus on search text field

				float deltaTime = Time.realtimeSinceStartup - lastClickInLog;
				if (deltaTime < DoubleClickTime && list[i].selected) {
					DoubleClickLog(list[i]);
				}
				list[i].selected = true;
				lastClickInLog = Time.realtimeSinceStartup;

				//focus on file in Project
				HightLightFile(list[i]);
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

	void DrawDetail (List<Log> list) {
		Log log = null;

		if (selectedLog >= 0 && selectedLog < list.Count) {
			log = list[selectedLog];
		} else {
			if (list.Count > 0){
				log = list.Last<Log>();
			}
		}

		float fixedHeight = position.height - currentScrollViewHeight;
		scrollDetail = GUILayout.BeginScrollView(scrollDetail, false, false, 
			GUIStyle.none, GUI.skin.verticalScrollbar);//, 
			//GUILayout.Width(position.width), GUILayout.Height(fixedHeight));

		if (log != null) {
			string[] lines = (log.condition + "\n" + log.stackTrace.Trim()).Split('\n');
			detailLines = new List<string>(lines);

			if (selectedTraceLine >= detailLines.Count) {
				selectedTraceLine = 0;
			}

			for (int i = 0; i < detailLines.Count; i++) {
				
				if (i == selectedTraceLine) {
					styleDetail.normal.textColor = Color.white;
					styleDetail.normal.background = texLogActive;

					styleDetail.onNormal.textColor = Color.white;
					styleDetail.onNormal.background = texLogActive;
				} else {
					styleDetail.normal.textColor = Color.black;
					if (i % 2 == 0)
						styleDetail.normal.background = texLogBlack;
					else
						styleDetail.normal.background = texLogWhite;
				}

				bool clicked = GUILayout.Button(detailLines[i], styleDetail);

				if (clicked) {
					FocusOnListDetail();
					CallStack callStack = LineToCallStack(detailLines[i]);

					float deltaTime = Time.realtimeSinceStartup - lastClickInTrace;
					if (deltaTime < DoubleClickTime && i == selectedTraceLine) {
						OpenCallStack(callStack);
					}

					selectedTraceLine = i;
					lastClickInTrace = Time.realtimeSinceStartup;

					//draw detail in file
					DrawDetailLine(callStack);
				}

				if (clicked) {
					selectedTraceLine = i;
				}
			}
		}

		GUILayout.EndScrollView();
	}

	void DrawDetailLine (CallStack callStack) {
		if (callStack != null) {
			string path = Path.Combine(Application.dataPath, callStack.path.Replace("Assets/", string.Empty));
			if (File.Exists(path)) {
				string[] lines = File.ReadAllLines(path);
				int beginLine = callStack.lineNumber - 3 > 0 ? callStack.lineNumber - 3 : 0;
				int endLine = callStack.lineNumber + 3 < lines.Length - 1 ? callStack.lineNumber + 3 : lines.Length - 1;

				string content = string.Empty;
				for (int i = beginLine; i < endLine; i++) {
					content += lines[i] + "\n";
				}

				GUILayout.Box(content, GUILayout.Width(position.width), GUILayout.Height(100));
			}
		}
	}

	float lastClickInTrace = 0;
	int selectedTraceLine = 0;

	string LogToString (Log log) {
		string str = log.condition + "\n" + log.stackTrace;
		//str = str.Trim();
		string[] strs = str.Split('\n');
		str = SpaceBeforeText + strs[0];
		if (strs.Length > 1)
			str += "\n" + SpaceBeforeText + strs[1];

		return str;
	}

	Texture2D MakeTex( int width, int height, Color col )
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

	static CallStack GetCallStackInLog (Log log) {
		string[] lines = (log.condition + "\n" + log.stackTrace).Split('\n');
		var line = lines.FirstOrDefault(x => x.Contains("Assets/") && x.Contains(".cs"));

		if (!string.IsNullOrEmpty(line)) {
			CallStack callStack = LineToCallStack(line);
			return callStack;
		}

		return null;
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
		} catch {
			//EditorUtility.DisplayDialog("Can not parse line", e.ToString(), "Ok");
		}

		return null;
	}

	static int StringToNumber(string input) {
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

	const float MinWidthToShowFileColumn = 486 - 20;
	const float MinWidthToShowCollapse = 431 - 20;
	const float MinWidthToShowClearOnPlay = 377 - 20;
	const float MinWidthToShowErrorPause = 315 - 20;

	const float TimeColumnWidth = 40;
	//const float FileColumeWidth = 120;
	const string SpaceBeforeText = "  ";
	const int ToolbarFontSize = 9;
	const float DoubleClickTime = 0.3f;
	const float ToolbarButtonWidth = 35;
	const float DetailLineHeight = 20;
	const float LogHeight = 33;
	const float ToolbarSpaceScrollView = 2;
	const float SplitHeight = 4;
	const float ToolbarHeight = 18;
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

	GUIStyle _styleDetail;
	public GUIStyle styleDetail {
		get {
			if (_styleDetail == null) 
			{
				_styleDetail = new GUIStyle(GUI.skin.button);
				_styleDetail.alignment = TextAnchor.UpperLeft;
				_styleDetail.fixedHeight = DetailLineHeight;
				_styleDetail.padding = new RectOffset(5, 0, 3, 3);
				_styleDetail.margin = new RectOffset(0, 0, 0, 0);
				_styleDetail.richText = true;

				_styleDetail.active.textColor = Color.white;
				_styleDetail.active.background = MakeTex(2, 2, new Color32(61, 128, 223, 255));

				_styleDetail.onActive.textColor = Color.white;
				_styleDetail.onActive.background = MakeTex(2, 2, new Color32(61, 128, 223, 255));

				_styleDetail.normal.textColor = Color.black;
				_styleDetail.normal.background = texLogWhite;

				_styleDetail.onNormal.textColor = Color.black;
				_styleDetail.onNormal.background = texLogWhite;
				
				_styleDetail.wordWrap = false;
			}
			return _styleDetail;
		}
	}

	GUIStyle _styleLog;
	public GUIStyle styleLog {
		get {
			if (_styleLog == null) 
			{
				_styleLog = new GUIStyle(GUI.skin.button);
				_styleLog.alignment = TextAnchor.UpperLeft;
				_styleLog.fixedHeight = LogHeight;
				_styleLog.padding = new RectOffset(5, 0, 3, 3);
				_styleLog.margin = new RectOffset(0, 0, 0, 0);
				_styleLog.richText = true;

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