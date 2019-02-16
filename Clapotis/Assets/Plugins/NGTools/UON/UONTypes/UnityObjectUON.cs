using System;
using System.Reflection;
#if NETFX_CORE
using System.Reflection;
#endif
using System.Text;

namespace NGTools.UON
{
	internal class UnityObjectUON : UONType
	{
		private MethodInfo	GetAssetPath;
		private MethodInfo	LoadAssetAtPath;

		public	UnityObjectUON()
		{
			Type	AssetDatabase = Type.GetType("UnityEditor.AssetDatabase,UnityEditor");

			if (AssetDatabase != null)
			{
				this.GetAssetPath = AssetDatabase.GetMethod("GetAssetPath", new Type[] { typeof(UnityEngine.Object) });
				this.LoadAssetAtPath = AssetDatabase.GetMethod("LoadAssetAtPath", new Type[] { typeof(string), typeof(Type) });
			}
		}

		public override bool	Can(Type t)
		{
			return typeof(UnityEngine.Object).IsAssignableFrom(t);
		}

		public override string	Serialize(UON.SerializationData data, object o)
		{
			if (this.GetAssetPath != null)
				return "\"" + this.GetAssetPath.Invoke(null, new object[] { o }) + "\"";
			return null;
		}

		public override object	Deserialize(UON.SerializationData data, StringBuilder raw, object instance)
		{
			if (this.GetAssetPath != null)
				return this.LoadAssetAtPath.Invoke(null, new object[] { raw.ToString(1, raw.Length - 2), typeof(UnityEngine.Object) });
			return null;
		}
	}
}