using NGTools.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace NGTools.NGRemoteScene
{
	using UnityEngine;

	internal sealed class ServerSceneExecuter
	{
		private static readonly PropertyInfo	hdr = typeof(Camera).GetProperty("hdr") ?? typeof(Camera).GetProperty("allowHDR");

		private NGServerScene		ssm;
		private List<GameObject>	cameraRaycastResult = new List<GameObject>();

		public	ServerSceneExecuter(PacketExecuter executer, NGServerScene ssm)
		{
			this.ssm = ssm;

			// NG Hierarchy
			executer.HandlePacket(PacketId.ClientHasDisconnect, this.Handle_Scene_ClientIsDisconnected);
			executer.HandlePacket(PacketId.ClientSendPing, this.Handle_ClientSendPing);

			executer.HandlePacket(RemoteScenePacketId.Scene_ClientRequestHierarchy, this.Handle_Scene_ClientRequestHierarchy);
			executer.HandlePacket(RemoteScenePacketId.Unity_ClientRequestLayers, this.Handle_Scene_ClientRequestLayers);
			executer.HandlePacket(RemoteScenePacketId.Asset_ClientRequestResources, this.Handle_Scene_ClientRequestResources);
			executer.HandlePacket(RemoteScenePacketId.Transform_ClientSetSibling, this.Handle_Scene_ClientSetSibling);
			executer.HandlePacket(RemoteScenePacketId.Scene_ClientSetActiveScene, this.Handle_Scene_ClientSetActiveScene);
			executer.HandlePacket(RemoteScenePacketId.Asset_ClientRequestRawAsset, this.Handle_Scene_ClientRequestRawAsset);

			// NG Inspector
			executer.HandlePacket(RemoteScenePacketId.GameObject_ClientRequestGameObjectData, this.Handle_Scene_ClientRequestGameObjectData);
			executer.HandlePacket(RemoteScenePacketId.Class_ClientUpdateFieldValue, this.Handle_Scene_ClientUpdateFieldValue);

			executer.HandlePacket(RemoteScenePacketId.Component_ClientInvokeBehaviourMethod, this.Handle_Scene_ClientInvokeBehaviourMethod);

			executer.HandlePacket(RemoteScenePacketId.GameObject_ClientWatchGameObjects, this.Handle_Scene_ClientWatchGameObjects);
			executer.HandlePacket(RemoteScenePacketId.Material_ClientWatchMaterials, this.Handle_Scene_ClientWatchMaterials);
			executer.HandlePacket(RemoteScenePacketId.GameObject_ClientDeleteGameObjects, this.Handle_Scene_ClientDeleteGameObjects);
			executer.HandlePacket(RemoteScenePacketId.Component_ClientDeleteComponents, this.Handle_Scene_ClientDeleteComponents);

			executer.HandlePacket(RemoteScenePacketId.Material_ClientRequestMaterialData, this.Handle_Scene_ClientRequestMaterialData);
			executer.HandlePacket(RemoteScenePacketId.Material_ClientUpdateMaterialProperty, this.Handle_Scene_ClientUpdateMaterialProperty);
			executer.HandlePacket(RemoteScenePacketId.Material_ClientUpdateMaterialVector2, this.Handle_Scene_ClientUpdateMaterialVector2);
			executer.HandlePacket(RemoteScenePacketId.Material_ClientChangeMaterialShader, this.Handle_Scene_ClientChangeMaterialShader);

			executer.HandlePacket(RemoteScenePacketId.Unity_ClientRequestEnumData, this.Handle_Scene_ClientRequestEnumData);
			executer.HandlePacket(RemoteScenePacketId.Class_ClientLoadBigArray, this.Handle_Scene_ClientLoadBigArray);

			executer.HandlePacket(RemoteScenePacketId.Asset_ClientRequestUserAssets, this.Handle_Scene_ClientRequestUserAssets);
			executer.HandlePacket(RemoteScenePacketId.Asset_ClientSendUserTexture2D, this.Handle_Scene_ClientSendUserTexture2D);
			executer.HandlePacket(RemoteScenePacketId.Asset_ClientSendUserSprite, this.Handle_Scene_ClientSendUserSprite);

			executer.HandlePacket(RemoteScenePacketId.Unity_ClientRequestAllComponentTypes, this.Handle_Scene_ClientRequestAllComponentTypes);
			executer.HandlePacket(RemoteScenePacketId.GameObject_ClientAddComponent, this.Handle_Scene_ClientAddComponent);

			// NG Project
			executer.HandlePacket(RemoteScenePacketId.Asset_ClientRequestProject, this.Handle_Scene_ClientRequestProject);

			// NG Camera
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientConnect, this.Handle_Camera_ClientConnect);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientDisconnect, this.Handle_Camera_ClientDisconnect);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientRequestAllCameras, this.Handle_Camera_ClientRequestAllCameras);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientPickCamera, this.Handle_Camera_ClientPickCamera);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientPickGhostCameraAtCamera, this.Handle_Camera_ClientPickGhostCameraAtCamera);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientSetSetting, this.Handle_Camera_ClientSetSetting);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientSendCameraInput, this.Handle_Camera_ClientSendCameraInput);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientSendCameraTransformPosition, this.Handle_Camera_ClientSendCameraTransformPosition);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientSendCameraTransformRotation, this.Handle_Camera_ClientSendCameraTransformRotation);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientSendCameraZoom, this.Handle_Camera_ClientSendCameraZoom);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientRaycastScene, this.Handle_Camera_ClientRaycastScene);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientToggleModule, this.Handle_Camera_ClientToggleModule);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientStickGhostCamera, this.Handle_Camera_ClientStickGhostCamera);

			executer.HandlePacket(RemoteScenePacketId.Camera_ClientSetDefaultGhostCamera, this.Handle_Camera_ClientSetDefaultGhostCamera);
			executer.HandlePacket(RemoteScenePacketId.Camera_ClientSetGhostCamera, this.Handle_Camera_ClientSetGhostCamera);

			executer.HandlePacket(RemoteScenePacketId.Camera_ClientResetCameraSettings, this.Handle_Camera_ClientResetCameraSettings);

			// NG Static Inspector
			executer.HandlePacket(RemoteScenePacketId.StaticClass_ClientRequestInspectableTypes, this.Handle_Scene_ClientRequestInspectableTypes);
			executer.HandlePacket(RemoteScenePacketId.StaticClass_ClientRequestTypeStaticMembers, this.Handle_Scene_ClientRequestTypeStaticMembers);
			executer.HandlePacket(RemoteScenePacketId.StaticClass_ClientWatchTypes, this.Handle_Scene_ClientWatchTypes);
		}

		private void	Handle_Scene_ClientIsDisconnected(Client sender, Packet _packet)
		{
			// Clean data from client.

			NGServerCamera	cam;
			if (this.NGGhostCams.TryGetValue(sender, out cam) == true)
			{
				this.NGGhostCams.Remove(sender);
				Object.DestroyImmediate(cam.gameObject);
			}
		}

		private void	Handle_ClientSendPing(Client sender, Packet _packet)
		{
			sender.AddPacket(new ServerAnswerPingPacket(_packet.NetworkId));
		}

		private void	Handle_Scene_ClientRequestHierarchy(Client sender, Packet _packet)
		{
			using (var h = ResponsePacketHandler.Get<ServerSendHierarchyPacket>(sender, _packet as ClientRequestHierarchyPacket))
			{
				try
				{
					h.response.serverScenes = this.ssm.ScanHierarchy();
					h.response.activeScene = SceneManager.GetActiveScene().buildIndex;
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientRequestLayers(Client sender, Packet _packet)
		{
			sender.AddPacket(new ServerSendLayersPacket(_packet.NetworkId));
		}

		private void	Handle_Scene_ClientRequestResources(Client sender, Packet _packet)
		{
			ClientRequestResourcesPacket	packet = _packet as ClientRequestResourcesPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendResourcesPacket>(sender, packet))
			{
				try
				{
					h.response.type = packet.type;
					this.ssm.GetResources(packet.type, packet.forceRefresh, out h.response.resourceNames, out h.response.instanceIDs);
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientSetSibling(Client sender, Packet _packet)
		{
			ClientSetSiblingPacket	packet = _packet as ClientSetSiblingPacket;

			using (var h = ResponsePacketHandler.Get<ServerSetSiblingPacket>(sender, packet))
			{
				try
				{
					if (this.ssm.SetSibling(packet.instanceID, packet.instanceIDParent, packet.siblingIndex) == false)
						h.Throw(Errors.Server_GameObjectNotFound, this.PrepareGameObjectNotFoundMessage(packet, packet.instanceID + " or " + packet.instanceIDParent));

					h.response.instanceID = packet.instanceID;
					h.response.instanceIDParent = packet.instanceIDParent;
					h.response.siblingIndex = packet.siblingIndex;
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientSetActiveScene(Client sender, Packet _packet)
		{
			ClientSetActiveScenePacket	packet = _packet as ClientSetActiveScenePacket;

			using (var h = ResponsePacketHandler.Get<ServerSetActiveScenePacket>(sender, packet))
			{
				try
				{
					switch (this.ssm.SetActiveScene(packet.index))
					{
						case ReturnSetActiveScene.Success:
							h.response.index = packet.index;
							break;
						case ReturnSetActiveScene.InternalError:
							h.Throw(Errors.InternalServerError, this.PrepareInternalErrorMessage(packet));
							break;
						case ReturnSetActiveScene.OutOfRange:
							h.Throw(Errors.Scene_SceneOutOfRange, "Scene " + packet.index + " is out of range.");
							break;
						case ReturnSetActiveScene.NotLoaded:
							h.Throw(Errors.Scene_SceneNotLoaded, "Scene " + packet.index + " is not loaded yet.");
							break;
						case ReturnSetActiveScene.AlreadyActive:
							h.Throw(Errors.Scene_SceneAlreadyActive, "Scene " + packet.index + " is already active.");
							break;
						case ReturnSetActiveScene.Invalid:
							h.Throw(Errors.Scene_SceneInvalid, "Scene " + packet.index + " is invalid.");
							break;
					}
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientRequestRawAsset(Client sender, Packet _packet)
		{
			ClientRequestRawAssetPacket	packet = _packet as ClientRequestRawAssetPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendRawAssetPacket>(sender, packet))
			{
				try
				{
					Type		realType;
					byte[]		data;
					Exception	exception;

					switch (this.ssm.GetRawDataFromObject(packet.type, packet.instanceID, out realType, out data, out exception))
					{
						case ReturnRawDataFromObject.Success:
							h.response.realType = realType ?? packet.type;
							h.response.data = data;
							break;
						case ReturnRawDataFromObject.AssetNotFound:
							h.Throw(Errors.Server_AssetNotFound, "Asset " + packet.type + " " + packet.instanceID + " was not found.");
							break;
						case ReturnRawDataFromObject.ExportThrownException:
							h.Throw(Errors.ServerException, exception.ToString());
							break;
					}
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientRequestGameObjectData(Client sender, Packet _packet)
		{
			ClientRequestGameObjectDataPacket	packet = _packet as ClientRequestGameObjectDataPacket;
			ErrorNotificationPacket				errorPacket = null;
			NotifyGameObjectsDeletedPacket		deletePacket = null;

			using (var h = ResponsePacketHandler.Get<ServerSendGameObjectDataPacket>(sender, packet))
			{
				try
				{
					for (int i = 0; i < packet.gameObjectInstanceIDs.Count; i++)
					{
						try
						{
							ServerGameObject	gameObject = this.ssm.GetGameObject(packet.gameObjectInstanceIDs[i]);

							if (gameObject != null)
								h.response.serverGameObjects.Add(gameObject);
							else
							{
								if (errorPacket == null)
									errorPacket = new ErrorNotificationPacket();

								errorPacket.errors.Add(Errors.Server_GameObjectNotFound);
								errorPacket.messages.Add(this.PrepareGameObjectNotFoundMessage(packet, packet.gameObjectInstanceIDs[i].ToString()));

								if (deletePacket == null)
									deletePacket = new NotifyGameObjectsDeletedPacket();

								deletePacket.instanceIDs.Add(packet.gameObjectInstanceIDs[i]);
							}
						}
						catch (Exception ex)
						{
							if (errorPacket == null)
								errorPacket = new ErrorNotificationPacket();

							errorPacket.errors.Add(Errors.ServerException);
							errorPacket.messages.Add(this.PrepareExceptionMessage(packet, ex));
						}
					}

					if (errorPacket != null)
					{
						// TODO Handle_ warning error code? One for missing GO, one for Exception + GO
						//if (deletePacket != null)
						//	h.Warning(Errors.Scene_Exception, "");
						//else
						//	h.Warning(Errors.Scene_Exception, "");

						sender.AddPacket(errorPacket);
					}

					if (deletePacket != null)
						sender.AddPacket(deletePacket);
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientUpdateFieldValue(Client sender, Packet _packet)
		{
			ClientUpdateFieldValuePacket	packet = _packet as ClientUpdateFieldValuePacket;

			using (var h = ResponsePacketHandler.Get<ServerUpdateFieldValuePacket>(sender, packet))
			{
				try
				{
					h.response.fieldPath = packet.fieldPath;

					switch (this.ssm.UpdateFieldValue(packet.fieldPath, packet.rawValue, out h.response.rawValue))
					{
						case ReturnUpdateFieldValue.InternalError:
							h.Throw(Errors.InternalServerError, this.PrepareInternalErrorMessage(packet));
							break;
						case ReturnUpdateFieldValue.TypeNotFound:
							h.Throw(Errors.Server_TypeNotFound, this.PrepareTypeNotFoundMessage(packet, this.ExtractGameObjectInstanceID(packet.fieldPath)));
							break;
						case ReturnUpdateFieldValue.GameObjectNotFound:
							h.Throw(Errors.Server_GameObjectNotFound, this.PrepareGameObjectNotFoundMessage(packet, this.ExtractGameObjectInstanceID(packet.fieldPath)));
							break;
						case ReturnUpdateFieldValue.ComponentNotFound:
							h.Throw(Errors.Server_ComponentNotFound, this.PrepareComponentNotFoundMessage(packet, this.ExtractComponentInstanceID(packet.fieldPath)));
							break;
						case ReturnUpdateFieldValue.PathNotResolved:
							h.Throw(Errors.Server_PathNotResolved, this.PreparePathNotResolvedMessage(packet, packet.fieldPath));
							break;
						case ReturnUpdateFieldValue.DisableServerForbidden:
							h.Throw(Errors.Server_DisableServerForbidden, "Server can not disable itself.");
							break;
					}
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientInvokeBehaviourMethod(Client sender, Packet _packet)
		{
			ClientInvokeBehaviourMethodPacket	packet = _packet as ClientInvokeBehaviourMethodPacket;

			using (var h = ResponsePacketHandler.Get<ServerReturnInvokeResultPacket>(sender, packet))
			{
				try
				{
					switch (this.ssm.InvokeComponentMethod(packet.gameObjectInstanceID, packet.componentInstanceID, packet.methodSignature, packet.arguments, ref h.response.result))
					{
						case ReturnInvokeComponentMethod.GameObjectNotFound:
							h.Throw(Errors.Server_GameObjectNotFound, this.PrepareGameObjectNotFoundMessage(packet, packet.gameObjectInstanceID.ToString()));
							break;
						case ReturnInvokeComponentMethod.ComponentNotFound:
							h.Throw(Errors.Server_ComponentNotFound, this.PrepareComponentNotFoundMessage(packet, packet.componentInstanceID.ToString()));
							break;
						case ReturnInvokeComponentMethod.MethodNotFound:
							h.Throw(Errors.Server_MethodNotFound, this.PrepareMethodNotFoundMessage(packet, packet.methodSignature));
							break;
						case ReturnInvokeComponentMethod.InvalidArgument:
							h.Throw(Errors.Server_InvalidArgument, this.PrepareInvalidArgumentMessage(packet));
							break;
						case ReturnInvokeComponentMethod.InvocationFailed:
							h.Throw(Errors.Server_InvocationFailed, this.PrepareInvocationFailedMessage(packet, packet.methodSignature));
							break;
					}
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientWatchGameObjects(Client sender, Packet _packet)
		{
			ClientWatchGameObjectsPacket	packet = _packet as ClientWatchGameObjectsPacket;

			this.ssm.WatchGameObjects(sender, packet.gameObjectInstanceIDs);
		}

		private void	Handle_Scene_ClientWatchMaterials(Client sender, Packet _packet)
		{
			ClientWatchMaterialsPacket	packet = _packet as ClientWatchMaterialsPacket;

			this.ssm.WatchMaterials(sender, packet.materialInstanceIDs);
		}

		private void	Handle_Scene_ClientDeleteGameObjects(Client sender, Packet _packet)
		{
			ClientDeleteGameObjectsPacket	packet = _packet as ClientDeleteGameObjectsPacket;

			using (var h = ResponsePacketHandler.Get<ServerDeleteGameObjectsPacket>(sender, packet))
			{
				try
				{
					if (this.ssm.DeleteGameObjects(sender, packet.gameObjectInstanceIDs) == ReturnDeleteGameObjects.Incomplete)
						h.Warning(1, "Not all GameObjects have been deleted.");
					h.response.gameObjectInstanceIDs = packet.gameObjectInstanceIDs;
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientDeleteComponents(Client sender, Packet _packet)
		{
			ClientDeleteComponentsPacket	packet = _packet as ClientDeleteComponentsPacket;

			using (var h = ResponsePacketHandler.Get<ServerDeleteComponentsPacket>(sender, packet))
			{
				try
				{
					if (packet.gameObjectInstanceIDs == null || packet.componentInstanceIDs == null)
						h.Throw(Errors.DataCorrupted, "Packet data is corrupted.");

					if (packet.gameObjectInstanceIDs.Count != packet.componentInstanceIDs.Count)
						h.Throw(Errors.DataCorrupted, "Packet instanceIDs are invalid.");

					h.response.gameObjectInstanceIDs = packet.gameObjectInstanceIDs;
					h.response.instanceIDs = packet.componentInstanceIDs;

					if (this.ssm.DeleteComponents(sender, packet.gameObjectInstanceIDs, packet.componentInstanceIDs) == ReturnDeleteComponents.Incomplete)
						h.Warning(Errors.Server_PartialGameObjectDeletion, "Not all Components have been deleted.");
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientRequestMaterialData(Client sender, Packet _packet)
		{
			ClientRequestMaterialDataPacket	packet = _packet as ClientRequestMaterialDataPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendMaterialDataPacket>(sender, packet))
			{
				try
				{
					h.response.serverMaterial = this.ssm.GetResource<Material>(packet.materialInstanceID);
					if (h.response.serverMaterial == null)
						h.Throw(Errors.Server_MaterialNotFound, this.PrepareMaterialNotFoundMessage(packet, packet.materialInstanceID.ToString()));

					h.response.ngShader = this.ssm.GetNGShader(h.response.serverMaterial.shader);
					if (h.response.ngShader == null)
						h.Warning(Errors.Server_NGShaderNotFound, this.PrepareShaderNotFoundMessage(packet, (h.response.serverMaterial.shader != null ? h.response.serverMaterial.shader.ToString() : "NULL")));
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientUpdateMaterialProperty(Client sender, Packet _packet)
		{
			ClientUpdateMaterialPropertyPacket	packet = _packet as ClientUpdateMaterialPropertyPacket;

			using (var h = ResponsePacketHandler.Get<ServerUpdateMaterialPropertyPacket>(sender, packet))
			{
				try
				{
					h.response.materialInstanceID = packet.instanceID;
					h.response.propertyName = packet.propertyName;

					switch (this.ssm.UpdateMaterialProperty(packet.instanceID, packet.propertyName, packet.rawValue, out h.response.rawValue))
					{
						case ReturnUpdateMaterialProperty.InternalError:
							h.Throw(Errors.InternalServerError, this.PrepareInternalErrorMessage(packet));
							break;
						case ReturnUpdateMaterialProperty.MaterialNotFound:
							h.Throw(Errors.Server_MaterialNotFound, this.PrepareMaterialNotFoundMessage(packet, packet.instanceID.ToString()));
							break;
						case ReturnUpdateMaterialProperty.ShaderNotFound:
							Material	material = this.ssm.GetResource<Material>(packet.instanceID);
							h.Throw(Errors.Server_ShaderNotFound, this.PrepareShaderNotFoundMessage(packet, (material.shader != null ? material.shader.ToString() : "NULL")));
							break;
						case ReturnUpdateMaterialProperty.PropertyNotFound:
							h.Throw(Errors.Server_ShaderPropertyNotFound, this.PrepareShaderPropertyNotFoundMessage(packet, packet.propertyName));
							break;
					}
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientUpdateMaterialVector2(Client sender, Packet _packet)
		{
			ClientUpdateMaterialVector2Packet	packet = _packet as ClientUpdateMaterialVector2Packet;

			using (var h = ResponsePacketHandler.Get<ServerUpdateMaterialVector2Packet>(sender, packet))
			{
				try
				{
					h.response.instanceID = packet.instanceID;
					h.response.propertyName = packet.propertyName;
					h.response.type = packet.type;

					if (this.ssm.UpdateMaterialVector2(packet.instanceID, packet.propertyName, packet.value, packet.type, out h.response.value) == ReturnUpdateMaterialVector2.MaterialNotFound)
						h.Throw(Errors.Server_MaterialNotFound, this.PrepareMaterialNotFoundMessage(packet, packet.instanceID.ToString()));
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientChangeMaterialShader(Client sender, Packet _packet)
		{
			ClientChangeMaterialShaderPacket	packet = _packet as ClientChangeMaterialShaderPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendMaterialDataPacket>(sender, packet))
			{
				try
				{
					h.response.serverMaterial = this.ssm.GetResource<Material>(packet.instanceID);

					if (h.response.serverMaterial == null)
						h.Throw(Errors.Server_MaterialNotFound, this.PrepareMaterialNotFoundMessage(packet, packet.instanceID.ToString()));

					Shader	shader = this.ssm.GetResource<Shader>(packet.shaderInstanceID);

					if (shader == null)
						h.Throw(Errors.Server_ShaderNotFound, this.PrepareShaderNotFoundMessage(packet, packet.shaderInstanceID.ToString()));

					h.response.serverMaterial.shader = shader;
					h.response.ngShader = this.ssm.GetNGShader(shader);

					if (h.response.ngShader == null)
						h.Warning(Errors.Server_NGShaderNotFound, this.PrepareShaderNotFoundMessage(packet, (h.response.serverMaterial.shader != null ? h.response.serverMaterial.shader.ToString() : "NULL")));
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientRequestEnumData(Client sender, Packet _packet)
		{
			ClientRequestEnumDataPacket	packet = _packet as ClientRequestEnumDataPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendEnumDataPacket>(sender, packet))
			{
				try
				{
					h.response.Init(packet.type);
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientLoadBigArray(Client sender, Packet _packet)
		{
			ClientLoadBigArrayPacket	packet = _packet as ClientLoadBigArrayPacket;
			IEnumerable					array;
			ReturnGetArray				r = this.ssm.GetArray(packet.arrayPath, out array);

			switch (r)
			{
				case ReturnGetArray.Success:
					if (array != null)
					{
						ArrayHandler	typeHandler = TypeHandlersManager.GetArrayHandler();

						ArrayData.forceBigArray = true;
						sender.AddPacket(new NotifyFieldValueUpdatedPacket(packet.arrayPath, typeHandler.Serialize(array.GetType(), array)));
					}
					break;
				case ReturnGetArray.TypeNotFound:
					sender.AddPacket(new ErrorNotificationPacket(Errors.Server_TypeNotFound, this.PrepareTypeNotFoundMessage(packet, this.ExtractGameObjectInstanceID(packet.arrayPath))));
					break;
				case ReturnGetArray.GameObjectNotFound:
					sender.AddPacket(new ErrorNotificationPacket(Errors.Server_GameObjectNotFound, this.PrepareGameObjectNotFoundMessage(packet, this.ExtractGameObjectInstanceID(packet.arrayPath))));
					break;
				case ReturnGetArray.ComponentNotFound:
					sender.AddPacket(new ErrorNotificationPacket(Errors.Server_ComponentNotFound, this.PrepareComponentNotFoundMessage(packet, this.ExtractComponentInstanceID(packet.arrayPath))));
					break;
				case ReturnGetArray.PathNotResolved:
					sender.AddPacket(new ErrorNotificationPacket(Errors.Server_PathNotResolved, this.PreparePathNotResolvedMessage(packet, packet.arrayPath)));
					break;
			}
		}

		private void	Handle_Scene_ClientRequestUserAssets(Client sender, Packet _packet)
		{
			ClientRequestUserAssetsPacket	packet = _packet as ClientRequestUserAssetsPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendUserAssetsPacket>(sender, packet))
			{
				try
				{
					h.response.type = packet.type;

					foreach (Object item in this.ssm.EachUserAssets(packet.type))
					{
						h.response.names.Add(item.name);
						h.response.instanceIDs.Add(item.GetInstanceID());

						Texture2D	texture = item as Texture2D;
						if (texture != null)
							h.response.data.Add(new string[] { "Width: " + texture.width, "Height: " + texture.height, "Format: " + texture.format });
						else
						{
							Sprite	sprite = item as Sprite;
							if (sprite != null)
								h.response.data.Add(new string[] { "Width: " + sprite.texture.width, "Height: " + sprite.texture.height, "Format: " + sprite.texture.format.ToString(), "PixelsPerUnit: " + sprite.pixelsPerUnit });
						}
					}
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientSendUserTexture2D(Client sender, Packet _packet)
		{
			ClientSendUserTexture2DPacket	packet = _packet as ClientSendUserTexture2DPacket;

			using (var h = ResponsePacketHandler.Get<ServerAcknowledgeUserTexturePacket>(sender, packet))
			{
				try
				{
					Texture2D	userTexture2D = this.ssm.CreateUserTexture2D(packet.name, packet.raw);

					h.response.type = typeof(Texture2D);
					h.response.name = packet.name;
					h.response.instanceID = userTexture2D.GetInstanceID();
					h.response.data = new string[] { "Width: " + userTexture2D.width, "Height: " + userTexture2D.height, "Format: " + userTexture2D.format };
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientSendUserSprite(Client sender, Packet _packet)
		{
			ClientSendUserSpritePacket	packet = _packet as ClientSendUserSpritePacket;

			using (var h = ResponsePacketHandler.Get<ServerAcknowledgeUserTexturePacket>(sender, packet))
			{
				try
				{
					Sprite	userSprite = this.ssm.CreateUserSprite(packet.name, packet.raw, packet.rect, packet.pivot, packet.pixelsPerUnit);

					h.response.type = typeof(Sprite);
					h.response.name = packet.name;
					h.response.instanceID = userSprite.GetInstanceID();
					h.response.data = new string[] { "Width: " + userSprite.texture.width, "Height: " + userSprite.texture.height, "Format: " + userSprite.texture.format.ToString(), "PixelsPerUnit: " + userSprite.pixelsPerUnit };
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientRequestAllComponentTypes(Client sender, Packet _packet)
		{
			sender.AddPacket(new ServerSendAllComponentTypesPacket(_packet.NetworkId));
		}

		private void	Handle_Scene_ClientAddComponent(Client sender, Packet _packet)
		{
			ClientAddComponentPacket	packet = _packet as ClientAddComponentPacket;

			using (var h = ResponsePacketHandler.Get<ServerAddedComponentPacket>(sender, _packet as ClientAddComponentPacket))
			{
				try
				{
					h.response.gameObjectInstanceID = packet.gameObjectInstanceID;

					Type	componentType = Type.GetType(packet.componentType);
					if (componentType == null)
						h.Throw(Errors.Server_TypeNotFound, "Type not found.");

					ServerGameObject	gameObject = this.ssm.GetGameObject(packet.gameObjectInstanceID);
					if (gameObject == null)
						h.Throw(Errors.Server_GameObjectNotFound, "GameObject not found.");

					h.response.serverComponent = gameObject.AddComponent(componentType);
					if (h.response.serverComponent == null)
						h.Throw(Errors.Server_AddingComponentFailed, "Adding Component failed.");
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientRequestInspectableTypes(Client sender, Packet _packet)
		{
			using (var h = ResponsePacketHandler.Get<ServerSendInspectableTypesPacket>(sender, _packet as ClientRequestInspectableTypesPacket))
			{
				try
				{
					h.response.inspectableTypes = this.ssm.LoadInspectableTypes();
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientRequestTypeStaticMembers(Client sender, Packet _packet)
		{
			ClientRequestTypeStaticMembersPacket	packet = _packet as ClientRequestTypeStaticMembersPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendTypeStaticMembersPacket>(sender, packet))
			{
				try
				{
					h.response.typeIndex = packet.typeIndex;
					h.response.members = this.ssm.LoadTypeStaticMembers(packet.typeIndex);
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Scene_ClientWatchTypes(Client sender, Packet _packet)
		{
			ClientWatchTypesPacket	packet = _packet as ClientWatchTypesPacket;

			this.ssm.WatchTypes(sender, packet.typeIndexes);
		}

		private void	Handle_Scene_ClientRequestProject(Client sender, Packet _packet)
		{
			ClientRequestProjectPacket	packet = _packet as ClientRequestProjectPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendProjectPacket>(sender, packet))
			{
				try
				{
					h.response.assets = this.ssm.resources.assets;
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private CameraModulesRunner					modulesRunner;
		private Dictionary<Client, NGServerCamera>	NGGhostCams = new Dictionary<Client, NGServerCamera>();
		private List<CameraServerDataModule>		modules;

		private void	Handle_Camera_ClientDisconnect(Client sender, Packet _packet)
		{
			NGServerCamera	cam;
			bool	mustSucceed = this.NGGhostCams.TryGetValue(sender, out cam);
			InternalNGDebug.Assert(mustSucceed, "Disconnecting NG Camera while the server has not been initialized.");

			cam.enabled = false;

			this.modulesRunner.RemoveClient(sender);
		}

		private void	Handle_Camera_ClientConnect(Client sender, Packet _packet)
		{
			ClientConnectPacket	packet = _packet as ClientConnectPacket;

			using (var h = ResponsePacketHandler.Get<ServerIsInitializedPacket>(sender, packet))
			{
				try
				{
					// Initialize modules.
					if (this.modulesRunner == null)
					{
						GameObject	modulesGameObject = new GameObject("NG Camera Modules");
						this.modules = new List<CameraServerDataModule>();

						this.modulesRunner = modulesGameObject.AddComponent<CameraModulesRunner>();
						this.modulesRunner.modules = this.modules;
						this.modulesRunner.scene = this.ssm;

						foreach (Type type in Utility.EachAllSubClassesOf(typeof(CameraServerDataModule)))
						{
							if (type == typeof(ScreenshotModule))
								continue;

							CameraServerDataModule	module = Activator.CreateInstance(type) as CameraServerDataModule;

							module.Awake(this.ssm);
							this.modules.Add(module);
						}

						this.modulesRunner.Init();
					}

					NGServerCamera	cam;

					if (this.NGGhostCams.TryGetValue(sender, out cam) == false || cam == null)
					{
						GameObject	gameObject = new GameObject("NG Server Camera " + (this.NGGhostCams.Count + 1));
						cam = gameObject.AddComponent<NGServerCamera>();
						cam.scene = this.ssm;
						cam.sender = sender;

						GameObject.DontDestroyOnLoad(gameObject);

						if (this.NGGhostCams.ContainsKey(sender) == true)
							this.NGGhostCams[sender] = cam;
						else
							this.NGGhostCams.Add(sender, cam);
					}

					cam.enabled = true;

					if (packet.overrideResolution == false)
					{
						cam.width = Mathf.CeilToInt(Screen.width * packet.shrinkRatio);
						cam.height = Mathf.CeilToInt(Screen.height * packet.shrinkRatio);
						cam.depth = 24;
						cam.renderTextureFormat = RenderTextureFormat.ARGB32;
					}
					else
					{
						cam.width = packet.width;
						cam.height = packet.height;
						cam.depth = packet.depth;
						cam.renderTextureFormat = packet.format;
					}

					cam.targetRefresh = packet.targetRefresh;
					cam.Init();

					h.response.width = cam.width;
					h.response.height = cam.height;
					h.response.modules = new byte[this.modules.Count];

					for (int i = 0; i < this.modules.Count; i++)
						h.response.modules[i] = this.modules[i].moduleID;

					for (int j = 0; j < packet.modulesAvailable.Length; j++)
						this.modulesRunner.EnableModule(packet.modulesAvailable[j], sender);
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Camera_ClientSetDefaultGhostCamera(Client sender, Packet _packet)
		{
			ClientSetDefaultGhostCameraPacket	packet = _packet as ClientSetDefaultGhostCameraPacket;

			using (var h = ResponsePacketHandler.Get<ServerSetGhostCameraPacket>(sender, packet))
			{
				try
				{
					NGServerCamera	cam;
					if (this.NGGhostCams.TryGetValue(sender, out cam) == false)
						h.Throw(Errors.Camera_NotInitialized, "Setting default ghost camera while the server has not been initialized.");

					Camera	camera = cam.GetComponent<Camera>();

					if (camera == null)
					{
						camera = cam.gameObject.AddComponent<Camera>();

						if (this.ssm.GetResource<Camera>(camera.GetInstanceID()) == null)
							this.ssm.RegisterResource(typeof(Camera), camera);

						if (cam.ghostCamera != null && cam.ghostCamera.gameObject != cam.gameObject)
							GameObject.DestroyImmediate(cam.ghostCamera.gameObject);

						cam.ghostCamera = camera;
						cam.ghostCamera.enabled = false;
						cam.targetCamera = camera;
					}

					h.response.ghostCameraID = camera.GetInstanceID();
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Camera_ClientSetGhostCamera(Client sender, Packet _packet)
		{
			ClientSetGhostCameraPacket	packet = _packet as ClientSetGhostCameraPacket;

			using (var h = ResponsePacketHandler.Get<ServerSetGhostCameraPacket>(sender, packet))
			{
				try
				{
					NGServerCamera	cam;
					if (this.NGGhostCams.TryGetValue(sender, out cam) == false)
						h.Throw(Errors.Camera_NotInitialized, "Setting ghost camera while the server has not been initialized.");

					Camera	camera = this.ssm.GetResource<Camera>(packet.cameraID);

					if (camera == null)
					{
						// TODO Handle_ change scene & resources
						Dictionary<int, Object>	d = this.ssm.RegisterResources(typeof(Camera), true);
						Object	o;
						d.TryGetValue(packet.cameraID, out o);
						camera = o as Camera;
						Debug.Log("Resources reloaded");
					}

					if (camera == null)
						h.Throw(Errors.Camera_CameraNotFound, "Camera #" + packet.cameraID + " was not found.");

					GameObject	clone = GameObject.Instantiate(camera.gameObject);

					GameObject.DontDestroyOnLoad(clone);

					if (this.ssm.GetResource<GameObject>(clone.GetInstanceID()) == null)
						this.ssm.RegisterResource(typeof(GameObject), clone);

					Component[]	components = clone.GetComponents<Component>();

					for (int i = 0; i < components.Length; i++)
					{
						if (packet.componentsIncluded[i] == false)
							Object.DestroyImmediate(components[i]);
						else
						{
							if (this.ssm.GetResource(components[i].GetType(), components[i].GetInstanceID()) == null)
								this.ssm.RegisterResource(components[i].GetType(), components[i]);
						}
					}

					clone.name = "NG Ghost Camera " + (this.NGGhostCams.Count + 1) + " (" + clone.name + ")";
					camera = clone.GetComponent<Camera>();

					if (cam.ghostCamera != null)
					{
						if (cam.ghostCamera.gameObject == cam.gameObject)
							GameObject.DestroyImmediate(cam.ghostCamera);
					}

					cam.ghostCamera = camera;
					cam.ghostCamera.enabled = false;
					cam.targetCamera = camera;

					h.response.ghostCameraID = camera.GetInstanceID();
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Camera_ClientResetCameraSettings(Client sender, Packet _packet)
		{
			ClientResetCameraSettingsPacket	packet = _packet as ClientResetCameraSettingsPacket;

			using (var h = ResponsePacketHandler.Get<ServerResetCameraSettingsPacket>(sender, packet))
			{
				try
				{
					Camera	camera = this.ssm.GetResource<Camera>(packet.cameraID);

					if (camera == null)
						h.Throw(Errors.Camera_CameraNotFound, "Camera #" + packet.cameraID + " was not found.");

					h.response.cameraID = packet.cameraID;
					h.response.clearFlags = (int)camera.clearFlags;
					h.response.background = camera.backgroundColor;
					h.response.cullingMask = camera.cullingMask;
					h.response.projection = camera.orthographic == true ? 1 : 0;
					h.response.fieldOfView = camera.fieldOfView;
					h.response.size = camera.orthographicSize;
					h.response.clippingPlanesNear = camera.nearClipPlane;
					h.response.clippingPlanesFar = camera.farClipPlane;
					h.response.viewportRect = camera.rect;
					h.response.cdepth = camera.depth;
					h.response.renderingPath = (int)camera.renderingPath;
					h.response.occlusionCulling = camera.useOcclusionCulling;
					h.response.HDR = camera.allowHDR;
					h.response.targetDisplay = camera.targetDisplay;
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Camera_ClientRequestAllCameras(Client sender, Packet _packet)
		{
			ClientRequestAllCamerasPacket	packet = _packet as ClientRequestAllCamerasPacket;

			using (var h = ResponsePacketHandler.Get<ServerSendAllCamerasPacket>(sender, packet))
			{
				try
				{
					NGServerCamera	cam;
					if (this.NGGhostCams.TryGetValue(sender, out cam) == false)
						h.Throw(Errors.Camera_NotInitialized, "Requesting all cameras while the server has not been initialized.");

					if (cam.ghostCamera != null)
						h.response.ghostCameraId = cam.ghostCamera.GetInstanceID();
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Camera_ClientPickCamera(Client sender, Packet _packet)
		{
			NGServerCamera	cam;
			bool	mustSucceed = this.NGGhostCams.TryGetValue(sender, out cam);
			InternalNGDebug.Assert(mustSucceed, "Picking a camera while the server has not been initialized.");
			ClientPickCameraPacket	packet = _packet as ClientPickCameraPacket;

			Camera[]	cameras = Resources.FindObjectsOfTypeAll<Camera>();

			for (int i = 0; i < cameras.Length; i++)
			{
				if (cameras[i].GetInstanceID() == packet.cameraID)
				{
					cam.targetCamera = cameras[i];
					break;
				}
			}
		}

		private void	Handle_Camera_ClientPickGhostCameraAtCamera(Client sender, Packet _packet)
		{
			NGServerCamera	cam;
			bool	mustSucceed = this.NGGhostCams.TryGetValue(sender, out cam);
			InternalNGDebug.Assert(mustSucceed, "Picking ghost camera while the server has not been initialized.");
			ClientPickGhostCameraAtCameraPacket	packet = _packet as ClientPickGhostCameraAtCameraPacket;

			Camera[]	cameras = Resources.FindObjectsOfTypeAll<Camera>();

			for (int i = 0; i < cameras.Length; i++)
			{
				if (cameras[i].GetInstanceID() == packet.cameraID)
				{
					cam.ghostCamera.transform.position = cameras[i].transform.position;
					cam.ghostCamera.transform.rotation = cameras[i].transform.rotation;
					cam.targetCamera = cam.ghostCamera;
					break;
				}
			}
		}
		
		private void	Handle_Camera_ClientSetSetting(Client sender, Packet _packet)
		{
			NGServerCamera	cam;
			bool	mustSucceed = this.NGGhostCams.TryGetValue(sender, out cam);
			InternalNGDebug.Assert(mustSucceed, "Picking a camera while the server has not been initialized.");
			ClientSetCameraSettingPacket	packet = _packet as ClientSetCameraSettingPacket;

			if (packet.setting == Setting.TargetRefresh)
				cam.targetRefresh = Mathf.Clamp((int)packet.value, NGServerCamera.TargetRefreshMin, NGServerCamera.TargetRefreshMax);
			else if (packet.setting == Setting.Wireframe)
				cam.wireframe = (bool)packet.value;
			else if (packet.setting == Setting.CameraClearFlags)
				cam.ghostCamera.clearFlags = (CameraClearFlags)packet.value;
			else if (packet.setting == Setting.CameraBackground)
				cam.ghostCamera.backgroundColor = (Color)packet.value;
			else if (packet.setting == Setting.CameraCullingMask)
				cam.ghostCamera.cullingMask = (int)packet.value;
			else if (packet.setting == Setting.CameraProjection)
				cam.ghostCamera.orthographic = (int)packet.value == 1;
			else if (packet.setting == Setting.CameraFieldOfView)
				cam.ghostCamera.fieldOfView = (float)packet.value;
			else if (packet.setting == Setting.CameraSize)
				cam.ghostCamera.orthographicSize = (float)packet.value;
			else if (packet.setting == Setting.CameraClippingPlanesFar)
				cam.ghostCamera.farClipPlane = (float)packet.value;
			else if (packet.setting == Setting.CameraClippingPlanesNear)
				cam.ghostCamera.nearClipPlane = (float)packet.value;
			else if (packet.setting == Setting.CameraViewportRect)
				cam.ghostCamera.rect = (Rect)packet.value;
			else if (packet.setting == Setting.CameraDepth)
				cam.ghostCamera.depth = (float)packet.value;
			else if (packet.setting == Setting.CameraRenderingPath)
				cam.ghostCamera.renderingPath = (RenderingPath)packet.value;
			else if (packet.setting == Setting.CameraOcclusionCulling)
				cam.ghostCamera.useOcclusionCulling = (bool)packet.value;
			else if (packet.setting == Setting.CameraHDR)
				ServerSceneExecuter.hdr.SetValue(cam.ghostCamera, (bool)packet.value); // #UNITY_MULTI_VERSION
			else if (packet.setting == Setting.CameraTargetDisplay)
				cam.ghostCamera.targetDisplay = (int)packet.value;
		}

		private void	Handle_Camera_ClientSendCameraInput(Client sender, Packet _packet)
		{
			NGServerCamera	cam;
			bool	mustSucceed = this.NGGhostCams.TryGetValue(sender, out cam);
			InternalNGDebug.Assert(mustSucceed, "Picking a camera while the server has not been initialized.");
			ClientSendCameraInputPacket	packet = _packet as ClientSendCameraInputPacket;

			cam.moveForward = (packet.directions & (1 << 0)) != 0;
			cam.moveBackward = (packet.directions & (1 << 1)) != 0;
			cam.moveLeft = (packet.directions & (1 << 2)) != 0;
			cam.moveRight = (packet.directions & (1 << 3)) != 0;
			cam.moveSpeed = packet.speed;
		}

		private void	Handle_Camera_ClientSendCameraTransformPosition(Client sender, Packet _packet)
		{
			NGServerCamera	cam;
			bool	mustSucceed = this.NGGhostCams.TryGetValue(sender, out cam);
			InternalNGDebug.Assert(mustSucceed, "Moving the camera while the server has not been initialized.");
			ClientSendCameraTransformPositionPacket	packet = _packet as ClientSendCameraTransformPositionPacket;

			cam.SetTransformPosition(packet.position);
		}

		private void	Handle_Camera_ClientSendCameraTransformRotation(Client sender, Packet _packet)
		{
			NGServerCamera	cam;
			bool	mustSucceed = this.NGGhostCams.TryGetValue(sender, out cam);
			InternalNGDebug.Assert(mustSucceed, "Rotating the camera while the server has not been initialized.");
			ClientSendCameraTransformRotationPacket	packet = _packet as ClientSendCameraTransformRotationPacket;

			cam.SetTransformRotation(packet.rotation);
		}

		private void	Handle_Camera_ClientSendCameraZoom(Client sender, Packet _packet)
		{
			NGServerCamera	cam;
			bool	mustSucceed = this.NGGhostCams.TryGetValue(sender, out cam);
			InternalNGDebug.Assert(mustSucceed, "Zooming the camera while the server has not been initialized.");
			ClientSendCameraZoomPacket	packet = _packet as ClientSendCameraZoomPacket;

			cam.Zoom(packet.factor);
		}

		private void	Handle_Camera_ClientRaycastScene(Client sender, Packet _packet)
		{
			ClientRaycastScenePacket	packet = _packet as ClientRaycastScenePacket;

			using (var h = ResponsePacketHandler.Get<ServerSendRaycastResultPacket>(sender, packet))
			{
				try
				{
					NGServerCamera	cam;
					if (this.NGGhostCams.TryGetValue(sender, out cam) == false)
						h.Throw(Errors.Camera_NotInitialized, "Raycasting while the server has not been initialized.");

					Camera	camera = this.ssm.GetResource<Camera>(packet.cameraID);
					if (camera == null)
						h.Throw(Errors.Camera_CameraNotFound, "Camera #" + packet.cameraID + " was not found.");

					cam.Raycast(camera, this.cameraRaycastResult, packet.viewportX, packet.viewportY);

					h.response.instanceIDs = new int[this.cameraRaycastResult.Count];
					h.response.names = new string[this.cameraRaycastResult.Count];

					for (int i = 0; i < this.cameraRaycastResult.Count; i++)
					{
						h.response.instanceIDs[i] = this.cameraRaycastResult[i].GetInstanceID();
						h.response.names[i] = this.cameraRaycastResult[i].name;
					}
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Camera_ClientToggleModule(Client sender, Packet _packet)
		{
			ClientToggleModulePacket	packet = _packet as ClientToggleModulePacket;

			using (var h = ResponsePacketHandler.Get<AckPacket>(sender, packet))
			{
				try
				{
					NGServerCamera	cam;
					if (this.NGGhostCams.TryGetValue(sender, out cam) == false)
						h.Throw(Errors.Camera_NotInitialized, "Toggling a module while the server has not been initialized.");

					if (packet.active == true)
					{
						if (this.modulesRunner.EnableModule(packet.moduleID, sender) == false)
							h.Throw(Errors.Camera_ModuleNotFound, "Module " + packet.moduleID + " was not found. Can not toggle to true.");
					}
					else if (this.modulesRunner.DisableModule(packet.moduleID, sender) == false)
						h.Throw(Errors.Camera_ModuleNotFound, "Module " + packet.moduleID + " was not found. Can not toggle to false.");
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private void	Handle_Camera_ClientStickGhostCamera(Client sender, Packet _packet)
		{
			ClientStickGhostCameraPacket	packet = _packet as ClientStickGhostCameraPacket;

			using (var h = ResponsePacketHandler.Get<ServerStickGhostCameraPacket>(sender, packet))
			{
				try
				{
					NGServerCamera	cam;
					if (this.NGGhostCams.TryGetValue(sender, out cam) == false)
						h.Throw(Errors.Camera_NotInitialized, "Anchoring ghost Camera while the server has not been initialized.");

					if (packet.instanceID == 0)
						cam.ghostCamera.transform.SetParent(null, true);
					else
					{
						if (packet.isGameObject == true)
						{
							GameObject	parent = this.ssm.GetResource<GameObject>(packet.instanceID);

							if (parent == null)
								h.Throw(Errors.Camera_AnchorNotFound, "The anchor was not found.");

							// Discard prefabs, apparently, their flags is set.
							if ((parent.hideFlags & HideFlags.HideInHierarchy) == 0)
							{
								h.response.transformInstanceID = parent.transform.GetInstanceID();
								cam.ghostCamera.transform.SetParent(parent.transform, true);
							}
							else
								h.Throw(Errors.Camera_InvalidAnchor, "The anchor is not in the scene. Might be a prefab.");
						}
						else
						{
							Transform	parent = this.ssm.GetResource<Transform>(packet.instanceID);

							if (parent == null)
								h.Throw(Errors.Camera_AnchorNotFound, "The anchor was not found.");

							// Discard prefabs, apparently, their flags is set.
							if ((parent.hideFlags & HideFlags.HideInHierarchy) == 0)
							{
								h.response.transformInstanceID = packet.instanceID;
								cam.ghostCamera.transform.SetParent(parent, true);
							}
							else
								h.Throw(Errors.Camera_InvalidAnchor, "The anchor is not in the scene. Might be a prefab.");
						}
					}
				}
				catch (Exception ex)
				{
					h.HandleException(ex);
				}
			}
		}

		private string	ExtractGameObjectInstanceID(string path)
		{
			return path.Substring(0, path.IndexOf(NGServerScene.ValuePathSeparator));
		}

		private string	ExtractComponentInstanceID(string path)
		{
			int	n = path.IndexOf(NGServerScene.ValuePathSeparator);
			return path.Substring(n + 1, path.IndexOf(NGServerScene.ValuePathSeparator, n + 1) - n - 1);
		}

		private string	PrepareInternalErrorMessage(Packet packet)
		{
			return "Internal error occurs on the server for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareExceptionMessage(Packet packet, Exception ex)
		{
			return "An exception has been thrown on the server for packet " + packet.GetType().Name + "." + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace;
		}

		private string	PrepareTypeNotFoundMessage(Packet packet, string type)
		{
			return "Type (" + type + ") was not found on the server for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareGameObjectNotFoundMessage(Packet packet, string instanceID)
		{
			return "GameObject (" + instanceID + ") was not found on the server for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareComponentNotFoundMessage(Packet packet, string instanceID)
		{
			return "Component (" + instanceID + ") was not found on the server for packet " + packet.GetType().Name + ".";
		}

		private string	PreparePathNotResolvedMessage(Packet packet, string path)
		{
			return "Server was not able to resolve the path (" + path + ") for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareMethodNotFoundMessage(Packet packet, string method)
		{
			return "Server failed to found method (" + method + ") for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareInvalidArgumentMessage(Packet packet)
		{
			return "An argument is invalid for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareInvocationFailedMessage(Packet packet, string methodSignature)
		{
			return "An method invocation (\"" + methodSignature + "\") has failed for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareMaterialNotFoundMessage(Packet packet, string instanceID)
		{
			return "Material (" + instanceID + ") was not found on the server for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareShaderNotFoundMessage(Packet packet, string shader)
		{
			return "Shader (" + shader + ") was not found on the server for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareShaderPropertyNotFoundMessage(Packet packet, string propertyName)
		{
			return "Shader property (" + propertyName + ") was not found on the server for packet " + packet.GetType().Name + ".";
		}

		private string	PrepareRenderTextureFormatNotSupportedMessage(Packet packet, RenderTextureFormat format)
		{
			return "The RenderTextureFormat \"" + format + "\" requested by " + packet.GetType().Name + " is not supported by the platform.";
		}
	}
}