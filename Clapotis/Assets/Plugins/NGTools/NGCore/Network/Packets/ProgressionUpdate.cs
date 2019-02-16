namespace NGTools.Network
{
	public class ProgressionUpdate
	{
		public int	bytesReceived;
		public int	totalBytes;

		public bool		IsComplete { get { return this.bytesReceived == this.totalBytes; } }
		public float	ProgressionRate { get { return (float)this.bytesReceived / (float)this.totalBytes; } }
	}
}