using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor
{
	public class GenericTypesSelectorWizard : ScriptableWizard
	{
		private const string	TextControlName = "filter";
		private static Color	MouseHoverBackgroundColor = new Color(.1F, .1F, .1F, .4F);
		private static Color	SelectedTypeBackgroundColor = Color.green * .5F;

		/// <summary>Enable it to display types sorted through categories using the attribute CategoryAttribute on types.</summary>
		private bool			enableCategories;
		public bool				EnableCategories
		{
			get
			{
				return this.enableCategories;
			}
			set
			{
				this.enableCategories = value;

				if (value == true && this.categories == null)
				{
					this.categories = new SortedDictionary<string, List<Type>>();

					for (int i = 0; i < this.types.Length; i++)
						this.AddTypeToCategory(this.types[i]);
				}
			}
		}

		/// <summary>Enable it to display a null value as first line.</summary>
		public bool	EnableNullValue { get; set; }
		public Type	SelectedType { get; set; }

		private Action<Type>	OnCreate;
		private Type			type;
		private bool			closeOnCreate;

		private Type[]		types;

		private string		filter;
		private List<Type>	displayingTypes = new List<Type>();
		private List<Type>	temporaryFilterTypes = new List<Type>();

		private SortedDictionary<string, List<Type>>	categories;

		private VerticalScrollbar	scrollbar;

		private float	allNamespaceWidth = 0F;
		private float	allNameWidth = 0F;
		private float	namespaceWidth;
		private float	nameWidth;

		private bool	GUIOnce;
		private bool	scrollWitnessOnce;

		private List<Type>	lastTypes = new List<Type>();
		private List<float>	lastOffsets = new List<float>();

		/// <summary>
		/// <para>Creates a form containing a list of <paramref name="type"/>.</para>
		/// <para>It will call your callback <paramref name="OnCreate"/> when a type is selected.</para>
		/// </summary>
		/// <see cref="CategoryAttribute"/>
		/// <param name="title">The title of the wizard.</param>
		/// <param name="type">The base type the wizard must use to display all the inherited types.</param>
		/// <param name="OnCreate">A method with the selected type as argument.</param>
		/// <param name="allAssemblies">Get types from all assemblies if true, or only in editor assembly.</param>
		/// <param name="closeOnCreate">Automatically close the wizard after calling the callback.</param>
		public static GenericTypesSelectorWizard	Start(string title, Type type, Action<Type> OnCreate, bool allAssemblies, bool closeOnCreate)
		{
			return GenericTypesSelectorWizard.GetWindow<GenericTypesSelectorWizard>(true, title).Init(title, type, OnCreate, allAssemblies, closeOnCreate);
		}

		public GenericTypesSelectorWizard	Init(string title, Type type, Action<Type> OnCreate, bool allAssemblies, bool closeOnCreate)
		{
			this.titleContent.text = title;
			this.type = type;
			this.OnCreate = OnCreate;
			this.closeOnCreate = closeOnCreate;
			this.EnableCategories = false;
			this.EnableNullValue = false;
			this.filter = string.Empty;
			this.GUIOnce = false;
			this.scrollWitnessOnce = false;

			List<Type>			list = new List<Type>(16);
			IEnumerable<Type>	types;
			if (allAssemblies == true)
				types = Utility.EachAllAssignableFrom(this.type);
			else
				types = Utility.EachAssignableFrom(this.type);

			foreach (Type t in types)
			{
				int	i = 0;

				for (; i < list.Count; i++)
				{
					if (t.Name.CompareTo(list[i].Name) <= 0)
					{
						if (list.Contains(t) == false)
							list.Insert(i, t);
						break;
					}
				}

				if (i >= list.Count)
					list.Add(t);
			}

			this.types = list.ToArray();

			if (this.EnableCategories == true)
			{
				this.categories = new SortedDictionary<string, List<Type>>();

				for (int i = 0; i < this.types.Length; i++)
					this.AddTypeToCategory(this.types[i]);
			}

			this.SelectedType = null;

			this.scrollbar = new VerticalScrollbar(0F, 0F, this.position.height);
			this.scrollbar.interceiptEvent = true;

			this.wantsMouseMove = true;

			return this;
		}

		protected virtual void	OnGUI()
		{
			if (this.GUIOnce == false && Event.current.type == EventType.Repaint)
			{
				this.GUIOnce = true;
				GUI.FocusControl(GenericTypesSelectorWizard.TextControlName);
				this.ProcessMaxNamespaceWidth(this.types);
				this.allNamespaceWidth = this.namespaceWidth;
				this.allNameWidth = this.nameWidth;
			}

			GUILayout.BeginHorizontal(GeneralStyles.Toolbar);
			{
				EditorGUI.BeginChangeCheck();

				using (BgColorContentRestorer.Get(this.temporaryFilterTypes.Count == 0 && string.IsNullOrEmpty(this.filter) == false, Color.red))
				{
					if (Event.current.type == EventType.KeyDown)
					{
						if (Event.current.keyCode == KeyCode.Escape)
							GUI.FocusControl(null);
						else if (Event.current.keyCode == KeyCode.KeypadEnter ||
								 Event.current.keyCode == KeyCode.Return)
						{
							if (this.EnableNullValue == true || this.SelectedType != null)
								this.Create(this.SelectedType);
						}
						else if (Event.current.keyCode == KeyCode.Home)
						{
							GUI.FocusControl(GenericTypesSelectorWizard.TextControlName);

							this.SelectedType = null;
							this.scrollWitnessOnce = false;
							this.scrollbar.Offset = 0F;
							this.scrollbar.ClearInterests();

							this.Repaint();
						}
						else if (Event.current.keyCode == KeyCode.End)
						{
							GUI.FocusControl(null);

							if (this.EnableCategories == true)
							{
								foreach (var pair in this.categories)
									this.SelectedType = pair.Value[pair.Value.Count - 1];
							}
							else if (string.IsNullOrEmpty(this.filter) == true)
								this.SelectedType = this.types[this.types.Length - 1];
							else
								this.SelectedType = this.displayingTypes[this.displayingTypes.Count - 1];

							this.scrollWitnessOnce = false;
							this.scrollbar.Offset = float.MaxValue;
							this.scrollbar.ClearInterests();
							this.Repaint();
						}
						else if (Event.current.keyCode == KeyCode.PageUp ||
								 Event.current.keyCode == KeyCode.UpArrow)
						{
							int	shift = Event.current.keyCode == KeyCode.PageUp ? (int)((this.position.height - Constants.SingleLineHeight) / Constants.SingleLineHeight) : 1;

							if (this.SelectedType == null)
							{
								GUI.FocusControl(GenericTypesSelectorWizard.TextControlName);
								this.scrollbar.Offset = 0F;
								this.scrollWitnessOnce = false;
								this.scrollbar.ClearInterests();
								this.Repaint();
							}
							else if (this.EnableCategories == true)
							{
								this.lastTypes.Clear();
								this.lastOffsets.Clear();

								float	shiftOffset = Event.current.keyCode == KeyCode.PageUp ? this.position.height - Constants.SingleLineHeight : Constants.SingleLineHeight;
								float	offset = 0F;
								float	lastHeight = 0F;
								Type	lastType = null;

								foreach (var pair in this.categories)
								{
									for (int i = 0; i < pair.Value.Count; i++)
									{
										if (pair.Value[i] == this.SelectedType)
										{
											float	targetOffset = offset + Constants.SingleLineHeight - shiftOffset;

											if (i == 0)
												targetOffset += GeneralStyles.BigCenterText.lineHeight + GeneralStyles.BigCenterText.padding.vertical;

											if (this.lastOffsets.Count > 0)
											{
												int	j = 0;

												for (j = this.lastOffsets.Count - 1; j >= 0; j--)
												{
													if (targetOffset > this.lastOffsets[j])
													{
														if (j + 1 < this.lastOffsets.Count)
															lastHeight = this.lastOffsets[j + 1] - this.lastOffsets[j];
														else
															lastHeight = offset - this.lastOffsets[j];

														lastType = this.lastTypes[j];
														offset = this.lastOffsets[j];
														goto bigBreak;
													}
												}

												lastType = this.lastTypes[0];
												offset = 0F;
											}
											else
											{
												GUI.FocusControl(GenericTypesSelectorWizard.TextControlName);
												lastType = null;
												offset = 0F;
											}

											bigBreak:

											if (this.SelectedType != lastType)
											{
												this.SelectedType = lastType;
												this.scrollWitnessOnce = false;
												this.scrollbar.ClearInterests();
											}

											if (offset < this.scrollbar.Offset)
												this.scrollbar.Offset = offset;
											else if (offset + lastHeight > this.scrollbar.Offset + this.scrollbar.MaxHeight)
												this.scrollbar.Offset = -this.scrollbar.MaxHeight + offset + lastHeight;

											this.Repaint();

											return;
										}

										this.lastOffsets.Add(offset);
										this.lastTypes.Add(pair.Value[i]);

										if (i == 0)
											offset += GeneralStyles.BigCenterText.lineHeight + GeneralStyles.BigCenterText.padding.vertical;

										offset += Constants.SingleLineHeight;
									}
								}
							}
							else if (string.IsNullOrEmpty(this.filter) == true)
							{
								for (int i = 0; i < this.types.Length; i++)
								{
									if (this.types[i] == this.SelectedType)
									{
										if (i == 0)
										{
											GUI.FocusControl(GenericTypesSelectorWizard.TextControlName);
											this.SelectedType = null;
										}
										else
										{
											i = Mathf.Max(0, i - shift);
											this.SelectedType = this.types[i];
										}

										if (i * Constants.SingleLineHeight < this.scrollbar.Offset)
											this.scrollbar.Offset = i * Constants.SingleLineHeight;
										else if ((i + 1) * Constants.SingleLineHeight - this.scrollbar.Offset > this.scrollbar.MaxHeight)
											this.scrollbar.Offset = -this.scrollbar.MaxHeight + (i + 1) * Constants.SingleLineHeight;

										this.scrollWitnessOnce = false;
										this.scrollbar.ClearInterests();
										this.Repaint();

										return;
									}
								}
							}
							else
							{
								for (int i = 0; i < this.displayingTypes.Count; i++)
								{
									if (this.displayingTypes[i] == this.SelectedType)
									{
										if (i == 0)
										{
											GUI.FocusControl(GenericTypesSelectorWizard.TextControlName);
											this.SelectedType = null;
										}
										else
										{
											i = Mathf.Max(0, i - shift);
											this.SelectedType = this.displayingTypes[i];
										}

										if (i * Constants.SingleLineHeight < this.scrollbar.Offset)
											this.scrollbar.Offset = i * Constants.SingleLineHeight;
										else if ((i + 1) * Constants.SingleLineHeight - this.scrollbar.Offset > this.scrollbar.MaxHeight)
											this.scrollbar.Offset = -this.scrollbar.MaxHeight + (i + 1) * Constants.SingleLineHeight;

										this.scrollWitnessOnce = false;
										this.scrollbar.ClearInterests();
										this.Repaint();

										return;
									}
								}
							}
						}
						else if (Event.current.keyCode == KeyCode.PageDown ||
								 Event.current.keyCode == KeyCode.DownArrow)
						{
							int	shift = Event.current.keyCode == KeyCode.PageDown ? (int)((this.position.height - Constants.SingleLineHeight) / Constants.SingleLineHeight) : 1;

							GUI.FocusControl(null);

							if (this.EnableCategories == true)
							{
								if (this.SelectedType == null)
								{
									this.scrollbar.Offset = 0F;
									this.scrollWitnessOnce = false;
									this.scrollbar.ClearInterests();
									this.Repaint();

									foreach (var pair in this.categories)
									{
										for (int i = 0; i < pair.Value.Count;)
										{
											this.SelectedType = pair.Value[i];
											return;
										}
									}
								}

								float	shiftOffset = Event.current.keyCode == KeyCode.PageDown ? this.position.height - Constants.SingleLineHeight - Constants.SingleLineHeight : 0F;
								float	offset = 0F;
								float	startOffset = -1F;
								float	lastHeight = 0F;
								Type	lastType = null;

								foreach (var pair in this.categories)
								{
									offset += GeneralStyles.BigCenterText.lineHeight + GeneralStyles.BigCenterText.padding.vertical;

									for (int i = 0; i < pair.Value.Count; i++)
									{
										offset += Constants.SingleLineHeight;

										lastType = pair.Value[i];

										if (startOffset > 0F && offset - startOffset > shiftOffset)
										{
											if (i == 0)
												lastHeight += GeneralStyles.BigCenterText.lineHeight + GeneralStyles.BigCenterText.padding.vertical;

											lastHeight += Constants.SingleLineHeight;

											lastType = pair.Value[i];
											goto doubleBreak;
										}

										if (pair.Value[i] == this.SelectedType)
											startOffset = offset;
									}
								}

								doubleBreak:

								if (this.SelectedType != lastType)
								{
									this.SelectedType = lastType;
									this.scrollWitnessOnce = false;
									this.scrollbar.ClearInterests();
								}

								if (offset - lastHeight < this.scrollbar.Offset)
									this.scrollbar.Offset = offset - lastHeight;
								else if (offset > this.scrollbar.Offset + this.scrollbar.MaxHeight)
									this.scrollbar.Offset = -this.scrollbar.MaxHeight + offset;

								this.Repaint();

								return;
							}
							else if (string.IsNullOrEmpty(this.filter) == true)
							{
								if (this.SelectedType == null && this.types.Length > 0)
								{
									this.scrollbar.Offset = 0F;
									this.SelectedType = this.types[0];
									this.scrollWitnessOnce = false;
									this.scrollbar.ClearInterests();
									this.Repaint();
									return;
								}

								for (int i = 0; i < this.types.Length; i++)
								{
									if (this.types[i] == this.SelectedType)
									{
										i = Mathf.Min(i + shift, this.types.Length - 1);

										if (i * Constants.SingleLineHeight < this.scrollbar.Offset)
											this.scrollbar.Offset = i * Constants.SingleLineHeight;
										else if ((i + 1) * Constants.SingleLineHeight - this.scrollbar.Offset > this.scrollbar.MaxHeight)
											this.scrollbar.Offset = -this.scrollbar.MaxHeight + (i + 1) * Constants.SingleLineHeight;

										this.SelectedType = this.types[i];
										this.scrollWitnessOnce = false;
										this.scrollbar.ClearInterests();
										this.Repaint();
										return;
									}
								}
							}
							else
							{
								if (this.SelectedType == null && this.displayingTypes.Count > 0)
								{
									this.scrollbar.Offset = 0F;
									this.SelectedType = this.displayingTypes[0];
									this.scrollWitnessOnce = false;
									this.scrollbar.ClearInterests();
									this.Repaint();
									return;
								}

								for (int i = 0; i < this.displayingTypes.Count; i++)
								{
									if (this.displayingTypes[i] == this.SelectedType)
									{
										i = Mathf.Min(i + shift, this.displayingTypes.Count - 1);

										if (i * Constants.SingleLineHeight < this.scrollbar.Offset)
											this.scrollbar.Offset = i * Constants.SingleLineHeight;
										else if ((i + 1) * Constants.SingleLineHeight - this.scrollbar.Offset > this.scrollbar.MaxHeight)
											this.scrollbar.Offset = -this.scrollbar.MaxHeight + (i + 1) * Constants.SingleLineHeight;

										this.SelectedType = this.displayingTypes[i];
										this.scrollWitnessOnce = false;
										this.scrollbar.ClearInterests();
										this.Repaint();
										return;
									}
								}
							}
						}
					}

					GUI.SetNextControlName(GenericTypesSelectorWizard.TextControlName);
					this.filter = GUILayout.TextField(this.filter, GeneralStyles.ToolbarSearchTextField);
					if (GUILayout.Button(GUIContent.none, GeneralStyles.ToolbarSearchCancelButton) == true)
						this.filter = string.Empty;
				}
				if (EditorGUI.EndChangeCheck() == true)
				{
					this.scrollWitnessOnce = false;
					this.scrollbar.ClearInterests();

					if (this.EnableCategories == true)
					{
						this.categories.Clear();

						this.ProcessMaxNamespaceWidth(this.types);

						for (int i = 0; i < this.types.Length; i++)
						{
							if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(this.types[i].FullName, this.filter, CompareOptions.IgnoreCase) == -1)
								continue;

							this.AddTypeToCategory(this.types[i]);
						}
					}
					else if (string.IsNullOrEmpty(this.filter) == false)
					{
						this.temporaryFilterTypes.Clear();

						for (int i = 0; i < this.types.Length; i++)
						{
							if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(this.types[i].FullName, this.filter, CompareOptions.IgnoreCase) != -1)
								this.temporaryFilterTypes.Add(this.types[i]);
						}

						if (this.temporaryFilterTypes.Count > 0)
						{
							List<Type>	tmp = this.displayingTypes;
							this.displayingTypes = this.temporaryFilterTypes;
							this.temporaryFilterTypes = tmp;
							// Add a fake value to avoid the input filter turning red.
							this.temporaryFilterTypes.Add(null);
							this.ProcessMaxNamespaceWidth(this.displayingTypes);
						}
						else
						{
							if (this.displayingTypes.Count == 0)
								this.displayingTypes.AddRange(this.types);

							this.ProcessMaxNamespaceWidth(this.displayingTypes);
						}
					}
					else
					{
						this.displayingTypes.Clear();
						this.namespaceWidth = this.allNamespaceWidth;
						this.nameWidth = this.allNameWidth;
						this.ResizeWindow();
					}
				}

				EditorGUI.BeginDisabledGroup(this.SelectedType == null && this.EnableNullValue == false);
				{
					if (GUILayout.Button(LC.G("Select"), GeneralStyles.ToolbarButton, GUILayoutOptionPool.ExpandWidthFalse) == true)
						this.Create(this.SelectedType);
				}
				EditorGUI.EndDisabledGroup();
			}
			GUILayout.EndHorizontal();

			Rect	r = GUILayoutUtility.GetRect(0F, Constants.SingleLineHeight);
			float	rowHeight = r.height;

			if (Event.current.type == EventType.Repaint)
			{
				float	totalHeight = 0F;

				if (this.EnableNullValue == true)
					totalHeight = rowHeight;

				if (this.EnableCategories == true)
				{
					float	categoryHeight = GeneralStyles.BigCenterText.lineHeight + GeneralStyles.BigCenterText.padding.vertical;

					foreach (var pair in this.categories)
						totalHeight += categoryHeight + (pair.Value.Count * rowHeight);
				}
				else if (string.IsNullOrEmpty(this.filter) == true)
					totalHeight += this.types.Length * rowHeight;
				else
					totalHeight += this.displayingTypes.Count * rowHeight;

				this.scrollbar.RealHeight = totalHeight;
				this.scrollbar.SetPosition(this.position.width - 15F, r.y);
				this.scrollbar.SetSize(this.position.height - r.y);
			}

			this.scrollbar.OnGUI();

			Rect	bodyRect = this.position;
			bodyRect.x = 0F;
			bodyRect.y = r.y;
			bodyRect.height -= r.y;

			GUI.BeginGroup(bodyRect);
			{
				r.y = -this.scrollbar.Offset;
				r.width -= this.scrollbar.MaxWidth;

				if (this.EnableNullValue == true)
				{
					this.DrawType(r, null, string.Empty, "Null");
					r.y += r.height;
				}

				if (this.EnableCategories == true)
				{
					float	categoryHeight = GeneralStyles.BigCenterText.lineHeight + GeneralStyles.BigCenterText.padding.vertical;

					foreach (var pair in this.categories)
					{
						r.height = categoryHeight;
						GUI.Label(r, pair.Key, GeneralStyles.BigCenterText);
						r.y += r.height;

						r.height = rowHeight;

						for (int i = 0; i < pair.Value.Count; i++)
						{
							if (this.scrollWitnessOnce == true && r.y + r.height <= 0)
							{
								r.y += r.height;
								continue;
							}

							this.DrawType(r, pair.Value[i], pair.Value[i].Namespace, pair.Value[i].Name);

							r.y += r.height;

							if (this.scrollWitnessOnce == true && r.y > this.scrollbar.MaxHeight)
								break;
						}
					}
				}
				else if (string.IsNullOrEmpty(this.filter) == true)
				{
					for (int i = 0; i < this.types.Length; i++)
					{
						if (this.scrollWitnessOnce == true && r.y + r.height <= 0)
						{
							r.y += r.height;
							continue;
						}

						this.DrawType(r, this.types[i], this.types[i].Namespace, this.types[i].Name);

						r.y += r.height;

						if (this.scrollWitnessOnce == true && r.y > this.scrollbar.MaxHeight)
							break;
					}
				}
				else
				{
					for (int i = 0; i < this.displayingTypes.Count; i++)
					{
						if (this.scrollWitnessOnce == true && r.y + r.height <= 0)
						{
							r.y += r.height;
							continue;
						}

						this.DrawType(r, this.displayingTypes[i], this.displayingTypes[i].Namespace, this.displayingTypes[i].Name);

						r.y += r.height;

						if (this.scrollWitnessOnce == true && r.y > this.scrollbar.MaxHeight)
							break;
					}
				}
			}
			GUI.EndGroup();
		}

		protected virtual void	Update()
		{
			if (EditorApplication.isCompiling == true)
				this.Close();
		}

		protected virtual void	OnLostFocus()
		{
			EditorApplication.delayCall += this.Close;
		}

		public void	FilterTypes(Func<Type, bool> predicate)
		{
			List<Type>	filteredTypes = new List<Type>(this.types.Length);

			for (int i = 0; i < this.types.Length; i++)
			{
				if (predicate(this.types[i]) == true)
					filteredTypes.Add(this.types[i]);
			}

			this.types = filteredTypes.ToArray();
		}

		private void	AddTypeToCategory(Type type)
		{
			CategoryAttribute[]	attributes = type.GetCustomAttributes(typeof(CategoryAttribute), true) as CategoryAttribute[];
			List<Type>			cat;
			string				categoryName;

			if (attributes.Length > 0)
				categoryName = attributes[0].name;
			else
				categoryName = CategoryAttribute.DefaultCategory;

			if (this.categories.TryGetValue(categoryName, out cat) == true)
				cat.Add(type);
			else
			{
				cat = new List<Type>();
				cat.Add(type);
				this.categories.Add(categoryName, cat);
			}
		}

		private void	DrawType(Rect r, Type type, string @namespace, string name)
		{
			if (this.scrollWitnessOnce == false &&
				Event.current.type == EventType.Repaint &&
				this.SelectedType == type)
			{
				this.scrollWitnessOnce = true;
				this.scrollbar.ClearInterests();
				this.scrollbar.AddInterest(r.y + (r.height * .5F) + this.scrollbar.Offset, GenericTypesSelectorWizard.SelectedTypeBackgroundColor / GenericTypesSelectorWizard.SelectedTypeBackgroundColor.a);
				this.Repaint();
			}

			if (Event.current.type == EventType.MouseMove && r.Contains(Event.current.mousePosition) == true)
				this.Repaint();

			if (Event.current.type == EventType.Repaint)
			{
				if (this.SelectedType == type)
					EditorGUI.DrawRect(r, GenericTypesSelectorWizard.SelectedTypeBackgroundColor);
				if (r.Contains(Event.current.mousePosition) == true)
					EditorGUI.DrawRect(r, GenericTypesSelectorWizard.MouseHoverBackgroundColor);
			}

			if (GUI.Button(r, @namespace, GUI.skin.label) == true)
			{
				if (this.SelectedType == type)
					this.Create(this.SelectedType);
				else
					this.SelectedType = type;

				this.scrollbar.ClearInterests();
				this.scrollbar.AddInterest(r.y + (r.height * .5F) + this.scrollbar.Offset, GenericTypesSelectorWizard.SelectedTypeBackgroundColor / GenericTypesSelectorWizard.SelectedTypeBackgroundColor.a);
			}

			r.x += this.namespaceWidth;
			r.width -= this.namespaceWidth;

			GUI.Label(r, name);
		}

		private void	ProcessMaxNamespaceWidth(IEnumerable<Type> types)
		{
			this.namespaceWidth = 0F;
			this.nameWidth = 0F;

			foreach (Type type in types)
			{
				Utility.content.text = type.Namespace;
				float	width = GUI.skin.label.CalcSize(Utility.content).x;
				if (this.namespaceWidth < width)
					this.namespaceWidth = width;
			}

			foreach (Type type in types)
			{
				Utility.content.text = type.Name;
				float	width = GUI.skin.label.CalcSize(Utility.content).x;
				if (this.nameWidth < width)
					this.nameWidth = width;
			}

			this.ResizeWindow();
		}

		private void	ResizeWindow()
		{
			// Resize the window if too small.
			if (this.namespaceWidth > 0F && this.position.width != this.namespaceWidth + this.nameWidth + 15F) // Namespace + Name + Scrollbar
				this.position = new Rect(this.position.x, this.position.y, this.namespaceWidth + this.nameWidth + 15F, this.position.height);
		}

		private void	Create(Type type)
		{
			this.OnCreate(type);

			if (this != null && this.closeOnCreate == true)
				this.Close();
		}
	}
}