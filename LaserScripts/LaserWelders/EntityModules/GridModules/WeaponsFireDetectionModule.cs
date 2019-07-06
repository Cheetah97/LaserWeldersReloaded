using EemRdx.EntityModules;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Text;
using VRage.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.GridModules
{
    public interface IWeaponsFireDetectionModule : IEntityModule
    {
        bool WeaponsFiring { get; }
    }

    public class WeaponsFireDetectionModule : EntityModuleBase<IGridKernel>, InitializableModule, UpdatableModule, IWeaponsFireDetectionModule
    {
        public WeaponsFireDetectionModule(IGridKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(WeaponsFireDetectionModule);

        public bool Inited { get; private set; }

        public bool RequiresOperable { get; } = false;

        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;

        private IMyGridTerminalSystem Term;
        private int Ticker => MyKernel.Session.Clock.Ticker;
        private List<IMyUserControllableGun> OnboardGuns = new List<IMyUserControllableGun>();
        public bool WeaponsFiring { get; private set; }

        void InitializableModule.Init()
        {
            if (Inited) return;
            Term = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(MyKernel.Grid);
            Inited = true;
        }

        void UpdatableModule.Update()
        {
            if (MyKernel.Session?.Settings?.PreventUseInCombat != true)
            {
                if (WeaponsFiring) WeaponsFiring = false;
                return;
            }

            int CheckFrequency = !WeaponsFiring ? 30 : (5 * 60);

            if (Ticker % (5 * 60) == 0) UpdateGunsList();
            if (Ticker % CheckFrequency == 0) CheckGunsFiring();
        }

        void CheckGunsFiring()
        {
            if (WeaponsFiring) WeaponsFiring = false;
            if (OnboardGuns.Count == 0) return;

            foreach (IMyUserControllableGun Gun in OnboardGuns)
            {
                if (Gun.IsShooting)
                {
                    WeaponsFiring = true;
                    break;
                }
            }
        }

        void UpdateGunsList()
        {
            OnboardGuns.Clear();
            Term.GetBlocksOfType(OnboardGuns, x => x.Enabled && x.IsFunctional);
        }
    }
}
