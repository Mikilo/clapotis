using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

#if NGTOOLS
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NGCoreEditor")]
[assembly: InternalsVisibleTo("NGAssetFinderEditor")]
[assembly: InternalsVisibleTo("NGComponentReplacerEditor")]
[assembly: InternalsVisibleTo("NGComponentsInspectorEditor")]
[assembly: InternalsVisibleTo("NGConsoleEditor")]
[assembly: InternalsVisibleTo("NGDraggableObjectEditor")]
[assembly: InternalsVisibleTo("NGFavEditor")]
[assembly: InternalsVisibleTo("NGFullscreenBindingsEditor")]
[assembly: InternalsVisibleTo("NGHierarchyEnhancerEditor")]
[assembly: InternalsVisibleTo("NGHubEditor")]
[assembly: InternalsVisibleTo("NGInspectorGadgetEditor")]
[assembly: InternalsVisibleTo("NGNavSelectionEditor")]
[assembly: InternalsVisibleTo("NGPrefsEditor")]
[assembly: InternalsVisibleTo("NGRemoteSceneEditor")]
[assembly: InternalsVisibleTo("NGRenamerEditor")]
[assembly: InternalsVisibleTo("NGScenesEditor")]
[assembly: InternalsVisibleTo("NGShaderFinderEditor")]
[assembly: InternalsVisibleTo("NGSyncFoldersEditor")]
[assembly: InternalsVisibleTo("NGHubEditor")]
[assembly: InternalsVisibleTo("RemoteModule_For_NGConsole")]

