using NGLicenses;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public class AboutWindow : EditorWindow
	{
		private sealed class PreviewVersionsBackupPopup : PopupWindowContent
		{
			private List<HQ.ToolAssemblyInfo> tools;

			public	PreviewVersionsBackupPopup(List<HQ.ToolAssemblyInfo> tools)
			{
				this.tools = tools;
			}

			public override Vector2		GetWindowSize()
			{
				return new Vector2(200F, tools.Count * 18F);
			}

			public override void	OnGUI(Rect r)
			{
				r.height = 18F;

				for (int i = 0; i < this.tools.Count; i++)
				{
					if (GUI.Button(r, "For " + this.tools[i].name, GeneralStyles.LeftButton) == true)
						Application.OpenURL(this.tools[i].assetStoreBuyLink);

					r.y += r.height;
				}
			}
		}

		public const string	Title = "About";

		[MenuItem(Constants.MenuItemPath + AboutWindow.Title, priority = Constants.MenuItemPriority - 20), Hotkey(AboutWindow.Title)]
		public static void	Open()
		{
			EditorWindow.GetWindow<AboutWindow>(true, "About");
		}

		protected virtual void	OnEnable()
		{
			this.minSize = new Vector2(310F, 280F);
			this.maxSize = this.minSize;
		}

		protected virtual void	OnGUI()
		{
			GUILayout.Label(Constants.PackageTitle, GeneralStyles.MainTitle);

			GUILayout.Label("Version " + Constants.Version);

			GUILayout.FlexibleSpace();

			Rect	r = GUILayoutUtility.GetRect(this.position.width, 150F);

			r.width = r.width * .5F - 5F;
			r.height = r.height * .33F - 5F;
			r.x += 3.33F;
			r.y += 5F;

			if (GUI.Button(r, Preferences.DiscordContent, GeneralStyles.BigButton) == true)
				Application.OpenURL(Constants.DiscordURL);

			r.y += r.height + 5F;

			if (GUI.Button(r, Preferences.TwitterContent, GeneralStyles.BigButton) == true)
				Application.OpenURL(Constants.TwitterURL);

			r.y += r.height + 5F;

			if (GUI.Button(r, Preferences.UnityForumContent, GeneralStyles.BigButton) == true)
				Application.OpenURL(Constants.SupportForumUnityThread);

			r.x += r.width + 3.33F;

			if (GUI.Button(r, LC.G("Preferences_Contact"), GeneralStyles.BigButton) == true)
				ContactFormWizard.Open(ContactFormWizard.Subject.Contact);

			r.y -= r.height + 5F;

			if (GUI.Button(r, "Bit Bucket", GeneralStyles.BigButton) == true)
				Application.OpenURL(Constants.TicketURL);

			r.y -= r.height + 5F;

			if (GUI.Button(r, "Documentation", GeneralStyles.BigButton) == true)
				Application.OpenURL(Constants.WikiBaseURL);

			r.height = 20F;
			r.y -= r.height + 5F;

			r.xMin -= 50F;
			if (GUI.Button(r, "Help with a review!") == true)
			{
				List<HQ.ToolAssemblyInfo>	licensedTools = new List<HQ.ToolAssemblyInfo>();

				foreach (HQ.ToolAssemblyInfo tool in HQ.EachTool)
				{
					if (NGLicensesManager.IsLicenseValid(tool.name) == true)
						licensedTools.Add(tool);
				}

				if (licensedTools.Count == 0)
				{
					if (NGLicensesManager.IsPro() == true)
						Application.OpenURL(Constants.AssetStoreNGToolsProURL);
					else
						Application.OpenURL(Constants.AssetStoreNGToolsFreeURL);
				}
				else
				{
					licensedTools.Insert(0, new HQ.ToolAssemblyInfo() { name = "NG Tools Free", assetStoreBuyLink = Constants.AssetStoreNGToolsFreeURL });
					PopupWindow.Show(r, new PreviewVersionsBackupPopup(licensedTools));
				}
			}

			r.y -= r.height + 5F;

			r.xMin = 220F;
			if (GUI.Button(r, "Change Log") == true)
				Application.OpenURL(Constants.ChangeLogURL);
		}
	}
}