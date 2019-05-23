using VRageMath;

namespace EemRdx.Extensions
{
	public static class VectorExtensions
	{
		public static double DistanceTo(this Vector3D @from, Vector3D to)
		{
			return (to - @from).Length();
		}

		//public static Vector3D LineTowards(this Vector3D @from, Vector3D to, double length)
		//{
		//	return @from + (Vector3D.Normalize(to - @from) * length);
		//}

		public static Vector3D InverseVectorTo(this Vector3D @from, Vector3D to, double length)
		{
			return @from + (Vector3D.Normalize(@from - to) * length);
		}
	}
}