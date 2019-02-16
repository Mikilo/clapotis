using NGTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;

namespace NGToolsEditor.NGConsole
{
	using UnityEngine;

	namespace Test
	{
		public class LambdaClass
		{
			public static class NestedClass
			{
				public static void	A()
				{
					Debug.Log("Log in NestedClass in nested namespace.");
				}
			}

			public void	B()
			{
				Debug.Log("Log in nested namespace.");
			}
		}

		public interface IObserver<T>
		{
			void OnNext(T v);
			void OnError(Exception e);
		}

		// same as AnonymousObserver...
		class Subscribe<T> : IObserver<T>
		{
			public Action<T> a = (v) => Debug.Log(v);

			public Subscribe(Action<T> onNext, Action<Exception> onError, Action onCompleted)
			{
				var t = typeof(Subscribe<T>);
				foreach (var item in t.GetMethods((BindingFlags)(-1)))
				{
					Debug.Log(item);
				}
			}

			public void OnNext(T value)
			{
				Action aa = () =>
			    {
				   this.a(value);
			    };

				aa();
			}

			public void OnError(Exception error)
			{
				Debug.Log(error);
			}

			public void OnCompleted()
			{
				Debug.Log("OnCompleted");
			}
		}
	}

	[Serializable]
	[ExcludeFromExport]
	internal sealed class DebugModule : Module
	{
		public bool	debug;
		public bool	increment;

		public int	outputCountPerIteration = 1;
		public int	outputIterations = 1;

		public override void	OnEnable(NGConsoleWindow editor, int id)
		{
			base.OnEnable(editor, id);

			Conf.DebugModeChanged += this.OnDebugModeChanged;

			if (Conf.DebugMode != Conf.DebugState.None)
				this.console.PostOnGUIHeader += DrawDebugBar;
		}

		public override void	OnDisable()
		{
			base.OnDisable();

			Conf.DebugModeChanged -= this.OnDebugModeChanged;

			this.console.PostOnGUIHeader -= DrawDebugBar;
		}

		private void	OnDebugModeChanged()
		{
			this.console.PostOnGUIHeader -= DrawDebugBar;
			if (Conf.DebugMode != Conf.DebugState.None)
				this.console.PostOnGUIHeader += DrawDebugBar;

			this.console.Repaint();
		}

