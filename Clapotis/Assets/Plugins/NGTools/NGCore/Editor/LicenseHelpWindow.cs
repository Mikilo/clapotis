using NGLicenses;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public class LicenseHelpWindow : EditorWindow
	{
		public const string	Title = "Troubleshooting License";
		public static readonly Vector2	Size = new Vector2(550F, 657F);

		[NonSerialized]
		private EditorWindow	preferencesWindow;

		public static void	Toggle()
		{
			LicenseHelpWindow[]	instances = Resources.FindObjectsOfTypeAll<LicenseHelpWindow>();

			if (instances.Length > 0)
			{
				for (int i = 0; i < instances.Length; i++)
					instances[i].Close();
			}
			else
				EditorWindow.GetWindow<LicenseHelpWindow>(true, LicenseHelpWindow.Title).OnEnable();
		}

		protected virtual void	OnEnable()
		{
			Utility.ShowPreferencesWindowAt(Constants.PreferenceTitle, "Licenses");

			if (Utility.settingsWindowType != null)
			{
				this.preferencesWindow = EditorWindow.GetWindow(Utility.settingsWindowType, true, "Unity Preferences");
				this.position = new Rect(this.preferencesWindow.position.xMax, this.preferencesWindow.position.y, LicenseHelpWindow.Size.x, LicenseHelpWindow.Size.y);
			}

			this.minSize = LicenseHelpWindow.Size;
			this.maxSize = LicenseHelpWindow.Size;
		}

		protected virtual void	OnGUI()
		{
			GUILayout.Label(@"In order to get full power from NG Tools, you need to activate licenses to remove limitations.

An ""NG Tools Pro"" license will remove all limitations.

Any other license will grant full power to only the tool that it is related.");

			GUILayout.Space(15F);

			GUILayout.Label("1. Get the invoice.", GeneralStyles.Title1);

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("Go to your Order History in the Asset Store.");

				if (GUILayout.Button("Go to Asset Store orders") == true)
					Help.BrowseURL("https://assetstore.unity.com/orders");

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Label("Top-right corner picture in the Asset Store, then \"<b>My Orders</b>\".", GeneralStyles.RichLabel);

			GUILayout.Space(10F);

			GUILayout.Label("Find your purchase line, then look for column \"<b>Invoice Number</b>\".", GeneralStyles.RichLabel);
			GUILayout.Label(@"It might be empty, Unity can require time to generate it.
If it is not appearing, please contact them directly.");

			GUILayout.Space(10F);

			GUILayout.Label("If you urgently need the license to work, contact me through the following medium:");

			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button(Preferences.DiscordContent, GeneralStyles.BigButton, GUILayoutOptionPool.Height(40F)) == true)
					Application.OpenURL(Constants.DiscordURL);

				if (GUILayout.Button(Preferences.UnityForumContent, GeneralStyles.BigButton, GUILayoutOptionPool.Height(40F)) == true)
					Application.OpenURL(Constants.SupportForumUnityThread);

				if (GUILayout.Button(LC.G("Preferences_Contact"), GeneralStyles.BigButton, GUILayoutOptionPool.Height(40F)) == true)
					ContactFormWizard.Open(ContactFormWizard.Subject.Contact);
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10F);

			GUILayout.Label("2. Check invoice.", GeneralStyles.Title1);

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("Go back to Preferences > NG Tools > Licenses.");

				if (GUILayout.Button("Go") == true)
				{
					Utility.ShowPreferencesWindowAt(Constants.PackageTitle, "Licenses");
					Preferences.tab = Preferences.Tab.Licenses;
				}

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("Write your invoice in the input at the bottom and add it.");

				if (GUILayout.Button("Highlight") == true)
				{
					Utility.ShowPreferencesWindowAt(Constants.PackageTitle, "Licenses");
					Preferences.tab = Preferences.Tab.Licenses;
					XGUIHighlightManager.Highlight(Preferences.Title + ".AddInvoice");
				}

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label(@"Then you need to check it, to ensure NG Tools recognizes it as a valid license.");

				if (GUILayout.Button("Highlight") == true)
				{
					Utility.ShowPreferencesWindowAt(Constants.PackageTitle, "Licenses");
					Preferences.tab = Preferences.Tab.Licenses;

					bool	hasUncheckedLicense = false;

					foreach (License i in NGLicenses.NGLicensesManager.EachInvoices())
					{
						if (i.status == Status.Unknown)
						{
							hasUncheckedLicense = true;
							break;
						}
					}

					if (hasUncheckedLicense == true)
						XGUIHighlightManager.Highlight(Preferences.Title + ".CheckLicense");
					else
						EditorUtility.DisplayDialog(Constants.PackageTitle, "You must first add invoice prior to check it.", "OK");
				}

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10F);

			GUILayout.Label("3. Activate license.", GeneralStyles.Title1);

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label(@"If the license is valid, its associated asset will be displayed.
Otherwise, it will show ""Invalid"" in grey letters.
Now you can activate it.");

				if (GUILayout.Button("Highlight") == true)
				{
					Utility.ShowPreferencesWindowAt(Constants.PackageTitle, "Licenses");
					Preferences.tab = Preferences.Tab.Licenses;

					bool	hasValidLicense = false;

					foreach (License i in NGLicenses.NGLicensesManager.EachInvoices())
					{
						if (i.status == Status.Valid)
						{
							hasValidLicense = true;
							break;
						}
					}

					if (hasValidLicense == true)
						XGUIHighlightManager.Highlight(Preferences.Title + ".ActivateLicense");
					else
						EditorUtility.DisplayDialog(Constants.PackageTitle, "You must first check an invoice prior to validate it.", "OK");
				}

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(10F);

			GUILayout.Label("4. Manage seats.", GeneralStyles.Title1);

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label(@"You can verifiy seats associated with your licenses.
And revoke them as it fits you.");

				if (GUILayout.Button("Highlight") == true)
				{
					Utility.ShowPreferencesWindowAt(Constants.PackageTitle, "Licenses");
					Preferences.tab = Preferences.Tab.Licenses;

					bool	hasValidLicense = false;

					foreach (License i in NGLicenses.NGLicensesManager.EachInvoices())
					{
						if (i.status == Status.Valid)
						{
							hasValidLicense = true;
							break;
						}
					}

					if (hasValidLicense == true)
						XGUIHighlightManager.Highlight(Preferences.Title + ".CheckSeats");
					else
						EditorUtility.DisplayDialog(Constants.PackageTitle, "You must first check an invoice prior to manage its seats.", "OK");
				}

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label(@"If you are encountering issues with seats, please contact me directly.

If you require a license without the 2 seats limitation, please contact me directly.");

				if (GUILayout.Button(LC.G("Preferences_Contact"), GUILayoutOptionPool.ExpandHeightTrue) == true)
					ContactFormWizard.Open(ContactFormWizard.Subject.Contact);

				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5F);

			EditorGUILayout.HelpBox("A license is an invoice.", MessageType.Info);
			EditorGUILayout.HelpBox("You can activate a license on 2 seats maximum! \"2 seats\" means 2 computers, implying many projects on those 2 computers, as much as you want.", MessageType.Info);

			if (this.preferencesWindow != null)
				this.preferencesWindow.Repaint();
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true)
				this.Close();
		}
	}
}