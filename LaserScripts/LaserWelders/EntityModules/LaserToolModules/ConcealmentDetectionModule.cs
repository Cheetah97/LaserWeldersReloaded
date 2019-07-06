using EemRdx.EntityModules;
using System;
using VRage.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface IConcealmentDetectionModule : IEntityModule
    {
        bool IsLikelyConcealed { get; }
    }

    public class ConcealmentDetectionModule : EntityModuleBase<ILaserToolKernel>, UpdatableModule, IConcealmentDetectionModule
    {
        public ConcealmentDetectionModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(ConcealmentDetectionModule);
        bool UpdatableModule.RequiresOperable { get; } = false;
        MyEntityUpdateEnum UpdatableModule.UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_100TH_FRAME;
        private DateTime LastUpdate = DateTime.UtcNow;
        public bool IsLikelyConcealed => ((int)MyKernel.Entity.Flags & 4) > 0 || (DateTime.UtcNow - LastUpdate) > TimeSpan.FromSeconds(10);

        void UpdatableModule.Update()
        {
            LastUpdate = DateTime.UtcNow;
        }
    }
}
