using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace NGToolsEditor
{
	using UnityEngine;

	public static class NGEditorGUILayout
	{
		[Flags]
		public enum FieldButtons
		{
			None = 0,
			All = Browse | Open,
			Browse = 1,
			Open = 2
		}

		private const string	LastSaveFilePathKeyPref = "NGEditorGUILayout.lastSaveFilePath";
		private const string	LastOpenFilePathKeyPref = "NGEditorGUILayout.lastOpenFilePath";
		private const string	LastOpenFolderPathKeyPref = "NGEditorGUILayout.lastOpenFolderPath";

		public static string	SaveFileField(Rect r2, string label, string path, string defaultName = "", string extension = "", FieldButtons buttons = FieldButtons.All)
		{
			Rect	rBrowse = new Rect(r2.x, r2.y, r2.width, r2.height);
			Rect	rOpen = new Rect(r2.x, r2.y, r2.width, r2.height);

			if ((buttons & FieldButtons.Browse) != 0)
			{
				Utility.content.text = "Browse";
				rBrowse.width = (buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button).CalcSize(Utility.content).x;
				r2.width -= rBrowse.width;
			}

			if ((buttons & FieldButtons.Open) != 0)
			{
				Utility.content.text = "Open";
				rOpen.width = (buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button).CalcSize(Utility.content).x;
				r2.width -= rOpen.width;
			}

			path = EditorGUI.TextField(r2, label, path);

			if ((buttons & FieldButtons.Browse) != 0)
			{
				rBrowse.x = r2.xMax;
				r2.xMax = rBrowse.xMax;
				Utility.content.text = "Browse";
				//Rect	r = GUILayoutUtility.GetRect(Utility.content, buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button, GUILayoutOptionPool.ExpandWidthFalse);
				//r.y -= 2F;
				if (GUI.Button(rBrowse, Utility.content, buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button) == true)
				{
					string	directory = string.IsNullOrEmpty(path) == false ? Path.GetDirectoryName(path) : EditorPrefs.GetString(NGEditorGUILayout.LastSaveFilePathKeyPref);
					string	projectPath = EditorUtility.SaveFilePanel(label, directory, defaultName, extension);

					if (string.IsNullOrEmpty(projectPath) == false)
					{
						EditorPrefs.SetString(NGEditorGUILayout.LastSaveFilePathKeyPref, Path.GetDirectoryName(projectPath));
						path = projectPath;
						GUI.FocusControl(null);
					}
				}
			}

			if ((buttons & FieldButtons.Open) != 0)
			{
				rOpen.x = r2.xMax;
				EditorGUI.BeginDisabledGroup(false);
				{
					GUI.enabled = true;
					Utility.content.text = "Open";
					//Rect	r = GUILayoutUtility.GetRect(Utility.content, buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button, GUILayoutOptionPool.ExpandWidthFalse);
					//r.y -= 2F;
					if (GUI.Button(rOpen, Utility.content, buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button) == true)
						EditorUtility.RevealInFinder(path);
				}
				EditorGUI.EndDisabledGroup();
			}

			return path;
		}

		public static string	SaveFileField(string label, string path, string defaultName = "", string extension = "", FieldButtons buttons = FieldButtons.All)
		{
			EditorGUILayout.BeginHorizontal();
			{
				path = EditorGUILayout.TextField(label, path);

				if ((buttons & FieldButtons.Browse) != 0)
				{
					Utility.content.text = "Browse";
					Rect	r = GUILayoutUtility.GetRect(Utility.content, buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button, GUILayoutOptionPool.ExpandWidthFalse);
					r.y -= 2F;
					if (GUI.Button(r, Utility.content, buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button) == true)
					{
						string	directory = string.IsNullOrEmpty(path) == false ? Path.GetDirectoryName(path) : EditorPrefs.GetString(NGEditorGUILayout.LastSaveFilePathKeyPref);
						string	projectPath = EditorUtility.SaveFilePanel(label, directory, defaultName, extension);

						if (string.IsNullOrEmpty(projectPath) == false)
						{
							EditorPrefs.SetString(NGEditorGUILayout.LastSaveFilePathKeyPref, Path.GetDirectoryName(projectPath));
							path = projectPath;
							GUI.FocusControl(null);
						}
					}
				}

				if ((buttons & FieldButtons.Open) != 0)
				{
					EditorGUI.BeginDisabledGroup(false);
					{
						GUI.enabled = true;
						Utility.content.text = "Open";
						Rect	r = GUILayoutUtility.GetRect(Utility.content, buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button, GUILayoutOptionPool.ExpandWidthFalse);
						r.y -= 2F;
						if (GUI.Button(r, Utility.content, buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button) == true)
							EditorUtility.RevealInFinder(path);
					}
					EditorGUI.EndDisabledGroup();
				}
			}
			EditorGUILayout.EndHorizontal();

			return path;
		}

		public static string	OpenFileField(string label, string path, string extension = "", FieldButtons buttons = FieldButtons.All)
		{
			EditorGUILayout.BeginHorizontal();
			{
				path = EditorGUILayout.TextField(label, path);

				if ((buttons & FieldButtons.Browse) != 0)
				{
					Utility.content.text = "Browse";
					Rect	r = GUILayoutUtility.GetRect(Utility.content, buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button, GUILayoutOptionPool.ExpandWidthFalse);
					r.y -= 2F;
					if (GUI.Button(r, Utility.content, buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button) == true)
					{
						string	directory = string.IsNullOrEmpty(path) == false ? Path.GetDirectoryName(path) : EditorPrefs.GetString(NGEditorGUILayout.LastOpenFilePathKeyPref);
						string	projectPath = EditorUtility.OpenFilePanel(label, directory, extension);

						if (string.IsNullOrEmpty(projectPath) == false)
						{
							EditorPrefs.SetString(NGEditorGUILayout.LastOpenFilePathKeyPref, Path.GetDirectoryName(projectPath));
							path = projectPath;
							GUI.FocusControl(null);
						}
					}
				}

				if ((buttons & FieldButtons.Open) != 0)
				{
					EditorGUI.BeginDisabledGroup(false);
					{
						GUI.enabled = true;
						Utility.content.text = "Open";
						Rect	r = GUILayoutUtility.GetRect(Utility.content, buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button, GUILayoutOptionPool.ExpandWidthFalse);
						r.y -= 2F;
						if (GUI.Button(r, Utility.content, buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button) == true)
							EditorUtility.RevealInFinder(path);
					}
					EditorGUI.EndDisabledGroup();
				}
			}
			EditorGUILayout.EndHorizontal();

			return path;
		}

		public static string	OpenFolderField(string label, string path, FieldButtons buttons = FieldButtons.All)
		{
			EditorGUILayout.BeginHorizontal();
			{
				path = EditorGUILayout.TextField(label, path);

				if ((buttons & FieldButtons.Browse) != 0)
				{
					Utility.content.text = "Browse";
					Rect	r = GUILayoutUtility.GetRect(Utility.content, buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button, GUILayoutOptionPool.ExpandWidthFalse);
					r.y -= 2F;
					if (GUI.Button(r, Utility.content, buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button) == true)
					{
						path = string.IsNullOrEmpty(path) == false ? path : EditorPrefs.GetString(NGEditorGUILayout.LastOpenFolderPathKeyPref);
						string	projectPath = EditorUtility.OpenFolderPanel(label, path, string.Empty);

						if (string.IsNullOrEmpty(projectPath) == false)
						{
							EditorPrefs.SetString(NGEditorGUILayout.LastOpenFolderPathKeyPref, Path.GetDirectoryName(projectPath));
							path = projectPath;
							GUI.FocusControl(null);
						}
					}
				}

				if ((buttons & FieldButtons.Open) != 0)
				{
					EditorGUI.BeginDisabledGroup(false);
					{
						GUI.enabled = true;
						Utility.content.text = "Open";
						Rect	r = GUILayoutUtility.GetRect(Utility.content, buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button, GUILayoutOptionPool.ExpandWidthFalse);
						r.y -= 2F;
						if (GUI.Button(r, Utility.content, buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button) == true)
							EditorUtility.RevealInFinder(path);
					}
					EditorGUI.EndDisabledGroup();
				}
			}
			EditorGUILayout.EndHorizontal();

			return path;
		}

		public static string	OpenFolderField(Rect r, string label, string path, string prefixPath, FieldButtons buttons = FieldButtons.All)
		{
			float	browseWidth = 0F;
			float	openWidth = 0F;

			if ((buttons & FieldButtons.Browse) != 0)
			{
				Utility.content.text = "Browse";
				browseWidth = 55F;
			}

			if ((buttons & FieldButtons.Open) != 0)
			{
				Utility.content.text = "Open";
				openWidth = 45F;
			}

			r.width -= browseWidth + openWidth;
			path = EditorGUI.TextField(r, label, path);
			r.x += r.width;
			r.y -= 1F;
			r.height += 1F;

			if ((buttons & FieldButtons.Browse) != 0)
			{
				r.width = browseWidth;
				if (GUI.Button(r, "Browse", buttons == FieldButtons.All ? "ButtonLeft" : GUI.skin.button) == true)
				{
					path = string.IsNullOrEmpty(path) == false ? path : EditorPrefs.GetString(NGEditorGUILayout.LastOpenFolderPathKeyPref);
					string	projectPath = EditorUtility.OpenFolderPanel(label, Path.Combine(prefixPath, path), string.Empty);

					if (string.IsNullOrEmpty(projectPath) == false)
					{
						EditorPrefs.SetString(NGEditorGUILayout.LastOpenFolderPathKeyPref, Path.GetDirectoryName(projectPath));
						path = projectPath;
						GUI.FocusControl(null);
					}
				}
				r.x += r.width;
			}

			if ((buttons & FieldButtons.Open) != 0)
			{
				EditorGUI.BeginDisabledGroup(false);
				{
					GUI.enabled = true;
					r.width = openWidth;
					if (GUI.Button(r, "Open", buttons == FieldButtons.All ? "ButtonRight" : GUI.skin.button) == true)
						EditorUtility.RevealInFinder(Path.Combine(prefixPath, path));
				}
				EditorGUI.EndDisabledGroup();
			}

			return path;
		}

		public static string	OpenFolderField(Rect r, string label, string path, FieldButtons buttons = FieldButtons.All)
		{
			return NGEditorGUILayout.OpenFolderField(r, label, path, string.Empty, buttons);
		}

		private static double		lastClick;
		private static GUIContent	content = new GUIContent();

		public static int	PingObject(Rect r, string label, UnityEngine.Object asset)
		{
			NGEditorGUILayout.content.text = label;
			return NGEditorGUILayout.PingObject(r, NGEditorGUILayout.content, asset, null);
		}

		public static int	PingObject(Rect r, GUIContent content, UnityEngine.Object asset, GUIStyle style)
		{
			if (style == null)
				style = GUI.skin.button;

			if (GUI.Button(r, content, style) == true)
				return NGEditorGUILayout.PingObject(asset);
			return 0;
		}

		public static int	PingObject(string label, UnityEngine.Object asset, params GUILayoutOption[] options)
		{
			NGEditorGUILayout.content.text = label;
			return NGEditorGUILayout.PingObject(NGEditorGUILayout.content, asset, null, options);
		}

		public static int	PingObject(string label, UnityEngine.Object asset, GUIStyle style, params GUILayoutOption[] options)
		{
			NGEditorGUILayout.content.text = label;
			return NGEditorGUILayout.PingObject(NGEditorGUILayout.content, asset, style, options);
		}

		public static int	PingObject(GUIContent content, UnityEngine.Object asset, GUIStyle style, params GUILayoutOption[] options)
		{
			if (style == null)
				style = GUI.skin.button;

			if (GUILayout.Button(content, style, options) == true)
				return NGEditorGUILayout.PingObject(asset);
			return 0;
		}

		public static int	PingObject(Object asset)
		{
			if (Event.current.button == 1 || NGEditorGUILayout.lastClick + Constants.DoubleClickTime > EditorApplication.timeSinceStartup)
			{
				Selection.activeObject = asset;
				return 2;
			}
			else
			{
				EditorGUIUtility.PingObject(asset);
				NGEditorGUILayout.lastClick = EditorApplication.timeSinceStartup;
				return 1;
			}
		}

		private struct ElasticCacheData
		{
			public string	content;
			public float	width;
			public GUIStyle	style;
		}

		public class ElasticCacheDataComparer : IEqualityComparer<ElasticCacheData>
		{
			bool	IEqualityComparer<ElasticCacheData>.Equals(ElasticCacheData x, ElasticCacheData y)
			{
				return x.content == y.content &&
					   x.width == y.width &&
					   x.style == y.style;
			}

			int	IEqualityComparer<ElasticCacheData>.GetHashCode(ElasticCacheData obj)
			{
				return obj.content.GetHashCode() + obj.style.GetHashCode() + (int)obj.width;
			}
		}

		private static Dictionary<ElasticCacheData, string>	elasticCache = new Dictionary<ElasticCacheData, string>(256, new ElasticCacheDataComparer());

		public static void	ElasticLabel(Rect r, string content, char separator, GUIStyle style = null)
		{
			if (style == null)
				style = GUI.skin.label;

			string		result;
			ElasticCacheData	b = new ElasticCacheData() { content = content, width = r.width, style = style };

			if (NGEditorGUILayout.elasticCache.TryGetValue(b, out result) == false)
			{
				result = content;
				Utility.content.text = content;
				if (style.CalcSize(Utility.content).x > r.width)
				{
					int	n = content.LastIndexOf(separator);

					if (n != -1)
					{
						string	firstPart = content.Substring(0, n);
						string	main = "…" + content.Substring(n + 1);

						Utility.content.text = main;
						float	mainWidth = style.CalcSize(Utility.content).x;

						Utility.content.text = firstPart;

						do
						{
							Utility.content.text = Utility.content.text.Substring(0, Utility.content.text.Length - 1);
						}
						while (Utility.content.text.Length > 1 && style.CalcSize(Utility.content).x + mainWidth > r.width);

						result = Utility.content.text + main;
					}
				}

				NGEditorGUILayout.elasticCache.Add(b, result);
			}

			GUI.Label(r, result, style);
		}

		private const float							SwitchAnimationSpeed = 3F;
		private static Color						TrueColor { get { return Utility.GetSkinColor(0F, 1F, .2F, 1F, 0F, 1F, .2F, 1F); } }
		private static Color						FalseColor { get { return Utility.GetSkinColor(0F, .5F, .5F, 1F, .2F, 1F, 1F, 1F); } }
		private static Dictionary<int, AnimFloat>	switchAnimations = new Dictionary<int, AnimFloat>();

		public static bool	Switch(Rect position, bool value)
		{
			return NGEditorGUILayout.Switch(position, string.Empty, value);
		}

		public static bool	Switch(string label, bool value)
		{
			return NGEditorGUILayout.Switch(GUILayoutUtility.GetRect(0F, Constants.SingleLineHeight), label, value);
		}

		public static bool	Switch(Rect position, string label, bool value)
		{
			AnimFloat	af = null;
			int			id = EditorGUIUtility.GetControlID("NGEditorGUILayout.Switch".GetHashCode(), FocusType.Keyboard, position);
			bool		mouseIn = position.Contains(Event.current.mousePosition);
			float		switchWidth = position.height * 2F;

			if (switchWidth < position.width)
			{
				Rect	r2 = position;
				r2.xMin += switchWidth;
				Utility.content.text = label;
				EditorGUI.PrefixLabel(r2, id, Utility.content);

				position.width = switchWidth;
			}

			if (mouseIn == true)
				EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

			if (Event.current.type == EventType.MouseMove)
			{
				// Need to check, in rare case, a MouseMove can be triggered without any mouseOverWindow.
				if (EditorWindow.mouseOverWindow != null)
					EditorWindow.mouseOverWindow.Repaint();
			}
			else if (Event.current.type == EventType.MouseDown)
			{
				if (mouseIn == true)
				{
					value = !value;

					GUI.changed = true;

					if (switchAnimations.TryGetValue(id, out af) == true)
					{
						if (value == true)
							af.target = 1F;
						else
							af.target = 0F;
					}
					else
					{
						af = new AnimFloat(0F, EditorWindow.mouseOverWindow.Repaint);
						af.speed = NGEditorGUILayout.SwitchAnimationSpeed;
						switchAnimations.Add(id, af);

						if (value == true)
						{
							af.value = 0F;
							af.target = 1F;
						}
						else
						{
							af.value = 1F;
							af.target = 0F;
						}
					}

					Event.current.Use();
				}
			}

			float	radius = position.height * .5F - 1F;

			if (radius < 3F)
				radius = 3F;

			position.xMin += radius + 1F + 4F;
			position.xMax -= radius + 1F;
			position.y += radius + 1F;

			Color	color;
			float	xOffset;

			if (af == null && switchAnimations.TryGetValue(id, out af) == true)
			{
				if (af.isAnimating == false)
				{
					if (value == false)
					{
						color = NGEditorGUILayout.FalseColor;
						xOffset = 0F;
					}
					else
					{
						color = NGEditorGUILayout.TrueColor;
						xOffset = position.width;
					}

					switchAnimations.Remove(id);
				}
				else
				{
					color = Color.Lerp(NGEditorGUILayout.FalseColor, NGEditorGUILayout.TrueColor, af.value);
					xOffset = Mathf.Lerp(0F, position.width, af.value);
				}
			}
			else
			{
				if (value == false)
				{
					color = NGEditorGUILayout.FalseColor;
					xOffset = 0F;
				}
				else
				{
					color = NGEditorGUILayout.TrueColor;
					xOffset = position.width;
				}
			}

			using (HandlesColorRestorer.Get(color))
			{
				Handles.BeginGUI();
				Handles.DrawSolidDisc(new Vector3(position.x + xOffset, position.y, 0F), new Vector3(0F, 0F, 1F), radius - 2F);
				Handles.DrawWireArc(new Vector3(position.x, position.y), Vector3.forward, Vector3.up, 180F, radius);
				Handles.DrawWireArc(new Vector3(position.x + position.width, position.y), Vector3.forward, Vector3.down, 180F, radius);
				Handles.DrawLine(new Vector3(position.x, position.y + radius), new Vector3(position.x + position.width, position.y + radius));
				Handles.DrawLine(new Vector3(position.x, position.y - radius + 1F), new Vector3(position.x + position.width, position.y - radius + 1F));
				Handles.EndGUI();
			}

			return value;
		}

		public readonly static Color	HighlightToggleOutlineColor = Color.yellow;

		public static bool	OutlineToggle(Rect r, string label, bool value, GUIStyle style)
		{
			Utility.content.text = label;
			if (GUI.Button(r, Utility.content, style) == true)
				value = !value;

			if (value == true && Event.current.type == EventType.Repaint)
			{
				r.xMin += 1F;
				r.height = 3F;
				EditorGUI.DrawRect(r, NGEditorGUILayout.HighlightToggleOutlineColor);
			}

			return value;
		}

		public static bool	OutlineToggle(Rect r, string label, bool value)
		{
			Utility.content.text = label;
			if (GUI.Button(r, Utility.content, GeneralStyles.ToolbarButton) == true)
				value = !value;

			if (value == true && Event.current.type == EventType.Repaint)
			{
				r.xMin += 1F;
				r.height = 3F;
				EditorGUI.DrawRect(r, NGEditorGUILayout.HighlightToggleOutlineColor);
			}

			return value;
		}

		public static bool	OutlineToggle(string label, bool value)
		{
			Utility.content.text = label;
			Rect	r = GUILayoutUtility.GetRect(Utility.content, GeneralStyles.ToolbarButton);
			if (GUI.Button(r, Utility.content, GeneralStyles.ToolbarButton) == true)
				value = !value;

			if (value == true && Event.current.type == EventType.Repaint)
			{
				r.xMin += 1F;
				r.height = 3F;
				EditorGUI.DrawRect(r, NGEditorGUILayout.HighlightToggleOutlineColor);
			}

			return value;
		}

		private static Dictionary<Type, GUIContent[]>	cachedEnumValues;

		public static Enum	EnumPopup(Rect r, Enum e)
		{
			return NGEditorGUILayout.EnumPopup(r, string.Empty, e);
		}

		public static Enum	EnumPopup(Rect r, string label, Enum e)
		{
			if (NGEditorGUILayout.cachedEnumValues == null)
				NGEditorGUILayout.cachedEnumValues = new Dictionary<Type, GUIContent[]>();

			Type			type = e.GetType();
			GUIContent[]	values;

			if (NGEditorGUILayout.cachedEnumValues.TryGetValue(type, out values) == false)
			{
				string[]	rawValues = Enum.GetNames(type);
				values = new GUIContent[rawValues.Length];

				for (int i = 0; i < values.Length; i++)
					values[i] = new GUIContent(Utility.NicifyVariableName(rawValues[i]));

				NGEditorGUILayout.cachedEnumValues.Add(type, values);
			}

			int	v = (int)(object)e;
			Utility.content.text = label;
			return (Enum)Enum.ToObject(type, EditorGUI.Popup(r, Utility.content, v, values));
		}

		public static void	RuleOverlay(EditorWindow window)
		{
			window.wantsMouseMove = true;

			if (Event.current.type == EventType.MouseMove)
				window.Repaint();

			int		n = 0;
			Rect	r = new Rect(0F, 0F, 1F, window.position.height);

			EditorGUI.DrawRect(r, Color.black);

			r.height = 1F;

			while (r.y < window.position.height)
			{
				if ((n % 10) == 0)
					r.width = 15F;
				else if ((n % 5) == 0)
					r.width = 10F;
				else
					r.width = 5F;

				EditorGUI.DrawRect(r, Color.black);
				r.y += 5F;
				++n;
			}

			r.y = 0F;
			r.width = window.position.width;
			r.height = 1F;

			EditorGUI.DrawRect(r, Color.black);

			r.width = 1F;

			n = 0;

			while (r.x < window.position.width)
			{
				if ((n % 10) == 0)
					r.height = 15F;
				else if ((n % 5) == 0)
					r.height = 10F;
				else
					r.height = 5F;

				EditorGUI.DrawRect(r, Color.black);
				r.x += 5F;
				++n;
			}

			r.x = 0F;
			r.y = Event.current.mousePosition.y;
			r.width = window.position.width;
			r.height = 1F;
			EditorGUI.DrawRect(r, Color.yellow * .8F);

			r.x = Event.current.mousePosition.x;
			r.y = 0F;
			r.width = 1F;
			r.height = window.position.height;
			EditorGUI.DrawRect(r, Color.yellow * .8F);

			GUI.Label(new Rect(15F, window.position.height - 24F, window.position.width, 16F), Event.current.mousePosition.ToString());
		}
	}
}