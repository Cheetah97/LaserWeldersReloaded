using EemRdx.EntityModules;
using EemRdx.LaserWelders.Helpers;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
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
    public abstract class WorkingModuleBase : EntityModuleBase<LaserToolKernel>, InitializableModule, UpdatableModule
    {
        public WorkingModuleBase(LaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(WorkingModuleBase);

        public bool RequiresOperable { get; } = false;

        public MyEntityUpdateEnum UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;
        protected int Ticker => MyKernel.Session.Clock.Ticker;
        public IMyInventory ToolCargo { get; private set; }

        public bool Inited { get; private set; }

        public const int WorkingFrequency = 10;

        IEnumerator<bool> CurrentWorkingProcess = null;

        protected ToolTierCharacteristics ToolCharacteristics => MyKernel.Session?.Settings?.ToolTiers?.ContainsKey(MyKernel.Block.BlockDefinition.SubtypeName) == true ? MyKernel.Session.Settings.ToolTiers[MyKernel.Block.BlockDefinition.SubtypeName] : default(ToolTierCharacteristics);

        void UpdatableModule.Update()
        {
            //if (Ticker % 120 == 0)
            //    WriteToLog("Update", $"Updating working module, MyKernel.Toggle.Toggled is {MyKernel.Toggle.Toggled.ToString()}, CurrentWorkingProcess is {(CurrentWorkingProcess == null ? "null" : CurrentWorkingProcess.ToString())}");

            if (MyKernel.Toggle.Toggled && MyKernel.OperabilityProvider?.CanOperate == true)
            {
                if (CurrentWorkingProcess == null && Ticker % WorkingFrequency == 0)
                {
                    MyKernel.ResponderModule.ClearStatusReport();
                    CurrentWorkingProcess = WorkingProcess();
                }
            }
            // Despite load distribution across several ticks, a Laser Tool should finish its cycle to avoid weird behavior
            if (CurrentWorkingProcess != null)
            {
                CurrentWorkingProcess.MoveNext();
                bool working = CurrentWorkingProcess.Current;
                if (!working)
                {
                    CurrentWorkingProcess.Dispose();
                    CurrentWorkingProcess = null;
                }
            }
        }

        IEnumerator<bool> WorkingProcess()
        {
            HashSet<IMyCubeGrid> Grids = null; HashSet<IMyCharacter> Characters = null; HashSet<IMyFloatingObject> Flobjes = null; HashSet<IMyVoxelBase> Voxels = null;
            GenerateEntityList(MyKernel.Session.Settings.EnableDrilling, out Grids, out Characters, out Flobjes, out Voxels);
            Grids.Remove(MyKernel.Block.CubeGrid);
            if (Grids.Count == 0 && Characters.Count == 0 && Flobjes.Count == 0 && Voxels.Count == 0)
                yield return false;
            else
                yield return true;
            ProcessCharacters(Characters);
            ProcessFlobjes(Flobjes);
            IEnumerator<bool> CurrentGridProcessor = null;
            if (Grids.Count > 0) CurrentGridProcessor = ProcessGrids(Grids);
            IEnumerator<bool> CurrentVoxelProcessor = null;
            if (Voxels.Count > 0) CurrentVoxelProcessor = ProcessVoxels(Voxels);
            //WriteToLog("Update", "Voxel working process created");
            if (CurrentGridProcessor != null)
            {
                bool working = true;
                while (working)
                {
                    CurrentGridProcessor.MoveNext();
                    working = CurrentGridProcessor.Current;
                    yield return true;
                }
                CurrentGridProcessor.Dispose();
                CurrentGridProcessor = null;
                yield return true;
            }

            if (CurrentVoxelProcessor != null)
            {
                bool working = true;
                while (working)
                {
                    CurrentVoxelProcessor.MoveNext();
                    working = CurrentVoxelProcessor.Current;
                    //WriteToLog("Update", $"Updating voxel processor, can work further: {working}");
                    yield return true;
                }
                CurrentVoxelProcessor.Dispose();
                CurrentVoxelProcessor = null;
                //WriteToLog("Update", "Voxel working process disposed");
            }
            //WriteToLog("Update", "Exiting main working process");
            yield return false;
        }

        void GenerateEntityList(bool DetectVoxels, out HashSet<IMyCubeGrid> Grids, out HashSet<IMyCharacter> Characters, out HashSet<IMyFloatingObject> Flobjes, out HashSet<IMyVoxelBase> Voxels)
        {
            Grids = new HashSet<IMyCubeGrid>();
            Characters = new HashSet<IMyCharacter>();
            Flobjes = new HashSet<IMyFloatingObject>();
            Voxels = new HashSet<IMyVoxelBase>();

            LineD WorkingRay = new LineD(MyKernel.BeamDrawer.BeamStart, MyKernel.BeamDrawer.BeamEnd);
            List<MyLineSegmentOverlapResult<MyEntity>> Overlaps = new List<MyLineSegmentOverlapResult<MyEntity>>();
            MyGamePruningStructure.GetTopmostEntitiesOverlappingRay(ref WorkingRay, Overlaps);
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
            ToolCargo = MyKernel.Tool.GetInventory(0);
            MyKernel.Tool.Synchronized = true;
        }
    }
}
