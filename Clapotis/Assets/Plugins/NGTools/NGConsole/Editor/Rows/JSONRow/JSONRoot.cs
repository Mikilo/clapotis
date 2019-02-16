using System.Text;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal class JSONRoot : IJSONObject
	{
		public bool	Open { get { return true; } set { } }

		private IJSONObject	child;

		public	JSONRoot(string raw)
		{
			int	i = 0;

			for (; i < raw.Length; i++)
			{
				if (JSONParseUtility.IsBlankChar(raw[i]) == true ||
					raw[i] == JSONRow.JSONKeySeparator)
				{
					continue;
				}

				int	j = JSONParseUtility.IsOpener(raw[i]);

				if (j != -1)
				{
					++i;
					this.child = new JSONObject(raw, ref i, raw.Length, j);
					break;
				}
				else
				{
					int	startOffset = JSONParseUtility.DigestString(raw, ref i, raw.Length);

					this.child = new JSONValue(raw.Substring(startOffset, i - startOffset + 1));
					break;
				}
			}

			if (this.child != null)
				this.child.Open = true;
		}

		float	IJSONObject.GetHeight()
		{
			if (this.child != null)
				return this.child.GetHeight();
			return 0F;
		}

		void	IJSONObject.Draw(Rect r, float offset)
		{
			if (this.child != null)
				this.child.Draw(r);
		}

		void	IJSONObject.Copy(StringBuilder buffer, bool forceFullExploded, string indent, bool skipIndent)
		{
			if (this.child != null)
				this.child.Copy(buffer, forceFullExploded);
		}
	}
}