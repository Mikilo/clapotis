using System.Text;
using UnityEngine;

namespace NGToolsEditor.NGConsole
{
	internal interface IJSONObject
	{
		bool	Open { get; set; }

		float	GetHeight();
		void	Draw(Rect r, float offset = 0F);
		void	Copy(StringBuilder buffer, bool forceFullExploded, string indent = "", bool skipIndent = false);
	}
}