using System;
using System.Collections.Generic;
using System.Text;

namespace NGToolsEditor.NGRemoteScene
{
	[Serializable]
	internal class IncompleteGameObjectException : Exception
	{
		public readonly List<string>	paths;
		public readonly List<Type>		types;
		public readonly List<int>		instanceIDs;
		public readonly List<int>		gameObjectInstanceIDs;
		public readonly List<int>		componentInstanceIDs;
		public readonly List<bool>		needGenerate;

		public	IncompleteGameObjectException()
		{
		}

		public	IncompleteGameObjectException(string path, Type type, int instanceID, int gameObjectInstanceID, int componentInstanceID) : this(path, type, instanceID, gameObjectInstanceID, componentInstanceID, false)
		{
		}

		public	IncompleteGameObjectException(string path, Type type, int instanceID, int gameObjectInstanceID, int componentInstanceID, bool needGenerate)
		{
			this.paths = new List<string>() { path };
			this.types = new List<Type>() { type };
			this.instanceIDs = new List<int>() { instanceID };
			this.gameObjectInstanceIDs = new List<int>() { gameObjectInstanceID };
			this.componentInstanceIDs = new List<int>() { componentInstanceID };
			this.needGenerate = new List<bool>() { needGenerate };
		}

		public void	Aggregate(IncompleteGameObjectException ex)
		{
			if (ex.paths != null)
			{
				this.paths.AddRange(ex.paths);
				this.types.AddRange(ex.types);
				this.instanceIDs.AddRange(ex.instanceIDs);
				this.gameObjectInstanceIDs.AddRange(ex.gameObjectInstanceIDs);
				this.componentInstanceIDs.AddRange(ex.componentInstanceIDs);
				this.needGenerate.AddRange(ex.needGenerate);
			}
		}

		public override string	Message
		{
			get
			{
				if (this.paths != null)
				{
					StringBuilder	buffer = Utility.GetBuffer();

					for (int i = 0; i < this.paths.Count; i++)
					{
						buffer.AppendLine(this.paths[i]);
						buffer.AppendLine(this.types[i].FullName);
						buffer.Append(this.instanceIDs[i]);
						buffer.Append(this.gameObjectInstanceIDs[i]);
						buffer.Append(this.componentInstanceIDs[i]);
						buffer.AppendLine();
					}

					return Utility.ReturnBuffer(buffer);
				}

				return "IncompleteGameObjectException";
			}
		}
	}
}