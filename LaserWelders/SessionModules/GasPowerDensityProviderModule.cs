using EemRdx.SessionModules;
using EemRdx.LaserWelders.Helpers;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using MyInventoryItem = VRage.Game.ModAPI.Ingame.MyInventoryItem;
using MySafeZoneAction = EemRdx.LaserWelders.Helpers.MyCustomSafeZoneAction;
using VRage.Utils;
using Sandbox.Engine.Voxels;
using VRage.Voxels;
using System.Diagnostics;
using Sandbox.Definitions;
using VRage.ObjectBuilders;
using VRage.ModAPI;

namespace EemRdx.LaserWelders.SessionModules
{
    public interface IGasPowerDensityProvider : ISessionModule
    {
        IReadOnlyDictionary<MyDefinitionId, float> GasPowerDensities { get; }
        float? GetGasPowerDensitySafe(MyDefinitionId GasDefinition);
    }

    public class GasPowerDensityProviderModule : SessionModuleBase<ILaserWeldersSessionKernel>, InitializableModule, IGasPowerDensityProvider
    {
        public GasPowerDensityProviderModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(GasPowerDensityProviderModule);

        IReadOnlyDictionary<MyDefinitionId, float> IGasPowerDensityProvider.GasPowerDensities => GasPowerDensities;
        private Dictionary<MyDefinitionId, float> GasPowerDensities = new Dictionary<MyDefinitionId, float>
        {
            { new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GasProperties), "Electricity"), 1 },
        };

        float? IGasPowerDensityProvider.GetGasPowerDensitySafe(MyDefinitionId GasDefinition)
        {
            if (GasPowerDensities.ContainsKey(GasDefinition)) return GasPowerDensities[GasDefinition];
            return null;
        }

        void InitializableModule.Init()
        {
            List<MyGasProperties> GasDefinitions = MyDefinitionManager.Static.GetAllDefinitions<MyGasProperties>().ToList();

            foreach (MyGasProperties GasDefinition in GasDefinitions)
            {
                if (!GasPowerDensities.ContainsKey(GasDefinition.Id))
                {
                    GasPowerDensities.Add(GasDefinition.Id, GasDefinition.EnergyDensity);
                }
            }
        }
    }
}
