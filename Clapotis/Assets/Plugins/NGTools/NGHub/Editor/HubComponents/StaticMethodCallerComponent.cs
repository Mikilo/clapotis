using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGHub
{
	[Serializable, Category("Misc")]
	internal sealed class StaticMethodCallerComponent : HubComponent
	{
		[NonSerialized]
		private GUIContent	content = new GUIContent();

		[Exportable]
		public string		alias;
		[Exportable]
		public string		typeString;
		[Exportable]
		public string		methodName;

		[NonSerialized]
		private MethodInfo	methodInfo;
		
		[NonSerialized]
		public string			type;
		[NonSerialized]
		private List<Type>		matchingTypes = new List<Type>();
		[NonSerialized]
		private Type			selectedType;
		[NonSerialized]
		private MethodInfo[]	availableStaticMethods;

		[NonSerialized]
		private Vector2	filterTypesScrollPosition;
		[NonSerialized]
		private Vector2	methodsScrollPosition;

		public	StaticMethodCallerComponent() : base("Call Static Method", true)
		{
		}

		public override void	Init(NGHubWindow hub)
		{
			base.Init(hub);

			if (string.IsNullOrEmpty(this.typeString) == false)
			{
				Type	type = Type.GetType(this.typeString);

				if (type != null && string.IsNullOrEmpty(this.methodName) == false)
					this.methodInfo = type.GetMethod(this.methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
			}
		}

		public override void	OnPreviewGUI(Rect r)
		{
			GUI.Label(r, "Call Static Method \"" + (this.methodInfo != null ? this.methodInfo.Name : "Method Unknown") + "\"");
		}

		public override void	OnEditionGUI()
		{
			using (LabelWidthRestorer.Get(100F))
			{
				if (this.methodInfo != null)
				{
					EditorGUILayout.LabelField("Static Method", this.methodInfo.DeclaringType.Name + ":" + this.methodInfo.Name + "()");
					this.alias = EditorGUILayout.TextField("Alias", this.alias);


					Rect	r2 = GUILayoutUtility.GetLastRect();
					r2.y += r2.height + 3F;
					r2.height = 1F;
					EditorGUI.DrawRect(r2, Color.gray);

					GUILayout.Space(5F);
				}

				Rect	r = GUILayoutUtility.GetRect(0F, Constants.SingleLineHeight, GUI.skin.textField);
				float	width = r.width;

				EditorGUI.BeginChangeCheck();
				this.type = EditorGUI.TextField(r, "Search Type", this.type);
				if (EditorGUI.EndChangeCheck() == true)
				{
					this.RefreshTypes();
					return;
				}

				this.filterTypesScrollPosition = EditorGUILayout.BeginScrollView(this.filterTypesScrollPosition, GUILayoutOptionPool.Height(Mathf.Min(this.matchingTypes.Count * 18F + 10F, 160F)), GUILayoutOptionPool.ExpandHeightFalse);
				{
					for (int i = 0; i < this.matchingTypes.Count; i++)
					{
						if (GUILayout.Button(this.matchingTypes[i].Name) == true)
						{
							this.selectedType = this.matchingTypes[i];
							this.availableStaticMethods = this.selectedType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
							return;
						}
					}
				}
				EditorGUILayout.EndScrollView();

				if (this.availableStaticMethods != null)
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField("Selected Type", this.selectedType.FullName);

					if (this.availableStaticMethods.Length == 0)
						GUILayout.Label("No static method found.");
					else
					{
						this.methodsScrollPosition = EditorGUILayout.BeginScrollView(this.methodsScrollPosition, GUILayoutOptionPool.Height(Mathf.Min(this.availableStaticMethods.Length * 18F + 10F, 160F)), GUILayoutOptionPool.ExpandHeightFalse);
						{
							for (int i = 0; i < this.availableStaticMethods.Length; i++)
							{
								if (GUILayout.Button(this.availableStaticMethods[i].Name) == true)
								{
									this.methodInfo = this.availableStaticMethods[i];
									this.typeString = this.selectedType.AssemblyQualifiedName;
									this.methodName = this.methodInfo.Name;
									return;
								}
							}
						}
						EditorGUILayout.EndScrollView();
					}
				}
			}
		}

		public override void	OnGUI()
		{
			using (ColorContentRestorer.Get(this.methodInfo == null, Color.red))
			{
				if (string.IsNullOrEmpty(this.alias) == false)
					this.content.text = this.alias;
				else
					this.content.text = this.methodInfo != null ? this.methodInfo.Name : "Method Unknown";

				if (GUILayout.Button(this.content, GUILayoutOptionPool.Height(this.hub.height)) == true)
					this.methodInfo.Invoke(null, null);
			}
		}

		private void	RefreshTypes()
		{
			this.matchingTypes.Clear();

			if (this.type.Length > 1)
			{
				foreach (Type type in Utility.EachAllSubClassesOf(typeof(object), (t) => t.Name.IndexOf(this.type, StringComparison.OrdinalIgnoreCase) != -1))
					this.matchingTypes.Add(type);
			}
		}
	}
}