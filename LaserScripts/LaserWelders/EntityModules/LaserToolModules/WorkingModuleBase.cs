using EemRdx.EntityModules;
using EemRdx.LaserWelders.Helpers;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public abstract class WorkingModuleBase : EntityModuleBase<LaserToolKernel>, InitializableModule, UpdatableModule, ClosableModule
    {
        public WorkingModuleBase(LaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(WorkingModuleBase);

        public bool RequiresOperable { get; } = false;

        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;
        protected int Ticker => MyKernel.Session.Clock.Ticker;
        public IMyInventory ToolCargo => MyKernel.Inventory.ToolCargo;

        public bool Inited { get; private set; }

        public const int NominalWorkingFrequency = 10;

        IEnumerator<bool> CurrentWorkingProcess = null;

        protected ToolTierCharacteristics ToolCharacteristics => MyKernel.Session?.Settings?.ToolTiers?.ContainsKey(MyKernel.Block.BlockDefinition.SubtypeName) == true ? MyKernel.Session.Settings.ToolTiers[MyKernel.Block.BlockDefinition.SubtypeName] : default(ToolTierCharacteristics);
        Stopwatch watch = new Stopwatch();
        protected int LastWorkingProcessTook { get; private set; } = 0;
        protected int DueRestartTick { get; private set; } = NominalWorkingFrequency * 5;
        void UpdatableModule.Update()
        {
            if (MyKernel.Toggle.Toggled && MyKernel.OperabilityProvider?.CanOperate == true)
            {
                if (CurrentWorkingProcess == null && Ticker >= DueRestartTick)
                {
                    MyKernel.Session.PerformanceLimiter.AddToStartWorkQueue(MyKernel);
                    if (MyKernel.Session.PerformanceLimiter.CanStartWork(MyKernel))
                    {
                        MyKernel.ResponderModule.ClearStatusReport();
                        LastWorkingProcessTook = 0;
                        CurrentWorkingProcess = WorkingProcess();
                    }
                }
            }
            else
            {
                MyKernel.Session.PerformanceLimiter.RemoveFromStartWorkQueue(MyKernel);
            }

            // Because of load distribution across several ticks, a Laser Tool should finish its cycle
            // even if it is destroyed in order to avoid weird behavior
            if (CurrentWorkingProcess != null)
            {
                watch.Start();
                bool caught = false;
                try
                {
                    CurrentWorkingProcess.MoveNext();
                }
                catch (Exception Scrap)
                {
                    caught = true;
                    LogError("Update", "CurrentWorkingProcess threw an error", Scrap);
                }
                watch.Stop();
                LastWorkingProcessTook++;
                bool working = CurrentWorkingProcess.Current == true && caught == false;
                if (!working)
                {
                    CurrentWorkingProcess.Dispose();
                    CurrentWorkingProcess = null;
                    MyKernel.Session.PerformanceLimiter.RegisterWorkFinished(MyKernel);
                    if (MyAPIGateway.Session.IsServer && MyKernel.Session.Settings.Debug)
                        WriteToLog($"Update[{MyKernel.Block.CustomName}]", $"Last cycle took {Math.Round(watch.Elapsed.TotalMilliseconds, 3)}ms and {LastWorkingProcessTook} ticks", EemRdx.SessionModules.LoggingLevelEnum.ProfilingLog);
                    watch.Reset();
                }
            }
            if (Ticker % 10 == 0 && MyKernel.Session.Settings.Debug)
                MyKernel.Block.RefreshCustomInfo();
        }

        IEnumerator<bool> WorkingProcess()
        {
            HashSet<IMyCubeGrid> Grids = null; HashSet<IMyCharacter> Characters = null; HashSet<IMyFloatingObject> Flobjes = null; HashSet<IMyVoxelBase> Voxels = null;

            GenerateEntityList(MyKernel.Session.Settings.EnableDrilling && MyKernel.TermControls.ToolMode == LaserTerminalControlsModule.ToolModeGrinder, out Grids, out Characters, out Flobjes, out Voxels);

            Grids.Remove(MyKernel.Block.CubeGrid);
            if (Grids.Count == 0 && Characters.Count == 0 && Flobjes.Count == 0 && Voxels.Count == 0)
                yield return false;

            MyKernel.Session.PerformanceLimiter.RegisterWorkStarted(MyKernel);

            ProcessCharacters(Characters);
            ProcessFlobjes(Flobjes);
            IEnumerator<bool> CurrentGridProcessor = null;
            if (Grids.Count > 0) CurrentGridProcessor = ProcessGrids(Grids);
            IEnumerator<bool> CurrentVoxelProcessor = null;
            if (Voxels.Count > 0) CurrentVoxelProcessor = ProcessVoxels(Voxels);

            if (CurrentGridProcessor != null)
            {
                bool working = true;
                while (working)
                {
                    CurrentGridProcessor.MoveNext();
                    working = CurrentGridProcessor.Current;
                    if (working) yield return true;
                }
                CurrentGridProcessor.Dispose();
                CurrentGridProcessor = null;
               if (CurrentVoxelProcessor != null) yield return true;
            }

            if (CurrentVoxelProcessor != null)
            {
                bool working = true;
                while (working)
                {
                    CurrentVoxelProcessor.MoveNext();
                    working = CurrentVoxelProcessor.Current;
                    if (working) yield return true;
                }
                CurrentVoxelProcessor.Dispose();
                CurrentVoxelProcessor = null;
            }

            MyKernel.Session.PerformanceLimiter.RegisterWorkFinished(MyKernel);
            DueRestartTick = Ticker + NominalWorkingFrequency;
            
            yield return false;
        }

        void GenerateEntityList(bool DetectVoxels, out HashSet<IMyCubeGrid> Grids, out HashSet<IMyCharacter> Characters, out HashSet<IMyFloatingObject> Flobjes, out HashSet<IMyVoxelBase> Voxels)
        {
            Grids = new HashSet<IMyCubeGrid>();
            Characters = new HashSet<IMyCharacter>();
            Flobjes = new HashSet<IMyFloatingObject>();
            Voxels = new HashSet<IMyVoxelBase>();

            LineD WorkingRay = new LineD(MyKernel.BeamDrawer.BeamStart, MyKernel.BeamDrawer.BeamEnd);
            List<MyLineSegmentOverlapResult<MyEntity>> Overlaps = new List<MyLineSegmentOverlapResult<MyEntity>>(50);
            MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref WorkingRay, Overlaps);
            if (Overlaps.Count == 0) return;

            if (DetectVoxels)
                Overlaps.Select(x => x.Element as IMyEntity).SortByType(Grids, Characters, Flobjes, Voxels);
            else
                Overlaps.Select(x => x.Element as IMyEntity).SortByType(Grids, Characters, Flobjes);
        }

        protected abstract void ProcessCharacters(HashSet<IMyCharacter> Characters);
        protected abstract void ProcessFlobjes(HashSet<IMyFloatingObject> Flobjes);
        protected abstract IEnumerator<bool> ProcessGrids(HashSet<IMyCubeGrid> Grids);
        protected abstract IEnumerator<bool> ProcessVoxels(HashSet<IMyVoxelBase> Voxels);

        void InitializableModule.Init()
        {
            MyKernel.Tool.Synchronized = true;
            MyKernel.Block.AppendingCustomInfo += Block_AppendingCustomInfo;
        }

        private void Block_AppendingCustomInfo(IMyTerminalBlock block, StringBuilder info)
        {
            int MaxToolUpdatePerTick = MyKernel.Session.Settings.MaxToolUpdatePerTick;
            int ToolsPending = MyKernel.Session.PerformanceLimiter.ToolsPendingWork;
            int ToolsWorking = MyKernel.Session.PerformanceLimiter.ToolsWorking;
            float ToolCapacity = 100;
            if (ToolsPending + ToolsWorking > MaxToolUpdatePerTick)
            {
                ToolCapacity = (float)Math.Round((MaxToolUpdatePerTick / (float)(ToolsPending + ToolsWorking)) * 100, 2);
            }
            info.AppendLine($"Tool capacity: {ToolCapacity}%");
            if (MyKernel.Session.Settings.Debug)
            {
                info.AppendLine($"Tools working: {ToolsWorking}");
                info.AppendLine($"Tools pending: {ToolsPending}");
            }
            if (ToolCapacity < 100)
            {
                info.AppendLine($"Attention: too many tools are enabled, working capacity has been decreased");
            }
        }

        void ClosableModule.Close()
        {
            MyKernel.Block.AppendingCustomInfo -= Block_AppendingCustomInfo;
        }
    }
}
