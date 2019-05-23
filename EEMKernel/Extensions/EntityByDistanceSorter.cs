using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace EemRdx.Extensions
{
	public class EntityByDistanceSorter : IComparer<IMyEntity>, IComparer<IMySlimBlock>, IComparer<Sandbox.ModAPI.Ingame.MyDetectedEntityInfo>
	{
		public Vector3D Position { get; set; }
		public EntityByDistanceSorter(Vector3D position)
		{
			Position = position;
		}

		public int Compare(IMyEntity x, IMyEntity y)
		{
			if (x == null || y == null) return 0;
			double distanceX = Vector3D.DistanceSquared(Position, x.GetPosition());
			double distanceY = Vector3D.DistanceSquared(Position, y.GetPosition());

			if (distanceX < distanceY) return -1;
			return distanceX > distanceY ? 1 : 0;
		}

		public int Compare(Sandbox.ModAPI.Ingame.MyDetectedEntityInfo x, Sandbox.ModAPI.Ingame.MyDetectedEntityInfo y)
		{
			double distanceX = Vector3D.DistanceSquared(Position, x.Position);
			double distanceY = Vector3D.DistanceSquared(Position, y.Position);

			if (distanceX < distanceY) return -1;
			return distanceX > distanceY ? 1 : 0;
		}

		public int Compare(IMySlimBlock x, IMySlimBlock y)
		{
			if (x == null || y == null) return 0;
			double distanceX = Vector3D.DistanceSquared(Position, x.CubeGrid.GridIntegerToWorld(x.Position));
			double distanceY = Vector3D.DistanceSquared(Position, y.CubeGrid.GridIntegerToWorld(y.Position));

			if (distanceX < distanceY) return -1;
			return distanceX > distanceY ? 1 : 0;
		}
	}
}