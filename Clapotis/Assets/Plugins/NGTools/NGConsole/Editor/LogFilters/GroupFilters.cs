using NGLicenses;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	[Serializable]
	public sealed class GroupFilters
	{
		private sealed class FilterPopup : PopupWindowContent
		{
			private readonly EditorWindow	window;
			private readonly ILogFilter		filter;
			private readonly Action			filterChanged;

			public	FilterPopup(EditorWindow window, ILogFilter filter, Action filterChanged)
			{
				this.window = window;
				this.filter = filter;
				this.filterChanged = filterChanged;
			}

			public override Vector2	GetWindowSize()
			{
				return new Vector2(Mathf.Max(this.window.position.width, 300F), 18F);
			}

			public override void	OnGUI(Rect r)
			{
				r.height = 16F;

				EditorGUI.BeginChangeCheck();
				this.filter.OnGUI(r, false);
				if (EditorGUI.EndChangeCheck() == true)
					this.filterChanged();
			}
		}

		public const float	MinFilterWidth = 250F;
		public const int	MaxFilters = 1;

		private static Type[]	logFilterTypes;

		public event Action	FilterAltered;
		public bool			canSelect = true;

		public ILogFilter	SelectedFilter
		{
			get
			{
				if (this.selectedFilter < this.filters.Count)
					return this.filters[this.selectedFilter];

				return null;
			}
		}

		[Exportable(ExportableAttribute.ArrayOptions.Immutable)]
		public List<ILogFilter>	filters = new List<ILogFilter>();

		private int	selectedFilter;

		/// <summary>
		/// <para>Checks whether the given <paramref name="Row"/> is accepted or refused by filters.</para>
		/// <para>If no filter enabled, it returns true.</para>
		/// <para>To accept a log, at least one filter must accept it by returning Accepted.</para>
		/// <para>But if one filter returns Refused, this overwhelms the final result and the log if rejected.</para>
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		public bool	Filter(Row row)
		{
			// By default, row is accepted if there is no filter activated.
			bool	isAccepted = true;

			//for (int i = 0; i < this.filters.Count; i++)
			//{
			//	// But is rejected if at least one filter is activated.
			//	if (this.filters[i].Enabled == true)
			//		isAccepted = false;
			//}

			for (int i = 0; i < this.filters.Count; i++)
			{
				if (this.filters[i].Enabled == true)
				{
					FilterResult	result = this.filters[i].CanDisplay(row);

					if (result == FilterResult.Refused)
						return false;

					if (result == FilterResult.Accepted)
						isAccepted = true;
				}
			}

			return isAccepted;
		}

		public Rect	OnGUI(Rect r)
		{
			GeneralSettings	settings = HQ.Settings.Get<GeneralSettings>();

			this.selectedFilter = Mathf.Clamp(this.selectedFilter, 0, this.filters.Count - 1);

			r.height = Constants.SingleLineHeight;

			for (int i = 0; i < this.filters.Count; i++)
			{
				Utility.content.text = this.filters[i].Name;
				r.width = settings.MenuButtonStyle.CalcSize(Utility.content).x;

				EditorGUI.BeginChangeCheck();
				bool	enabled = GUI.Toggle(r, this.filters[i].Enabled, Utility.content.text, settings.MenuButtonStyle);
				if (EditorGUI.EndChangeCheck() == true)
				{
					GUI.FocusControl(null);

					if (Event.current.button == 0)
					{
						this.selectedFilter = i;
						if (this.canSelect == false)
							this.filters[i].Enabled = enabled;
					}
					// Delete on middle click.
					else if (Event.current.button == 2)
					{
						this.filters.RemoveAt(i);
						this.OnFilterAltered();
					}
					// Show context menu on right click.
					else if (Event.current.button == 1)
					{
						if (this.canSelect == false)
							this.filters[i].Enabled = enabled;

						GenericMenu	menu = new GenericMenu();
						menu.AddItem(new GUIContent("Edit"), false, this.EditFilter, new object[] { this.filters[i], r });
						menu.AddItem(new GUIContent("Delete"), false, this.DeleteFilter, this.filters[i]);
						menu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0));
					}
				}

				if (this.canSelect == true && i == this.selectedFilter && this.filters.Count >= 2)
				{
					r.height = 1F;
					EditorGUI.DrawRect(r, Color.yellow);
					r.height = Constants.SingleLineHeight;
				}
				r.x += r.width;
			}

			r.width = 16F;
			if (GUI.Button(r, string.Empty, GeneralStyles.ToolbarDropDown) == true)
			{
				if (this.CheckMaxFilters(this.filters.Count) == true)
				{
					if (GroupFilters.logFilterTypes == null)
					{
						List<Type>	listTypes = new List<Type>(4);

						foreach (Type c in Utility.EachNGTAssignableFrom(typeof(ILogFilter)))
							listTypes.Add(c);

						GroupFilters.logFilterTypes = listTypes.ToArray();
					}

					GenericMenu	menu = new GenericMenu();

					menu.AddDisabledItem(new GUIContent("Add Filter :"));
					menu.AddSeparator(string.Empty);
					for (int i = 0; i < GroupFilters.logFilterTypes.Length; ++i)
						menu.AddItem(new GUIContent(Utility.NicifyVariableName(LC.G(GroupFilters.logFilterTypes[i].Name))), false, this.AddFilter, i);

					menu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0));
				}
			}
			r.x += r.width;

			return r;
		}

		private void	AddFilter(object data)
		{
			ILogFilter	filter = Activator.CreateInstance(GroupFilters.logFilterTypes[(int)data]) as ILogFilter;

			this.filters.Add(filter);

			filter.ToggleEnable += this.OnFilterAltered;
			filter.Enabled = true;

			this.OnFilterAltered();
		}

		private void	EditFilter(object rawData)
		{
			try
			{
				object[]		data = (object[])rawData;
				Rect			v = (Rect)data[1];
				EditorWindow	console = EditorWindow.focusedWindow;

				// Must force the call in a GUI context, Unity >2017 does not like the normal way.
				GUICallbackWindow.Open(() =>
				{
					FilterPopup	window = new FilterPopup(console, data[0] as ILogFilter, this.OnFilterAltered);
					PopupWindow.Show(new Rect(console.position.x, console.position.y + v.y - 4F, 0F, 0F), window);
				});
			}
			catch (ExitGUIException)
			{
			}
		}

		private void	DeleteFilter(object data)
		{
			((ILogFilter)data).ToggleEnable -= this.OnFilterAltered;
			this.filters.Remove((ILogFilter)data);

			this.OnFilterAltered();
		}

		private void	OnFilterAltered()
		{
			if (this.FilterAltered != null)
				this.FilterAltered();
		}

		private bool	CheckMaxFilters(int count)
		{
			return NGLicensesManager.Check(count < GroupFilters.MaxFilters, NGTools.NGConsole.NGAssemblyInfo.Name + " Pro", "Free version does not allow more than " + GroupFilters.MaxFilters + " filters.\n\nClick no more, I can see you want more! :D");
		}
	}
}