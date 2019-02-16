using NGLicenses;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public class ContactFormWizard : ScriptableWizard
	{
		public enum Subject
		{
			Contact,
			BugReport,
			Support,
			Feedback,
			Translation,
			Other,
		}

		public Subject	subject = Subject.Contact;
		[DefaultValueEditorPref("Anonymous")]
		public string	contactName;
		public string	contactEMail = string.Empty;
		[DefaultValueEditorPref(true)]
		public bool		packageInformation;
		[DefaultValueEditorPref(true)]
		public bool		unityInformation;
		[DefaultValueEditorPref(true)]
		public bool		osInformation;
		[DefaultValueEditorPref(true)]
		public bool		hardwareInformation;
		public string	complementaryInformation = string.Empty;
		public string	specificTools;

		[NonSerialized]
		private Vector2		scrollPosition;
		[NonSerialized]
		private string		requestError;
		[NonSerialized]
		private bool		requesting;

		public static void	Open(Subject subject, string tool, string complementaryInformation)
		{
			ScriptableWizard.GetWindow<ContactFormWizard>().Close();
			ContactFormWizard	wizard = ScriptableWizard.DisplayWizard<ContactFormWizard>(Constants.PackageTitle);
			wizard.subject = subject;
			wizard.complementaryInformation = complementaryInformation;
			wizard.specificTools = tool;
		}

		public static void	Open(Subject subject, string complementaryInformation)
		{
			ContactFormWizard.Open(subject, string.Empty, complementaryInformation);
		}

		public static void	Open(Subject subject)
		{
			ContactFormWizard.Open(subject, string.Empty, string.Empty);
		}

		protected virtual void	OnEnable()
		{
			Utility.LoadEditorPref(this);

			if (string.IsNullOrEmpty(this.contactName) == true ||
				string.IsNullOrEmpty(this.contactEMail) == true)
			{
				try
				{
					Type			UnityConnect = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.Connect.UnityConnect");
					FieldInfo		s_Instance = UnityAssemblyVerifier.TryGetField(UnityConnect, "s_Instance", BindingFlags.Static | BindingFlags.NonPublic);
					PropertyInfo	userInfo = UnityAssemblyVerifier.TryGetProperty(UnityConnect, "userInfo", BindingFlags.Instance | BindingFlags.Public);
					//PropertyInfo	projectInfo = UnityAssemblyVerifier.TryGetProperty(UnityConnect, "projectInfo", BindingFlags.Instance | BindingFlags.Public);
					//PropertyInfo	connectInfo = UnityAssemblyVerifier.TryGetProperty(UnityConnect, "connectInfo", BindingFlags.Instance | BindingFlags.Public);

					object		UnityConnectInstance = s_Instance.GetValue(null);
					object		ui = userInfo.GetValue(UnityConnectInstance, null);
					Type		UserInfo = UnityAssemblyVerifier.TryGetType(typeof(Editor).Assembly, "UnityEditor.Connect.UserInfo");

					//NGDebug.Snapshot(UnityConnectInstance);
					//NGDebug.Snapshot(ui);
					//NGDebug.Snapshot(projectInfo.GetValue(UnityConnectInstance, null));
					//NGDebug.Snapshot(connectInfo.GetValue(UnityConnectInstance, null));

					if (string.IsNullOrEmpty(this.contactName) == true)
					{
						FieldInfo	displayName = UnityAssemblyVerifier.TryGetField(UserInfo, "m_DisplayName", BindingFlags.Instance | BindingFlags.NonPublic);
						this.contactName = displayName.GetValue(ui) as string;
					}

					if (string.IsNullOrEmpty(this.contactEMail) == true)
					{
						FieldInfo	m_UserName = UnityAssemblyVerifier.TryGetField(UserInfo, "m_UserName", BindingFlags.Instance | BindingFlags.NonPublic);
						this.contactEMail = m_UserName.GetValue(ui) as string;
					}
				}
				catch
				{
				}
			}
		}

		protected virtual void	OnDestroy()
		{
			Utility.SaveEditorPref(this);
		}

		protected virtual void	OnGUI()
		{
			if (string.IsNullOrEmpty(this.requestError) == false)
				EditorGUILayout.HelpBox(this.requestError, MessageType.Error);

			GUILayout.Label(LC.G("ContactFormWizard_Title"), GeneralStyles.MainTitle);

			this.contactName = EditorGUILayout.TextField(LC.G("ContactFormWizard_ContactName"), this.contactName);
			if (string.IsNullOrEmpty(this.contactName) == true)
				EditorGUILayout.HelpBox(LC.G("ContactFormWizard_NameRequired"), MessageType.Warning);

			this.contactEMail = EditorGUILayout.TextField(LC.G("ContactFormWizard_ContactEMail"), this.contactEMail);
			if (Regex.IsMatch(this.contactEMail, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase) == false)
				EditorGUILayout.HelpBox(LC.G("ContactFormWizard_ValidEMailRequired"), MessageType.Warning);

			this.subject = (Subject)EditorGUILayout.EnumPopup("Subject", this.subject);
			this.specificTools = EditorGUILayout.TextField("Specific Tools", this.specificTools);

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("Add information about", GUILayoutOptionPool.Width(145F));

				this.packageInformation = GUILayout.Toggle(this.packageInformation, "Package", "ButtonLeft");
				Rect	r2 = GUILayoutUtility.GetLastRect();
				r2.x += 2F;
				r2.y += 1F;
				GUI.Toggle(r2, this.packageInformation, string.Empty);

				this.unityInformation = GUILayout.Toggle(this.unityInformation, LC.G("ContactFormWizard_UnityInformation"), "ButtonMid");
				r2 = GUILayoutUtility.GetLastRect();
				r2.x += 2F;
				r2.y += 1F;
				GUI.Toggle(r2, this.unityInformation, string.Empty);

				this.osInformation = GUILayout.Toggle(this.osInformation, LC.G("ContactFormWizard_OSInformation"), "ButtonMid");
				r2 = GUILayoutUtility.GetLastRect();
				r2.x += 2F;
				r2.y += 1F;
				GUI.Toggle(r2, this.osInformation, string.Empty);

				this.hardwareInformation = GUILayout.Toggle(this.hardwareInformation, LC.G("ContactFormWizard_HardwareInformation"), "ButtonRight");
				r2 = GUILayoutUtility.GetLastRect();
				r2.x += 2F;
				r2.y += 1F;
				GUI.Toggle(r2, this.hardwareInformation, string.Empty);
			}
			EditorGUILayout.EndHorizontal();

			Rect r = this.position;
			r.x = 0F;
			r.y = 20F;
			r.height -= 20F;

			Utility.content.text = this.complementaryInformation;

			EditorGUILayout.LabelField(LC.G("ContactFormWizard_ComplementaryInformation"));
			this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);
			{
				this.complementaryInformation = EditorGUILayout.TextArea(this.complementaryInformation, GUI.skin.textArea, GUILayoutOptionPool.ExpandWidthTrue, GUILayoutOptionPool.ExpandHeightTrue);
			}
			GUILayout.EndScrollView();

			if (this.subject == Subject.BugReport)
			{
				if (this.packageInformation == false ||
					this.unityInformation == false ||
					this.osInformation == false ||
					this.hardwareInformation == false)
				{
					EditorGUILayout.HelpBox(LC.G("ContactFormWizard_BugReportRecommendation"), MessageType.Info);
				}
			}

			EditorGUILayout.HelpBox(LC.G("ContactFormWizard_SupportLanguagesWarning"), MessageType.Info);

			EditorGUILayout.BeginHorizontal();
			{
				if (GUILayout.Button("Preview", GUILayoutOptionPool.Width(this.position.width * .45F)) == true)
				{
					string	tempFilePath = Path.Combine(Application.temporaryCachePath, Path.GetRandomFileName() + ".txt");
					File.WriteAllText(tempFilePath, this.PrepareTheEmail());

					EditorUtility.OpenWithDefaultApp(tempFilePath);
				}

				GUILayout.FlexibleSpace();

				using (BgColorContentRestorer.Get(GeneralStyles.HighlightActionButton))
				{
					if (this.requesting == true)
					{
						GUILayout.Button("Requesting...", GUILayoutOptionPool.Width(this.position.width * .5F));
						GUI.Label(GUILayoutUtility.GetLastRect(), GeneralStyles.StatusWheel);

						this.Repaint();
					}
					else
					{
						EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(this.contactName) || string.IsNullOrEmpty(this.contactEMail));
						{
							if (GUILayout.Button("Send", GUILayoutOptionPool.Width(this.position.width * .45F)) == true)
								this.SendEmail();
						}
						EditorGUI.EndDisabledGroup();
					}
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		private void	SendEmail()
		{
			try
			{
				this.requestError = null;
				this.requesting = true;

				StringBuilder	buffer = Utility.GetBuffer("https://ngtools.tech/send_contact.php?dn=");

				buffer.Append(SystemInfo.deviceName);
				buffer.Append("&un=");
				buffer.Append(Environment.UserName);

				HttpWebRequest	request = (HttpWebRequest)WebRequest.Create(Utility.ReturnBuffer(buffer));

				StringBuilder	post = Utility.GetBuffer();

				post.Append("subject=[");
				post.Append(Constants.PackageTitle);
				post.Append("] ");
				post.Append(Utility.NicifyVariableName(Enum.GetName(typeof(Subject), this.subject)));
				post.Append(" from " + this.contactName);
				post.Append("&content=");
				post.Append(this.PrepareTheEmail());

				byte[]	data = Encoding.UTF8.GetBytes(Utility.ReturnBuffer(post));

				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = data.Length;

				request.BeginGetRequestStream(a =>
				{
					using (Stream stream = request.EndGetRequestStream(a))
					{
						stream.Write(data, 0, data.Length);
					}
				}, null);

				request.BeginGetResponse(r =>
				{
					try
					{
						// Pipes the stream to a higher level stream reader with the required encoding format.
						using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(r))
						using (StreamReader readStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
						{
							string	result = readStream.ReadToEnd();

							if (result == "0")
								EditorApplication.delayCall += () => EditorUtility.DisplayDialog(Constants.PackageTitle, "Request sent successfully.", "OK");
							else
								this.requestError = "An error occurred with the request. Please contact the author directly at " + Constants.SupportEmail + ".\nResponse: " + result;
						}
					}
					catch (Exception ex)
					{
						this.requestError = ex.Message;
					}
					finally
					{
						this.requesting = false;
					}
				}, null);
			}
			catch (Exception ex)
			{
				this.requestError = ex.Message;
				this.requesting = false;
			}
		}

		private string	PrepareTheEmail()
		{
			StringBuilder	body = Utility.GetBuffer();

			body.AppendLine("Name: " + this.contactName);
			body.AppendLine("E-mail: " + this.contactEMail);
			body.AppendLine("Subject: " + Utility.NicifyVariableName(Enum.GetName(typeof(Subject), this.subject)));
			body.AppendLine("Specific tools: " + this.specificTools);
			body.AppendLine("Package: " + Constants.PackageTitle);
			body.AppendLine("NG Tools Version: " + Constants.Version);
			body.AppendLine("Unity Version: " + Utility.UnityVersion);

			if (string.IsNullOrEmpty(this.complementaryInformation) == false)
			{
				body.AppendLine();
				body.AppendLine(this.complementaryInformation);
			}

			if (this.packageInformation == true)
			{
				body.AppendLine();
				body.AppendLine("Packages:");

				foreach (HQ.ToolAssemblyInfo tool in HQ.EachTool)
					body.AppendLine("[" + tool.version + "] " + (NGLicensesManager.IsPro(tool.name + " Pro") == true ? "[PRO] " : string.Empty) + tool.name);

				body.AppendLine();
				body.AppendLine("Invoices:");
				foreach (License l in NGLicensesManager.EachInvoices())
				{
					if (l.active == true)
						body.AppendLine(l.assetName + " (" + l.invoice + ")");
				}
			}

			if (this.unityInformation == true)
			{
				body.AppendLine();
				body.AppendLine("Active Build Target: " + EditorUserBuildSettings.activeBuildTarget);
				body.AppendLine("Platform: " + Application.platform);
				body.AppendLine("Run In Background: " + Application.runInBackground);
				body.AppendLine("System Language: " + Application.systemLanguage);
				body.AppendLine("Application Version: " + Application.version);
			}

			if (this.osInformation == true)
			{
				body.AppendLine();
				body.AppendLine("Operating System: " + SystemInfo.operatingSystem);
			}

			if (this.hardwareInformation == true)
			{
				body.AppendLine();
				body.AppendLine("Processor Count: " + SystemInfo.processorCount);
				body.AppendLine("Processor Type: " + SystemInfo.processorType);
				body.AppendLine("Device Model: " + SystemInfo.deviceModel);
				body.AppendLine("Device Name: " + SystemInfo.deviceName);
				body.AppendLine("Device Type: " + SystemInfo.deviceType);
				body.AppendLine("Graphics Device Name: " + SystemInfo.graphicsDeviceName);
				body.AppendLine("Graphics Device Vendor: " + SystemInfo.graphicsDeviceVendor);
				body.AppendLine("Graphics Device VendorID: " + SystemInfo.graphicsDeviceVendorID);
				body.AppendLine("Graphics Device Version: " + SystemInfo.graphicsDeviceVersion);
				body.AppendLine("Graphics Memory Size: " + SystemInfo.graphicsMemorySize);
				body.AppendLine("Graphics Multi Threaded: " + SystemInfo.graphicsMultiThreaded);
				body.AppendLine("Graphics Shader Level: " + SystemInfo.graphicsShaderLevel);
				body.AppendLine("System Memory Size: " + SystemInfo.systemMemorySize);
			}

			body.Length -= Environment.NewLine.Length;

			return Utility.ReturnBuffer(body);
			//Application.OpenURL("mailto:" + Constants.SupportEmail + "?subject=[" + Constants.PackageTitle + "]%20" + Utility.NicifyVariableName(Enum.GetName(typeof(Subject), this.subject)) + "%20from%20" + this.contactName + "&body=" + Uri.EscapeUriString(Utility.ReturnBuffer(body)));
		}
	}
}