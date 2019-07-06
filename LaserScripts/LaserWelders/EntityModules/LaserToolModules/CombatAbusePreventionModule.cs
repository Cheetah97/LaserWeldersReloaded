using EemRdx.EntityModules;
using EemRdx.Extensions;
using EemRdx.LaserWelders.EntityModules.GridModules;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Text;
using VRage.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface ICombatAbusePreventionModule : IEntityModule
    {
        bool ToolBlocked { get; }
    }

    public class CombatAbusePreventionModule : EntityModuleBase<ILaserToolKernel>, InitializableModule, UpdatableModule, ClosableModule, ICombatAbusePreventionModule
    {
        public CombatAbusePreventionModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(CombatAbusePreventionModule);

        public bool Inited { get; private set; }

        public bool RequiresOperable { get; } = false;

        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;

        private int Ticker => MyKernel.Session.Clock.Ticker;
        private IWeaponsFireDetectionModule MyGridWFDModule;
        public bool ToolBlocked { get; private set; }

        void InitializableModule.Init()
        {
            if (Inited) return;
            UpdateGridLink();
            MyKernel.Block.AppendingCustomInfo += Block_AppendingCustomInfo;
            Inited = true;
        }

        private void Block_AppendingCustomInfo(IMyTerminalBlock arg1, StringBuilder info)
        {
            if (ToolBlocked)
            {
                info.AppendLine($"Attention! This {(MyKernel.Block.CubeGrid.IsStatic ? "station" : "ship")} has engaged in combat. Processing power was diverted from the tool to weapons systems.");
            }
        }

        void UpdatableModule.Update()
        {
            if (MyKernel.Session?.Settings?.PreventUseInCombat != true)
            {
                if (ToolBlocked) ToolBlocked = false;
                return;
            }

            int CheckFrequency = !ToolBlocked ? 30 : 180;

            if (Ticker % (5 * 60) == 0) UpdateGridLink();
            if (Ticker % CheckFrequency == 0) CheckGunsFiring();
        }

        void CheckGunsFiring()
        {
            ToolBlocked = MyGridWFDModule?.WeaponsFiring == true;
        }

        void UpdateGridLink()
        {
            IGridKernel myGridKernel = MyKernel.Block.CubeGrid.GetComponent<GridKernel>();
            if (myGridKernel != null) MyGridWFDModule = myGridKernel.WeaponsFireDetection;
            else MyGridWFDModule = null;
        }

        void ClosableModule.Close()
        {
            MyKernel.Block.AppendingCustomInfo -= Block_AppendingCustomInfo;
        }
    }
}
