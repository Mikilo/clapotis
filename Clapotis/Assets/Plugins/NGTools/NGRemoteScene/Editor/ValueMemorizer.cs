using UnityEditor;
using UnityEngine;

namespace NGToolsEditor.NGRemoteScene
{
	public class ValueMemorizer<T>
	{
		private static GUIStyle	smallLabel;

		public T		serverValue;
		public T		realTimeValue;
		public string	serverValueStringified;
		public bool		isPending;
		public float	labelWidth = EditorGUIUtility.labelWidth;

		public void	NewValue(T value)
		{
			if (this.isPending == true &&
				this.realTimeValue.Equals(value) == true)
			{
				this.isPending = false;
			}
		}

		public void	Set(T value)
		{
			this.isPending = true;

			this.realTimeValue = value;
			this.serverValueStringified = this.serverValue.ToString();
		}

		public T	Get(T @default)
		{
			this.serverValue = @default;
			return this.isPending == true ? this.realTimeValue : @default;
		}

		public virtual void	Draw(Rect r)
		{
			if (this.isPending == true)
			{
				if (ValueMemorizer<T>.smallLabel == null)
				{
					ValueMemorizer<T>.smallLabel = new GUIStyle(EditorStyles.label);
					ValueMemorizer<T>.smallLabel.alignment = TextAnchor.LowerRight;
					ValueMemorizer<T>.smallLabel.fontSize = 8;
				}

				r.x += this.labelWidth;
				r.y += 3F;
				r.width -= this.labelWidth;
				EditorGUI.LabelField(r, this.serverValueStringified, ValueMemorizer<T>.smallLabel);
			}
		}
	}
}