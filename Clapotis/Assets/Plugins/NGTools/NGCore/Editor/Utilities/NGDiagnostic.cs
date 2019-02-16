using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace NGToolsEditor
{
	using UnityEngine;

	public class NGDiagnostic : EditorWindow
	{
		public const string	Title = "NG Diagnostic";

		private static Dictionary<string, List<KeyValuePair<string, string>>>	logs = new Dictionary<string, List<KeyValuePair<string, string>>>();

		private static bool				initialized = false;
		private static bool				inDiagnostic = false;
		private static Stack<Action>	diagnosis = new Stack<Action>();
		private static int				forceEndDiagnosis = 20;

		private string	result;
		private Vector2	scrollPosition;

		static	NGDiagnostic()
		{
			if (NGDiagnostic.IsInDiagnostic() == true)
			{
				Utility.SafeDelayCall(() => EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Diagnosing...", 6F / 7F));
				Utility.RegisterIntervalCallback(NGDiagnostic.PrepareResult, 100);
			}
		}

		public static void	Log(string group, string key, object content)
		{
			if (NGDiagnostic.IsInDiagnostic() == false)
				return;

			if (Conf.DebugMode == Conf.DebugState.Verbose)
				Debug.Log("[" + group + "] " + key + "=" + content);

			List<KeyValuePair<string, string>>	logs;

			if (NGDiagnostic.logs.TryGetValue(group, out logs) == false)
			{
				logs = new List<KeyValuePair<string, string>>();
				NGDiagnostic.logs.Add(group, logs);
			}

			if (content == null)
				logs.Add(new KeyValuePair<string, string>(key, "null"));
			else
				logs.Add(new KeyValuePair<string, string>(key, content.ToString()));

			int	totalLogs = 0;
			foreach (var l in NGDiagnostic.logs)
				totalLogs += l.Value.Count;

			EditorApplication.delayCall += () => EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Diagnosing... (" + totalLogs + " / " + NGDiagnostic.logs.Count + ")", 6F / 7F);
		}

		public static void	DelayDiagnostic(Action callback)
		{
			if (NGDiagnostic.IsInDiagnostic() == false)
				return;

			NGDiagnostic.diagnosis.Push(callback);
		}

		public static bool	IsInDiagnostic()
		{
			if (NGDiagnostic.initialized == false)
			{
				NGDiagnostic.initialized = true;
				NGDiagnostic.inDiagnostic = File.Exists(Path.Combine(Path.GetTempPath(), "NGT_Diagnosing"));
			}

			return NGDiagnostic.inDiagnostic;
		}

		public static void	Diagnose()
		{
			try
			{
				EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Starting diagnostic...", 0F / 7F);
				File.Create(Path.Combine(Path.GetTempPath(), "NGT_Diagnosing")).Close();

				if (EditorApplication.isPlaying == true)
				{
					EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Stopping scene...", 1F / 7F);
					EditorApplication.isPlaying = false;
				}

				EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Saving assets...", 2F / 7F);
				AssetDatabase.SaveAssets();

				EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Saving scene...", 3F / 7F);
				EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

				EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Refresh assets...", 4F / 7F);

				Utility.RecompileUnityEditor();

				//EditorUtility.DisplayProgressBar(Constants.PackageTitle, "Fetching windows...", 5F / 7F);
				//foreach (Type t in Utility.EachSubClassesOf(typeof(EditorWindow), t => t.Namespace.StartsWith("NGToolsEditor")))
				//{
				//	Object[]	instances = Resources.FindObjectsOfTypeAll(t);
				//	Debug.Log(t + " " + instances.Length);
				//}
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}
			finally
			{
			}
		}

		private static void	PrepareResult()
		{
			if (NGDiagnostic.diagnosis.Count > 0)
			{
				if (HQ.Settings == null)
				{
					--NGDiagnostic.forceEndDiagnosis;

					if (NGDiagnostic.forceEndDiagnosis <= 0)
						NGDiagnostic.diagnosis.Clear();
					return;
				}

				try
				{
					NGDiagnostic.diagnosis.Pop()();
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}
				return;
			}

			try
			{
				EditorUtility.ClearProgressBar();
				Utility.UnregisterIntervalCallback(NGDiagnostic.PrepareResult);
				File.Delete(Path.Combine(Path.GetTempPath(), "NGT_Diagnosing"));
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}

			NGDiagnostic	window = EditorWindow.GetWindow<NGDiagnostic>(true, NGDiagnostic.Title, true);
			StringBuilder	buffer = Utility.GetBuffer();

			GUI.FocusControl(null);
			foreach (var item in NGDiagnostic.logs)
			{
				buffer.AppendLine(item.Key);
				for (int i = 0; i < item.Value.Count; i++)
				{
					buffer.Append(item.Value[i].Key);
					buffer.Append("=");
					buffer.AppendLine(item.Value[i].Value);
				}

				buffer.AppendLine();
			}

			if (buffer.Length > Environment.NewLine.Length << 1)
				buffer.Length -= Environment.NewLine.Length << 1;

			window.result = Utility.ReturnBuffer(buffer);
		}

		protected virtual void	OnGUI()
		{
			if (GUILayout.Button("Contact the author") == true)
				ContactFormWizard.Open(ContactFormWizard.Subject.Support, this.result);

			Rect	r = this.position;
			r.x = 0F;
			r.y = 20F;
			r.height -= 20F;

			Utility.content.text = this.result;
			float	minHeight = GUI.skin.textArea.CalcHeight(Utility.content, r.width);

			this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);
			{
				EditorGUILayout.SelectableLabel(this.result, GUI.skin.textArea,
					new GUILayoutOption[]
					{
						GUILayoutOptionPool.ExpandWidthTrue,
						GUILayoutOptionPool.ExpandHeightTrue,
						GUILayout.MinHeight(minHeight),
					}
				);
			}
			GUILayout.EndScrollView();
		}
	}
}