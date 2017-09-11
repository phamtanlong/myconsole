using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
//using System.IO;
using System;
using Object = UnityEngine.Object;
//using System.Text.RegularExpressions;

public class MyConsole : EditorWindow, IHasCustomMenu
{
	public static MyConsole Instance;

	#region IHasCustomMenu implementation

	public void AddItemsToMenu(GenericMenu menu)
	{
		menu.AddItem(new GUIContent("Reload Resources"), false, ClearCacheResources);

		menu.AddSeparator(string.Empty);

		// Column settings

		menu.AddItem(new GUIContent("Columns/File"), logAsset.columnFile, () => {
			logAsset.columnFile = ! logAsset.columnFile;
		});

		menu.AddItem(new GUIContent("Columns/Time"), logAsset.columnTime, () => {
			logAsset.columnTime = ! logAsset.columnTime;
		});

		menu.AddItem(new GUIContent("Columns/Frame"), logAsset.columnFrame, () => {
			logAsset.columnFrame = ! logAsset.columnFrame;
		});

		menu.AddSeparator(string.Empty);

		// Toolbar settings

		menu.AddItem(new GUIContent("Collapse"), logAsset.collapse, () => {
			logAsset.collapse = ! logAsset.collapse;
		});

		menu.AddItem(new GUIContent("Clear On Play"), logAsset.clearOnPlay, () => {
			logAsset.clearOnPlay = ! logAsset.clearOnPlay;
		});

		menu.AddItem(new GUIContent("Error Pause"), logAsset.errorPause, () => {
			logAsset.errorPause = ! logAsset.errorPause;
		});

		menu.AddSeparator(string.Empty);

		// Log Type Show/Hide

		menu.AddItem(new GUIContent("Log"), logAsset.showLog, () => {
			logAsset.showLog = ! logAsset.showLog;
		});

		menu.AddItem(new GUIContent("Warning"), logAsset.showWarn, () => {
			logAsset.showWarn = ! logAsset.showWarn;
		});

		menu.AddItem(new GUIContent("Error"), logAsset.showError, () => {
			logAsset.showError = ! logAsset.showError;
		});

	}

	#endregion

	[MenuItem("Window/MyConsole")]
	static void ShowMe()
	{
		MyConsole window = (MyConsole)EditorWindow.GetWindow(typeof(MyConsole));
		window.Show();
		Instance = window;
	}

	const string assetPath = "Assets/Editor/MyConsole/Resources/ConsoleAsset.asset";
	static bool isInited = false;

