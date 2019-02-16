using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NGPackageHelpers
{
	/// <summary>
	/// <para>Gives this public non-static field a default value when calling Utility.LoadEditorPref.</para>
	/// <para>Only works on integer, float, bool, string and enum.</para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class DefaultValueEditorPrefAttribute : Attribute
	{
		public readonly object	defaultValue;

		public	DefaultValueEditorPrefAttribute(object defaultValue)
		{
			this.defaultValue = defaultValue;
		}
	}

	public static class Utility
	{
		public static GUIContent	content = new GUIContent();

		private static EditorPrefType[]	editorPrefInstances;

		/// <summary>
		/// <para>Saves all public non-static fields of the given <paramref name="instance"/> in EditorPrefs.</para>
		/// <para>Only works on integer, float, bool, string, string[], Vector3, enum and Object.</para>
		/// <para>Use NonSerializedAttribute to prevent serializing.</para>
		/// <para>Use SerializeField to serialize protected or private fields.</para>
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="prefix"></param>
		public static void	SaveEditorPref(object instance, string prefix = "")
		{
			if (Utility.editorPrefInstances == null)
			{
				Type[]	types = Utility.GetSubClassesOf(typeof(EditorPrefType));

				Utility.editorPrefInstances = new EditorPrefType[types.Length];

				for (int i = 0; i < types.Length; i++)
					Utility.editorPrefInstances[i] = Activator.CreateInstance(types[i]) as EditorPrefType;
			}

			foreach (var field in Utility.EachFieldHierarchyOrdered(instance.GetType(), typeof(object), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (field.IsDefined(typeof(NonSerializedAttribute), true) == true ||
					(field.IsPublic == false && field.IsDefined(typeof(SerializeField), true) == false))
				{
					continue;
				}

				for (int j = 0; j < Utility.editorPrefInstances.Length; j++)
				{
					if (Utility.editorPrefInstances[j].CanHandle(field) == true)
					{
						Utility.editorPrefInstances[j].Save(instance, field, prefix);
						break;
					}
				}
			}
		}

		/// <summary>
		/// <para>Restores values from EditorPrefs to all public non-static fields.</para>
		/// <para>Only works on integer, float, bool, string, string[], Vector3, enum and Object.</para>
		/// <para>Use NonSerializedAttribute to prevent serializing.</para>
		/// <para>Use SerializeField to serialize protected or private fields.</para>
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="prefix"></param>
		public static void	LoadEditorPref(object instance, string prefix = "")
		{
			if (Utility.editorPrefInstances == null)
			{
				Type[]	types = Utility.GetSubClassesOf(typeof(EditorPrefType));

				Utility.editorPrefInstances = new EditorPrefType[types.Length];

				for (int i = 0; i < types.Length; i++)
					Utility.editorPrefInstances[i] = Activator.CreateInstance(types[i]) as EditorPrefType;
			}

			foreach (var field in Utility.EachFieldHierarchyOrdered(instance.GetType(), typeof(object), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				if (field.IsDefined(typeof(NonSerializedAttribute), true) == true ||
					(field.IsPublic == false && field.IsDefined(typeof(SerializeField), true) == false))
				{
					continue;
				}

				for (int j = 0; j < Utility.editorPrefInstances.Length; j++)
				{
					if (Utility.editorPrefInstances[j].CanHandle(field) == true)
					{
						Utility.editorPrefInstances[j].Load(instance, field, prefix);
						break;
					}
				}
			}
		}

		public static string	GetPerProjectPrefix()
		{
			return PlayerSettings.productName + '.';
		}

		private static Type[]	assemblyTypes;

		public static Type[]	GetSubClassesOf(Type baseType)
		{
			if (Utility.assemblyTypes == null)
				Utility.assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();

			return Utility.assemblyTypes.Where(t => t.IsSubclassOf(baseType)).ToArray();
		}

		/// <summary>
		/// Gets element's type of array supported by Unity Inspector.
		/// </summary>
		/// <param name="arrayType"></param>
		/// <returns></returns>
		/// <seealso cref="TypeExtension.IsUnityArray"/>
		public static Type		GetArraySubType(Type arrayType)
		{
			if (arrayType.IsArray == true)
				return arrayType.GetElementType();

			if (arrayType.GetInterface(typeof(IList<>).Name) != null) // IList<> with Serializable elements.
				return arrayType.GetInterface(typeof(IList<>).Name).GetGenericArguments()[0];

			return null;
		}

		public static IEnumerable<FieldInfo>	EachFieldHierarchyOrdered(Type t, Type stopType, BindingFlags flags)
		{
			var	inheritances = new Stack<Type>();

			inheritances.Push(t);

			if (t.BaseType != null)
			{
				while (t.BaseType != stopType)
				{
					inheritances.Push(t.BaseType);
					t = t.BaseType;
				}
			}

			foreach (var type in inheritances)
			{
				FieldInfo[]	fields = type.GetFields(flags | BindingFlags.DeclaredOnly);

				for (int i = 0; i < fields.Length; i++)
					yield return fields[i];
			}
		}

		public static byte[]	SerializeField(object field)
		{
			using (MemoryStream	ms = new MemoryStream())
			{
				BinaryFormatter	bin = new BinaryFormatter();
				bin.Serialize(ms, field);
				return ms.ToArray();
			}
		}

		public static T			DeserializeField<T>(byte[] raw)
		{
			using (MemoryStream	ms = new MemoryStream(raw))
			{
				BinaryFormatter	bin = new BinaryFormatter();
				return (T)bin.Deserialize(ms);
			}
		}

		public static void	ShowExplorer(string itemPath)
		{
			itemPath = itemPath.Replace(@"/", @"\"); // explorer doesn't like front slashes
			Process.Start("explorer.exe", "/select," + itemPath);
		}

		/// <summary>
		/// <para>Copies a folder to an other folder. Can skip folders or files following the rules followed by Unity.</para>
		/// <para>See http://docs.unity3d.com/Manual/SpecialFolders.html"</para>
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <param name="skipSpecial"></param>
		public static void	CopyFolder(string source, string destination, bool skipSpecial = false)
		{
			foreach (string dirPath in Directory.GetDirectories(source, "*", System.IO.SearchOption.TopDirectoryOnly))
			{
				if (skipSpecial == true)
				{
					string	lastDirName = dirPath.Substring(dirPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
					if (lastDirName[0] == '.' ||
						lastDirName[0] == '~' ||
						lastDirName == "cvs")
					{
						continue;
					}

					DirectoryInfo	dirInfo = new DirectoryInfo(dirPath);
					if ((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
						continue;
				}

				string	destSubDir = destination + dirPath.Substring(source.Length);
				Directory.CreateDirectory(destSubDir);

				foreach (string filePath in Directory.GetFiles(dirPath, "*.*", System.IO.SearchOption.TopDirectoryOnly))
				{
					if (skipSpecial == true)
					{
						string	lastDirName = filePath.Substring(filePath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
						if (lastDirName[0] == '.' ||
							lastDirName[0] == '~' ||
							lastDirName == "cvs" ||
							lastDirName.EndsWith("tmp") == true)
						{
							continue;
						}

						FileInfo	fileInfo = new FileInfo(filePath);
						if ((fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
							continue;
					}

					File.Copy(filePath, destination + filePath.Substring(source.Length));
				}

				Utility.CopyFolder(dirPath, destSubDir, skipSpecial);
			}
		}

		public static void		StartBackgroundTask(IEnumerator update, Action end = null)
		{
			EditorApplication.CallbackFunction	closureCallback = null;

			closureCallback = () =>
			{
				try
				{
					if (update.MoveNext() == false)
					{
						if (end != null)
							end();
						EditorApplication.update -= closureCallback;
					}
				}
				catch (Exception ex)
				{
					if (end != null)
						end();
					InternalNGDebug.LogException(ex);
					EditorApplication.update -= closureCallback;
				}
			};

			EditorApplication.update += closureCallback;
		}

		private class CallbackSchedule
		{
			public Action	action;
			public int		intervalTicks;
			public int		ticksLeft;
		}

		private static List<CallbackSchedule>	schedules;

		public static void	RegisterIntervalCallback(Action action, int ticks)
		{
			if (Utility.schedules == null || Utility.schedules.Count == 0)
			{
				Utility.schedules = new List<CallbackSchedule>();
				EditorApplication.update += Utility.UpdateWindows;
			}

			Utility.schedules.Add(new CallbackSchedule() { action = action, ticksLeft = ticks, intervalTicks = ticks });
		}

		public static void	UnregisterIntervalCallback(Action action)
		{
			for (int i = 0; i < Utility.schedules.Count; i++)
			{
				if (Utility.schedules[i].action == action)
				{
					Utility.schedules.RemoveAt(i);

					if (Utility.schedules.Count == 0)
						EditorApplication.update -= Utility.UpdateWindows;

					break;
				}
			}
		}

		private static void	UpdateWindows()
		{
			for (int i = 0; i < Utility.schedules.Count; i++)
			{
				if (--Utility.schedules[i].ticksLeft <= 0)
				{
					Utility.schedules[i].ticksLeft = Utility.schedules[i].intervalTicks;
					Utility.schedules[i].action();
				}
			}
		}

		private static Stack<StringBuilder>	poolBuffers = new Stack<StringBuilder>(2);

		public static StringBuilder	GetBuffer()
		{
			if (Utility.poolBuffers.Count > 0)
				return Utility.poolBuffers.Pop();
			return new StringBuilder(64);
		}

		public static string		ReturnBuffer(StringBuilder buffer)
		{
			string	result = buffer.ToString();
			buffer.Length = 0;
			Utility.poolBuffers.Push(buffer);
			return result;
		}

		/// <summary>
		/// Checks if "ProjectSettings/ProjectVersion.txt" exists.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static bool	IsUnityProject(string path)
		{
			return string.IsNullOrEmpty(path) == false && (File.Exists(Path.Combine(path, "ProjectSettings/ProjectVersion.txt")) || Directory.Exists(Path.Combine(path, "ProjectSettings")));
		}

		public static string	GetUnityVersion(string path)
		{
			// Search into install directory.
			string	uninstallPath = Path.Combine(path, @"Editor\Uninstall.exe");

			if (File.Exists(uninstallPath) == true)
			{
				FileVersionInfo	fileVersion = FileVersionInfo.GetVersionInfo(uninstallPath);
				return fileVersion.ProductName.Replace("Unity", string.Empty).Replace("(64-bit)", string.Empty).Replace(" ", string.Empty);
			}

			// Search into Unity project.
			uninstallPath = Path.Combine(path, @"ProjectSettings\ProjectVersion.txt");

			if (File.Exists(uninstallPath) == true)
			{
				using (FileStream fs = File.Open(uninstallPath, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (BufferedStream bs = new BufferedStream(fs))
				using (StreamReader sr = new StreamReader(bs))
				{
					string	line;

					while ((line = sr.ReadLine()) != null)
					{
						if (line.StartsWith("m_EditorVersion: ") == true)
							return line.Substring("m_EditorVersion: ".Length);
					}
				}
			}

			// Search through directory name.
			int	n = path.Length;

			if (n < 7)
				return string.Empty;

			// Minor version.
			int	dot = path.LastIndexOf('.');
			if (dot == -1)
				return string.Empty;

			// Major version.
			dot = path.LastIndexOf('.', dot - 1);
			if (dot == -1)
				return string.Empty;

			// Find the earliest non-numeric char.
			int	offset = 1;
			while (path[dot - offset - 1] >= '0' && path[dot - offset - 1] <= '9')
				++offset;

			string	unityVersion = path.Substring(dot - offset, path.Length - (dot - offset));

			for (int i = unityVersion.LastIndexOf('.') + 1; i < unityVersion.Length; i++)
			{
				if ((unityVersion[i] < '0' || unityVersion[i] > '9') &&
					unityVersion[i] != 'a' && unityVersion[i] != 'b' && unityVersion[i] != 'f' && unityVersion[i] != 'p' && unityVersion[i] != 'x')
				{
					return unityVersion.Substring(0, i);
				}
			}

			return unityVersion;
		}
	}
}