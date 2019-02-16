using System;
using System.Collections.Generic;
using UnityEditor;

namespace NGToolsEditor
{
	using UnityEngine;

	[Serializable]
	public sealed class AssetsSelection
	{
		private static List<int>	cacheObjects = new List<int>();

		public Object	this[int i]
		{
			get
			{
				return this.refs[i].@object;
			}
		}

		public List<SelectionItem>	refs = new List<SelectionItem>();

		public	AssetsSelection(Object[] objects, int[] instanceIDs)
		{
			for (int i = 0; i < objects.Length; i++)
			{
				try
				{
					this.refs.Add(new SelectionItem(objects[i], instanceIDs[i]));
				}
				catch (MissingMethodException)
				{
				}
			}
		}

		public	AssetsSelection(Object[] objects)
		{
			for (int i = 0; i < objects.Length; i++)
			{
				try
				{
					this.refs.Add(new SelectionItem(objects[i], objects[i].GetInstanceID()));
				}
				catch (MissingMethodException)
				{
				}
			}
		}

		public	AssetsSelection(int[] instanceIDs)
		{
			for (int i = 0; i < instanceIDs.Length; i++)
			{
				try
				{
					Object	obj = EditorUtility.InstanceIDToObject(instanceIDs[i]);

					if (obj != null)
						this.refs.Add(new SelectionItem(obj, instanceIDs[i]));
				}
				catch (MissingMethodException)
				{
				}
			}
		}

		public void	Select()
		{
			if (this.refs.Count == 1)
				Selection.instanceIDs = new int[] { this.refs[0].instanceID };
			else
			{
				AssetsSelection.cacheObjects.Clear();

				for (int i = 0; i < this.refs.Count; i++)
					AssetsSelection.cacheObjects.Add(this.refs[i].instanceID);

				Selection.instanceIDs = AssetsSelection.cacheObjects.ToArray();
			}
		}

		public int	GetSelectionHash()
		{
			int	hash = 0;

			for (int i = 0; i < this.refs.Count; i++)
			{
				// Yeah, what? Is there a problem with my complex anti-colisionning hash function?
				hash += this.refs[i].instanceID;
			}

			return hash;
		}
	}
}