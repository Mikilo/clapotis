using NGTools.Network;
using NGTools.NGRemoteScene;
using System;
using System.Collections.Generic;
using System.Text;

namespace NGToolsEditor.NGRemoteScene
{
	public sealed class DataDrawer
	{
		public sealed class LayerPath : IDisposable
		{
			private DataDrawer	data;

			public	LayerPath(DataDrawer data)
			{
				this.data = data;
				this.data.PushPath();
			}

			public void	Dispose()
			{
				this.data.PopPath();
			}
		}

		private static StringBuilder	valuePath = new StringBuilder(64);

		public string	Name { get; private set; }
		public object	Value { get; private set; }
		public IDataDrawerTool	Inspector { get; private set; }

		public readonly IUnityData	unityData;

		public	DataDrawer(IUnityData unityData)
		{
			this.unityData = unityData;
		}

		public void	Init(string path, IDataDrawerTool inspector)
		{
			this.Inspector = inspector;
			DataDrawer.valuePath.Length = 0;
			DataDrawer.valuePath.Append(path);
		}

		public DataDrawer	DrawChild(string pathName, string displayName, object value)
		{
			if (this.layerPaths.Count > 0)
				DataDrawer.valuePath.Length = this.layerPaths.Peek();

			DataDrawer.valuePath.Append(NGServerScene.ValuePathSeparator);
			DataDrawer.valuePath.Append(pathName);

			this.Name = Utility.NicifyVariableName(displayName);

			this.Value = value;

			return this;
		}

		public DataDrawer	DrawChild(string pathName, object value)
		{
			return this.DrawChild(pathName, pathName, value);
		}

		public string	GetPath()
		{
			return DataDrawer.valuePath.ToString();
		}

		public LayerPath	CreateLayerChildScope()
		{
			return new LayerPath(this);
		}

		private Stack<int>	layerPaths = new Stack<int>();

		public void	PushPath()
		{
			this.layerPaths.Push(DataDrawer.valuePath.Length);
		}

		public void	PopPath()
		{
			DataDrawer.valuePath.Length = this.layerPaths.Pop();
		}
	}
}