using Sandbox.ModAPI;
using System;
using System.Text;
using VRage.Game;
using VRage.Utils;
using EemRdx.SessionModules;
using VRage.ModAPI;
using Sandbox.Game.EntityComponents;
using System.Collections.Concurrent;
using System.Linq;
using System.Collections.Generic;

namespace EemRdx.LaserWelders.SessionModules
{
    public interface IPerformanceLimiterModule : ISessionModule
    {
        void AddToStartWorkQueue(long ToolId);
        bool CanStartWork(long ToolId);
        void RegisterWorkStarted(long ToolId);
        void RegisterWorkFinished(long ToolId);
        int ToolsPendingWork { get; }
        int ToolsWorking { get; }
    }

    public class PerformanceLimiterModule : SessionModuleBase<ILaserWeldersSessionKernel>, InitializableModule, UpdatableModule, IPerformanceLimiterModule
    {
        public PerformanceLimiterModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(PerformanceLimiterModule);

        void InitializableModule.Init()
        {
            
        }

        void UpdatableModule.Update()
        {
            
        }

        private List<long> CurrentlyWorkingTools = new List<long>();
        public int ToolsWorking => CurrentlyWorkingTools.Count;
        public int ToolsPendingWork => ToolsPendingWorkStart.Count;
        private Queue<long> ToolsPendingWorkStart = new Queue<long>();

        public void RegisterWorkStarted(long ToolId)
        {
            if (!CurrentlyWorkingTools.Contains(ToolId)) CurrentlyWorkingTools.Add(ToolId);
        }

        public void RegisterWorkFinished(long ToolId)
        {
            if (CurrentlyWorkingTools.Contains(ToolId)) CurrentlyWorkingTools.Remove(ToolId);
        }

        public void AddToStartWorkQueue(long ToolId)
        {
            if (!ToolsPendingWorkStart.Contains(ToolId))
                ToolsPendingWorkStart.Enqueue(ToolId);
        }

        public bool CanStartWork(long ToolId)
        {
            if (!ToolsPendingWorkStart.Contains(ToolId)) return false;
            if (CurrentlyWorkingTools.Contains(ToolId)) return false;
            if (ToolsPendingWorkStart.Peek() == ToolId)
            {
                int MaxConcurrentTools = MySessionKernel.Settings.MaxToolUpdatePerTick;
                if (CurrentlyWorkingTools.Count < MaxConcurrentTools)
                {
                    ToolsPendingWorkStart.Dequeue();
                    return true;
                }
                else return false;
            }
            else return false;
        }
    }
}
