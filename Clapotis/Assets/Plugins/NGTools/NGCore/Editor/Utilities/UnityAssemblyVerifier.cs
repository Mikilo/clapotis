using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public class UnityAssemblyVerifier : EditorWindow
	{
		private const string	SkipWarningPrefKey = "UnityAssemblyVerifier_Check";

		private static List<KeyValuePair<string, string>>	missing = new List<KeyValuePair<string, string>>();
		private static string								cachedMissing = null;

		public static Type			TryGetType(Assembly assembly, string type)
		{
			Type	result = assembly.GetType(type);

			if (result == null)
				UnityAssemblyVerifier.OpenWindow(assembly.FullName, type);

			return result;
		}

		public static FieldInfo		TryGetField(Type type, string fieldName, BindingFlags bindingFlags)
		{
			FieldInfo	result = type.GetField(fieldName, bindingFlags);

			if (result == null)
				UnityAssemblyVerifier.OpenWindow(type.FullName, fieldName);

			return result;
		}

		public static PropertyInfo	TryGetProperty(Type type, string propertyName, BindingFlags bindingFlags)
		{
			PropertyInfo result = type.GetProperty(propertyName, bindingFlags);

			if (result == null)
				UnityAssemblyVerifier.OpenWindow(type.FullName, propertyName);

			return result;
		}

		public static MethodInfo	TryGetMethod(Type type, string methodName, BindingFlags bindingFlags)
		{
			MethodInfo	result = type.GetMethod(methodName, bindingFlags);

			if (result == null)
				UnityAssemblyVerifier.OpenWindow(type.FullName, methodName);

			return result;
		}

		public static MethodInfo	TryGetMethod(Type type, string methodName, Type[] arguments)
		{
			MethodInfo	result = type.GetMethod(methodName, arguments);

			if (result == null)
				UnityAssemblyVerifier.OpenWindow(type.FullName, methodName);

			return result;
		}

		public static Type			TryGetNestedType(Type type, string subTypeName, BindingFlags bindingFlags)
		{
			Type	result = type.GetNestedType(subTypeName, bindingFlags);

			if (result == null)
				UnityAssemblyVerifier.OpenWindow(type.FullName, subTypeName);

			return result;
		}

		private static void			OpenWindow(string type, string value)
		{
			UnityAssemblyVerifier.missing.Add(new KeyValuePair<string, string>(type, value));
			UnityAssemblyVerifier.cachedMissing = null;

			EditorApplication.delayCall += () =>
			{
				if (NGEditorPrefs.GetBool(UnityAssemblyVerifier.SkipWarningPrefKey) == false)
					EditorWindow.GetWindow<UnityAssemblyVerifier>(true, Constants.PackageTitle).CenterOnMainWin();
			};
		}

		protected virtual void	OnGUI()
		{
			if (UnityAssemblyVerifier.cachedMissing == null)
			{
				if (UnityAssemblyVerifier.missing.Count > 0)
				{
					StringBuilder	buffer = Utility.GetBuffer();

					for (int i = 0; i < UnityAssemblyVerifier.missing.Count; i++)
					{
						if (buffer.Length > 0)
							buffer.AppendLine();
						buffer.AppendLine(UnityAssemblyVerifier.missing[i].Key);
						buffer.AppendLine(UnityAssemblyVerifier.missing[i].Value);
					}

					buffer.Length -= Environment.NewLine.Length;

					UnityAssemblyVerifier.cachedMissing = Utility.ReturnBuffer(buffer);
				}
				else
					UnityAssemblyVerifier.cachedMissing = string.Empty;
			}

			EditorGUILayout.HelpBox(Constants.PackageTitle + " has detected a change in Unity code. Please contact the author.", MessageType.Error);

			if (GUILayout.Button("Contact the author") == true)
				ContactFormWizard.Open(ContactFormWizard.Subject.BugReport, "The following metadata are missing :" + Environment.NewLine + UnityAssemblyVerifier.cachedMissing);

			if (GUILayout.Button("Don't show the message again") == true)
			{
				NGEditorPrefs.SetBool(UnityAssemblyVerifier.SkipWarningPrefKey, true);
				this.Close();
			}

			GUILayout.Label("The following metadata are missing :");
			EditorGUILayout.TextArea(UnityAssemblyVerifier.cachedMissing);
		}
	}
}