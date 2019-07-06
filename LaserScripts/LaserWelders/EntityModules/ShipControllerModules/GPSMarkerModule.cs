using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Utils;
using EemRdx.EntityModules;
using VRage.ModAPI;
using Draygo.API;
using Sandbox.Game.EntityComponents;
using System.Linq;
using System.Collections.Generic;
using LaserToolStatus = EemRdx.LaserWelders.EntityModules.LaserToolModules.LaserToolStatus;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRageMath;
using VRage.Game.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.ShipControllerModules
{
    public interface IGPSMarkerModule : IEntityModule
    {
        void AddLocalGPS(IMyGps GPS);
        void RemoveLocalGPS(IMyGps GPS);
    }

    public class GPSMarkerModule : EntityModuleBase<SCKernel>, InitializableModule, UpdatableModule, IGPSMarkerModule
    {
        public GPSMarkerModule(SCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(GPSMarkerModule);
        public bool Inited { get; private set; }
        bool UpdatableModule.RequiresOperable { get; } = false;
        MyEntityUpdateEnum UpdatableModule.UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_10TH_FRAME;
        private int Ticker => MyKernel.Session.Clock.Ticker;
        private Dictionary<IMyGps, int> AddedGPS = new Dictionary<IMyGps, int>();
        private HudAPIv2 HudAPI => MyKernel.Session?.HUDAPIProvider?.HudAPI;

        void InitializableModule.Init()
        {
            if (Inited) return;

            Inited = true;
        }

        void UpdatableModule.Update()
        {
            List<IMyGps> toRemove = new List<IMyGps>();

            foreach (IMyGps GPS in AddedGPS.Keys)
            {
                if (GPS.DiscardAt == null) return;
                int AddedTick = AddedGPS[GPS];
                if (MyAPIGateway.Session.ElapsedPlayTime >= GPS.DiscardAt.Value) toRemove.Add(GPS);
            }

            foreach (IMyGps GPS in toRemove)
                RemoveLocalGPS(GPS);
        }

        public void AddLocalGPS(IMyGps GPS)
        {
            if (MyAPIGateway.Session.LocalHumanPlayer == null) return;
            AddedGPS.Add(GPS, Ticker);
            MyAPIGateway.Session.GPS.AddGps(MyAPIGateway.Session.LocalHumanPlayer.IdentityId, GPS);
        }

        public void RemoveLocalGPS(IMyGps GPS)
        {
            if (MyAPIGateway.Session.LocalHumanPlayer == null) return;
            MyAPIGateway.Session.GPS.RemoveGps(MyAPIGateway.Session.LocalHumanPlayer.IdentityId, GPS);
            AddedGPS.Remove(GPS);
        }
    }
}
