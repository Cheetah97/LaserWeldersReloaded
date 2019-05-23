using EemRdx.EntityModules;
using VRage.ModAPI;
using System.Collections.Generic;
using MyItemType = VRage.Game.ModAPI.Ingame.MyItemType;
using System.Text;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface IResponder : IEntityModule
    {
        IReadOnlyDictionary<MyItemType, float> LastReportedMissingComponents { get; }
        LaserToolStatus ToolStatus { get; }
        string ToolStatusReport { get; }
    }

    public interface IResponderModuleInternal : IResponder
    {
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

        private StringBuilder ToolStatusReport = new StringBuilder();

        void UpdatableModule.Update()
        {
            UpdateToolStatus();
            if (ToolStatus == LaserToolStatus.Standby || ToolStatus == LaserToolStatus.Damaged)
            {
                LastReportedMissingComponents.Clear();
                ToolStatusReport.Clear();
            }
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
