using NGTools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public class NGChangeLogWindow : EditorWindow
	{
		public struct ChangeLog
		{
			public string	name;
			public string	version;
			public string	date;
			public string	content;
		}

		public const string	Title = "NG Change Log";
		public const string	DontNotifyNewUpdateKeyPref = "NGChangeLog_DontNotifyNewUpdate";
		public const float	NextButtonWidth = 75F;
		public const float	NextButtonHeight = 24F;

		private static Dictionary<string, List<ChangeLog>>	changeLogs = new Dictionary<string, List<ChangeLog>>();
		private static Queue<KeyValuePair<string, int>>		pendingTools = new Queue<KeyValuePair<string, int>>();
		private static NGChangeLogWindow					updateNotificationWindow;

		private Vector2		scrollPosition;
		private string		currentReviewingToolName;
		private string		currentToolName;
		private int			currentChangeLog;
		private GUIStyle	contentStyle;
		private bool		notifyNewUpdate;

		public static void	Open(string toolName)
		{
			Utility.OpenWindow<NGChangeLogWindow>(true, NGChangeLogWindow.Title, true, null, w =>
			{
				w.currentToolName = toolName;
				NGChangeLogWindow.pendingTools.Clear();
			});
		}

		private static void	Open()
		{
			if (NGChangeLogWindow.updateNotificationWindow == null)
			{
				NGChangeLogWindow.updateNotificationWindow = EditorWindow.CreateInstance<NGChangeLogWindow>();
				NGChangeLogWindow.updateNotificationWindow.name = NGChangeLogWindow.Title;
				NGChangeLogWindow.updateNotificationWindow.minSize = new Vector2(400F, 350F);
				NGChangeLogWindow.updateNotificationWindow.maxSize = NGChangeLogWindow.updateNotificationWindow.minSize;

				Rect	r = Utility.GetEditorMainWindowPos();
				r.x += r.width / 2F - 200F;
				r.y += r.height / 2F - 175F;
				r.width = 400F;
				r.height = 350F;
				NGChangeLogWindow.updateNotificationWindow.position = r;
				NGChangeLogWindow.updateNotificationWindow.ShowPopup();
			}
			else
				NGChangeLogWindow.updateNotificationWindow.Repaint();
		}

		protected virtual void	OnEnable()
		{
			this.notifyNewUpdate = !NGEditorPrefs.GetBool(DontNotifyNewUpdateKeyPref);
		}

		protected virtual void	OnDisable()
		{
			NGEditorPrefs.SetBool(DontNotifyNewUpdateKeyPref, !this.notifyNewUpdate);
		}

		protected virtual void	OnGUI()
		{
			if (this.contentStyle == null)
			{
				this.contentStyle = new GUIStyle(EditorStyles.label);
				this.contentStyle.wordWrap = true;
				this.contentStyle.fontSize = 11;
			}

			Rect	r = this.position;

			r.x = 0F;
			r.y = 0F;
			r.height -= NGChangeLogWindow.NextButtonHeight;

			if (NGChangeLogWindow.pendingTools.Count > 0)
			{
				List<ChangeLog>	changeLog2 = null;

				while (NGChangeLogWindow.pendingTools.Count > 0 && NGChangeLogWindow.changeLogs.TryGetValue(NGChangeLogWindow.pendingTools.Peek().Key, out changeLog2) == false)
					NGChangeLogWindow.pendingTools.Dequeue();

				if (changeLog2 == null)
					return;

				if (this.currentReviewingToolName != NGChangeLogWindow.pendingTools.Peek().Key)
				{
					this.currentReviewingToolName = NGChangeLogWindow.pendingTools.Peek().Key;
					this.currentChangeLog = NGChangeLogWindow.pendingTools.Peek().Value;
				}

				if (this.currentChangeLog >= 0)
				{
					ChangeLog	changeLog = changeLog2[this.currentChangeLog];

					r.height = 32F;
					GUI.Label(r, "[UPDATE] " + changeLog.name, GeneralStyles.MainTitle);
					r.y += r.height - 1F;

					r.height = 1F;
					EditorGUI.DrawRect(r, Color.gray);
					r.y += r.height;

					if (this.currentChangeLog < changeLog2.Count)
					{
						r.height = 24F;
						GUI.Label(r, changeLog.date + " - " + changeLog.version, GeneralStyles.Title1);
						r.y += r.height;

						r.height = this.position.height - r.y - NGChangeLogWindow.NextButtonHeight;
						Rect		bodyRect = r;
						Utility.content.text = changeLog.content;
						float		height = this.contentStyle.CalcHeight(Utility.content, bodyRect.width);
						Rect		viewRect = new Rect(0F, 0F, 0F, height);

						if (height > bodyRect.height)
							viewRect.height = this.contentStyle.CalcHeight(Utility.content, bodyRect.width - 16F);

						this.scrollPosition = GUI.BeginScrollView(bodyRect, this.scrollPosition, viewRect);
						{
							bodyRect.x = 0F;
							bodyRect.y = 0F;
							if (height > bodyRect.height)
								bodyRect.width -= 16F;
							bodyRect.height = viewRect.height;

							GUI.Label(bodyRect, changeLog.content, this.contentStyle);
						}
						GUI.EndScrollView();

						r.y += r.height;
					}

					r.height = NGChangeLogWindow.NextButtonHeight;
					if (this.currentChangeLog + 1 >= changeLog2.Count)
					{
						string	button = "Close";

						if (NGChangeLogWindow.pendingTools.Count > 1)
						{
							r.xMin = r.xMax - NGChangeLogWindow.NextButtonWidth - 50F;
							button = "Next tool";
						}
						else
							r.xMin = r.xMax - NGChangeLogWindow.NextButtonWidth;

						if (GUI.Button(r, button) == true)
						{
							NGChangeLogWindow.pendingTools.Dequeue();

							this.WriteLatestToolVersion(changeLog2[this.currentChangeLog].name,
														changeLog2[this.currentChangeLog].version);

							if (NGChangeLogWindow.pendingTools.Count == 0)
								this.Close();
						}
					}
					else
					{
						r.xMin = r.xMax - NGChangeLogWindow.NextButtonWidth;
						if (GUI.Button(r, changeLog2.Count - this.currentChangeLog == 1 ? "Next" : "Next (" + (changeLog2.Count - this.currentChangeLog - 1) + ")") == true)
						{
							this.WriteLatestToolVersion(changeLog2[this.currentChangeLog].name,
														changeLog2[this.currentChangeLog].version);
							++this.currentChangeLog;
						}
					}

					r.x = 10F;
					r.y += 3F;
					r.width = 150F;
					this.notifyNewUpdate = GUI.Toggle(r, this.notifyNewUpdate, "Notify New Update");
				}
			}
			else
			{
				List<ChangeLog>	changeLog2 = null;

				if (this.currentToolName != null && NGChangeLogWindow.changeLogs.TryGetValue(this.currentToolName, out changeLog2) == false)
				{
					this.currentToolName = null;
				}

				if (changeLog2 == null)
					return;

				r.height = 32F;
				GUI.Label(r, this.currentToolName, GeneralStyles.MainTitle);
				r.y += r.height - 1F;

				r.height = 1F;
				EditorGUI.DrawRect(r, Color.gray);
				r.y += r.height;

				Rect	bodyRect = r;
				float	totalHeight = changeLog2.Count * (24F + 15F) - 15F + 7F;

				for (int i = 0; i < changeLog2.Count; i++)
				{
					Utility.content.text = changeLog2[i].content;
					totalHeight += this.contentStyle.CalcHeight(Utility.content, bodyRect.width - 16F - 5F - 2F);
				}

				Rect	viewRect = new Rect(0F, 0F, 0F, totalHeight);

				bodyRect.height = this.position.height - r.y - NGChangeLogWindow.NextButtonHeight;

				this.scrollPosition = GUI.BeginScrollView(bodyRect, this.scrollPosition, viewRect);
				{
					r.x = 5F;
					r.y = 0F;
					r.width = bodyRect.width - 16F - 5F - 2F;

					for (int i = changeLog2.Count - 1; i >= 0; i--)
					{
						r.height = 24F;
						GUI.Label(r, changeLog2[i].date + " - " + changeLog2[i].version, GeneralStyles.Title1);
						r.y += r.height;

						Utility.content.text = changeLog2[i].content;
						r.height = this.contentStyle.CalcHeight(Utility.content, bodyRect.width - 16F - 5F - 2F);
						GUI.Label(r, Utility.content, this.contentStyle);
						r.y += r.height + 15F;

						r.x += 30F;
						r.width -= 60F;
						r.height = 1F;
						r.y -= 7F;
						EditorGUI.DrawRect(r, Color.gray);
						r.y += 7F;
						r.x -= 30F;
						r.width += 60F;
					}
				}
				GUI.EndScrollView();

				bodyRect.x = 10F;
				bodyRect.y += bodyRect.height + 3F;
				bodyRect.width = 150F;
				this.notifyNewUpdate = GUI.Toggle(bodyRect, this.notifyNewUpdate, "Notify New Update");
			}
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true)
				this.Close();
		}

		public static void	AddChangeLog(string toolName, string version, string date, string content)
		{
			List<ChangeLog>	changeLog;

			if (NGChangeLogWindow.changeLogs.TryGetValue(toolName, out changeLog) == false)
			{
				changeLog = new List<ChangeLog>();
				NGChangeLogWindow.changeLogs.Add(toolName, changeLog);
			}

			changeLog.Add(new ChangeLog() { name = toolName, version = version, date = date, content = content });
		}

		public static void	CheckLatestVersion(string toolName)
		{
			if (NGEditorPrefs.GetBool(NGChangeLogWindow.DontNotifyNewUpdateKeyPref) == true)
				return;

			try
			{
				// Prevent repeating initialization.
				foreach (KeyValuePair<string, int> item in NGChangeLogWindow.pendingTools)
				{
					if (item.Key == toolName)
						return;
				}

				List<ChangeLog>	changeLog;

				if (NGChangeLogWindow.changeLogs.TryGetValue(toolName, out changeLog) == true)
				{
					changeLog.Sort((a, b) => a.version.CompareTo(b.version));

					string	local = NGChangeLogWindow.GetToolPath(toolName);
					if (File.Exists(local) == true)
					{
						string	version = File.ReadAllText(local);

						for (int i = 0; i < changeLog.Count; i++)
						{
							if (version.CompareTo(changeLog[i].version) < 0)
							{
								NGChangeLogWindow.pendingTools.Enqueue(new KeyValuePair<string, int>(toolName, i));
								NGChangeLogWindow.Open();
								break;
							}
						}
					}
					else
						File.WriteAllText(local, changeLog[changeLog.Count - 1].version);
				}
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}
		}

		public static bool	HasChangeLog(string toolName)
		{
			return NGChangeLogWindow.changeLogs.ContainsKey(toolName);
		}

		private static string	GetToolPath(string toolName)
		{
			string	local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

			local = Path.Combine(local, Constants.InternalPackageTitle);
			return Path.Combine(local, toolName + " Version.txt");
		}

		private void	WriteLatestToolVersion(string toolName, string version)
		{
			try
			{
				string	path = NGChangeLogWindow.GetToolPath(toolName);
				File.WriteAllText(path, version);
			}
			catch (Exception ex)
			{
				InternalNGDebug.LogException(ex);
			}
		}
	}
}