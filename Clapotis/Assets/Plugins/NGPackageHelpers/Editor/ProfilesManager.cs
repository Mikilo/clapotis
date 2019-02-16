using NGTools.UON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace NGPackageHelpers
{
	[InitializeOnLoad]
	public class ProfilesManager
	{
		public const string	ProfilesPrefKey = "NGPH_Profiles";
		public const string	CurrentProfilePrefKey = "NGPH_Current";

		public static Action	SetProfile;

		public static bool		IsReady { get { return ProfilesManager.profiles != null;  } }
		public static Profile	Profile { get { return ProfilesManager.profiles[ProfilesManager.current]; } }

		public static List<Profile>	Profiles { get { return ProfilesManager.profiles; } }
		public static int			Current { get { return ProfilesManager.current; } set { ProfilesManager.current = value; } }

		private static List<Profile>	profiles;
		private static int				current;

		static	ProfilesManager()
		{
			EditorApplication.delayCall += () =>
			{
				try
				{
					ProfilesManager.profiles = Utility.DeserializeField<List<Profile>>(Convert.FromBase64String(EditorPrefs.GetString(ProfilesManager.ProfilesPrefKey)));
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
					ProfilesManager.profiles = new List<Profile>();
				}

				if (ProfilesManager.profiles.Count == 0)
					ProfilesManager.profiles.Add(new Profile() { name = "Profile 1" });

				ProfilesManager.current = Mathf.Clamp(EditorPrefs.GetInt(ProfilesManager.CurrentProfilePrefKey), 0, ProfilesManager.profiles.Count - 1);

				ProfilesManager.Profile.outputPath = ProfilesManager.Profile.outputPath ?? string.Empty;
				ProfilesManager.Profile.outputEditorPath = ProfilesManager.Profile.outputEditorPath ?? string.Empty;

				if (ProfilesManager.SetProfile != null)
					ProfilesManager.SetProfile();

				InternalEditorUtility.RepaintAllViews();
			};
		}

		public static void	OnProfilesBarGUI()
		{
			if (Event.current.type == EventType.KeyDown &&
				Event.current.modifiers == EventModifiers.Control)
			{
				if (Event.current.keyCode == KeyCode.Q && ProfilesManager.current > 0)
				{
					GUI.FocusControl(null);
					ProfilesManager.SwitchFavorite(ProfilesManager.current - 1);
				}
				else if (Event.current.keyCode == KeyCode.E && ProfilesManager.current < ProfilesManager.profiles.Count - 1)
				{
					GUI.FocusControl(null);
					ProfilesManager.SwitchFavorite(ProfilesManager.current + 1);
				}

				Event.current.Use();
			}

			EditorGUILayout.BeginHorizontal("Toolbar");
			{
				if (ProfilesManager.profiles == null)
				{
					GUILayout.Label("Profiles not loaded yet.");
				}
				else
				{
					if (GUILayout.Button("", "ToolbarDropDown", GUILayout.Width(20F)) == true)
					{
						Rect	r = GUILayoutUtility.GetLastRect();
						r.y += 16F;
						new ProfilesPopup().Open(r);
					}

					EditorGUILayout.BeginHorizontal();
					{
						EditorGUI.BeginDisabledGroup(ProfilesManager.current <= 0);
						if (GUILayout.Button("<", "ToolbarButton", GUILayout.Width(30F)) == true)
							ProfilesManager.SwitchFavorite(ProfilesManager.current - 1);
						EditorGUI.EndDisabledGroup();

						EditorGUI.BeginDisabledGroup(ProfilesManager.current >= ProfilesManager.profiles.Count - 1);
						if (GUILayout.Button(">", "ToolbarButton", GUILayout.Width(30F)) == true)
							ProfilesManager.SwitchFavorite(ProfilesManager.current + 1);
						EditorGUI.EndDisabledGroup();

						EditorGUI.BeginChangeCheck();
						ProfilesManager.Profile.name = EditorGUILayout.TextField(ProfilesManager.Profile.name, new GUIStyle("ToolbarTextField"), GUILayout.ExpandWidth(true));
						if (EditorGUI.EndChangeCheck() == true)
							ProfilesManager.Save();
					}
					EditorGUILayout.EndHorizontal();

					EditorGUI.BeginDisabledGroup(ProfilesManager.profiles.Count <= 1);
					if (GUILayout.Button("X", "ToolbarButton") == true && EditorUtility.DisplayDialog("Profiles", "Confirm deletion of " + ProfilesManager.Profile.name, "Yes", "No") == true)
					{
						ProfilesManager.profiles.RemoveAt(ProfilesManager.current);
						ProfilesManager.current = Mathf.Clamp(ProfilesManager.current, 0, ProfilesManager.profiles.Count - 1);
						ProfilesManager.Save();
					}
					EditorGUI.EndDisabledGroup();
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		public static Profile	GetProfile(string name)
		{
			for (int i = 0; i < ProfilesManager.profiles.Count; i++)
			{
				if (ProfilesManager.profiles[i].name == name)
					return ProfilesManager.profiles[i];
			}

			return null;
		}

		public static void	Save()
		{
			EditorPrefs.SetInt(ProfilesManager.CurrentProfilePrefKey, ProfilesManager.current);
			EditorPrefs.SetString(ProfilesManager.ProfilesPrefKey, Convert.ToBase64String(Utility.SerializeField(ProfilesManager.profiles)));

			if (ProfilesManager.SetProfile != null)
				ProfilesManager.SetProfile();
		}

		private static void	SwitchFavorite(int i)
		{
			ProfilesManager.current = Mathf.Clamp(i, 0, ProfilesManager.profiles.Count - 1);
			EditorPrefs.SetInt(ProfilesManager.CurrentProfilePrefKey, ProfilesManager.current);

			if (ProfilesManager.SetProfile != null)
				ProfilesManager.SetProfile();
		}

		private static void	AddFavorite()
		{
			Profile	p = Utility.DeserializeField<Profile>(Utility.SerializeField(ProfilesManager.Profile));
			p.name = "Profile " + (ProfilesManager.profiles.Count + 1);
			ProfilesManager.profiles.Add(p);
			ProfilesManager.Save();
		}

		public sealed class ProfilesPopup : PopupWindowContent
		{
			private Vector2			size;
			private ReorderableList	list;

			public void	Open(Rect position)
			{
				float	height = 21F + ProfilesManager.profiles.Count * 21F;

				this.size = new Vector2(300F, height);

				this.list = new ReorderableList(ProfilesManager.profiles, typeof(Profile), true, false, true, true);
				this.list.drawElementCallback += this.DrawElement;
				this.list.onAddCallback += this.AddElement;
				this.list.onRemoveCallback += this.DeleteElement;
				this.list.onReorderCallback += this.ReorderElement;
				this.list.drawFooterCallback += this.DrawFooter;
				this.list.drawFooterCallback += (r) => new ReorderableList.Defaults().DrawFooter(r, this.list);
				this.list.headerHeight = 0F;
				this.list.footerHeight = 21F;

				PopupWindow.Show(position, this);
			}

			public override Vector2	GetWindowSize()
			{
				return this.size;
			}

			public override void	OnGUI(Rect r)
			{
				this.list.DoList(r);
			}

			private void	DrawElement(Rect r, int index, bool isActive, bool isFocused)
			{
				r.width -= 30F;
				r.x += 30F;
				EditorGUI.BeginChangeCheck();
				ProfilesManager.profiles[index].name = EditorGUI.TextField(r, ProfilesManager.profiles[index].name);
				if (EditorGUI.EndChangeCheck() == true)
					ProfilesManager.Save();
				r.x -= 30;

				r.width = 30F;
				if (GUI.Button(r, ">") == true)
				{
					ProfilesManager.SwitchFavorite(index);

					if (ProfilesManager.SetProfile != null)
						ProfilesManager.SetProfile();

					InternalEditorUtility.RepaintAllViews();
					this.editorWindow.Close();
				}
			}

			private void	AddElement(ReorderableList list)
			{
				ProfilesManager.AddFavorite();

				this.size = new Vector2(300, 21F + ProfilesManager.profiles.Count * 21F);

				this.editorWindow.Close();
				PopupWindow.Show(new Rect(0F, 0F, 1F, 1F), this);
			}

			private void	ReorderElement(ReorderableList list)
			{
				ProfilesManager.Save();
			}

			private void	DeleteElement(ReorderableList list)
			{
				ProfilesManager.profiles.RemoveAt(list.index);
				ProfilesManager.current = Mathf.Clamp(ProfilesManager.current, 0, ProfilesManager.profiles.Count - 1);
				ProfilesManager.Save();

				this.size = new Vector2(300, 21F + ProfilesManager.profiles.Count * 21F);

				this.editorWindow.Close();
				PopupWindow.Show(new Rect(0F, 0F, 1F, 1F), this);
			}

			private void	DrawFooter(Rect r)
			{
				r.y -= 3F;
				r.width = (r.width - 56F) / 2F;
				r.height -= 5F;
				if (GUI.Button(r, "Export") == true)
				{
					string	path = EditorUtility.SaveFilePanel(Constants.PackageTitle, "", "profiles", "uon");

					if (string.IsNullOrEmpty(path) == false)
						File.WriteAllText(path, UON.ToUON(ProfilesManager.profiles));
				}
				r.x += r.width;

				if (GUI.Button(r, "Import") == true)
				{
					string	path = EditorUtility.OpenFilePanel(Constants.PackageTitle, "", "uon");

					if (string.IsNullOrEmpty(path) == false)
					{
						try
						{
							string			uon = File.ReadAllText(path);
							List<Profile>	importedProfiles = UON.FromUON(uon) as List<Profile>;
							ProfilesManager.profiles = importedProfiles;
							ProfilesManager.Save();
						}
						catch (Exception ex)
						{
							Debug.LogException(ex);
						}
					}
				}
			}
		}
	}
}