[assembly: InternalsVisibleTo("NGToolsEditor")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-firstpass")]
#endif

namespace NGLicenses
{
	internal static class NGLicensesManager
	{
		// Keep them static readonly and not const, to obfuscate them.
		private static readonly string	ServerEndPoint = "https://unityapi.ngtools.tech/";
#if !FORCE_INVOICE
		private static readonly string	LicensesPath = "Licenses.txt";
		private static readonly int		MinimumMinutesBeforeInvoicesUpdate = 15;
#endif

		public static event Action							LicensesLoaded;
		public static event Action<string>					ActivationSucceeded;
		public static event Action<string, string, string>	ActivationFailed;
		public static event Action<string, string, string>	RevokeFailed;

		public static string	Title { get; set; }
		private static string	intermediatePath;
		public static string	IntermediatePath { get { return NGLicensesManager.intermediatePath; } set { NGLicensesManager.intermediatePath = value; NGLicensesManager.Load(); } }
		private static bool		isServerOperationnal = true;
		public static bool		IsServerOperationnal { get { return NGLicensesManager.isServerOperationnal; } }

		private static readonly List<string>	requestingInvoices = new List<string>();
		private static readonly List<License>	invoices = new List<License>();

		private static void	Load()
		{
#if !FORCE_INVOICE
			try
			{
				string	oldPath = Path.Combine(Application.persistentDataPath, Path.Combine(NGLicensesManager.intermediatePath, NGLicensesManager.LicensesPath));
				string	path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.Combine(NGLicensesManager.intermediatePath, NGLicensesManager.LicensesPath));

				if (File.Exists(oldPath) == true)
				{
					try
					{
						Directory.CreateDirectory(Path.GetDirectoryName(path));
						File.Move(oldPath, path);
					}
					catch
					{
						if (File.Exists(path) == false)
							path = oldPath;
					}
				}

				if (File.Exists(path) == true)
				{
					string		rawContent = File.ReadAllText(path).Substring(1);
					byte[]		b = Convert.FromBase64String(rawContent);
					rawContent = Encoding.ASCII.GetString(b);
					string[]	content = rawContent.Split('\n');

					for (int i = 0; i + 3 < content.Length; i += 4)
					{
						License	l = new License()
						{
							invoice = content[i],
							assetName = content[i + 1],
							status = (Status)int.Parse(content[i + 2]),
							active = content[i + 3] == "true" ? true : false,
						};
						NGLicensesManager.invoices.Add(l);
					}

					if ((DateTime.Now - File.GetLastWriteTime(path)).TotalMinutes < NGLicensesManager.MinimumMinutesBeforeInvoicesUpdate)
						return;

					List<string>	invoices = new List<string>();

					for (int i = 0; i < NGLicensesManager.invoices.Count; i++)
					{
						if (NGLicensesManager.invoices[i].active == true)
							invoices.Add(NGLicensesManager.invoices[i].invoice);
					}

					File.SetLastWriteTime(path, DateTime.Now);

					NGLicensesManager.VerifyActiveLicenses(invoices.ToArray());
				}
			}
			catch
			{
				NGLicensesManager.invoices.Clear();
			}
			finally
			{
				if (NGLicensesManager.LicensesLoaded != null)
					NGLicensesManager.LicensesLoaded();
			}
#else
			NGLicensesManager.invoices.Add(new License() { active = true, assetName = "NG Tools Pro", invoice = "3181282670", status = 0 });
			NGLicensesManager.VerifyLicenses(NGLicensesManager.invoices[0].invoice);

			if (NGLicensesManager.LicensesLoaded != null)
				NGLicensesManager.LicensesLoaded();
#endif
		}

		private static void	SaveLicenses()
		{
#if !FORCE_INVOICE
			string	path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.Combine(NGLicensesManager.intermediatePath, NGLicensesManager.LicensesPath));

			if (NGLicensesManager.invoices.Count == 0)
				File.Delete(path);
			else
			{
				StringBuilder	buffer = Utility.GetBuffer();

				for (int i = 0; i < NGLicensesManager.invoices.Count; i++)
				{
					buffer.Append(NGLicensesManager.invoices[i].invoice);
					buffer.Append('\n');
					buffer.Append(NGLicensesManager.invoices[i].assetName);
					buffer.Append('\n');
					buffer.Append((int)NGLicensesManager.invoices[i].status);
					buffer.Append('\n');
					buffer.Append(NGLicensesManager.invoices[i].active ? "true" : "false");
					buffer.Append('\n');
				}

				buffer.Length -= 1;

				string	rawContent = Utility.ReturnBuffer(buffer);
				byte[]	b = Encoding.ASCII.GetBytes(rawContent);
				File.WriteAllText(path, (char)('A' + ((uint)rawContent.GetHashCode() % 26)) + Convert.ToBase64String(b));
			}
#endif
		}

		public static IEnumerable<License>	EachInvoices()
		{
			for (int i = 0; i < NGLicensesManager.invoices.Count; i++)
				yield return NGLicensesManager.invoices[i];
		}

		public static void	AddInvoice(string invoice)
		{
			invoice = invoice.Trim();
			License	license = NGLicensesManager.invoices.Find(l => l.invoice == invoice);
			if (license == null)
			{
				NGLicensesManager.invoices.Add(new License() { invoice = invoice });
				NGLicensesManager.SaveLicenses();
			}
		}

		public static void	RemoveInvoice(string invoice)
		{
			int	i = NGLicensesManager.invoices.FindIndex(l => l.invoice == invoice);
			if (i != -1)
			{
				NGLicensesManager.invoices.RemoveAt(i);
				NGLicensesManager.SaveLicenses();
			}
		}

		public static bool	IsLicenseValid(string asset)
		{
			for (int i = 0; i < NGLicensesManager.invoices.Count; i++)
			{
				if (NGLicensesManager.invoices[i].assetName == asset &&
					NGLicensesManager.invoices[i].active == true &&
					NGLicensesManager.invoices[i].status != Status.Banned)
				{
					return true;
				}
			}

			return false;
		}

		public static bool	HasValidLicense()
		{
			for (int i = 0; i < NGLicensesManager.invoices.Count; i++)
			{
				if (NGLicensesManager.invoices[i].active == true &&
					NGLicensesManager.invoices[i].status != Status.Banned)
				{
					return true;
				}
			}

			return false;
		}

		public static bool	IsPro(string assetName = null)
		{
			if (string.IsNullOrEmpty(assetName) == false && NGLicensesManager.IsLicenseValid(assetName) == true)
				return true;
			return NGLicensesManager.IsLicenseValid("NG Tools Pro") == true;
		}

		public static bool	Check(bool condition, string assetName, string ad = null)
		{
			if (condition == false && NGLicensesManager.IsPro(assetName) == false)
			{
				if (string.IsNullOrEmpty(ad) == false)
					EditorUtility.DisplayDialog(assetName, ad, "OK");
				return false;
			}
			return true;
		}

		public static bool	IsCheckingInvoice(string invoice)
		{
			return NGLicensesManager.requestingInvoices.Contains(invoice);
		}

		public static void	VerifyLicenses(params string[] invoices)
		{
			if (invoices.Length == 0)
				return;

			StringBuilder	buffer = Utility.GetBuffer(NGLicensesManager.ServerEndPoint + "check_invoices.php?i=");

			buffer.Append(string.Join(",", invoices));
			buffer.Append("&dn=");
			buffer.Append(SystemInfo.deviceName);
			buffer.Append("&un=");
			buffer.Append(Environment.UserName);

			// Is is apparently possible to send an invoice and receive a completely different invoice, but still valid.
			bool	soloInvoiceFallback = invoices.Length == 1;

			NGLicensesManager.RequestServer(Utility.ReturnBuffer(buffer), (error, json) =>
			{
				if (error == false)
				{
					if (json == "[]")
					{
						lock (NGLicensesManager.invoices)
						{
							for (int j = 0; j < NGLicensesManager.invoices.Count; j++)
							{
								for (int i = 0; i < invoices.Length; i++)
								{
									if (NGLicensesManager.invoices[j].invoice == invoices[i])
										NGLicensesManager.invoices[j].status = Status.Invalid;
								}
							}

							EditorApplication.delayCall += () =>
							{
								NGLicensesManager.SaveLicenses();
								InternalEditorUtility.RepaintAllViews();
							};
						}
					}
					else if (json.StartsWith("[") == true)
					{
						string[]		results = json.Split('{');
						List<string>	unchangedInvoices = new List<string>(invoices);

						lock (NGLicensesManager.invoices)
						{
							for (int i = 1; i < results.Length; i++)
							{
								string[]	values = results[i].Split(',');
								string		invoice = values[0].Split(':')[1];

								invoice = invoice.Substring(1, invoice.Length - 2); // Remove quotes.

								if (soloInvoiceFallback == true)
								{
									soloInvoiceFallback = false;

									for (int j = 0; j < NGLicensesManager.invoices.Count; j++)
									{
										if (NGLicensesManager.invoices[j].invoice == invoices[0])
										{
											string	asset_name = values[1].Split(':')[1];
											string	rawStatus = values[2].Split(':')[1];
											Status	status = (Status)int.Parse(rawStatus.Substring(0, rawStatus.Length - 2));

											if (asset_name.Length > 2)
											{
												asset_name = asset_name.Substring(1, asset_name.Length - 2);
												NGLicensesManager.invoices[j].assetName = asset_name;
											}
											NGLicensesManager.invoices[j].status = status;

											unchangedInvoices.Remove(invoices[0]);
											break;
										}
									}
								}
								else
								{
									for (int j = 0; j < NGLicensesManager.invoices.Count; j++)
									{
										if (NGLicensesManager.invoices[j].invoice.Contains(invoice) == true)
										{
											string	asset_name = values[1].Split(':')[1];
											string	rawStatus = values[2].Split(':')[1];
											Status	status = (Status)int.Parse(rawStatus.Substring(0, rawStatus.Length - 2));

											if (asset_name.Length > 2)
											{
												asset_name = asset_name.Substring(1, asset_name.Length - 2);
												NGLicensesManager.invoices[j].assetName = asset_name;
											}
											NGLicensesManager.invoices[j].status = status;

											unchangedInvoices.RemoveAll(inv => inv.Contains(invoice));
											break;
										}
									}
								}
							}

							NGLicensesManager.SaveLicenses();
						}

						EditorApplication.delayCall += () =>
						{
							if (unchangedInvoices.Count > 0)
							{
								buffer = Utility.GetBuffer("Invoice(s)");

								for (int i = 0; i < unchangedInvoices.Count; i++)
								{
									buffer.Append(" \"");
									buffer.Append(unchangedInvoices[i]);
									buffer.Append('"');
								}

								buffer.Append(" might be invalid.\nPlease use real invoice and not voucher.");
								EditorUtility.DisplayDialog(NGLicensesManager.Title, Utility.ReturnBuffer(buffer), "OK");
							}

							InternalEditorUtility.RepaintAllViews();
						};
					}
					else if (json == "-2")
					{
						EditorApplication.delayCall += () => EditorUtility.DisplayDialog(NGLicensesManager.Title, "Don't spam.", "OK");
					}
				}
				else
				{
					EditorApplication.delayCall += () => EditorUtility.DisplayDialog(NGLicensesManager.Title, "Request has failed. Please retry or contact the author.", "OK");
				}
			}, invoices);
		}

		private static void	VerifyActiveLicenses(params string[] invoices)
		{
			if (invoices.Length == 0)
				return;

			StringBuilder	buffer = Utility.GetBuffer(NGLicensesManager.ServerEndPoint + "check_active_invoices.php?i=");

			buffer.Append(string.Join(",", invoices));
			buffer.Append("&dn=");
			buffer.Append(SystemInfo.deviceName);
			buffer.Append("&un=");
			buffer.Append(Environment.UserName);

			NGLicensesManager.RequestServer(Utility.ReturnBuffer(buffer), (error, json) =>
			{
				if (error == false)
				{
					string[]		lines = json.Split('\n');
					List<string>	changes = new List<string>();

					lock (NGLicensesManager.invoices)
					{
						for (int i = 0; i + 2 < lines.Length; i += 3)
						{
							for (int j = 0; j < NGLicensesManager.invoices.Count; j++)
							{
								if (NGLicensesManager.invoices[j].invoice == lines[i])
								{
									if (lines[i + 1] == "-1") // Banned
									{
										if (NGLicensesManager.invoices[j].status != Status.Banned ||
											NGLicensesManager.invoices[j].active != false)
										{
											changes.Add("License \"" + NGLicensesManager.invoices[j].invoice + "\" has been revoked and blocked." + (string.IsNullOrEmpty(lines[i + 2]) == false ? " (" + lines[i + 2] + ")" : string.Empty));
											NGLicensesManager.invoices[j].status = Status.Banned;
											NGLicensesManager.invoices[j].active = false;
										}
									}
									else if (lines[i + 1] == "0") // Revoked
									{
										if (NGLicensesManager.invoices[j].status != Status.Valid ||
											NGLicensesManager.invoices[j].active != false)
										{
											changes.Add("License \"" + NGLicensesManager.invoices[j].invoice + "\" has been revoked." + (string.IsNullOrEmpty(lines[i + 2]) == false ? " (" + lines[i + 2] + ")" : string.Empty));
											NGLicensesManager.invoices[j].status = Status.Valid;
											NGLicensesManager.invoices[j].active = false;
										}
									}
									else if (lines[i + 1] == "1") // Activated
									{
										if (NGLicensesManager.invoices[j].status != Status.Valid ||
											NGLicensesManager.invoices[j].active != true)
										{
											changes.Add("License \"" + NGLicensesManager.invoices[j].invoice + "\" has been activated." + (string.IsNullOrEmpty(lines[i + 2]) == false ? " (" + lines[i + 2] + ")" : string.Empty));
											NGLicensesManager.invoices[j].status = Status.Valid;
											NGLicensesManager.invoices[j].active = true;
										}
									}
								}
							}
						}

						if (changes.Count > 0)
							NGLicensesManager.SaveLicenses();
					}

					if (changes.Count > 0)
					{
						EditorApplication.delayCall += () =>
						{
							for (int i = 0; i < changes.Count; i++)
								Debug.LogWarning("[" + NGLicensesManager.Title + "] " + changes[i]);

							InternalEditorUtility.RepaintAllViews();
						};
					}
				}
			}, invoices);
		}

		public static void	ActivateLicense(string invoice)
		{
			License	license;

			lock (NGLicensesManager.invoices)
			{
				license = NGLicensesManager.invoices.Find(l => l.invoice == invoice);

				if (license == null)
					return;
			}

			StringBuilder	buffer = Utility.GetBuffer(NGLicensesManager.ServerEndPoint + "active_invoice.php?i=");

			buffer.Append(invoice);
			buffer.Append("&dn=");
			buffer.Append(SystemInfo.deviceName);
			buffer.Append("&un=");
			buffer.Append(Environment.UserName);

			NGLicensesManager.RequestServer(Utility.ReturnBuffer(buffer), (error, result) =>
			{
				if (error == false)
				{
					if (result == "1")
					{
						EditorApplication.delayCall += () =>
						{
							lock (NGLicensesManager.invoices)
							{
								license.active = true;
								if (NGLicensesManager.ActivationSucceeded != null)
									NGLicensesManager.ActivationSucceeded(license.invoice);
								NGLicensesManager.SaveLicenses();
							}

							InternalEditorUtility.RepaintAllViews();
							EditorUtility.DisplayDialog(NGLicensesManager.Title, "License activated.", "OK");
						};
					}
					else
					{
						string	message;

						if (result == "-2") // Banned
						{
							message = "You are not allowed to use this invoice. Please contact the author.";
							license.status = Status.Banned;
							license.active = false;
						}
						else if (result == "-3") // Invalid
						{
							message = "Invoice is invalid.";
							license.status = Status.Invalid;
							license.active = false;
						}
						else if (result == "-4") // Max reached
						{
							message = "No more activation is allowed, limitation per seat is already reached. Please first revoke your license on other computers before activating here.";
							license.status = Status.Valid;
							license.active = false;
						}
						else
							message = "Server has encountered an issue. Please contact the author.";

						EditorApplication.delayCall += () =>
						{
							if (NGLicensesManager.ActivationFailed != null)
								NGLicensesManager.ActivationFailed(invoice, message, result);
						};
					}
				}
				else
				{
					EditorApplication.delayCall += () =>
					{
						if (NGLicensesManager.ActivationFailed != null)
							NGLicensesManager.ActivationFailed(invoice, "Request has failed.", result);
					};
				}
			}, invoice);
		}

		public static void	RevokeSeat(string invoice, string deviceName, string userName, Action<string, string, string> onCompleted)
		{
			StringBuilder	buffer = Utility.GetBuffer(NGLicensesManager.ServerEndPoint + "revoke_seats.php?i=");

			buffer.Append(invoice);
			buffer.Append("&dn=");
			buffer.Append(SystemInfo.deviceName);
			buffer.Append("&un=");
			buffer.Append(Environment.UserName);
			buffer.Append("&sdn[]=");
			buffer.Append(deviceName);
			buffer.Append("&sun[]=");
			buffer.Append(userName);

			NGLicensesManager.RequestServer(Utility.ReturnBuffer(buffer), (error, result) =>
			{
				if (error == false)
				{
					if (result == "1")
					{
						EditorApplication.delayCall += () =>
						{
							lock (NGLicensesManager.invoices)
							{
								License	license = NGLicensesManager.invoices.Find(l => l.invoice == invoice);

								if (license != null)
								{
									license.active = false;
									license.status = 0;
									NGLicensesManager.SaveLicenses();
								}
							}

							onCompleted(invoice, deviceName, userName);
							InternalEditorUtility.RepaintAllViews();
							EditorUtility.DisplayDialog(NGLicensesManager.Title, "Seat \"" + deviceName + " " + userName + "\" revoked.", "OK");
						};
					}
					else
					{
						string	message;

						if (result == "0") // Not using
							message = "The server could not revoke the seat \"" + deviceName + " " + userName + "\". Please contact the author.";
						else
							message = "Server has encountered an issue. Please contact the author.";

						EditorApplication.delayCall += () =>
						{
							EditorUtility.DisplayDialog(NGLicensesManager.Title, message, "OK");
						};
					}
				}
				else
				{
					EditorApplication.delayCall += () =>
					{
						EditorUtility.DisplayDialog(NGLicensesManager.Title, "Request has failed.", "OK");
					};
				}
			}, invoice);
		}

		public static void	RevokeLicense(string invoice)
		{
			License	license;

			lock (NGLicensesManager.invoices)
			{
				license = NGLicensesManager.invoices.Find(l => l.invoice == invoice);

				if (license == null)
					return;
			}

			StringBuilder	buffer = Utility.GetBuffer(NGLicensesManager.ServerEndPoint + "revoke_invoice.php?i=");

			buffer.Append(invoice);
			buffer.Append("&dn=");
			buffer.Append(SystemInfo.deviceName);
			buffer.Append("&un=");
			buffer.Append(Environment.UserName);

			NGLicensesManager.RequestServer(Utility.ReturnBuffer(buffer), (error, result) =>
			{
				if (error == false)
				{
					if (result == "1")
					{
						EditorApplication.delayCall += () =>
						{
							lock (NGLicensesManager.invoices)
							{
								license.status = 0;
								license.active = false;
								NGLicensesManager.SaveLicenses();
							}

							InternalEditorUtility.RepaintAllViews();
							EditorUtility.DisplayDialog(NGLicensesManager.Title, "License revoked.", "OK");
						};
					}
					else
					{
						string	message;

						if (result == "0") // Not using
						{
							message = "You are not using this invoice.";
							license.status = Status.Valid;
							license.active = false;
						}
						else if (result == "-2") // Banned
						{
							message = "You are not allowed to revoke this invoice. Please contact the author.";
							license.status = Status.Banned;
							license.active = false;
						}
						else
							message = "Server has encountered an issue. Please contact the author.";

						EditorApplication.delayCall += () =>
						{
							if (NGLicensesManager.RevokeFailed != null)
								NGLicensesManager.RevokeFailed(invoice, message, result);
						};
					}
				}
				else
				{
					EditorApplication.delayCall += () =>
					{
						if (NGLicensesManager.RevokeFailed != null)
							NGLicensesManager.RevokeFailed(invoice, "Request has failed.", result);
					};
				}
			}, invoice);
		}

		public static void	ShowActiveSeatsFromLicense(string invoice, Action<string, string[]> onCompleted)
		{
			if (string.IsNullOrEmpty(invoice) == true)
				return;

			StringBuilder	buffer = Utility.GetBuffer(NGLicensesManager.ServerEndPoint + "get_active_seats.php?i=");

			buffer.Append(invoice);
			buffer.Append("&dn=");
			buffer.Append(SystemInfo.deviceName);
			buffer.Append("&un=");
			buffer.Append(Environment.UserName);

			NGLicensesManager.RequestServer(Utility.ReturnBuffer(buffer), (error, json) =>
			{
				if (error == false)
				{
					if (json == "[]")
						onCompleted(invoice, new string[0]);
					else if (json.StartsWith("[") == true)
					{
						string[]		raw = json.Split(',');
						List<string>	results = new List<string>();

						for (int i = 0; i < raw.Length; i++)
						{
							int	n = raw[i].IndexOf('"');

							results.Add(raw[i].Substring(n + 1, raw[i].IndexOf('"', n + 1) - n - 1));
						}

						onCompleted(invoice, results.ToArray());
					}
					else
						EditorApplication.delayCall += () => EditorUtility.DisplayDialog(NGLicensesManager.Title, "Server has encountered an issue. Please contact the author.", "OK");

					EditorApplication.delayCall += InternalEditorUtility.RepaintAllViews;
				}
				else
				{
					EditorApplication.delayCall += () => EditorUtility.DisplayDialog(NGLicensesManager.Title, "Request for active seats has failed. Please retry or contact the author.", "OK");
				}
			}, invoice);
		}

		private static void	RequestServer(string endpoint, Action<bool, string> resultCallback, params string[] invoices)
		{
			string	unityVersion = Application.unityVersion;
			System.Diagnostics.Stopwatch	startTime = new System.Diagnostics.Stopwatch();
			Action	asyncWebRequestInvoke = () =>
			{
				System.Timers.Timer	autoRequestKill = new System.Timers.Timer(15000);

				try
				{
					// Due to a random issue where the request is never ending, the timer will fallback.
					autoRequestKill.Enabled = true;

					HttpWebRequest	request = (HttpWebRequest)WebRequest.Create(endpoint);

					autoRequestKill.Elapsed += (sender, e) =>
					{
						lock (startTime)
						{
							autoRequestKill.Stop();

							// Little trick to avoid a double call to onComplete.
							if (resultCallback == null)
								return;

							request.Abort();

							Action<bool, string>	local = resultCallback;

							resultCallback = null;

							EditorApplication.delayCall += () => local(true, "Request expired. Please retry.");

							//EditorUtility.DisplayDialog(NGLicensesManager.Title, "An error occurred while verifying the invoices, please retry later or contact the author. (" + invoices.Length + " / " + NGLicensesManager.requestingInvoices.Count + ")", "OK");

							for (int i = 0; i < invoices.Length; i++)
							{
								int	n = NGLicensesManager.requestingInvoices.IndexOf(invoices[i]);
								if (n != -1)
									NGLicensesManager.requestingInvoices.RemoveAt(n);
							}
						}
					};
					autoRequestKill.Start();

					NGLicensesManager.isServerOperationnal = true;
					EditorApplication.delayCall += InternalEditorUtility.RepaintAllViews;

					NGLicensesManager.requestingInvoices.AddRange(invoices);

					request.UserAgent = "Unity/" + unityVersion + " NG Tools/" + NGAssemblyInfo.Name + "/" + NGAssemblyInfo.Version;
					request.Timeout = 5000;
					request.ReadWriteTimeout = 15000;

					// Pipes the stream to a higher level stream reader with the required encoding format.
					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					using (StreamReader readStream = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
					{
						lock (startTime)
						{
							// Little trick to avoid a double call to resultCallback.
							if (resultCallback == null)
								return;

							Action<bool, string>	local = resultCallback;

							resultCallback = null;
							autoRequestKill.Stop();

							bool	error = false;
							string	result = string.Empty;

							try
							{
								result = readStream.ReadToEnd();
							}
							catch (Exception ex)
							{
								error = true;
								result = ex.Message;
								NGLicensesManager.isServerOperationnal = false;
							}
							finally
							{
								EditorApplication.delayCall += () => local(error, result);

								bool	op = NGLicensesManager.isServerOperationnal;

								NGLicensesManager.isServerOperationnal = true;
								EditorApplication.delayCall += InternalEditorUtility.RepaintAllViews;

								// Wait at least 500ms in case request is instant.
								if (startTime.ElapsedMilliseconds < 500)
									Thread.Sleep(500 - (int)startTime.ElapsedMilliseconds);

								if (op == false)
									NGLicensesManager.isServerOperationnal = false;

								EditorApplication.delayCall += () =>
								{
									InternalEditorUtility.RepaintAllViews();

									for (int i = 0; i < invoices.Length; i++)
									{
										int	n = NGLicensesManager.requestingInvoices.IndexOf(invoices[i]);
										if (n != -1)
											NGLicensesManager.requestingInvoices.RemoveAt(n);
									}
								};
							}
						}
					}
				}
				catch (WebException ex)
				{
					NGLicensesManager.AppendExceptionToComplementary(endpoint, ex);

					using (WebResponse response = ex.Response)
					{
						HttpWebResponse	httpResponse = (HttpWebResponse)response;
						using (Stream data = response.GetResponseStream())
						using (StreamReader reader = new StreamReader(data))
						{
							string	text = reader.ReadToEnd();

							lock (startTime)
							{
								// Little trick to avoid a double call to resultCallback.
								if (resultCallback == null)
									return;

								Action<bool, string>	local = resultCallback;

								resultCallback = null;
								EditorApplication.delayCall += () => local(true, ex.Message + Environment.NewLine + httpResponse.StatusCode + Environment.NewLine + text);
							}
						}
					}
				}
				catch (Exception ex)
				{
					NGLicensesManager.AppendExceptionToComplementary(endpoint, ex);

					lock (startTime)
					{
						// Little trick to avoid a double call to resultCallback.
						if (resultCallback == null)
							return;

						Action<bool, string>	local = resultCallback;

						resultCallback = null;
						EditorApplication.delayCall += () => local(true, ex.Message);
					}
				}
			};

			asyncWebRequestInvoke.BeginInvoke(iar => ((Action)iar.AsyncState).EndInvoke(iar), asyncWebRequestInvoke);
		}

		private static void	AppendExceptionToComplementary(string endpoint, Exception ex)
		{
			EditorApplication.delayCall += () =>
			{
				string	valec = NGLicensesManager.GetStatsComplementary("VALEC");
				byte[]	raw;

				if (valec != string.Empty)
					raw = Convert.FromBase64String(valec);
				else
					raw = new byte[0];

				byte[]	append = Encoding.ASCII.GetBytes(endpoint + Environment.NewLine + ex.Message + Environment.NewLine);
				byte[]	newRaw = new byte[raw.Length + append.Length];

				Buffer.BlockCopy(raw, 0, newRaw, 0, raw.Length);
				Buffer.BlockCopy(append, 0, newRaw, raw.Length, append.Length);

				NGLicensesManager.SetStatsComplementary("VALEC", Convert.ToBase64String(newRaw));
			};
		}

		// Code copied from NGCore.HQ
		private const string	ComplementaryKeyPref = "NGTools_Complementary";
		private const char		ComplementarySeparator = ';';

		private static void	SetStatsComplementary(string key, string value)
		{
			string			complementary = EditorPrefs.GetString(NGLicensesManager.ComplementaryKeyPref);
			List<string>	data = new List<string>();
			bool			found = false;

			if (string.IsNullOrEmpty(complementary) == false)
				data.AddRange(complementary.Split(NGLicensesManager.ComplementarySeparator));

			for (int i = 0; i < data.Count; i++)
			{
				if (data[i].StartsWith(key + '=') == true)
				{
					found = true;
					if (string.IsNullOrEmpty(value) == true)
						data.RemoveAt(i--);
					else
						data[i] = key + '=' + value;
				}
			}

			if (found == false)
				data.Add(key + '=' + value);

			EditorPrefs.SetString(NGLicensesManager.ComplementaryKeyPref, string.Join(NGLicensesManager.ComplementarySeparator.ToString(), data.ToArray()));
		}

		private static string	GetStatsComplementary(string key)
		{
			string		complementary = EditorPrefs.GetString(NGLicensesManager.ComplementaryKeyPref);
			string[]	data = complementary.Split(NGLicensesManager.ComplementarySeparator);

			for (int i = 0; i < data.Length; i++)
			{
				if (data[i].StartsWith(key + '=') == true)
					return data[i].Substring(key.Length + 1);
			}

			return string.Empty;
		}
	}
}