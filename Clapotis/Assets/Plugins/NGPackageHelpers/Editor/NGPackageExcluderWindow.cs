using System.Text;
using UnityEditor;

namespace NGPackageHelpers
{
	using UnityEngine;

	public class NGPackageExcluderWindow : EditorWindow
	{
		public const string		Title = "NG Package Excluder";
		public static char		KeywordSeparator = ';';
		public static string[]	DefaultKeywords = { "/Internal/" };

		[MenuItem(Constants.MenuItemPath + NGPackageExcluderWindow.Title, priority = 101)]
		private static void	Open()
		{
			EditorWindow.GetWindow<NGPackageExcluderWindow>(true, NGPackageExcluderWindow.Title, true);
		}

		protected virtual void	OnGUI()
		{
			ProfilesManager.OnProfilesBarGUI();
			if (ProfilesManager.IsReady == false)
				return;

			EditorGUILayout.LabelField("Default keywords:");
			EditorGUI.BeginDisabledGroup(true);
			for (int i = 0; i < NGPackageExcluderWindow.DefaultKeywords.Length; i++)
				EditorGUILayout.TextField(NGPackageExcluderWindow.DefaultKeywords[i]);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.LabelField("Excluded keywords:");
			for (int i = 0; i < ProfilesManager.Profile.excludeKeywords.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				{
					ProfilesManager.Profile.excludeKeywords[i] = EditorGUILayout.TextField(ProfilesManager.Profile.excludeKeywords[i]);
					if (GUILayout.Button("X", GUILayout.Width(20F)) == true)
					{
						ProfilesManager.Profile.excludeKeywords.RemoveAt(i);
						ProfilesManager.Save();
						break;
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			
			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Add") == true)
				{
					ProfilesManager.Profile.excludeKeywords.Add(string.Empty);
					ProfilesManager.Save();
				}

				if (GUILayout.Button("Save") == true)
				{
					StringBuilder	buffer = Utility.GetBuffer();

					for (int i = 0; i < ProfilesManager.Profile.excludeKeywords.Count; i++)
					{
						if (ProfilesManager.Profile.excludeKeywords[i] != string.Empty)
						{
							buffer.Append(ProfilesManager.Profile.excludeKeywords[i]);
							buffer.Append(NGPackageExcluderWindow.KeywordSeparator);
						}
					}

					if (buffer.Length > 0)
						--buffer.Length;

					ProfilesManager.Save();
				}
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10F);

			EditorGUILayout.LabelField("Include keywords:");
			for (int i = 0; i < ProfilesManager.Profile.includeKeywords.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				{
					ProfilesManager.Profile.includeKeywords[i] = EditorGUILayout.TextField(ProfilesManager.Profile.includeKeywords[i]);
					if (GUILayout.Button("X", GUILayout.Width(20F)) == true)
					{
						ProfilesManager.Profile.includeKeywords.RemoveAt(i);
						ProfilesManager.Save();
						break;
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			
			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Add") == true)
				{
					ProfilesManager.Profile.includeKeywords.Add(string.Empty);
					ProfilesManager.Save();
				}

				if (GUILayout.Button("Save") == true)
				{
					StringBuilder	buffer = Utility.GetBuffer();

					for (int i = 0; i < ProfilesManager.Profile.includeKeywords.Count; i++)
					{
						if (ProfilesManager.Profile.includeKeywords[i] != string.Empty)
						{
							buffer.Append(ProfilesManager.Profile.includeKeywords[i]);
							buffer.Append(NGPackageExcluderWindow.KeywordSeparator);
						}
					}

					if (buffer.Length > 0)
						--buffer.Length;

					ProfilesManager.Save();
				}
			}
			EditorGUILayout.EndHorizontal();
		}
	}
}