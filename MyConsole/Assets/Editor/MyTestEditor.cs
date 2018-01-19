using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using System.Linq;

public class MethodContext {
	public bool fold;
	public ContextMenu context;
	public Dictionary<ParameterInfo, object> paras;
}

[CustomEditor(typeof(MonoBehaviour), true)]
[CanEditMultipleObjects]
public class MyTestEditor : Editor
{
	public static readonly GUILayoutOption[] opts = new GUILayoutOption[]{};

	public static Type TypeContextMenu = typeof(ContextMenu);

	public Dictionary<MethodInfo, MethodContext> dict;

	protected Dictionary<MethodInfo, MethodContext>  GetAllMethods () {
		var allMethods = new Dictionary<MethodInfo, MethodContext>();

		var methods = target.GetType().GetMethods();
		foreach (var method in methods) {
			var customAttributes = method.GetCustomAttributes(true);
			foreach (var att in customAttributes) {
				Type type = att.GetType();
				if (type == TypeContextMenu) {

					MethodContext methodContext = new MethodContext();
					methodContext.context = att as ContextMenu;
					methodContext.paras = new Dictionary<ParameterInfo, object>();

					var arrParameter = method.GetParameters();
					foreach (var para in arrParameter) {
						object value = null;

						if (para.ParameterType.IsEnum) {
							string defaultName = Enum.GetValues(para.ParameterType).GetValue(0).ToString();
							value = Enum.Parse(para.ParameterType, defaultName);
						} else if (para.ParameterType == typeof(bool)) {
							value = false;
						} else if (para.ParameterType == typeof(int)) {
							value = 0;
						} else if (para.ParameterType == typeof(float)) {
							value = 0.0f;
						} else if (para.ParameterType == typeof(double)) {
							value = 0.0;
						} else if (para.ParameterType == typeof(string)) {
							value = string.Empty;
						} else if (para.ParameterType == typeof(Vector2)) {
							value = Vector2.zero;
						} else if (para.ParameterType == typeof(Vector3)) {
							value = Vector3.zero;
						} else if (para.ParameterType == typeof(Vector4)) {
							value = Vector4.zero;
						} else {
							value = null;
						}

						methodContext.paras.Add(para, value);
					}

					allMethods.Add(method, methodContext);
				}
			}
		}

		return allMethods;
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (dict == null)
			dict = GetAllMethods();

		if (dict.Count > 0) {
			int colum = (int)(Screen.width / 200f);

			foreach (var pair in dict) {
				var methodInfo = pair.Key;
				var methodContext = pair.Value;
				var paras = methodInfo.GetParameters();

				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				EditorGUILayout.BeginHorizontal();
				{
					//button
					if (GUILayout.Button(methodContext.context.menuItem, GUILayout.Width(160))) {
						var arr = methodContext.paras.Values.ToArray();
						methodInfo.Invoke(target, arr);
					}

					if (paras.Length > 0) {
						GUILayout.Space(15);
						methodContext.fold = EditorGUILayout.Foldout(methodContext.fold, "Parameters");
					}
				}
				EditorGUILayout.EndHorizontal();

				if (methodContext.fold)
				{
					//parameters
					EditorGUILayout.BeginVertical();
					{
						foreach (var para in paras) {
							if (para.ParameterType.IsEnum) {
								string res = EditorGUILayout.EnumPopup(para.Name, (Enum)methodContext.paras[para]).ToString();
								methodContext.paras[para] = Enum.Parse(para.ParameterType, res);
							} else if (para.ParameterType == typeof(bool)) {
								methodContext.paras[para] = EditorGUILayout.Toggle(para.Name, (bool)methodContext.paras[para]);
							} else if (para.ParameterType == typeof(int)) {
								methodContext.paras[para] = EditorGUILayout.IntField(para.Name, (int)methodContext.paras[para]);
							} else if (para.ParameterType == typeof(float)) {
								methodContext.paras[para] = EditorGUILayout.FloatField(para.Name, (float)methodContext.paras[para]);
							} else if (para.ParameterType == typeof(double)) {
								methodContext.paras[para] = EditorGUILayout.DoubleField(para.Name, (double)methodContext.paras[para]);
							} else if (para.ParameterType == typeof(string)) {
								methodContext.paras[para] = EditorGUILayout.TextField(para.Name, methodContext.paras[para].ToString());
							} else if (para.ParameterType == typeof(Vector2)) {
								methodContext.paras[para] = EditorGUILayout.Vector2Field(para.Name, (Vector2)methodContext.paras[para]);
							} else if (para.ParameterType == typeof(Vector3)) {
								methodContext.paras[para] = EditorGUILayout.Vector3Field(para.Name, (Vector3)methodContext.paras[para]);
							} else if (para.ParameterType == typeof(Vector4)) {
								methodContext.paras[para] = EditorGUILayout.Vector4Field(para.Name, (Vector4)methodContext.paras[para]);
							} else {
								methodContext.paras[para] = EditorGUILayout.ObjectField(para.Name, methodContext.paras[para] as UnityEngine.Object, para.ParameterType, true);
							}
						}
					}
					EditorGUILayout.EndVertical();
				}
				EditorGUILayout.EndVertical();

			}//foreach
		}
	}
}