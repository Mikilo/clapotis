using NGTools.NGRemoteScene;
using System;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(EnumHandler))]
	internal sealed class EnumDrawer : TypeHandlerDrawer
	{
		private BgColorContentAnimator	anim;

		public	EnumDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			string	path = data.GetPath();
			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
				this.anim.Start();

			EnumInstance	enumInstance = data.Value as EnumInstance;

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				EnumData	enumData = data.Inspector.Hierarchy.GetEnumData(enumInstance.type);

				if (enumData != null)
				{
					if (enumInstance.GetFlag() == EnumInstance.IsFlag.Unset)
					{
						if (enumData.hasFlagAttribute == true)
							enumInstance.SetFlag(EnumInstance.IsFlag.Flag);
						else
							enumInstance.SetFlag(EnumInstance.IsFlag.Value);
					}

					float	width = r.width;

					r.width = 16F;
					r.x += (EditorGUI.indentLevel - 1) * 15F;
					if (GUI.Button(r, "F", enumInstance.GetFlag() == EnumInstance.IsFlag.Flag ? GUI.skin.button : GUI.skin.textField) == true)
					{
						if (enumInstance.GetFlag() == EnumInstance.IsFlag.Value)
							enumInstance.SetFlag(EnumInstance.IsFlag.Flag);
						else
							enumInstance.SetFlag(EnumInstance.IsFlag.Value);
					}
					r.x -= (EditorGUI.indentLevel - 1) * 15F;

					r.width = width;

					EditorGUI.BeginChangeCheck();

					int	newValue;

					if (enumInstance.GetFlag() == EnumInstance.IsFlag.Value)
						newValue = EditorGUI.IntPopup(r, data.Name, enumInstance.value, enumData.names, enumData.values);
					else
						newValue = EditorGUI.MaskField(r, data.Name, enumInstance.value, enumData.names);

					if (EditorGUI.EndChangeCheck() == true &&
						this.AsyncUpdateCommand(data.unityData, path, newValue, typeof(Enum)) == true)
					{
						data.unityData.RecordChange(path, typeof(Enum), data.Value, newValue);
					}
				}
				else
				{
					EditorGUI.LabelField(r, data.Name, LC.G("NGInspector_NotAvailableYet"));

					if (data.Inspector.Hierarchy.IsChannelBlocked(enumInstance.type.GetHashCode()) == true)
						GUI.Label(r, GeneralStyles.StatusWheel);
				}
			}
		}
	}
}