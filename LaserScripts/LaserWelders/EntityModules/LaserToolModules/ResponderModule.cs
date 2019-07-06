using EemRdx.EntityModules;
using VRage.ModAPI;
using System.Collections.Generic;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using System.Text;
using VRage.Game.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface IResponder : IEntityModule
    {
        IMyCubeGrid LastOperatedGrid { get; }
        IMyCubeGrid LastOperatedProjectedGrid { get; }
        IReadOnlyDictionary<MyItemType, float> LastReportedMissingComponents { get; }
        LaserToolStatus ToolStatus { get; }
        string ToolStatusReport { get; }
    }

    public interface IResponderModuleInternal : IResponder
    {
        void UpdateLastOperatedGrid(IMyCubeGrid LastOperatedGrid);
        void UpdateLastOperatedProjectedGrid(IMyCubeGrid LastOperatedProjectedGrid);
        void UpdateMissing(Dictionary<MyItemType, float> _missingComponents);
        void UpdateStatusReport(StringBuilder newReport, bool append);
        void UpdateStatusReport(string newReport, bool append);
        void ClearStatusReport();
    }

    public class ResponderModule : EntityModuleBase<ILaserToolKernel>, UpdatableModule, IResponderModuleInternal
    {
        public ResponderModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(ResponderModule);

        public bool RequiresOperable { get; } = false;

        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_10TH_FRAME;

        IReadOnlyDictionary<MyItemType, float> IResponder.LastReportedMissingComponents => LastReportedMissingComponents;

        Dictionary<MyItemType, float> LastReportedMissingComponents = new Dictionary<MyItemType, float>();

        public LaserToolStatus ToolStatus { get; private set; }

        string IResponder.ToolStatusReport => ToolStatusReport.ToString();

        public IMyCubeGrid LastOperatedGrid { get; private set; }
        private int LastOperatedGridLastSet = 0;

        public IMyCubeGrid LastOperatedProjectedGrid { get; private set; }
        private int LastOperatedProjectedGridLastSet = 0;

        private StringBuilder ToolStatusReport = new StringBuilder();

        void UpdatableModule.Update()
        {
            UpdateToolStatus();
            if (ToolStatus == LaserToolStatus.Standby || ToolStatus == LaserToolStatus.Damaged)
            {
                LastReportedMissingComponents.Clear();
                ToolStatusReport.Clear();
            }

            const int resetAfter = 3 * 60;
            if (MyKernel.Session.Clock.Ticker >= (LastOperatedGridLastSet + resetAfter))
                LastOperatedGrid = null;

            if (MyKernel.Session.Clock.Ticker >= (LastOperatedProjectedGridLastSet + resetAfter))
                LastOperatedProjectedGrid = null;
        }

        public void UpdateMissing(Dictionary<MyItemType, float> _missingComponents)
        {
            LastReportedMissingComponents.Clear();
            foreach (KeyValuePair<MyItemType, float> kvp in _missingComponents)
            {
                LastReportedMissingComponents.Add(kvp.Key, kvp.Value);
            }
        }

        public void ClearStatusReport()
        {
            ToolStatusReport.Clear();
        }

        public void UpdateStatusReport(StringBuilder newReport, bool append)
        {
            if (!append) ToolStatusReport.Clear();
            ToolStatusReport.Append(newReport.ToString());
        }

        public void UpdateStatusReport(string newReport, bool append)
        {
            if (!append) ToolStatusReport.Clear();
            ToolStatusReport.Append(newReport);
        }

        private void UpdateToolStatus()
        {
            if (!MyKernel.Tool.IsFunctional)
            {
                ToolStatus = LaserToolStatus.Damaged;
                return;
            }

            if (MyKernel.PowerModule.SufficientPower)
            {
                if (MyKernel.Toggle.Toggled)
                    ToolStatus = LaserToolStatus.Online;
                else
                    ToolStatus = LaserToolStatus.Standby;
            }
            else
            {
                ToolStatus = LaserToolStatus.OutOfPower;
            }
        }

        public void UpdateLastOperatedGrid(IMyCubeGrid LastOperatedGrid)
        {
            this.LastOperatedGrid = LastOperatedGrid;
            LastOperatedGridLastSet = MyKernel.Session.Clock.Ticker;
        }

        public void UpdateLastOperatedProjectedGrid(IMyCubeGrid LastOperatedProjectedGrid)
        {
            this.LastOperatedProjectedGrid = LastOperatedProjectedGrid;
            LastOperatedProjectedGridLastSet = MyKernel.Session.Clock.Ticker;
        }
    }

    public enum LaserToolStatus
    {
        /// <summary>
        /// Tool is damaged beyond functional level.
        /// </summary>
        Damaged,
        /// <summary>
        /// Tool is operational and turned off.
        /// </summary>
        Standby,
        /// <summary>
        /// Tool is operational, turned on, but has not enough power.
        /// </summary>
        OutOfPower,
        /// <summary>
        /// Tool is operational, turned on, and has sufficient power.
        /// </summary>
        Online,
    }
}
