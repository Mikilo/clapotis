using System;
using System.Text;
using UnityEngine;

namespace NGTools.NGGameConsole
{
	public class SystemInfoData : DataConsole
	{
		[SerializeField, HideInInspector]
		private bool	unusedSystemInfo;

		private string	cacheSupportRenderTextureFormat;
		private string	cacheSupportTextureFormat;

		private ClassInspector	inspectedClass;

		protected virtual void	Awake()
		{
			this.inspectedClass = new ClassInspector();
			this.inspectedClass.ExtractTypeProperties(typeof(SystemInfo));
			this.inspectedClass.Construct();

			this.cacheSupportRenderTextureFormat = this.RequestSupportedRenderTextureFormat();
			this.cacheSupportTextureFormat = this.RequestSupportedTextureFormat();
		}

		public override void	FullGUI()
		{
			this.inspectedClass.OnGUI();

			GUILayout.Space(10F);
			GUILayout.Label("Render Texture Formats", this.subTitleStyle);
			GUILayout.Label(this.cacheSupportRenderTextureFormat, this.fullStyle);

			GUILayout.Space(10F);
			GUILayout.Label("Texture Formats", this.subTitleStyle);
			GUILayout.Label(this.cacheSupportTextureFormat, this.fullStyle);
		}

		public override string	Copy()
		{
			StringBuilder	buffer = Utility.GetBuffer(this.inspectedClass.Copy());

			buffer.AppendLine();
			buffer.AppendLine("Render Texture Formats");
			buffer.AppendLine(this.cacheSupportRenderTextureFormat);

			buffer.AppendLine();
			buffer.AppendLine("Texture Formats");
			buffer.Append(this.cacheSupportTextureFormat);

			return Utility.ReturnBuffer(buffer);
		}

		private string	RequestSupportedRenderTextureFormat()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			foreach (object item in Enum.GetValues(typeof(RenderTextureFormat)))
			{
				if ((int)item <= 0)
					continue;

				if (buffer.Length > 0)
					buffer.AppendLine();
				buffer.Append(item.ToString() + " : " + SystemInfo.SupportsRenderTextureFormat((RenderTextureFormat)item));
			}

			return Utility.ReturnBuffer(buffer);
		}

		private string	RequestSupportedTextureFormat()
		{
			StringBuilder	buffer = Utility.GetBuffer();

			foreach (TextureFormat item in Enum.GetValues(typeof(TextureFormat)))
			{
				if ((int)item <= 0)
					continue;

				try
				{
					bool	supported = SystemInfo.SupportsTextureFormat(item);

					if (buffer.Length > 0)
						buffer.AppendLine();
					buffer.Append(item.ToString() + " : " + supported);
				}
				catch (Exception ex)
				{
					InternalNGDebug.VerboseLogException(ex);
				}
			}

			return Utility.ReturnBuffer(buffer);
		}
	}
}