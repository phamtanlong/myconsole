using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using Object = UnityEngine.Object;

public class MyConsole : EditorWindow, IHasCustomMenu
{
	#region Instance tracker

	protected static MyConsole _instance = null;
	public static MyConsole instance {
		get {
			if (_instance == null) {
				MyConsole[] windows = Resources.FindObjectsOfTypeAll<MyConsole>();
				if(windows != null && windows.Length > 0) {
					_instance = EditorWindow.GetWindow<MyConsole>("MyConsole", false);
				}
			}
			return _instance;
		}
	}

	#endregion

	#region IHasCustomMenu implementation

	public void AddItemsToMenu(GenericMenu menu)
	{
		// Column settings

		menu.AddItem(new GUIContent("Column File"), logAsset.columnFile, () => {
			logAsset.columnFile = ! logAsset.columnFile;
			PrepareData();
		});

		menu.AddItem(new GUIContent("Column Time"), logAsset.columnTime, () => {
			logAsset.columnTime = ! logAsset.columnTime;
			PrepareData();
		});

		menu.AddItem(new GUIContent("Column Frame"), logAsset.columnFrame, () => {
			logAsset.columnFrame = ! logAsset.columnFrame;
			PrepareData();
		});

		menu.AddSeparator(string.Empty);

		// Toolbar settings

		menu.AddItem(new GUIContent("Collapse"), logAsset.collapse, () => {
			logAsset.collapse = ! logAsset.collapse;
			PrepareData();
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
			PrepareData();
		});

		menu.AddItem(new GUIContent("Warning"), logAsset.showWarn, () => {
			logAsset.showWarn = ! logAsset.showWarn;
			PrepareData();
		});

		menu.AddItem(new GUIContent("Error"), logAsset.showError, () => {
			logAsset.showError = ! logAsset.showError;
			PrepareData();
		});

	}

	#endregion

	MyConsoleAsset logAsset {
		get {
			return MyConsoleService.logAsset;
		}
	}

	bool needPause = false;

	string keySearch = string.Empty;
	string keyIgnore = string.Empty;

	//list log lines
	protected List<Log> visiableLogs = new List<Log>();
	protected string[] arrLogContents;
	protected Texture[] arrLogIcons;
	protected string[] arrLogCounts;
	protected string[] arrLogTimes;
	protected string[] arrLogFrames;
	protected string[] arrLogFiles;

	float columnCollapseWidth;
	float columnTimeWidth;
	float columnFrameWidth;
	float columnFileWidth;

	Vector2 scrollViewDetail;
	int selectedLogLine = -1;
	float lastTimeClickInDetail = 0;

	//list detail lines
	List<string> detailLines = new List<string>();
	Vector2 scrollViewLogs;
	int selectedDetailLine = -1;
	float lastTimeClickInLog = 0;

	bool editingDetail = false;

	bool isMovingListLog = false;
	bool isMovingListDetail = false;

	public float GetLogPanelHeight () {
		return topPartHeight - ToolbarHeight - TitleRowHeight - ToolbarSpaceScrollView;
	}

	public float GetDetailPanelHeight () {
		return position.height - topPartHeight - ToolbarSpaceScrollView;	
	}

	public bool HasScrollBar () {
		float calculateH = visiableLogs.Count * LogHeight;
		float logPanelH = GetLogPanelHeight();
		if (calculateH > logPanelH)
			return true;
		else
			return false;
	}

	#region Lifecycle

	void CalculateWindowSize () {
		topPartHeight = position.height / 2;
		columnCollapseWidth = MincolumnCollapseWidth;
		columnTimeWidth = MincolumnTimeWidth;
		columnFrameWidth = MincolumnFrameWidth;
		columnFileWidth = MincolumnFileWidth;
	}

	//start here
	void Awake () {
		CalculateWindowSize();
		ClearData();
		RegisterHandlers();
	}

	void Update () {
		Event e = Event.current;

		if (e != null && e.type == EventType.KeyDown && e.command && e.keyCode == KeyCode.A)
		{
			var kbdCtrlId = GUIUtility.keyboardControl;
			var t = GUIUtility.GetStateObject(typeof(TextEditor), kbdCtrlId) as TextEditor;
			t.SelectAll();
		}
	}

	[UnityEditor.Callbacks.DidReloadScripts]
	public static void OnScriptsReloaded() {
		if (MyConsole.instance != null) {
			MyConsole.instance.PrepareData();
		}
	}