		private Rect	DrawDebugBar(Rect r)
		{
			GeneralSettings	general = HQ.Settings.Get<GeneralSettings>();
			float			x = r.x;

			r.height = 16F;

			GUI.Box(r, GUIContent.none);
			GUI.Label(r, "Debug");

			r.x += 50F;

			r.width = 20F;
			this.debug = GUI.Toggle(r, this.debug, "");
			r.x += r.width;

			r.width = 50F;
			if (GUI.Button(r, "Sync", general.MenuButtonStyle) == true)
				this.console.syncLogs.Sync();
			r.x += r.width;

			if (GUI.Button(r, "GC", general.MenuButtonStyle) == true)
			{
				long	before = GC.GetTotalMemory(false);
				GC.Collect();
				Debug.Log(before + " > " + GC.GetTotalMemory(true));
			}
			r.x += r.width;

			r.width = 75F;
			if (GUI.Button(r, "ClrCFiles", general.MenuButtonStyle) == true)
				ConsoleUtility.files.Reset();
			r.x += r.width;
			if (GUI.Button(r, "LogJSON", general.MenuButtonStyle) == true)
				NGDebug.LogJSON(@"{""array"":[{""ante"":{""currency"",""amount"":100},""unlocked"":{},""currentCount"":0},{""ante"":{""currency"":""JADE""},""unlocked"":false,""currentCount"":1}],""action"":""/somewhere-here"",""status"":""OK""}");
			r.x += r.width;
			if (GUI.Button(r, "LogJSON1", general.MenuButtonStyle) == true)
				NGDebug.LogJSON("BOUYA", @"{""antes"":[{""ante"":{""currency"":""GOLD"",""amount"":100},""unlocked"":{},""requiredCountForUnlock"":0,""currentCount"":0},{""ante"":{""currency"":""JADE"",""amount"":5},""unlocked"":false,""requiredCountForUnlock"":5,""currentCount"":1},{""ante"":{""currency"":""GOLD"",""amount"":1000},""unlocked"":false,""requiredCountForUnlock"":10,""currentCount"":0},{""ante"":{""currency"":""JADE"",""amount"":25},""unlocked"":false,""requiredCountForUnlock"":20,""currentCount"":0}],""action"":""/game-lobby/duel/list-unlocked-fuzzy-antes"",""status"":""OK""}", null);
			r.x += r.width;
			if (GUI.Button(r, "LogJSON2", general.MenuButtonStyle) == true)
				NGDebug.LogJSON(string.Empty, @"{""antes"":[{""ante"":{""currency"":""GOLD"",""amount"":100},""unlocked"":{},""requiredCountForUnlock"":0,""currentCount"":0},{""ante"":{""currency"":""JADE"",""amount"":5},""unlocked"":false,""requiredCountForUnlock"":5,""currentCount"":1},{""ante"":{""currency"":""GOLD"",""amount"":1000},""unlocked"":false,""requiredCountForUnlock"":10,""currentCount"":0},{""ante"":{""currency"":""JADE"",""amount"":25},""unlocked"":false,""requiredCountForUnlock"":20,""currentCount"":0}],""action"":""/game-lobby/duel/list-unlocked-fuzzy-antes"",""status"":""OK""}", null);
			r.x += r.width;
			if (GUI.Button(r, "LogJSON3", general.MenuButtonStyle) == true)
				NGDebug.LogJSON(string.Empty, string.Empty, null);
			r.x += r.width;
			if (GUI.Button(r, "LogJSON4", general.MenuButtonStyle) == true)
				NGDebug.LogJSON(@"{""log"":""2018-03-28 02:19:00,970 DEBUG vert.x-eventloop-thread-2 PlatformVerticle.createErrorResponse - snd >> action=/sign-up/android data={\""responseMessage\"":\""Version is required,Platform is required\"",\""error\"":true,\""responseCode\"":\""UNEXPECTED_ERROR\"",\""httpCode\"":0,\""action\"":\""/sign-up/android\"",\""status\"":\""ERROR\""}\n"",""stream"":""stdout"",""docker"":{""container_id"":""c4d937e3dd6e942cea9af038ebd1df73f19aa214a246ad07e51fa013265fef39""},""kubernetes"":{""namespace_name"":""production"",""pod_id"":""af967c32-3207-11e8-9583-025b712b992c"",""pod_name"":""net-server-7f7b589947-c9nn5"",""container_name"":""net-server"",""labels"":{""app"":""net-server"",""component"":""hz-discovery-svc"",""pod-template-hash"":""3936145503""},""host"":""ip-192-168-51-179.us-west-2.compute.internal""}}");
			r.x += r.width;
			if (GUI.Button(r, "LogJSON5", general.MenuButtonStyle) == true)
				NGDebug.LogJSON("BABA", string.Empty, null);
			r.x += r.width;
			if (GUI.Button(r, "LogJSON6", general.MenuButtonStyle) == true)
				NGDebug.LogJSON("{[]");
			r.x += r.width;
			if (GUI.Button(r, "ClrCClasses", general.MenuButtonStyle) == true)
				ConsoleUtility.classes = new FastClassCache();
			r.x += r.width;
			if (GUI.Button(r, "ClrLCP", general.MenuButtonStyle) == true)
				LogConditionParser.cachedFrames.Clear();
			r.x += r.width;
			if (GUI.Button(r, "DiffLcl", general.MenuButtonStyle) == true)
				this.DiffLocalesMissingKeysFromDefaultLanguage();
			r.x += r.width;
			if (GUI.Button(r, "CheckLcl", general.MenuButtonStyle) == true)
				this.CheckUnusedLocales();
			r.x += r.width;
			if (GUI.Button(r, "MultCtxts", general.MenuButtonStyle) == true)
				NGDebug.Log(general, general, general);
			r.x += r.width;
			if (GUI.Button(r, "Snapshot", general.MenuButtonStyle) == true)
				NGDebug.Snapshot(this);
			r.x += r.width;
			if (GUI.Button(r, "TestParam", general.MenuButtonStyle) == true)
			{
				int	n = 0;
				int? m = 0;
				Type	t = null;
				Vector3?	p = null;
				this.TestParameterTypes<Type>(0, 1, true, 3, 4, 5, 6, ConsoleColor.Black, null, p, new Rect(), new Rect(),
											  out n, ref n, out m, ref m, null, out t);
			}
			r.x += r.width;

			if (GUI.Button(r, "ListReg", general.MenuButtonStyle) == true)
				this.OutputWinReg();

			r.x = x;
			r.y += r.height;

			r.width = 50F;

			// Test cases
			if (GUI.Button(r, "1L", general.MenuButtonStyle) == true)
				Debug.Log("monoline");
			r.x += r.width;

			r.width = 40F;
			this.outputCountPerIteration = EditorGUI.IntField(r, this.outputCountPerIteration);
			r.x += r.width;

			r.width = 10F;
			GUI.Label(r, "x");
			r.x += r.width;

			r.width = 40F;
			this.outputIterations = EditorGUI.IntField(r, this.outputIterations);
			r.x += r.width;

