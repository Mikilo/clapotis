using NGLicenses;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public class LicenseSeatsWindow : EditorWindow
	{
		public const string	Title = "License's Seats";

		private string			invoice;
		private string[]		seats;
		private List<string>	seatsRevoked = new List<string>();

		private bool	once;
		private Vector2	scrollPosition;

		protected virtual void	OnEnable()
		{
			this.minSize = new Vector2(400F, 150F);
		}

		protected virtual void	OnGUI()
		{
			if (this.once == false)
			{
				this.once = true;
				float	maxHeight = Mathf.Min(40F + 20F + this.seats.Length * (35F + 10F), 150F);
				this.minSize = new Vector2(this.minSize.x, maxHeight);
			}

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label("Invoice: ", GeneralStyles.VerticalCenterLabel, GUILayoutOptionPool.ExpandWidthFalse, GUILayoutOptionPool.Height(40F));
				GUILayout.Label(invoice, GeneralStyles.MainTitle);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				GUILayout.Label("Active Seat", GeneralStyles.AllCenterTitle);
			}
			EditorGUILayout.EndHorizontal();

			this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
			{
				if (this.seats.Length == 0)
					GUILayout.Label("No active seat.", GeneralStyles.InnerBoxText);
				else
				{
					for (int i = 0; i < this.seats.Length; i += 2)
					{
						bool	isRevoked = this.seatsRevoked.Contains(this.seats[i]);

						EditorGUI.BeginDisabledGroup(isRevoked);
						EditorGUILayout.BeginHorizontal();
						{
							GUILayout.Label(this.seats[i], GeneralStyles.AllCenterTitle, GUILayoutOptionPool.Width(160F), GUILayoutOptionPool.Height(35F));
							GUILayout.Label(this.seats[i + 1], GeneralStyles.AllCenterTitle, GUILayoutOptionPool.Width(140F), GUILayoutOptionPool.Height(35F));

							GUILayout.FlexibleSpace();

							if (isRevoked == true)
								GUILayout.Label("[Revoked]", GeneralStyles.AllCenterTitle, GUILayoutOptionPool.ExpandWidthFalse, GUILayoutOptionPool.Height(35F), GUILayoutOptionPool.Width(85F));
							else if (this.seats[i] != "PUBLIC" && this.seats[i + 1] != "LICENSE" && GUILayout.Button("Revoke", GUILayoutOptionPool.ExpandWidthFalse, GUILayoutOptionPool.Height(35F), GUILayoutOptionPool.Width(60F)) == true && ((Event.current.modifiers & Constants.ByPassPromptModifier) != 0 || EditorUtility.DisplayDialog(Constants.PackageTitle, "Confirm revoking \"" + this.seats[i] + " " + this.seats[i + 1] + "\"?", "Yes", "No") == true))
								NGLicensesManager.RevokeSeat(this.invoice, this.seats[i], this.seats[i + 1], this.OnRemoveSeat);
						}
						EditorGUILayout.EndHorizontal();
						EditorGUI.EndDisabledGroup();

						GUILayout.Space(10F);
					}
				}
			}
			EditorGUILayout.EndScrollView();
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true)
				this.Close();
		}

		public void	Set(string invoice, string[] seats)
		{
			this.invoice = invoice;
			this.seats = seats;
			this.seatsRevoked.Clear();
		}

		private void	OnRemoveSeat(string invoice, string deviceName, string userName)
		{
			this.seatsRevoked.Add(deviceName);
		}
	}
}