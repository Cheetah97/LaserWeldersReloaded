using Sandbox.Game;
using Sandbox.ModAPI;

namespace EemRdx.LaserWelders
{
    /// <summary>
    /// They're not really constant, but they are the same until world settings are changed.
    /// </summary>
    public static class VanillaToolConstants
    {
        public static float WorkCoefficient => MyShipGrinderConstants.GRINDER_COOLDOWN_IN_MILISECONDS * 0.001f;
        public static float GrinderSpeed => MyAPIGateway.Session.GrinderSpeedMultiplier * MyShipGrinderConstants.GRINDER_AMOUNT_PER_SECOND * WorkCoefficient / 8;
        public static float WelderSpeed => MyAPIGateway.Session.WelderSpeedMultiplier * 2 * WorkCoefficient / 8; // 2 is WELDER_AMOUNT_PER_SECOND from MyShipWelder.cs
        public static float WelderBoneRepairSpeed => 0.6f * WorkCoefficient; // 0.6f is WELDER_MAX_REPAIR_BONE_MOVEMENT_SPEED from MyShipWelder.cs
    }
}