	#endregion //Lifecycle

	#region Process

	void ClearData () {
		logAsset.removeAll();
		PrepareData();
	}

	public void RegisterHandlers () {
		UnRegisterHandlers();
		Application.logMessageReceivedThreaded += LogHandler;
		EditorApplication.playmodeStateChanged += PlayModeChange;
	}

	public void UnRegisterHandlers () {
		Application.logMessageReceivedThreaded -= LogHandler;
		EditorApplication.playmodeStateChanged -= PlayModeChange;
	}

	public void LogHandler(string condition, string stackTrace, LogType type)
	{
		bool isCompileError = condition.Contains("): error CS");
		if (isCompileError) {
			//clear all without compile error
			ClearData();

			//do not duplicate 1 compile error
			var hadIt = logAsset.containsCompileErrorLog(condition);
			if (hadIt)
				return;
		} else if (logAsset.compileErrorCount > 0) {
			ClearData();
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

		CheckAutoScrollToBottom();
	}

	void CheckAutoScrollToBottom () {
		if (HasScrollBar()) {
			float scrollHeight = GetLogPanelHeight();
			float numberVisiableRow = scrollHeight / LogHeight;
			float maxTopLine = visiableLogs.Count - numberVisiableRow;
			float currentTopLine = scrollViewLogs.y / LogHeight;
			if (maxTopLine - currentTopLine <= 1.1f) {
				scrollViewLogs.y = maxTopLine * LogHeight;
			}
		}
	}

	public void PlayModeChange()
	{
		//clear on play
		if (EditorApplication.isPlayingOrWillChangePlaymode) {
			if (!EditorApplication.isPlaying) {
				ClearData();
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
		editingDetail = false;

		isMovingListLog = true;
		isMovingListDetail = false;
		GUI.FocusControl("balah"); //do not focus on search text field
	}

	void FocusMoveOnListDetail () {
		editingDetail = false;

		isMovingListLog = false;
		isMovingListDetail = true;
		GUI.FocusControl("balah"); //do not focus on search text field
	}

	void CheckInputMoveInList () {
		if (Event.current == null)
			return;
		
		if (isMovingListLog) {
			//Press key Up + Down in list log -------------------

			if (selectedLogLine >= 0) {

				if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape) {
					selectedLogLine = -1;
					Event.current.Use();
				}

				if (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown) {
					bool isMoveUp = false;
					bool isMoveDown = false;

					if (Event.current.keyCode == KeyCode.UpArrow) { //move up
						if (selectedLogLine > 0) {
							selectedLogLine = selectedLogLine - 1;
							isMoveUp = true;
						}
					}

					if (Event.current.keyCode == KeyCode.DownArrow) { //move down
						if (selectedLogLine < visiableLogs.Count - 1) {
							selectedLogLine = selectedLogLine + 1;
							isMoveDown = true;
						}
					}

					//change scrollbar
					if (isMoveUp || isMoveDown) {
						selectedDetailLine = -1;//reset current detail line
						Event.current.Use();

						if (isMoveDown) {
							float scrollHeight = GetLogPanelHeight();
							float numberVisiableRow = scrollHeight / LogHeight;
							float topLine = selectedLogLine - numberVisiableRow + 1;
							float currentTopLine = scrollViewLogs.y / LogHeight;
							if (topLine < currentTopLine) {
								//nothing
							} else {
								float posY = topLine * LogHeight;
								scrollViewLogs.y = posY;	
							}
						} else {
							float topLine = selectedLogLine;
							float currentTopLine = scrollViewLogs.y / LogHeight;
							if (topLine > currentTopLine) {
								//nothing
							} else {
								float posY = topLine * LogHeight;
								scrollViewLogs.y = posY;
							}
						}
					}
				}
			}
		} else if (isMovingListDetail) {
			//Press key Up + Down in list detail lines -------------------

			if (selectedDetailLine >= 0) {

				if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape) {
					selectedDetailLine = -1;
					Event.current.Use();
				}

				if (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown) {
					bool isMoveUp = false;
					bool isMoveDown = false;

					if (Event.current.keyCode == KeyCode.UpArrow) { //move up
						if (selectedDetailLine > 0) {
							selectedDetailLine--;
							isMoveUp = true;
						}
					}

					if (Event.current.keyCode == KeyCode.DownArrow) { //move down
						if (selectedDetailLine < detailLines.Count - 1) {
							selectedDetailLine++;
							isMoveDown = true;
						}
					}

					//change scrollbar
					if (isMoveUp || isMoveDown) {
						Event.current.Use();

						if (isMoveDown) {
							float scrollHeight = GetDetailPanelHeight();
							float numberVisiableRow = scrollHeight / DetailLineHeight;
							float topLine = selectedDetailLine - numberVisiableRow + 1;
							float currentTopLine = scrollViewDetail.y / DetailLineHeight;
							if (topLine < currentTopLine) {
								//nothing
							} else {
								float posY = topLine * DetailLineHeight;
								scrollViewDetail.y = posY;	
							}
						} else {
							float topLine = selectedDetailLine;
							float currentTopLine = scrollViewDetail.y / DetailLineHeight;
							if (topLine > currentTopLine) {
								//nothing
							} else {
								float posY = topLine * DetailLineHeight;
								scrollViewDetail.y = posY;
							}
						}
					}
				}
			}
		}
	}

	void PrepareData () {
		editingDetail = false;

		//to count collapse count
		Dictionary<string, Log> collapseDict = new Dictionary<string, Log>();

		bool isSearching = !string.IsNullOrEmpty(keySearch);
		bool isIgnoring = !string.IsNullOrEmpty(keyIgnore);

		string[] keySearchLowers = keySearch.ToLower().Split(Splitter).Where(x => x.Length > 0).ToArray();
		string[] keyIgnoreLowers = keyIgnore.ToLower().Split(Splitter).Where(x => x.Length > 0).ToArray();

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

				string conditionLower = log.condition.ToLower();

				//check search key
				if (isSearching) {
					bool contains = false;
					foreach (var key in keySearchLowers) {
						if (conditionLower.Contains(key)) {
							contains = true;
							break;
						}
					}

					if (!contains)
						continue;
				}

				//check ignore
				if (isIgnoring) {
					bool contains = false;
					foreach (var key in keyIgnoreLowers) {
						if (conditionLower.Contains(key)) {
							contains = true;
						}
					}

					if (contains)
						continue;
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

		columnTimeWidth = Math.Max(columnTimeWidth, MincolumnTimeWidth);
		columnFrameWidth = Math.Max(columnFrameWidth, MincolumnFrameWidth);
		columnFileWidth = Math.Max(columnFileWidth, MincolumnFileWidth);
		columnCollapseWidth = Math.Max(columnCollapseWidth, MincolumnCollapseWidth);

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
		DrawToolbar();
		DrawTitleRow();

		GUILayout.Space(ToolbarSpaceScrollView);

		GUILayout.BeginVertical();
		{
			DrawLogList(visiableLogs);

			ResizeScrollView();

			GUILayout.Space(ToolbarSpaceScrollView2);
			GUILayout.Space(ToolbarSpaceScrollView2);

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
		GUILayout.BeginHorizontal(EditorStyles.toolbar);
		{
			//clear log
			if (GUILayout.Button("Clear", EditorStyles.toolbarButton)) {
				ClearData();
			}

			GUILayout.Space(6);

			#region Toggle: Collapse, Clear on Play, Error Pause
			{
				//collapse toggle
				//EditorGUI.BeginChangeCheck();
				//logAsset.collapse = GUILayout.Toggle(logAsset.collapse, "Collapse", EditorStyles.toolbarButton);
				//bool changeCollapse = EditorGUI.EndChangeCheck();
				//if (changeCollapse)
				//	PrepareData();

				//clear on play toggle
				logAsset.clearOnPlay = GUILayout.Toggle(logAsset.clearOnPlay, "Clear on Play", EditorStyles.toolbarButton);

				//error pause toggle
				//logAsset.errorPause = GUILayout.Toggle(logAsset.errorPause, "Error Pause", EditorStyles.toolbarButton);
			}
			#endregion

			#region Search Filter
			{
				var searchFieldToolbarStyle = GUI.skin.FindStyle("ToolbarSeachTextField");
				var cancelToolbarStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton");
				GUILayout.Label("+");
				var lastSearchKey = keySearch;
				keySearch = GUILayout.TextField(keySearch, searchFieldToolbarStyle, GUILayout.Width(80));

				if (lastSearchKey != keySearch) {
					if (keySearch.EndsWith("\n")) {
						keySearch = keySearch.TrimEnd('\n');
					}
					PrepareData();
				}

				bool clearSearch = GUILayout.Button("X", cancelToolbarStyle);
				if (clearSearch) {
					keySearch = string.Empty;
					PrepareData();
				}

				//ignore
				GUILayout.Label("-");
				var lastIgnoreKey = keyIgnore;
				keyIgnore = GUILayout.TextField(keyIgnore, searchFieldToolbarStyle, GUILayout.Width(80));

				if (lastIgnoreKey != keyIgnore) {
					if (keyIgnore.EndsWith("\n")) {
						keyIgnore = keyIgnore.TrimEnd('\n');
					}
					PrepareData();
				}

				bool clearIgnore = GUILayout.Button("X", cancelToolbarStyle);
				if (clearIgnore) {
					keyIgnore = string.Empty;
					PrepareData();
				}
			}
			#endregion

			GUILayout.FlexibleSpace();

			#region Log - Warning - Error
			{
				GUIContent logcontent = new GUIContent("" + logAsset.countLog, logIcon);
				GUIContent warncontent = new GUIContent("" + logAsset.countWarn, warnIcon);
				GUIContent errorcontent = new GUIContent("" + logAsset.countError, errorIcon);
				GUIContent errorNoneContent = new GUIContent("0", errorIconInactive);

				bool changeLogType = false;
				EditorGUI.BeginChangeCheck();
				logAsset.showLog = GUILayout.Toggle(logAsset.showLog, logcontent, EditorStyles.toolbarButton, GUILayout.MaxHeight(ToolbarHeight-2));
				changeLogType |= EditorGUI.EndChangeCheck();

				EditorGUI.BeginChangeCheck();
				logAsset.showWarn = GUILayout.Toggle(logAsset.showWarn, warncontent, EditorStyles.toolbarButton, GUILayout.MaxHeight(ToolbarHeight-2));
				changeLogType |= EditorGUI.EndChangeCheck();

				EditorGUI.BeginChangeCheck();
				if (logAsset.countError > 0)
					logAsset.showError = GUILayout.Toggle(logAsset.showError, errorcontent, EditorStyles.toolbarButton, GUILayout.MaxHeight(ToolbarHeight-2));
				else
					logAsset.showError = GUILayout.Toggle(logAsset.showError, errorNoneContent, EditorStyles.toolbarButton, GUILayout.MaxHeight(ToolbarHeight-2));
				changeLogType |= EditorGUI.EndChangeCheck();

				if (changeLogType)
					PrepareData();
			}
			#endregion
		}
		GUILayout.EndHorizontal();
	}

	void DrawTitleRow () {
		GUILayout.BeginHorizontal();
		{
			float contentWidth = position.width - IconLogWidth;

			if (logAsset.collapse) {
				styleTitle.fixedWidth = columnCollapseWidth;
				GUILayout.Box("No", styleTitle);
				contentWidth -= columnCollapseWidth;
			}

			contentWidth -= IconLogWidth;
			styleTitle.fixedWidth = IconLogWidth;
			GUILayout.Box("T", styleTitle);

			if (logAsset.columnTime) {
				styleTitle.normal.textColor = new Color32(122, 51, 0, 255);
				styleTitle.fixedWidth = columnTimeWidth + 2;
				GUILayout.Box("Time", styleTitle);
				contentWidth -= columnTimeWidth;
			}

			if (logAsset.columnFrame) {
				styleTitle.normal.textColor = Color.blue;
				styleTitle.fixedWidth = columnFrameWidth + 2;
				GUILayout.Box("Frame", styleTitle);
				contentWidth -= columnFrameWidth;
			}

			if (logAsset.columnFile) {
				styleTitle.normal.textColor = new Color32(98, 0, 173, 255);
				styleTitle.fixedWidth = columnFileWidth + 2;
				GUILayout.Box("File", styleTitle);
				contentWidth -= columnFileWidth;
			}

			styleTitle.normal.textColor = Color.black;
			styleTitle.fixedWidth = contentWidth + 40; //hardcode
			GUILayout.Box("Log", styleTitle);
		}
		GUILayout.EndHorizontal();
	}

	void DrawLogList (List<Log> list) {
		
		//start scrollview
		scrollViewLogs = GUILayout.BeginScrollView(scrollViewLogs, 
			GUIStyle.none, 
			GUI.skin.verticalScrollbar,
			GUILayout.Width(position.width),
			GUILayout.Height(topPartHeight - ToolbarHeight - TitleRowHeight - ToolbarSpaceScrollView));
		
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

			if (editingDetail) {
				string text = currentLog.condition + "\n" + currentLog.stackTrace;
				var newValue = EditorGUILayout.DelayedTextField(text, styleDetailText, GUILayout.ExpandHeight(true));
				
				if (!newValue.Equals(text)) {
					editingDetail = false;
				}
			} else {
				//convert to lines
				string[] lines = (currentLog.condition + "\n" + currentLog.stackTrace.Trim()).Split('\n');
				detailLines = new List<string>(lines);

				if (selectedDetailLine >= detailLines.Count) {
					selectedDetailLine = -1;
				}

				for (int i = 0; i < detailLines.Count; i++) {

					//line content here
					bool clickedInLine = false;

					//change background of line
					if (i == selectedDetailLine) { // if is selected => BLUE background
						styleDetail.normal.textColor = Color.white;
						styleDetail.normal.background = texLogActive;

						styleDetail.onNormal.textColor = Color.white;
						styleDetail.onNormal.background = texLogActive;

					} else { //other hand, just make caro lines

						styleDetail.normal.textColor = Color.black;
						if (i % 2 == 0)
							styleDetail.normal.background = texLogBlack;
						else
							styleDetail.normal.background = texLogWhite;
					}

					clickedInLine = GUILayout.Button(detailLines[i], styleDetail);

					//line content here

					if (clickedInLine) {
						FocusMoveOnListDetail();
						CallStack callStack = LineToCallStack(detailLines[i]);

						//check double click to open script file
						if (i == selectedDetailLine) {
							float deltaTime = Time.realtimeSinceStartup - lastTimeClickInDetail;
							if (deltaTime < DoubleClickTime) {
								OpenCallStack(callStack);
							} else if (deltaTime < TimeEditDetail) {
								selectedDetailLine = i;
								editingDetail = true;
							}
						}

						selectedDetailLine = i;
						lastTimeClickInDetail = Time.realtimeSinceStartup;
					}

					if (clickedInLine) {
						selectedDetailLine = i;
					}
				}
			}
		}

		GUILayout.EndScrollView();
	}

	#endregion //Draw

	#region Resizable panel

	float topPartHeight;
	bool isResizing = false;
	Rect cursorChangeRect;
	Rect cursorChangeRectDraw;

	void ResizeScrollView(){
		cursorChangeRect = new Rect(0, topPartHeight, this.position.width, SplitHeight);
		cursorChangeRectDraw = new Rect(0, topPartHeight, this.position.width, 2);

		GUI.DrawTexture(cursorChangeRectDraw, texSeperator);

		EditorGUIUtility.AddCursorRect(cursorChangeRect, MouseCursor.ResizeVertical);

		if (Event.current.type == EventType.mouseDown && cursorChangeRect.Contains(Event.current.mousePosition)) {
			isResizing = true;
		}

		if (isResizing) {
			topPartHeight = Event.current.mousePosition.y;
			cursorChangeRect.Set(cursorChangeRect.x, topPartHeight, cursorChangeRect.width, cursorChangeRect.height);
		}

		if (Event.current.type == EventType.MouseUp) {
			isResizing = false;
		}

		if (topPartHeight < ToolbarHeight + TitleRowHeight + MinScrollHeight)
			topPartHeight = ToolbarHeight + TitleRowHeight + MinScrollHeight;
		
		if (position.height - topPartHeight < MinDetailHeight)
			topPartHeight = position.height - MinDetailHeight;
	}

	#endregion //Resizable panel

	#region Constants

	const char Splitter = '`';
	const float TimeEditDetail = 0.73f;

	const float MincolumnCollapseWidth = 30;
	const float MincolumnTimeWidth = 60;
	const float MincolumnFrameWidth = 50;
	const float MincolumnFileWidth = 70;

	const float FontWidth = 6.2f;
	const float DoubleClickTime = 0.3f;
	const float DetailLineHeight = 20;
	const float IconLogWidth = 26;
	const float LogHeight = 33;
	const float ToolbarSpaceScrollView = 0;
	const float ToolbarSpaceScrollView2 = 3;
	const float SplitHeight = 4;
	const float ToolbarHeight = 19;
	const float TitleRowHeight = 19;
	const float MinScrollHeight = 70;
	const float MinDetailHeight = 80;

	#endregion //Constants

	#region Resources Cache

	static Texture _logIcon;
	static public Texture logIcon {
		get {
			if (_logIcon == null) {
				_logIcon = EditorGUIUtility.Load("console.infoicon.sml") as Texture;
			}
			return _logIcon;
		}
	}

	static Texture _warnIcon;
	static public Texture warnIcon {
		get {
			if (_warnIcon == null) {
				_warnIcon = EditorGUIUtility.Load("console.warnicon.sml") as Texture;
			}
			return _warnIcon;
		}
	}

	static Texture _errorIcon;
	static public Texture errorIcon {
		get {
			if (_errorIcon == null) {
				_errorIcon = EditorGUIUtility.Load("console.erroricon.sml") as Texture;
			}
			return _errorIcon;
		}
	}

	static Texture _errorIconInactive;
	static public Texture errorIconInactive {
		get {
			if (_errorIconInactive == null) {
				_errorIconInactive = EditorGUIUtility.Load("console.erroricon.inactive.sml") as Texture;
			}
			return _errorIconInactive;
		}
	}

	static GUIStyle _styleDetailText;
	static public GUIStyle styleDetailText {
		get {
			if (_styleDetailText == null) {
				_styleDetailText = new GUIStyle(EditorStyles.label);
				_styleDetailText.fontSize = 11;
				_styleDetailText.fontStyle = FontStyle.Normal;
				_styleDetailText.margin = new RectOffset(0, 0, 0, 0);
				_styleDetailText.padding = new RectOffset(2, 2, 2, 2);
				_styleDetailText.wordWrap = true;
				_styleDetailText.richText = true;

				_styleDetailText.active.textColor = Color.black;
				_styleDetailText.onActive.textColor = Color.black;

				_styleDetailText.focused.textColor = Color.black;
				_styleDetailText.onFocused.textColor = Color.black;
			}

			return _styleDetailText;
		}
	}

	static GUIStyle _styleDetail;
	static public GUIStyle styleDetail {
		get {
			if (_styleDetail == null) 
			{
				_styleDetail = new GUIStyle(GUI.skin.textField);
				_styleDetail.alignment = TextAnchor.MiddleLeft;
				_styleDetail.fixedHeight = DetailLineHeight;

				_styleDetail.padding = new RectOffset(4, 0, 0, 0);
				_styleDetail.margin = new RectOffset(0, 0, 0, 0);
				_styleDetail.border = new RectOffset(0, 0, 0, 0);

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
			//if (_styleLogIcon == null) 
			{
				_styleLogIcon = new GUIStyle(GUI.skin.box);
				_styleLogIcon.alignment = TextAnchor.UpperLeft;
				_styleLogIcon.fixedWidth = IconLogWidth;
				_styleLogIcon.fixedHeight = LogHeight;

				_styleLogIcon.padding = new RectOffset(3, 0, 0, 0);
				_styleLogIcon.margin = new RectOffset(0, 1, 0, 1);
				_styleLogIcon.border = new RectOffset(0, 0, 0, 0);

				_styleLogIcon.alignment = TextAnchor.MiddleCenter;

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
				_styleLog.margin = new RectOffset(0, 1, 0, 1);
				_styleLog.border = new RectOffset(0, 0, 0, 0);
				_styleLog.richText = true;

				_styleLog.alignment = TextAnchor.MiddleLeft;

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

	static GUIStyle _styleTitle;
	static public GUIStyle styleTitle {
		get {
			if (_styleTitle == null) {
				_styleTitle = new GUIStyle(GUI.skin.box);
				_styleTitle.fontSize = 10;
				_styleTitle.padding = new RectOffset(0, 0, 0, 0);
				_styleTitle.margin = new RectOffset(0, 0, 0, 0);
				_styleTitle.fixedHeight = TitleRowHeight;
				_styleTitle.alignment = TextAnchor.MiddleCenter;
			}
			return _styleTitle;
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
				_texLogWhite = MakeTex(2, 2, new Color32(225, 225, 225, 255));
			}
			return _texLogWhite;
		}
	}

	static Texture2D _texSeperator;
	static public Texture2D texSeperator {
		get {
			if (_texSeperator == null)
				_texSeperator = MakeTex(1, 1, new Color32(100, 100, 100, 255));
			return _texSeperator;
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
		//only for compiling warning
		if (log.callstack != null && log.type != LogType.Warning && string.IsNullOrEmpty(log.stackTrace)) {
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
		} catch {}

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