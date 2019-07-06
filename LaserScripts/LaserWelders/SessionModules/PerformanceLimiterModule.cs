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
        void AddToStartWorkQueue(ILaserToolKernel Tool);
        void RemoveFromStartWorkQueue(ILaserToolKernel Tool);
        bool CanStartWork(ILaserToolKernel Tool);
        void RegisterWorkStarted(ILaserToolKernel Tool);
        void RegisterWorkFinished(ILaserToolKernel Tool);
        int ToolsPendingWork { get; }
        int ToolsWorking { get; }
    }

    public class PerformanceLimiterModule : SessionModuleBase<ILaserWeldersSessionKernel>, InitializableModule, UpdatableModule, IPerformanceLimiterModule
    {
        public PerformanceLimiterModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }

        public override string DebugModuleName { get; } = nameof(PerformanceLimiterModule);
        private int Ticker => MySessionKernel.Clock.Ticker;

        void InitializableModule.Init()
        {
            
        }

        void UpdatableModule.Update()
        {
            if (Ticker % (15 * 60) == 0) PurgeToolList();
        }

        private void PurgeToolList()
        {
            List<ILaserToolKernel> CWT_ToRemove = new List<ILaserToolKernel>();
            foreach (ILaserToolKernel Tool in CurrentlyWorkingTools)
            {
                if (Tool.ConcealmentDetectionModule.IsLikelyConcealed)
                    CWT_ToRemove.Add(Tool);
            }

            foreach (ILaserToolKernel Tool in CWT_ToRemove) CurrentlyWorkingTools.Remove(Tool);

            List<ILaserToolKernel> TPWS_ToRemove = new List<ILaserToolKernel>();
            foreach (ILaserToolKernel Tool in ToolsPendingWorkStart)
            {
                if (Tool.ConcealmentDetectionModule.IsLikelyConcealed)
                    TPWS_ToRemove.Add(Tool);
            }

            List<ILaserToolKernel> TPWS_Queue = ToolsPendingWorkStart.Where(x => !TPWS_ToRemove.Contains(x)).ToList();
            ToolsPendingWorkStart.Clear();
            foreach (ILaserToolKernel Tool in TPWS_Queue)
            {
                ToolsPendingWorkStart.Enqueue(Tool);
            }
        }

        private List<ILaserToolKernel> CurrentlyWorkingTools = new List<ILaserToolKernel>();
        public int ToolsWorking => CurrentlyWorkingTools.Count;
        public int ToolsPendingWork => ToolsPendingWorkStart.Count;
        private Queue<ILaserToolKernel> ToolsPendingWorkStart = new Queue<ILaserToolKernel>();

        public void RegisterWorkStarted(ILaserToolKernel Tool)
        {
            if (!CurrentlyWorkingTools.Contains(Tool)) CurrentlyWorkingTools.Add(Tool);
        }

        public void RegisterWorkFinished(ILaserToolKernel Tool)
        {
            if (CurrentlyWorkingTools.Contains(Tool)) CurrentlyWorkingTools.Remove(Tool);
        }

        public void AddToStartWorkQueue(ILaserToolKernel Tool)
        {
            if (!ToolsPendingWorkStart.Contains(Tool))
                ToolsPendingWorkStart.Enqueue(Tool);
        }

        public void RemoveFromStartWorkQueue(ILaserToolKernel Tool)
        {
            if (!ToolsPendingWorkStart.Contains(Tool)) return;

            var list = ToolsPendingWorkStart.ToList();
            list.Remove(Tool);
            ToolsPendingWorkStart.Clear();

            foreach (ILaserToolKernel tool in list)
            {
                ToolsPendingWorkStart.Enqueue(tool);
            }
        }

        public bool CanStartWork(ILaserToolKernel Tool)
        {
            if (!ToolsPendingWorkStart.Contains(Tool)) return false;
            if (CurrentlyWorkingTools.Contains(Tool)) return false;
            if (ToolsPendingWorkStart.Peek() == Tool)
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