			r.width = 40F;

			if (GUI.Button(r, "Go", general.MenuButtonStyle) == true)
				Utility.StartBackgroundTask(this.TaskWriteLogs());
			r.x += r.width;

			r.width = 20F;
			this.increment = GUI.Toggle(r, this.increment, "");
			r.x += r.width;

			r.width = 40F;
			if (GUI.Button(r, "2L", general.MenuButtonStyle) == true)
				Debug.Log("first\nSECOND");
			r.x += r.width;
			if (GUI.Button(r, "3L", general.MenuButtonStyle) == true)
				Debug.Log("first\nSECOND\nThird");
			r.x += r.width;
			if (GUI.Button(r, "10L", general.MenuButtonStyle) == true)
				Debug.Log("first\nSECOND\nThird\n4\n5\n6\n7\n8\n9\n10");
			r.x += r.width;
			if (GUI.Button(r, "20L", general.MenuButtonStyle) == true)
				Debug.Log("first\nSECOND\nThird\n4\n5\n6\n7\n8\n9\n10\nfirst\nSECOND\nThird\n4\n5\n6\n7\n8\n9\n10");
			r.x += r.width;
			if (GUI.Button(r, "Warn.", general.MenuButtonStyle) == true)
				Debug.LogWarning("Warning");
			r.x += r.width;
			if (GUI.Button(r, "Err.", general.MenuButtonStyle) == true)
				Debug.LogError("Error");
			r.x += r.width;
			if (GUI.Button(r, "NExc.", general.MenuButtonStyle) == true)
				Debug.LogException(new NotImplementedException("NotImp", new Exception("innerException")));
			r.x += r.width;
			if (GUI.Button(r, "Exc.", general.MenuButtonStyle) == true)
			{
				Action a = () =>
				{
					try
					{
						//this.NewMethod();
					}
					catch (Exception ex)
					{
						Debug.LogException(ex);
					}
				};
				a.BeginInvoke(null, null);
			}
			r.x += r.width;
			if (GUI.Button(r, "CatA", general.MenuButtonStyle) == true)
				this.CatA("TestA");
			r.x += r.width;
			if (GUI.Button(r, "CatB", general.MenuButtonStyle) == true)
				this.CatB("TestB");
			r.x += r.width;
			r.width = 60F;
			if (GUI.Button(r, "Assert", general.MenuButtonStyle) == true)
				Debug.Assert(false, "Assert test");
			r.x += r.width;
			if (GUI.Button(r, "NestedA", general.MenuButtonStyle) == true)
				Test.LambdaClass.NestedClass.A();
			r.x += r.width;
			if (GUI.Button(r, "NestedB", general.MenuButtonStyle) == true)
				new Test.LambdaClass().B();
			r.x += r.width;
			if (GUI.Button(r, "HardcoreNestedB", general.MenuButtonStyle) == true)
			{
				Test.IObserver<string> s = new Test.Subscribe<string>(null, null, null);
				s.OnNext("test");
			}
			r.x += r.width;
			if (GUI.Button(r, "DeepLog", general.MenuButtonStyle) == true)
			{
				try
				{
					using (MemoryStream ms = new MemoryStream())
					{
						ms.Read(null, 0, 0);
					}
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException(ex);
				}
			}
			// Syntax error
			//();
			// Warning unused variable
			//int	unusedVariable = 123;

			r.x = x;
			r.y += r.height;

			return r;
		}

		//private void NewMethod()
		//{
		//	var f = typeof(NGTools.Tests.ComplexClass).GetProperty("setFloatProp", BindingFlags.Static | BindingFlags.Public);
		//	f.GetValue(null);

		//	int[] a = new int[] { 0, 2 };
		//	a[3] = 4;
		//}

		[NGLogger("A")]
		public void	CatA(object message)
		{
			UnityEngine.Debug.Log("[" + Constants.PackageTitle + "] " + message);
		}

		[NGLogger("B")]
		public void	CatB(object message)
		{
			UnityEngine.Debug.LogWarning("[" + Constants.PackageTitle + "] " + message);
		}

