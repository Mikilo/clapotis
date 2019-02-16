using NGTools;
using NGTools.Network;
using NGTools.NGRemoteScene;
using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	[TypeHandlerDrawerFor(typeof(UnityObjectHandler))]
	internal sealed class UnityObjectDrawer : TypeHandlerDrawer
	{
		public const float	PickerButtonWidth = 20F;

		private static Vector2	dragOriginPosition;

		private BgColorContentAnimator	anim;
		private double					lastClick;

		private static IUnityData	unityData;
		private static UnityObject	currentUnityObject;

		public	UnityObjectDrawer(TypeHandler typeHandler) : base(typeHandler)
		{
		}

		public override void	Draw(Rect r, DataDrawer data)
		{
			string		path = data.GetPath();
			UnityObject	unityObject = data.Value as UnityObject;
			int			controlID = GUIUtility.GetControlID("NGObjectFieldHash".GetHashCode(), FocusType.Keyboard, r);

			if (Event.current.type == EventType.KeyDown &&
				Event.current.keyCode == KeyCode.Delete &&
				GUIUtility.keyboardControl == controlID)
			{
				UnityObject	nullObject = new UnityObject(unityObject.type, 0);

				data.unityData.RecordChange(path, unityObject.type, unityObject, nullObject);
				data.unityData.AddPacket(new ClientUpdateFieldValuePacket(path, this.typeHandler.Serialize(nullObject.type, nullObject), this.typeHandler), p =>
				{
					if (p.CheckPacketStatus() == true)
					{
						ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);
						UnityObject	newValue = this.typeHandler.Deserialize(Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue), unityObject.type) as UnityObject;

						unityObject.Assign(newValue.type, newValue.gameObjectInstanceID, newValue.instanceID, newValue.name);
						Utility.RestoreBBuffer(buffer);
					}
				});

				Event.current.Use();
			}

			if (r.Contains(Event.current.mousePosition) == true)
			{
				if (Event.current.type == EventType.MouseDown)
				{
					if (Event.current.button == 1 && unityObject.instanceID != 0)
					{
						UnityObjectDrawer.unityData = data.unityData;
						UnityObjectDrawer.currentUnityObject = unityObject;

						GenericMenu	menu = new GenericMenu();

						menu.AddItem(new GUIContent("Import asset"), false, this.ImportAsset);

						menu.ShowAsContext();
					}
					else
					{
						UnityObjectDrawer.dragOriginPosition = Event.current.mousePosition;

						// Initialize drag data.
						DragAndDrop.PrepareStartDrag();

						DragAndDrop.objectReferences = new Object[0];
						DragAndDrop.SetGenericData("r", unityObject);
					}
				}
				else if (Event.current.type == EventType.MouseDrag && (UnityObjectDrawer.dragOriginPosition - Event.current.mousePosition).sqrMagnitude >= Constants.MinStartDragDistance)
				{
					DragAndDrop.StartDrag("Dragging Game Object");
					Event.current.Use();
				}
				else if (Event.current.type == EventType.DragUpdated)
				{
					UnityObject	dragItem = DragAndDrop.GetGenericData("r") as UnityObject;

					if (dragItem != null && dragItem.instanceID != unityObject.instanceID &&
						(dragItem.type == null || unityObject.type == null || unityObject.type.IsAssignableFrom(dragItem.type) == true))
					{
						DragAndDrop.visualMode = DragAndDropVisualMode.Move;
					}
					else
						DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

					Event.current.Use();
				}
				else if (Event.current.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();

					UnityObject	dragItem = DragAndDrop.GetGenericData("r") as UnityObject;

					data.unityData.RecordChange(path, unityObject.type, unityObject, dragItem);
					data.unityData.AddPacket(new ClientUpdateFieldValuePacket(path, this.typeHandler.Serialize(dragItem.type, dragItem), this.typeHandler), p =>
					{
						if (p.CheckPacketStatus() == true)
						{
							ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);
							UnityObject	newValue = this.typeHandler.Deserialize(Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue), unityObject.type) as UnityObject;

							unityObject.Assign(newValue.type, newValue.gameObjectInstanceID, newValue.instanceID, newValue.name);
							Utility.RestoreBBuffer(buffer);
						}
					});
				}
				else if (Event.current.type == EventType.Repaint &&
						 DragAndDrop.visualMode == DragAndDropVisualMode.Move)
				{
					Rect	r2 = r;

					r2.width += r2.x;
					r2.x = 0F;

					EditorGUI.DrawRect(r2, Color.yellow);
				}
			}

			float	x = r.x;
			float	width = r.width;

			r.width = UnityObjectDrawer.PickerButtonWidth;
			r.x = width - UnityObjectDrawer.PickerButtonWidth;

			if (Event.current.type == EventType.MouseDown &&
				r.Contains(Event.current.mousePosition) == true)
			{
				UnityObjectDrawer.unityData = data.unityData;
				UnityObjectDrawer.currentUnityObject = unityObject;
				data.Inspector.Hierarchy.PickupResource(unityObject.type, path, UnityObjectDrawer.CreatePacket, UnityObjectDrawer.OnFieldUpdated, unityObject.instanceID);
				Event.current.Use();
			}

			r.width = width;
			r.x = x;

			if (this.anim == null)
				this.anim = new BgColorContentAnimator(data.Inspector.Repaint, 1F, 0F);

			if (data.Inspector.Hierarchy.GetUpdateNotification(path) != NotificationPath.None)
				this.anim.Start();

			using (this.anim.Restorer(0F, .8F + this.anim.Value, 0F, 1F))
			{
				Utility.content.text = data.Name;

				Rect	prefixRect = EditorGUI.PrefixLabel(r, Utility.content);

				if (unityObject.instanceID != 0)
					Utility.content.text = unityObject.name + " (" + unityObject.type.Name + ")";
				else
					Utility.content.text = "None (" + unityObject.type.Name + ")";

				if (GUI.Button(prefixRect, GUIContent.none, GUI.skin.label) == true)
				{
					GUIUtility.keyboardControl = controlID;

					if (unityObject.instanceID != 0 &&
						typeof(Object).IsAssignableFrom(unityObject.type) == true)
					{
						if (this.lastClick + Constants.DoubleClickTime < Time.realtimeSinceStartup)
							data.Inspector.Hierarchy.PingObject(unityObject.gameObjectInstanceID);
						else
							data.Inspector.Hierarchy.SelectGameObject(unityObject.gameObjectInstanceID);

						this.lastClick = Time.realtimeSinceStartup;
					}
				}

				if (Event.current.type == EventType.Repaint)
					GeneralStyles.UnityObjectPicker.Draw(prefixRect, Utility.content, controlID);
			}
		}

		private void	ImportAsset()
		{
			UnityObjectDrawer.unityData.ImportAsset(null, UnityObjectDrawer.currentUnityObject.type, UnityObjectDrawer.currentUnityObject.instanceID);
		}

		private static Packet	CreatePacket(string valuePath, byte[] rawValue)
		{
			unityData.RecordChange(valuePath, typeof(UnityObject), UnityObjectDrawer.currentUnityObject, rawValue);
			return new ClientUpdateFieldValuePacket(valuePath, rawValue, TypeHandlersManager.GetTypeHandler<UnityObject>());
		}

		private static void	OnFieldUpdated(ResponsePacket p)
		{
			if (p.CheckPacketStatus() == true)
			{
				ByteBuffer	buffer = Utility.GetBBuffer((p as ServerUpdateFieldValuePacket).rawValue);
				UnityObject	unityObject = (UnityObject)TypeHandlersManager.GetTypeHandler<UnityObject>().Deserialize(buffer, typeof(UnityObject));

				UnityObjectDrawer.currentUnityObject.Assign(unityObject.type, unityObject.gameObjectInstanceID, unityObject.instanceID, unityObject.name);
				Utility.RestoreBBuffer(buffer);
			}
		}
	}
}