	MyConsoleAsset _logAsset;
	MyConsoleAsset logAsset {
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

	bool needPause = false;

	string keySearch = string.Empty;

	//list log lines
	List<Log> visiableLogs = new List<Log>();
	string[] arrLogContents;
	Texture[] arrLogIcons;
	string[] arrLogCounts;
	string[] arrLogTimes;
	string[] arrLogFrames;
	string[] arrLogFiles;

	float columnCollapseWidth = 40;
	float columnTimeWidth = 40;
	float columnFrameWidth = 40;
	float columnFileWidth = 40;

	Vector2 scrollViewDetail;
	int selectedLogLine = 0;
	float lastTimeClickInDetail = 0;

	//list detail lines
	List<string> detailLines = new List<string>();
	Vector2 scrollViewLogs;
	int selectedDetailLine = 0;
	float lastTimeClickInLog = 0;

	bool isMovingListLog = false;
	bool isMovingListDetail = false;
	string mylog = string.Empty;

	#region Lifecycle

	void InitFirstTime () {
		scrollViewLogsHeight = position.height / 2;
	}

	//start here
	void Awake () {
		InitFirstTime();
		ClearData();
		Init();
	}

	void OnDestroy () {
		UnRegisterHandlers();
		ClearData();
	}

	void OnProjectChange () {
		RegisterHandlers();
	}

	void OnFocus () {
		RegisterHandlers();
	}

	void OnLostFocus () {
		RegisterHandlers();
	}

	void Update () {
		if (logAsset == null || isInited == false) {
			//when reimport code or something change
			//may be some data lost
			//so we need to re-init, register handlers, ...
			Init();
		}
	}

	[UnityEditor.Callbacks.DidReloadScripts]
	public static void OnScriptsReloaded() {
		//Debug.Log("Compile Done");
		var logAsset = MyConsole.LoadOrCreateAsset();
		logAsset.removeAllCompileError();
		if (Instance != null)
			Instance.PrepareData();
	}

	#endregion //Lifecycle

	#region Process

	void ClearCacheResources () {
		_border1 = null;
		_border2 = null;
		_borderSelected = null;
		_errorIcon = null;
		_logAsset = null;
		_logIcon = null;
		_styleCollapseNumber = null;
		_styleDetail = null;
		_styleLog = null;
		_styleLogIcon = null;
		_styleToolbar = null;
		_texLogActive = null;
		_texLogBlack = null;
		_texLogWhite = null;
		_warnIcon = null;
	}

	void ClearData () {
		logAsset.removeAll();
		PrepareData();
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

	void RegisterHandlers () {
		UnRegisterHandlers();
		Application.logMessageReceivedThreaded += LogHandler;
		EditorApplication.playmodeStateChanged += PlayModeChange;
	}

	void UnRegisterHandlers () {
		Application.logMessageReceivedThreaded -= LogHandler;
		EditorApplication.playmodeStateChanged -= PlayModeChange;
	}

	void Init () {
		LoadOrCreateAsset();
		RegisterHandlers();
		isInited = true;
	}

	void LogHandler(string condition, string stackTrace, LogType type)
	{
		bool isCompileError = condition.Contains("): error CS");
		if (isCompileError) {
			var hadIt = logAsset.containsCompileErrorLog(condition);
			if (hadIt)
				return; //do not duplicate 1 compile error
		}

		Log log = new Log {
			condition = condition,
			stackTrace = stackTrace,
			type = type
		};

		log.isCompileError = isCompileError;

		CallStack callStack = GetCallStackInLog(log);
		log.callstack = callStack;

		if (callStack != null) {
			var index = callStack.path.LastIndexOf("/");
			string fileName = callStack.path.Substring(index + 1);
			log.file = fileName;
		}

		log.time = Time.realtimeSinceStartup;

		log.frame = Time.frameCount;

		log.content = LogToString(log);

		logAsset.addLog(log);

		PrepareData();

		if (logAsset.errorPause && (type == LogType.Exception || type == LogType.Assert)) {
			if (EditorApplication.isPlaying) {
				needPause = true;
			}
		}
	}

	void PlayModeChange()
	{
		//reload resource
		ClearCacheResources();

		//clear on play
		if (EditorApplication.isPlayingOrWillChangePlaymode) {
			if (!EditorApplication.isPlaying) {
				logAsset.removeAll();
				PrepareData();
			}
		}
	}

	void DoubleClickLog(Log log)
	{
		//open file in script editor
		OpenCallStack(log.callstack);
	}

	#endregion //Process

	#region Draw

	void FocusMoveOnListLog () {
		isMovingListLog = true;
		isMovingListDetail = false;
		GUI.FocusControl("balah"); //do not focus on search text field
	}

	void FocusMoveOnListDetail () {
		isMovingListLog = false;
		isMovingListDetail = true;
		GUI.FocusControl("balah"); //do not focus on search text field
	}

	void CheckInputMoveInList () {
		
		if (isMovingListLog) {
			//Press key Up + Down in list log -------------------

			if (selectedLogLine >= 0) {
				if (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown) {
					bool changed = false;

					if (Event.current.keyCode == KeyCode.UpArrow) { //move up
						if (selectedLogLine > 0) {
							selectedLogLine = selectedLogLine - 1;
							changed = true;
						}
					}

					if (Event.current.keyCode == KeyCode.DownArrow) { //move down
						if (selectedLogLine < visiableLogs.Count - 1) {
							selectedLogLine = selectedLogLine + 1;
							changed = true;
						}
					}

					//change scrollbar
					if (changed) {
						selectedDetailLine = 0;//reset current detail line
						//scrollViewLogs.y = selectedLogLine * LogHeight;
						//TODO: scrollview to current selected line
					}
				}
			}
		} else if (isMovingListDetail) {
			//Press key Up + Down in list detail lines -------------------

			if (selectedDetailLine >= 0) {
				if (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown) {
					bool changed = false;

					if (Event.current.keyCode == KeyCode.UpArrow) { //move up
						if (selectedDetailLine > 0) {
							selectedDetailLine--;
							changed = true;
						}
					}

					if (Event.current.keyCode == KeyCode.DownArrow) { //move down
						if (selectedDetailLine < detailLines.Count - 1) {
							selectedDetailLine++;
							changed = true;
						}
					}

					//change scrollbar
					if (changed) {
						scrollViewDetail.y = selectedDetailLine * DetailLineHeight;
					}
				}
			}
		}
	}

	void PrepareData () {

		//to count collapse count
		Dictionary<string, Log> collapseDict = new Dictionary<string, Log>();

		bool isSearching = !string.IsNullOrEmpty(keySearch);
		string keySearchLower = keySearch.ToLower();

		visiableLogs = new List<Log>();

		var listContents = new List<string>();
		var listIcons = new List<Texture>();
		var listTimes = new List<string>();
		var listFrames = new List<string>();
		var listFiles = new List<string>();

		columnCollapseWidth = 0;
		columnTimeWidth = 0;
		columnFrameWidth = 0;
		columnFileWidth = 0;

		foreach (var log in logAsset.logs) {
			log.number = 1;
			bool ok = false;

			//check filter LogType: Log, Warning, Error, ...
			if ((log.type == LogType.Log && logAsset.showLog)
				|| (log.type == LogType.Warning && logAsset.showWarn)
				|| (log.type != LogType.Log && log.type != LogType.Warning && logAsset.showError)) {

				//check search key
				if (isSearching) {
					//TODO: Regex search
					//if (logAsset.searchRegex) {
					//	try {
					//		Regex r = new Regex(keySearch, RegexOptions.IgnoreCase);
					//		Match m = r.Match(log.condition);
					//		if (!m.Success)
					//			continue;
					//	} catch {
					//	}
					//} else
					{
						if (!log.condition.ToLower().Contains(keySearchLower)) {
							continue; //not match search => skip to next log
						}
					}
				}

				//count repeat if collapse enabled
				if (logAsset.collapse) {
					string key = log.condition + log.stackTrace;
					Log lastLog;

					if (collapseDict.TryGetValue(key, out lastLog)) { //already contain log => increase count
						lastLog.number++;
						if (lastLog.number > columnCollapseWidth)
							columnCollapseWidth = lastLog.number;
					} else {
						collapseDict.Add(key, log); //new log => add to visiable logs
						ok = true;
					}
				} else {
					ok = true;
				}
			}

			if (ok) {
				visiableLogs.Add(log);

				listContents.Add(log.content);

				if (log.type == LogType.Log) {
					listIcons.Add(logIcon);
				} else if (log.type == LogType.Warning) {
					listIcons.Add(warnIcon);
				} else {
					listIcons.Add(errorIcon);
				}

				string time = log.time.ToString();
				listTimes.Add(time);
				if (time.Length > columnTimeWidth)
					columnTimeWidth = time.Length;

				string frame = log.frame.ToString();
				listFrames.Add(frame);
				if (frame.Length > columnFrameWidth)
					columnFrameWidth = frame.Length;
				
				listFiles.Add(log.file);
				if (log.file.Length > columnFileWidth)
					columnFileWidth = log.file.Length;
			}
		}

		//todo; calculate to pixel
		columnTimeWidth = columnTimeWidth * FontWidth + 8;
		columnFrameWidth = columnFrameWidth * FontWidth + 8;
		columnFileWidth = columnFileWidth * FontWidth + 8;
		columnCollapseWidth = columnCollapseWidth.ToString().Length * FontWidth + 8;

		//list to array
		arrLogContents = listContents.ToArray();
		arrLogIcons = listIcons.ToArray();
		arrLogTimes = listTimes.ToArray();
		arrLogFrames = listFrames.ToArray();
		arrLogFiles = listFiles.ToArray();

		//count collapse
		arrLogCounts = visiableLogs.Select(x => x.number.ToString()).ToArray();
	}

	void OnGUI()
	{
//		PrepareData();

		DrawToolbar();

		GUILayout.Space(ToolbarSpaceScrollView);

		GUILayout.BeginVertical();
		{
			DrawLogList(visiableLogs);

			ResizeScrollView();

			GUILayout.Space(ToolbarSpaceScrollView);

			DrawDetail(visiableLogs);
		}
		GUILayout.EndVertical();

		Repaint();

		//draw new log before pause editor
		if (needPause) {
			Debug.DebugBreak();
			Debug.Break();
			needPause = false;
		}

		//always check input after all GUI draw
		CheckInputMoveInList();
	}

	void DrawToolbar () {

		//background of toolbar, just for beauty
		GUI.enabled = false;
		GUI.Box(new Rect(-5, 0, position.width + 5, ToolbarHeight), string.Empty);
		GUI.enabled = true;

		GUILayout.BeginHorizontal();
		{
			//clear log
			if (GUILayout.Button(" Clear ", styleToolbar)) {
				logAsset.removeAll();
				PrepareData();
			}
			GUILayout.Space(1);

			//collapse toggle
			if (position.width >= MinWidthToShowCollapse) {
				EditorGUI.BeginChangeCheck();
				logAsset.collapse = GUILayout.Toggle(logAsset.collapse, "Collapse", styleToolbar);
				bool changeCollapse = EditorGUI.EndChangeCheck();
				if (changeCollapse)
					PrepareData();
				GUILayout.Space(1);
			}

			//clear on play toggle
			if (position.width >= MinWidthToShowClearOnPlay) {
				logAsset.clearOnPlay = GUILayout.Toggle(logAsset.clearOnPlay, "ClearOnPlay", styleToolbar);
				GUILayout.Space(1);
			}

			//error pause toggle
			if (position.width >= MinWidthToShowErrorPause) {
				logAsset.errorPause = GUILayout.Toggle(logAsset.errorPause, "ErrorPause", styleToolbar);
				GUILayout.Space(1);
			}

			GUILayout.Space(1);
			//search
			var lastSearchKey = keySearch;
			keySearch = GUILayout.TextField(keySearch, GUILayout.Width(100), GUILayout.Height(ToolbarHeight - 2));

			if (lastSearchKey != keySearch) {
				if (keySearch.EndsWith("\n")) {
					keySearch = keySearch.TrimEnd('\n');
				}
				PrepareData();
			}

			//TODO: Regex search
			//bool showSearchOption = GUILayout.Button("...", styleToolbarButton);
			//if (showSearchOption) {
			//	GenericMenu menu = new GenericMenu();
			//	menu.AddItem(new GUIContent("Regex"), logAsset.searchRegex, () => {
			//		logAsset.searchRegex = !logAsset.searchRegex;
			//	});
			//	menu.ShowAsContext();
			//}

			bool clearSearch = GUILayout.Button("X", styleToolbar);
			if (clearSearch) {
				keySearch = string.Empty;
				PrepareData();
			}

			GUILayout.Space(1);
			bool test = GUILayout.Button("Test", styleToolbar);
			if (test) {
				mylog = selectedLogLine + " => " + scrollViewLogs.y + ", " + (scrollViewLogs.y / LogHeight);
			}

			GUILayout.Label(mylog);

			GUILayout.Space(1);
			GUILayout.FlexibleSpace();

			//Log + Warning + Error (toggle + number)

			GUIContent logcontent = new GUIContent("" + logAsset.countLog, logIcon);
			GUIContent warncontent = new GUIContent("" + logAsset.countWarn, warnIcon);
			GUIContent errorcontent = new GUIContent("" + logAsset.countError, errorIcon);

			bool changeLogType = false;
			EditorGUI.BeginChangeCheck();
			logAsset.showLog = GUILayout.Toggle(logAsset.showLog, logcontent, styleToolbar, GUILayout.MaxHeight(ToolbarHeight-2));
			changeLogType |= EditorGUI.EndChangeCheck();

			GUILayout.Space(1);

			EditorGUI.BeginChangeCheck();
			logAsset.showWarn = GUILayout.Toggle(logAsset.showWarn, warncontent, styleToolbar, GUILayout.MaxHeight(ToolbarHeight-2));
			changeLogType |= EditorGUI.EndChangeCheck();

			GUILayout.Space(1);

			EditorGUI.BeginChangeCheck();
			logAsset.showError = GUILayout.Toggle(logAsset.showError, errorcontent, styleToolbar, GUILayout.MaxHeight(ToolbarHeight-2));
			changeLogType |= EditorGUI.EndChangeCheck();

			if (changeLogType)
				PrepareData();
		}
		GUILayout.EndHorizontal();
	}

	void DrawLogList (List<Log> list) {
		
		//start scrollview
		scrollViewLogs = GUILayout.BeginScrollView(scrollViewLogs, GUIStyle.none, GUI.skin.verticalScrollbar, 
			GUILayout.Width(position.width), GUILayout.Height(scrollViewLogsHeight - ToolbarHeight - ToolbarSpaceScrollView));
		
		EditorGUI.BeginChangeCheck();

		int lastSelectedLogLine = selectedLogLine;
		GUILayout.BeginHorizontal();
		{
			float contentWidth = position.width - IconLogWidth;

			if (logAsset.collapse) {
				styleLog.fixedWidth = columnCollapseWidth;
				selectedLogLine = GUILayout.SelectionGrid(selectedLogLine, arrLogCounts, 1, styleLog);
				contentWidth -= columnCollapseWidth;
			}

			selectedLogLine = GUILayout.SelectionGrid(selectedLogLine, arrLogIcons, 1, styleLogIcon);

			if (logAsset.columnTime) {
				styleLog.normal.textColor = new Color32(122, 51, 0, 255);
				styleLog.fixedWidth = columnTimeWidth;
				selectedLogLine = GUILayout.SelectionGrid(selectedLogLine, arrLogTimes, 1, styleLog);
				contentWidth -= columnTimeWidth;
			}

			if (logAsset.columnFrame) {
				styleLog.normal.textColor = Color.blue;
				styleLog.fixedWidth = columnFrameWidth;
				selectedLogLine = GUILayout.SelectionGrid(selectedLogLine, arrLogFrames, 1, styleLog);
				contentWidth -= columnFrameWidth;
			}

			if (logAsset.columnFile) {
				styleLog.normal.textColor = new Color32(98, 0, 173, 255);
				styleLog.fixedWidth = columnFileWidth;
				selectedLogLine = GUILayout.SelectionGrid(selectedLogLine, arrLogFiles, 1, styleLog);
				contentWidth -= columnFileWidth;
			}

			styleLog.normal.textColor = Color.black;
			styleLog.fixedWidth = contentWidth;
			selectedLogLine = GUILayout.SelectionGrid(selectedLogLine, arrLogContents, 1, styleLog);
		}
		GUILayout.EndHorizontal();

		bool changeLogLine = EditorGUI.EndChangeCheck();

		if (changeLogLine) {
			FocusMoveOnListLog();

			//check double click in log line
			float deltaTime = Time.realtimeSinceStartup - lastTimeClickInLog;
			if (deltaTime < DoubleClickTime && lastSelectedLogLine == selectedLogLine) {
				DoubleClickLog(list[selectedLogLine]);
			}

			lastTimeClickInLog = Time.realtimeSinceStartup;

			//focus on file in Project
			HightLightFile(list[selectedLogLine]);
		}

		GUILayout.EndScrollView();
	}

	void DrawDetail (List<Log> list) {
		Log currentLog = null;

		//get the current log
		if (selectedLogLine >= 0 && selectedLogLine < list.Count) {
			currentLog = list[selectedLogLine];
		} else {
			if (list.Count > 0){
				currentLog = list.Last<Log>();
			}
		}

		//begin scrollview
		scrollViewDetail = GUILayout.BeginScrollView(scrollViewDetail, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

		if (currentLog != null) {
			//convert to lines
			string[] lines = (currentLog.condition + "\n" + currentLog.stackTrace.Trim()).Split('\n');
			detailLines = new List<string>(lines);

			if (selectedDetailLine >= detailLines.Count) {
				selectedDetailLine = 0;
			}

			EditorGUI.BeginChangeCheck();
			int lastSelect = selectedDetailLine;
			selectedDetailLine = GUILayout.SelectionGrid(selectedDetailLine, detailLines.ToArray(), 1, styleDetail);
			bool changeSelectedLine = EditorGUI.EndChangeCheck();

			if (changeSelectedLine) {
				FocusMoveOnListDetail();
				CallStack callStack = LineToCallStack(detailLines[selectedDetailLine]);

				//check double click to open script file
				float deltaTime = Time.realtimeSinceStartup - lastTimeClickInDetail;
				if (deltaTime < DoubleClickTime && lastSelect == selectedDetailLine) {
					OpenCallStack(callStack);
				}

				lastTimeClickInDetail = Time.realtimeSinceStartup;
			}
		}

		GUILayout.EndScrollView();
	}

	#endregion //Draw

	#region Resizable panel

	float scrollViewLogsHeight;
	bool isResizing = false;
	Rect cursorChangeRect;

	void ResizeScrollView(){
		cursorChangeRect = new Rect(0, scrollViewLogsHeight, this.position.width, SplitHeight);

		GUI.DrawTexture(cursorChangeRect, EditorGUIUtility.whiteTexture);

		EditorGUIUtility.AddCursorRect(cursorChangeRect, MouseCursor.ResizeVertical);

		if (Event.current.type == EventType.mouseDown && cursorChangeRect.Contains(Event.current.mousePosition)) {
			isResizing = true;
		}

		if (isResizing) {
			scrollViewLogsHeight = Event.current.mousePosition.y;
			cursorChangeRect.Set(cursorChangeRect.x, scrollViewLogsHeight, cursorChangeRect.width, cursorChangeRect.height);
		}

		if (Event.current.type == EventType.MouseUp) {
			isResizing = false;
		}

		if (scrollViewLogsHeight < ToolbarHeight + MinScrollHeight)
			scrollViewLogsHeight = ToolbarHeight + MinScrollHeight;
		
		if (position.height - scrollViewLogsHeight < MinDetailHeight)
			scrollViewLogsHeight = position.height - MinDetailHeight;
	}

	#endregion //Resizable panel

	#region Constants

	const float MinWidthToShowCollapse = 471;
	const float MinWidthToShowClearOnPlay = 421;
	const float MinWidthToShowErrorPause = 354;

	const float FontWidth = 6.2f;
	const int ToolbarFontSize = 9;
	const float DoubleClickTime = 0.3f;
	const float ToolbarButtonWidth = 35;
	const float DetailLineHeight = 20;
	const float IconLogWidth = 20;
	const float LogHeight = 33;
	const float ToolbarSpaceScrollView = 2;
	const float SplitHeight = 4;
	const float ToolbarHeight = 19;
	const float MinScrollHeight = 70;
	const float MinDetailHeight = 80;

	#endregion //Constants

	#region Resources Cache

	static Texture2D _border1;
	static public Texture2D border1 {
		get {
			if (_border1 == null) {
				_border1 = Resources.Load("border") as Texture2D;
			}
			return _border1;
		}
	}

	static Texture2D _border2;
	static public Texture2D border2 {
		get {
			if (_border2 == null) {
				_border2 = Resources.Load("border") as Texture2D;
			}
			return _border2;
		}
	}

	static Texture2D _borderSelected;
	static public Texture2D borderSelected {
		get {
			if (_borderSelected == null) {
				_borderSelected = Resources.Load("border") as Texture2D;
			}
			return _borderSelected;
		}
	}

	static Texture _logIcon;
	static public Texture logIcon {
		get {
			if (_logIcon == null) {
				_logIcon = Resources.Load("log") as Texture;
			}
			return _logIcon;
		}
	}

	static Texture _warnIcon;
	static public Texture warnIcon {
		get {
			if (_warnIcon == null) {
				_warnIcon = Resources.Load("warn") as Texture;
			}
			return _warnIcon;
		}
	}

	static Texture _errorIcon;
	static public Texture errorIcon {
		get {
			if (_errorIcon == null) {
				_errorIcon = Resources.Load("error") as Texture;
			}
			return _errorIcon;
		}
	}

	static GUIStyle _styleDetail;
	static public GUIStyle styleDetail {
		get {
			if (_styleDetail == null) 
			{
				_styleDetail = new GUIStyle(GUI.skin.textField);
				_styleDetail.alignment = TextAnchor.UpperLeft;
				_styleDetail.fixedHeight = DetailLineHeight;
				_styleDetail.padding = new RectOffset(4, 0, 0, 0);
				_styleDetail.margin = new RectOffset(0, 0, 1, 1);
				_styleDetail.richText = true;

				_styleDetail.active.textColor = Color.white;
				_styleDetail.active.background = texLogActive;

				_styleDetail.onActive.textColor = Color.white;
				_styleDetail.onActive.background = texLogActive;

				_styleDetail.wordWrap = true;

				_styleDetail.normal.textColor = Color.black;
				_styleDetail.normal.background = texLogWhite;

				_styleDetail.onNormal.textColor = Color.white;
				_styleDetail.onNormal.background = texLogActive;
			}
			return _styleDetail;
		}
	}

	static GUIStyle _styleLogIcon;
	static public GUIStyle styleLogIcon {
		get {
			if (_styleLogIcon == null) 
			{
				_styleLogIcon = new GUIStyle(GUI.skin.button);
				_styleLogIcon.alignment = TextAnchor.UpperLeft;
				_styleLogIcon.fixedWidth = IconLogWidth;
				_styleLogIcon.fixedHeight = LogHeight;
				_styleLogIcon.padding = new RectOffset(5, 0, 0, 0);
				_styleLogIcon.margin = new RectOffset(0, 0, 1, 1);

				_styleLogIcon.active.background = texLogActive;
				_styleLogIcon.onActive.background = texLogActive;
				_styleLogIcon.normal.background = texLogWhite;
				_styleLogIcon.onNormal.background = texLogActive;

			}
			return _styleLogIcon;
		}
	}

	static GUIStyle _styleLog;
	static public GUIStyle styleLog {
		get {
			if (_styleLog == null) 
			{
				_styleLog = new GUIStyle(GUI.skin.box);
				_styleLog.alignment = TextAnchor.UpperLeft;
				_styleLog.fixedHeight = LogHeight;
				_styleLog.padding = new RectOffset(4, 0, 0, 0);
				_styleLog.margin = new RectOffset(0, 0, 1, 1);
				_styleLog.richText = true;

				_styleLog.active.textColor = Color.white;
				_styleLog.active.background = texLogActive;

				_styleLog.onActive.textColor = Color.white;
				_styleLog.onActive.background = texLogActive;

				_styleLog.wordWrap = false;

				_styleLog.normal.textColor = Color.black;
				_styleLog.normal.background = texLogWhite;

				_styleLog.onNormal.textColor = Color.white;
				_styleLog.onNormal.background = texLogActive;

			}
			return _styleLog;
		}
	}

	static GUIStyle _styleToolbar;
	static public GUIStyle styleToolbar {
		get {
			if (_styleToolbar == null) {
				_styleToolbar = new GUIStyle(GUI.skin.button);
				_styleToolbar.fontSize = ToolbarFontSize;
				_styleToolbar.margin = new RectOffset(0, 0, 2, 1);
				_styleToolbar.fixedHeight = ToolbarHeight-3;

				Texture2D texOff = MakeTex(3, 3, new Color32(180, 180, 180, 255));
				_styleToolbar.normal.textColor = Color.black;
				_styleToolbar.normal.background = texOff;

				Texture2D texOn = MakeTex(3, 3, new Color32(244, 244, 244, 255));
				_styleToolbar.onNormal.textColor = Color.black;
				_styleToolbar.onNormal.background = texOn;

				_styleToolbar.active.textColor = Color.black;
				_styleToolbar.active.background = texOn;

				_styleToolbar.onActive.textColor = Color.black;
				_styleToolbar.onActive.background = texOff;
			}
			return _styleToolbar;
		}
	}

	static GUIStyle _styleCollapseNumber ;
	static public GUIStyle styleCollapseNumber {
		get {
			if (_styleCollapseNumber == null) {
				_styleCollapseNumber = new GUIStyle(GUI.skin.box);
				_styleCollapseNumber.normal.background = MakeTex(2, 2, new Color(1, 1, 1, 0));
				_styleCollapseNumber.normal.textColor = Color.blue;
				_styleCollapseNumber.margin = new RectOffset(0, 0, 0, 0);
				_styleCollapseNumber.padding = new RectOffset(3, 3, 0, 0);
				_styleCollapseNumber.alignment = TextAnchor.MiddleCenter;
			}
			return _styleCollapseNumber;
		}
	}

	static Texture2D _texLogActive;
	static public Texture2D texLogActive {
		get {
			if (_texLogActive == null) {
				_texLogActive = MakeTex(2, 2, new Color32(61, 128, 223, 255));
			}
			return _texLogActive;
		}
	}

	static Texture2D _texLogBlack;
	static public Texture2D texLogBlack {
		get {
			if (_texLogBlack == null) {
				_texLogBlack = MakeTex(2, 2, new Color32(216, 216, 216, 255));
			}
			return _texLogBlack;
		}
	}

	static Texture2D _texLogWhite;
	static public Texture2D texLogWhite {
		get {
			if (_texLogWhite == null) {
				_texLogWhite = MakeTex(2, 2, new Color32(200, 200, 200, 255));
			}
			return _texLogWhite;
		}
	}

	#endregion //Resources Cache

	#region Utilities

	static void OpenCallStack (CallStack callStack) {
		if (callStack != null) {
			Object obj = AssetDatabase.LoadAssetAtPath<Object>(callStack.path);
			AssetDatabase.OpenAsset(obj, callStack.lineNumber);
		}
	}

	static void HightLightFile(Log log)
	{
		if (log.callstack != null) {
			Object obj = AssetDatabase.LoadAssetAtPath<Object>(log.callstack.path);
			EditorGUIUtility.PingObject(obj);
			Selection.activeObject = obj;
		}
	}

	static string LogToString (Log log) {
		string str = log.condition + "\n" + log.stackTrace;
		string[] strs = str.Split('\n');
		str = strs[0];
		if (strs.Length > 1)
			str += "\n" + strs[1];

		return str;
	}

	static Texture2D MakeTex( int width, int height, Color col )
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

	#endregion //Ultilities
}