		private void	OutputWinReg()
		{
			Debug.Log("x32");

			string	registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
			using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registry_key))
			{
				foreach (string subkey_name in key.GetSubKeyNames())
				{
					using (Microsoft.Win32.RegistryKey subkey = key.OpenSubKey(subkey_name))
					{
						Debug.Log(subkey.GetValue("DisplayName"));
					}
				}
			}

			Debug.Log("x64");

			registry_key = @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall";
			using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(registry_key))
			{
				foreach (string subkey_name in key.GetSubKeyNames())
				{
					using (Microsoft.Win32.RegistryKey subkey = key.OpenSubKey(subkey_name))
					{
						Debug.LogError(subkey.GetValue("DisplayName"));
					}
				}
			}
		}

		private void	CheckUnusedLocales()
		{
			try
			{
				Dictionary<string, string>	defaultLocale = new Dictionary<string, string>();

				Localization.LoadLanguage(Constants.DefaultLanguage, defaultLocale, true);

				Dictionary<string, int>	keys = new Dictionary<string, int>(defaultLocale.Count);

				foreach (string key in defaultLocale.Keys)
				{
					keys.Add(key, 0);
				}

				foreach (Type type in Utility.EachAllSubClassesOf(typeof(object)))
				{
					FieldInfo[]	fields = type.GetFields();

					for (int i = 0; i < fields.Length; i++)
					{
						LocaleHeaderAttribute[]	attributes = fields[i].GetCustomAttributes(typeof(LocaleHeaderAttribute), false) as LocaleHeaderAttribute[];

						if (attributes.Length > 0)
						{
							if (keys.ContainsKey(attributes[0].key) == true)
								keys[attributes[0].key]++;
							else
								Debug.Log("LocaleHeader \"" + attributes[0].key + "\" is missing from default locale.");
						}
					}
				}

				//foreach (var item in keys)
				//{
				//	if (item.Value == 0)
				//		Debug.Log("Unused key \"" + item.Key + "\".");
				//}
			}
			catch
			{
			}
		}

		private IEnumerator	TaskWriteLogs()
		{
			int	n = this.outputCountPerIteration;
			int	m = this.outputIterations;

			if (this.increment == false)
			{
				for (int i = 0; i < m; i++)
				{
					for (int j = 0; j < n; j++)
						Debug.Log("Line");

					yield return null;
				}
			}
			else
			{
				for (int i = 0; i < m; i++)
				{
					for (int j = 1; j <= n; j++)
						Debug.Log("Line" + (j + i * j));

					yield return null;
				}
			}
		}

		private void	TestParameterTypes<T>(float a, int b, bool c, byte d, short f, double g, decimal h,
											  Enum i, ILogFilter j, Vector3? j2, Rect l, Rect? m,
											  out int n, ref int o, out int? p, ref int? q,
											  T r, out T t, ILogFilter v = null, int w = 13, string x = "Jojo") where T : class
		{
			n = 0;
			p = 0;
			t = default(T);
			Debug.Log("Test parameter types.");
		}
		

		/// <summary>
		/// Checks missing keys from all languages against default language, then checks for new keys missing in the default language.
		/// </summary>
		public void	DiffLocalesMissingKeysFromDefaultLanguage()
		{
			try
			{
				string	rootPath = Path.Combine(HQ.RootPath, Constants.RelativeLocaleFolder);

				string[]						languages = Directory.GetDirectories(rootPath);
				Dictionary<string, string>		defaultLocale = new Dictionary<string, string>();
				Dictionary<string, string>		workingLocale = new Dictionary<string, string>();
				Dictionary<string, TextAsset>	assetFiles = new Dictionary<string, TextAsset>();
				TextAsset						file = null;

				Localization.LoadLanguage(Constants.DefaultLanguage, defaultLocale, true);

				// Remove full path from languages.
				for (int i = 0; i < languages.Length; i++)
					languages[i] = languages[i].Substring(rootPath.Length);

				for (int i = 0; i < languages.Length; i++)
				{
					if (languages[i] == Constants.DefaultLanguage)
						continue;

					Localization.LoadLanguage(languages[i], workingLocale, true);

					foreach (var pair in defaultLocale)
					{
						// Check missing key from current locale against default one.
						if (workingLocale.ContainsKey(pair.Key) == false)
						{
							if (assetFiles.TryGetValue(pair.Value, out file) == false)
							{
								file = AssetDatabase.LoadAssetAtPath(pair.Value, typeof(TextAsset)) as TextAsset;
								assetFiles.Add(pair.Value, file);
							}

							Debug.LogError("Localization[" + languages[i] + "][" + pair.Key + "] is missing, detected at \"" + pair.Value + "\".", file);
						}
					}

					foreach (var pair in workingLocale)
					{
						// Check for new keys.
						if (defaultLocale.ContainsKey(pair.Key) == false)
						{
							if (assetFiles.TryGetValue(pair.Value, out file) == false)
							{
								file = AssetDatabase.LoadAssetAtPath(pair.Value, typeof(TextAsset)) as TextAsset;
								assetFiles.Add(pair.Value, file);
							}

							Debug.LogWarning("Localization[" + languages[i] + "][" + pair.Key + "] is new from \"" + pair.Value + "\"", file);
						}
					}
				}
			}
			catch
			{
			}
		}
	}
}