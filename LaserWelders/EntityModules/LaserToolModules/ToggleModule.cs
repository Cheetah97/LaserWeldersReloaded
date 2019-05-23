using EemRdx.EntityModules;
using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using VRage.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface IToggle : IEntityModule
    {
        bool Toggled { get; }
    }

    public class ToggleModule : EntityModuleBase<ILaserToolKernel>, UpdatableModule, IToggle
    {
        public ToggleModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(ToggleModule);

        public bool Toggled { get; protected set; }

        public bool RequiresOperable => false;

        public MyEntityUpdateEnum UpdateFrequency => MyEntityUpdateEnum.EACH_10TH_FRAME;

        void UpdatableModule.Update()
        {
            IMyGunObject<Sandbox.Game.Weapons.MyToolBase> gunObject = MyKernel.Tool as IMyGunObject<Sandbox.Game.Weapons.MyToolBase>;
            Toggled = MyKernel.Tool.Enabled || gunObject.IsShooting;
        }
    }
}
