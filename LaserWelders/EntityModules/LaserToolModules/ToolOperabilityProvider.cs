using EemRdx.EntityModules;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using VRage.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public class ToolOperabilityProvider : EntityModuleBase<ILaserToolKernel>, IOperabilityProvider
    {
        public ToolOperabilityProvider(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(ToolOperabilityProvider);

        public bool CanOperate
        {
            get
            {
                return MyKernel.Block.IsFunctional && MyKernel.PowerModule?.SufficientPower == true;
            }
        }
    }
}
