using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using VRage.ModAPI;
using VRageMath;

namespace EemRdx.LaserWelders.Helpers
{
    public enum MyCustomSafeZoneAction
    {
        Damage = 1,
        Shooting = 2,
        Drilling = 4,
        Welding = 8,
        Grinding = 16,
        VoxelHand = 32,
        Building = 64,
        All = 127
    }

    /// <summary>
    /// Keens and their whitelisting
    /// </summary>
    public static class SafeZonesHelper
    {
        static T GetHacks<T>(T ptr, object val) => (T)val;

        public static bool IsActionAllowed(BoundingBoxD aabb, MyCustomSafeZoneAction action, long sourceEntityId = 0)
        {
            return MySessionComponentSafeZones.IsActionAllowed(aabb, GetHacks(MySessionComponentSafeZones.AllowedActions, (int)action), sourceEntityId);
        }
        public static bool IsActionAllowed(IMyEntity entity, MyCustomSafeZoneAction action, long sourceEntityId = 0)
        {
            return MySessionComponentSafeZones.IsActionAllowed((entity as VRage.Game.Entity.MyEntity), GetHacks(MySessionComponentSafeZones.AllowedActions, (int)action), sourceEntityId);
        }
        public static bool IsActionAllowed(Vector3D point, MyCustomSafeZoneAction action, long sourceEntityId = 0)
        {
            return MySessionComponentSafeZones.IsActionAllowed(point, GetHacks(MySessionComponentSafeZones.AllowedActions, (int)action), sourceEntityId);
        }
    }
}
