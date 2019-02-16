using NGTools;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;

namespace NGToolsEditor.NGRemoteScene
{
	internal static class TypeHandlerDrawersManager
	{
		private static Dictionary<Type, Type>	typeHandlerDrawers;
		private static TypeHandlerDrawer		defaultDrawer;

		/// <summary>
		/// <para>Create a drawer from the given <paramref name="typeHandler"/>.</para>
		/// <para>Returns the default drawer if <paramref name="typeHandler"/> is null.</para>
		/// <para>Returns the array drawer if <paramref name="typeHandler"/> is an array.</para>
		/// <para>Returns the class drawer if <paramref name="typeHandler"/> is a class not inheriting from <see cref="UnityEngine.Object"/>.</para>
		/// </summary>
		/// <param name="typeHandler"></param>
		/// <param name="type">Only use for ArrayDrawer purpose.</param>
		/// <returns>Always return an instance of TypeHandlerDrawer.</returns>
		public static TypeHandlerDrawer	CreateTypeHandlerDrawer(TypeHandler typeHandler, Type type)
		{
			if (typeHandler != null)
			{
				Type			typeHandlerType;
				TypeSignature	typeSignature;

				try
				{
					typeSignature = TypeHandlersManager.GetTypeSignature(type);

					if (TypeHandlerDrawersManager.typeHandlerDrawers == null)
					{
						TypeHandlerDrawersManager.typeHandlerDrawers = new Dictionary<Type, Type>();

						foreach (Type t in Utility.EachNGTSubClassesOf(typeof(TypeHandlerDrawer)))
						{
							TypeHandlerDrawerForAttribute[]	typeHandlerAttributes = t.GetCustomAttributes(typeof(TypeHandlerDrawerForAttribute), false) as TypeHandlerDrawerForAttribute[];

							if (typeHandlerAttributes.Length >= 1)
								TypeHandlerDrawersManager.typeHandlerDrawers.Add(typeHandlerAttributes[0].type, t);
						}
					}

					if (TypeHandlerDrawersManager.typeHandlerDrawers.TryGetValue(typeHandler.GetType(), out typeHandlerType) == true)
						return Activator.CreateInstance(typeHandlerType, typeHandler) as TypeHandlerDrawer;

					if ((typeSignature & TypeSignature.Array) != 0)
						return Activator.CreateInstance(typeof(ArrayDrawer), typeHandler, type) as TypeHandlerDrawer;

					if (typeSignature == TypeSignature.Class)
						return Activator.CreateInstance(typeof(ClassDrawer), typeHandler) as TypeHandlerDrawer;

					throw new Exception("TypeHandler " + typeHandler + " is not known.");
				}
				catch (Exception ex)
				{
					InternalNGDebug.LogException("TypeHandler=" + typeHandler + Environment.NewLine + "Type=" + type, ex);
				}
			}

			if (TypeHandlerDrawersManager.defaultDrawer == null)
				TypeHandlerDrawersManager.defaultDrawer = Activator.CreateInstance<UnsupportedTypeDrawer>();
			return TypeHandlerDrawersManager.defaultDrawer;
		}
	}
}