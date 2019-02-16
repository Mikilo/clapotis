using NGTools;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UnityEngine;

public static class NGDebug
{
	public static void	LogHierarchy(GameObject gameObject)
	{
		if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.OSXEditor)
			return;

		if (gameObject != null)
			NGDebug.LogHierarchy(gameObject.transform);
		else
			NGDebug.LogHierarchy((UnityEngine.Component)null);
	}

	public static void	LogHierarchy(RaycastHit hit)
	{
		if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.OSXEditor)
			return;

		NGDebug.LogHierarchy(hit.transform);
	}

	public static void	LogHierarchy(UnityEngine.Component component)
	{
		if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.OSXEditor)
			return;

		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.MultiContextsStartChar);

		if (component != null)
		{
			Transform	transform = component.transform;

			while (transform != null)
			{
				buffer.Append(transform.GetInstanceID());
				buffer.Append(InternalNGDebug.MultiContextsSeparator);

				transform = transform.parent;
			}

			// Remove last separator.
			buffer.Length -= 1;
		}

		buffer.Append(InternalNGDebug.MultiContextsEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer));
	}

	public static void	Log(params Object[] objects)
	{
		if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.OSXEditor)
			return;

		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.MultiContextsStartChar);

		if (objects.Length > 0)
		{
			for (int i = 0; i < objects.Length; i++)
			{
				if (objects[i] != null)
					buffer.Append(objects[i].GetInstanceID());
				else
					buffer.Append(0);

				buffer.Append(InternalNGDebug.MultiContextsSeparator);
			}

			// Remove last separator.
			buffer.Length -= 1;
		}

		buffer.Append(InternalNGDebug.MultiContextsEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer));
	}

	public static void	Log(params RaycastHit[] hits)
	{
		if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.OSXEditor)
			return;

		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.MultiContextsStartChar);

		if (hits.Length > 0)
		{
			for (int i = 0; i < hits.Length; i++)
			{
				if (hits[i].collider != null)
					buffer.Append(hits[i].collider.GetInstanceID());
				else
					buffer.Append(0);

				buffer.Append(InternalNGDebug.MultiContextsSeparator);
			}

			// Remove last separator.
			buffer.Length -= 1;
		}

		buffer.Append(InternalNGDebug.MultiContextsEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer));
	}

	public static void	LogCollection(IEnumerable<RaycastHit> hits)
	{
		if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.OSXEditor)
			return;

		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.MultiContextsStartChar);

		foreach (var hit in hits)
		{
			if (hit.collider != null)
				buffer.Append(hit.collider.GetInstanceID());
			else
				buffer.Append(0);

			buffer.Append(InternalNGDebug.MultiContextsSeparator);
		}

		// Remove last separator.
		if (buffer.Length > 0)
			buffer.Length -= 1;

		buffer.Append(InternalNGDebug.MultiContextsEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer));
	}

	public static void	LogCollection<T>(IEnumerable<T> objects) where T : Object
	{
		if (Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.OSXEditor)
			return;

		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.MultiContextsStartChar);

		foreach (var obj in objects)
		{
			if (obj != null)
				buffer.Append(obj.GetInstanceID());
			else
				buffer.Append(0);

			buffer.Append(InternalNGDebug.MultiContextsSeparator);
		}

		// Remove last separator.
		if (buffer.Length > 0)
			buffer.Length -= 1;

		buffer.Append(InternalNGDebug.MultiContextsEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer));
	}

	private static readonly Dictionary<int, FieldInfo[]>	cachedFieldInfos = new Dictionary<int, FieldInfo[]>();
	private static readonly Dictionary<int, PropertyInfo[]>	cachedPropertiesInfos = new Dictionary<int, PropertyInfo[]>();

	public static void	StaticSnapshot<T>()
	{
		NGDebug.StaticSnapshot(typeof(T));
	}

	public static void	StaticSnapshot(System.Type type)
	{
		BindingFlags	bindingFlags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
		FieldInfo[]		fields;
		int				hash = type.GetHashCode() + bindingFlags.GetHashCode();
		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.DataStartChar);
		buffer.Append(type.Name);

		if (NGDebug.cachedFieldInfos.TryGetValue(hash, out fields) == false)
		{
			fields = type.GetFields(bindingFlags);
			NGDebug.cachedFieldInfos.Add(hash, fields);
		}

		for (int i = 0; i < fields.Length; i++)
			NGDebug.SubSnap(buffer, fields[i].FieldType, fields[i].Name, fields[i].GetValue(null));

		PropertyInfo[]	properties;

		if (NGDebug.cachedPropertiesInfos.TryGetValue(hash, out properties) == false)
		{
			properties = type.GetProperties(bindingFlags);

			List<PropertyInfo>	filteredProperties = new List<PropertyInfo>(properties.Length);

			for (int i = 0; i < properties.Length; i++)
			{
				if (properties[i].CanRead == false ||
					properties[i].GetGetMethod(false) == null ||
					properties[i].GetIndexParameters().Length != 0)
				{
					continue;
				}

				filteredProperties.Add(properties[i]);
			}

			properties = filteredProperties.ToArray();
			NGDebug.cachedPropertiesInfos.Add(hash, properties);
		}

		for (int i = 0; i < properties.Length; i++)
		{
			try
			{
				NGDebug.SubSnap(buffer, properties[i].PropertyType, properties[i].Name, properties[i].GetValue(null));
			}
			catch (System.Exception ex)
			{
				InternalNGDebug.VerboseLogException(ex);
			}
		}

		buffer.Append(InternalNGDebug.DataEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer));
	}

	public static void	Snapshot(object o)
	{
		NGDebug.Snapshot(o, BindingFlags.Public | BindingFlags.Instance, null);
	}

	public static void	Snapshot(object o, Object context)
	{
		NGDebug.Snapshot(o, BindingFlags.Public | BindingFlags.Instance, context);
	}

	public static void	Snapshot(object o, BindingFlags bindingFlags)
	{
		NGDebug.Snapshot(o, bindingFlags, null);
	}

	public static void	Snapshot(object o, BindingFlags bindingFlags, Object context, string suffix = null)
	{
		NGDebug.Snapshot(o, bindingFlags, context, null, suffix);
	}

	public static void	Snapshot(object o, BindingFlags bindingFlags, Object context, string prefix, string suffix)
	{
		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.DataStartChar);

		if (o != null)
		{
			System.Type	type = o.GetType();

			if (prefix != null)
				buffer.Append(prefix);
			buffer.Append(type.Name);
			if (suffix != null)
				buffer.Append(suffix);

			if (type.IsPrimitive() == true || type == typeof(string) || type == typeof(decimal))
			{
				buffer.Append("=");
				buffer.Append(o);
			}
			else
			{
				FieldInfo[]	fields;
				int			hash = type.GetHashCode() + bindingFlags.GetHashCode();

				if (NGDebug.cachedFieldInfos.TryGetValue(hash, out fields) == false)
				{
					fields = type.GetFields(bindingFlags);
					NGDebug.cachedFieldInfos.Add(hash, fields);
				}

				for (int i = 0; i < fields.Length; i++)
					NGDebug.SubSnap(buffer, fields[i].FieldType, fields[i].Name, fields[i].GetValue(o));

				PropertyInfo[]	properties;

				if (NGDebug.cachedPropertiesInfos.TryGetValue(hash, out properties) == false)
				{
					properties = type.GetProperties(bindingFlags);

					List<PropertyInfo>	filteredProperties = new List<PropertyInfo>(properties.Length);

					for (int i = 0; i < properties.Length; i++)
					{
						if (properties[i].CanRead == false ||
							properties[i].GetGetMethod(false) == null ||
							properties[i].GetIndexParameters().Length != 0)
						{
							continue;
						}

						filteredProperties.Add(properties[i]);
					}

					properties = filteredProperties.ToArray();
					NGDebug.cachedPropertiesInfos.Add(hash, properties);
				}

				for (int i = 0; i < properties.Length; i++)
				{
					try
					{
						NGDebug.SubSnap(buffer, properties[i].PropertyType, properties[i].Name, properties[i].GetValue(o));
					}
					catch (System.Exception ex)
					{
						InternalNGDebug.VerboseLogException(ex);
					}
				}
			}
		}
		else
			buffer.Append("NULL");

		buffer.Append(InternalNGDebug.DataEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer), context);
	}

	private static void	SubSnap(StringBuilder buffer, System.Type type, string name, object v)
	{
		buffer.Append(InternalNGDebug.DataSeparator);
		buffer.Append(name);
		buffer.Append("=");

		if (v == null)
			buffer.Append("NULL");
		else if (type.IsPrimitive() == true || type == typeof(string) || type == typeof(decimal))
		{
			if ('\0'.Equals(v) == true || "\0".Equals(v) == true)
				buffer.Append("\\0");
			else
				buffer.Append(v);
		}
		else if (type.IsArray == true)
			buffer.Append(type.GetElementType().Name + "[" + (v as System.Array).Length + "]");
		else if (typeof(IList).IsAssignableFrom(type) == true)
			buffer.Append(type.Name + "<" + Utility.GetArraySubType(type).Name + ">[" + (v as IList).Count + "]");
		else
			buffer.Append(v.ToString().Replace(InternalNGDebug.DataSeparator, InternalNGDebug.DataSeparatorReplace));
	}

	public static void	Snapshots<T>(IEnumerable<T> enumerable, int offset, Object context)
	{
		NGDebug.Snapshots<T>(enumerable, offset, int.MaxValue, BindingFlags.Public | BindingFlags.Instance, context);
	}

	public static void	Snapshots<T>(IEnumerable<T> enumerable, int offset, BindingFlags bindingFlags, Object context = null)
	{
		NGDebug.Snapshots<T>(enumerable, offset, int.MaxValue, bindingFlags, context);
	}

	public static void	Snapshots<T>(IEnumerable<T> enumerable, int offset, int count, Object context)
	{
		NGDebug.Snapshots<T>(enumerable, offset, count, BindingFlags.Public | BindingFlags.Instance, context);
	}

	public static void	Snapshots<T>(IEnumerable<T> enumerable, int offset = 0, int count = int.MaxValue, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance, Object context = null)
	{
		if (enumerable == null)
		{
			NGDebug.Snapshot(null);
			return;
		}

		int			i = 0;
		System.Type	type = enumerable.GetType();

		if (type.IsArray == true)
			Debug.Log(Utility.GetArraySubType(type).Name + "[" + (enumerable as IList).Count + "]");
		else if (typeof(IList).IsAssignableFrom(enumerable.GetType()) == true)
			Debug.Log(type.Name + "<" + Utility.GetArraySubType(type).Name + ">[" + (enumerable as IList).Count + "]");
		else
			Debug.Log(enumerable.GetType().Name, context);

		if (Utility.GetArraySubType(type) == typeof(string))
		{
			foreach (T element in enumerable)
			{
				if (i >= offset)
				{
					Debug.Log("String[" + i + "]=" + element, context);
					--count;
					if (count <= 0)
						break;
				}
				++i;
			}

			return;
		}

		foreach (T element in enumerable)
		{
			if (i >= offset)
			{
				NGDebug.Snapshot(element, bindingFlags, context, "[" + i + "]");
				--count;
				if (count <= 0)
					break;
			}
			++i;
		}
	}

	/// <summary>
	/// <para>Log generating an exception to output a log with a stack trace to the console.</para>
	/// <para>Works on multi-threads.</para>
	/// </summary>
	/// <param name="message"></param>
	public static void	MTLog(string message)
	{
		Debug.LogException(new MTLog(message));
	}

	/// <summary>
	/// <para>Log generating an exception to output a log with a stack trace to the console.</para>
	/// <para>Works on multi-threads.</para>
	/// </summary>
	/// <param name="message"></param>
	/// <param name="context"></param>
	public static void	MTLog(string message, Object context)
	{
		Debug.LogException(new MTLog(message), context);
	}

	public static void	LogTags(string message, params string[] tags)
	{
		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.MultiTagsStartChar);

		buffer.Append(message);
		buffer.Append(InternalNGDebug.MultiTagsSeparator);

		for (int i = 0; i < tags.Length; i++)
		{
			buffer.Append(tags[i]);
			buffer.Append(InternalNGDebug.MultiTagsSeparator);
		}

		// Remove last separator.
		buffer.Length -= 1;
		buffer.Append(InternalNGDebug.MultiTagsEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer));
	}

	public static void	LogJSON(string message, string json, Object context = null)
	{
		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.JSONStartChar);
		buffer.Append(message);
		buffer.Append(InternalNGDebug.JSONSeparator);
		buffer.Append(json);
		buffer.Append(InternalNGDebug.JSONEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer), context);
	}

	public static void	LogJSON(string json, Object context = null)
	{
		StringBuilder	buffer = Utility.GetBuffer();

		buffer.Length = 0;
		buffer.Append(InternalNGDebug.JSONStartChar);
		buffer.Append(InternalNGDebug.JSONSeparator);
		buffer.Append(json);
		buffer.Append(InternalNGDebug.JSONEndChar);
		Debug.Log(Utility.ReturnBuffer(buffer), context);
	}
}

#if !UNITY_WSA_10_0 && !UNITY_WSA_8_1 && !UNITY_WP_8_1
[Browsable(false)]
#endif
[EditorBrowsable(EditorBrowsableState.Never)]
[System.Serializable]
public class MTLog : System.Exception
{
	public override string	Message { get { return this.message; } }

	private readonly string	message;

	public	MTLog(string message)
	{
		this.message = message;
	}
}

internal class DataStore : ScriptableObject
{
	public List<object>	array = new List<object>();
}