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
using System.Collections.Concurrent;
using VRage.ModAPI;
using GamelogicHelpers = EemRdx.Extensions.GamelogicHelpers;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public class WorkingModule : WorkingModuleBase
    {
        private int VoxelIterationLimit => ToolCharacteristics.DrillingVoxelsPerTick;
        private float CutoutRadius => ToolCharacteristics.DrillingZoneRadius;
        private float WeldGrindSpeedMultiplier => ToolCharacteristics.WeldGrindSpeedMultiplier;
        private float DrillingYield => ToolCharacteristics.DrillingYield;

        public WorkingModule(LaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(WorkingModule);

        protected override void ProcessCharacters(HashSet<IMyCharacter> Characters)
        {
            if (Characters.Count == 0) return;
            if (!MyAPIGateway.Session.IsServer) return;
            foreach (IMyCharacter Char in Characters)
            {
                LineD WorkingRay = new LineD(MyKernel.BeamDrawer.BeamStart, MyKernel.BeamDrawer.BeamEnd);
                if (Char.WorldAABB.Intersects(ref WorkingRay))
                    Char.DoDamage(VanillaToolConstants.GrinderSpeed * NominalWorkingFrequency / 2, MyDamageType.Grind, true, null, MyKernel.Block.EntityId);
            }
            //WriteToLog("ProcessCharacters", $"Attempting to damage {Characters.Count} characters");
        }

        protected override void ProcessFlobjes(HashSet<IMyFloatingObject> Flobjes)
        {
            if (Flobjes.Count == 0) return;
            if (!MyAPIGateway.Session.IsServer) return;
            float CargoFillRatio = (float)((double)ToolCargo.CurrentVolume / (double)ToolCargo.MaxVolume);
            foreach (IMyFloatingObject Flobj in Flobjes)
            {
                if (CargoFillRatio < 0.75)
                    ToolCargo.PickupItem(Flobj);
                else break;
            }
        }

        protected override IEnumerator<bool> ProcessGrids(HashSet<IMyCubeGrid> Grids)
        {
            List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
            List<LineD> RayGrid = null;

            List<IMyCubeGrid> ProjectedGrids = new List<IMyCubeGrid>(Grids.Count);
            List<IMyCubeGrid> RealGrids = new List<IMyCubeGrid>(Grids.Count);

            foreach (IMyCubeGrid Grid in Grids)
            {
                if (Grid.Physics?.Enabled == true) RealGrids.Add(Grid);
                else ProjectedGrids.Add(Grid);
            }

            bool AnyGridFiring = false;

            foreach (IMyCubeGrid Grid in RealGrids)
            {
                if (IsGridFiring(Grid) == true)
                {
                    AnyGridFiring = true;
                    break;
                }
            }

            if (AnyGridFiring) yield return false;

            int RayGridSize = ToolCharacteristics.WelderGrinderWorkingZoneWidth;
            if (!MyKernel.TermControls.DistanceMode)
                RayGrid = BuildLineGrid(RayGridSize, RayGridSize);
            else
                RayGrid = BuildLineGrid(0, 0);

            if (RayGrid == null || RayGrid.Count == 0) yield return false;

            //WriteToLog("ProcessGrids", $"Attempting to process {RealGrids.Count} real grids and {ProjectedGrids.Count} projected grids");
            if (MyKernel.TermControls.ToolMode == false)
            {
                List<IMyCubeGrid> ownGrids = new List<IMyCubeGrid>();
                foreach (IMyCubeGrid Grid in RealGrids)
                {
                    if (Grid.IsSameConstructAs(MyKernel.Block.CubeGrid))
                        ownGrids.Add(Grid);
                }

                foreach (IMyCubeGrid GridToRemove in ownGrids)
                {
                    RealGrids.Remove(GridToRemove);
                }
            }

            HashSet<IMySlimBlock> ProjectedBlocks = new HashSet<IMySlimBlock>();
            HashSet<IMySlimBlock> RealBlocks = new HashSet<IMySlimBlock>();

            AcquireBlocks(RayGrid, ProjectedGrids, RealGrids, ProjectedBlocks, RealBlocks);

            IEnumerator<bool> CurrentBlockWorkingProcess = null;
            //WriteToLog("ProcessGrids", $"MyKernel.TermControls.ToolMode is {MyKernel.TermControls.ToolMode.ToString()}");
            if (MyKernel.TermControls.ToolMode == true)
            {
                CurrentBlockWorkingProcess = WeldAndPlaceBlocks(RealBlocks, ProjectedBlocks);
            }
            else if (MyKernel.TermControls.ToolMode == false)
            {
                CurrentBlockWorkingProcess = GrindBlocks(RealBlocks);
                //WriteToLog("ProcessGrids", $"Created block processor (grinder)");
            }

            if (CurrentBlockWorkingProcess == null)
            {
                WriteToLog("ProcessGrids", $"Early exit: CurrentBlockWorkingProcess is null, MyKernel.TermControls.ToolMode is {MyKernel.TermControls.ToolMode.ToString()}");
            }
            else
            {
                //WriteToLog("ProcessGrids", $"Starting cycling the block worker");
                bool working = true;
                while (working)
                {
                    bool caught = false;
                    try
                    {
                        CurrentBlockWorkingProcess.MoveNext();
                        working = CurrentBlockWorkingProcess.Current;
                        //WriteToLog("ProcessGrids", $"Block worker yielded {working}");
                    }
                    catch (Exception Scrap)
                    {
                        caught = true;
                        LogError("ProcessGrids", $"Broke at CurrentBlockWorkingProcess.MoveNext()", Scrap);
                    }
                    if (caught)
                    {
                        WriteToLog("ProcessGrids", $"Block worker broke");
                        break;
                    }
                    else
                    {
                        if (working) yield return true;
                        else break;
                    }
                }
                CurrentBlockWorkingProcess?.Dispose();
                CurrentBlockWorkingProcess = null;
                //WriteToLog("ProcessGrids", $"Block worker disposed");
            }
            MyKernel.ResponderModule.UpdateLastOperatedGrid(RealGrids.OrderByDescending(x => (x as MyCubeGrid).BlocksCount).FirstOrDefault());
            MyKernel.ResponderModule.UpdateLastOperatedProjectedGrid(ProjectedGrids.OrderByDescending(x => (x as MyCubeGrid).BlocksCount).FirstOrDefault());

            yield return false;
        }

        private void AcquireBlocks(List<LineD> RayGrid, List<IMyCubeGrid> ProjectedGrids, List<IMyCubeGrid> RealGrids, HashSet<IMySlimBlock> ProjectedBlocks, HashSet<IMySlimBlock> RealBlocks)
        {
            if (ProjectedGrids.Count > 0 && MyKernel.TermControls.ToolMode == true)
            {
                foreach (IMyCubeGrid Grid in ProjectedGrids)
                {
                    if (Grid == null) continue;
                    if (!SafeZonesHelper.IsActionAllowed(Grid.WorldAABB, MySafeZoneAction.Building, MyKernel.Block.EntityId)) continue;
                    Grid.GetBlocksOnRay(RayGrid, ProjectedBlocks, IsBlockPlaceable);
                }
                //yield return true;
            }

            foreach (IMyCubeGrid Grid in RealGrids)
            {
                try
                {
                    if (MyKernel.TermControls.ToolMode == true)
                    {
                        if (!SafeZonesHelper.IsActionAllowed(Grid.WorldAABB, MySafeZoneAction.Welding, MyKernel.Block.EntityId)) continue;
                        Grid.GetBlocksOnRay(RayGrid, RealBlocks, IsBlockWeldable);
                    }
                    else
                    {
                        Stopwatch watch = Stopwatch.StartNew();
                        if (!SafeZonesHelper.IsActionAllowed(Grid.WorldAABB, MySafeZoneAction.Grinding, MyKernel.Block.EntityId)) continue;
                        //watch.Stop();
                        //WriteToLog("ProcessGrids", $"Safe zone check took {Math.Round(watch.Elapsed.TotalMilliseconds, 3)}ms", EemRdx.SessionModules.LoggingLevelEnum.ProfilingLog);
                        //watch.Restart();
                        Grid.GetBlocksOnRay(RayGrid, RealBlocks, IsBlockGrindable);
                        //watch.Stop();
                        //WriteToLog("ProcessGrids", $"Getting blocks took {Math.Round(watch.Elapsed.TotalMilliseconds, 3)}ms", EemRdx.SessionModules.LoggingLevelEnum.ProfilingLog);
                    }
                }
                catch (Exception Scrap)
                {
                    LogError("ProcessGrids", $"Error while processing real grid {Grid?.DisplayName}", Scrap);
                }
            }
        }

        private static bool? IsGridFiring(IMyCubeGrid Grid)
        {
            GridModules.IWeaponsFireDetectionModule GridWFDModule = GamelogicHelpers.GetComponent<GridKernel>(Grid)?.WeaponsFireDetection; ;
            bool? firing = GridWFDModule?.WeaponsFiring;
            return firing;
        }

        static Dictionary<long, List<Vector3I>> BlockedVoxels = new Dictionary<long, List<Vector3I>>();
        protected override IEnumerator<bool> ProcessVoxels(HashSet<IMyVoxelBase> Voxels)
        {
            if (!MyKernel.Session.Settings.EnableDrilling) yield return false;
            if (Voxels.Count == 0) yield return false;
            if (MyKernel.TermControls.ToolMode == true) yield return false;
            if (!SafeZonesHelper.IsActionAllowed(MyKernel.Block.GetPosition(), MySafeZoneAction.Drilling, MyKernel.Block.EntityId)) yield return false;
            //WriteToLog("ProcessVoxels", $"Processing {Voxels.Count} voxels");
            Stopwatch stopwatch = Stopwatch.StartNew();
            LineD WorkingRay = new LineD(MyKernel.BeamDrawer.BeamStart, MyKernel.BeamDrawer.BeamEnd);
            
            foreach (MyVoxelBase Voxel in Voxels.OfType<MyVoxelBase>())
            {
                if (!SafeZonesHelper.IsActionAllowed((Voxel as IMyEntity).WorldAABB, MySafeZoneAction.Drilling, MyKernel.Block.EntityId)) continue;
                if (Voxel.GetOrePriority() == -1) continue; // Idk why, but the same early exit is found in MyShipDrill
                Vector3D? VoxelHit = Voxel.GetClosestPointOnRay(WorkingRay, step: 0.99f);
                if (VoxelHit.HasValue)
                {
                    lock(BlockedVoxels)
                    {
                        if (!BlockedVoxels.ContainsKey(Voxel.EntityId))
                        {
                            BlockedVoxels.Add(Voxel.EntityId, new List<Vector3I>());
                        }
                    }
                    Vector3D hitpos = VoxelHit.Value;
                    //MyVoxelMaterialDefinition Material = Voxel.GetMaterialAt(ref hitpos);
                    Vector3D CutoutSphereCenter = hitpos;// + (-WorkingRay.Direction * (0.6f * CutoutRadius));
                    stopwatch.Stop();
                    //WriteToLog("ProcessVoxels", $"Hit: {Math.Round(Vector3D.Distance(WorkingRay.From, hitpos), 2)}m, cutout: {Math.Round(Vector3D.Distance(WorkingRay.From, CutoutSphereCenter), 2)} m away, Material: {(Material != null ? Material.MaterialTypeName : "null")}");
                    stopwatch.Start();

                    BoundingSphereD Cutout = new BoundingSphereD(CutoutSphereCenter, CutoutRadius);

                    //List<Vector3I> VoxelPoints = Voxel.GetVoxelPointsInSphere(Cutout);
                    Vector3I refCorner;
                    MyStorageData cutoutVoxels = Voxel.GetVoxelCacheInSphere(Cutout, out refCorner);
                    //WriteToLog("ProcessVoxels", $"Cutout cache Min is {Math.Round(Vector3D.Distance(hitpos, Voxel.PositionLeftBottomCorner + refCorner), 2)}m away");
                    //WriteToLog("ProcessVoxels", $"Cutout cache Max is {Math.Round(Vector3D.Distance(hitpos, Voxel.PositionLeftBottomCorner + refCorner + cutoutVoxels.Size3D), 2)}m away");
                    Dictionary<MyVoxelMaterialDefinition, float> Materials = new Dictionary<MyVoxelMaterialDefinition, float>();

                    int nullMaterials = 0;
                    int totalPoints = 0;
                    using (MyStorageData.MortonEnumerator VoxelLoop = new MyStorageData.MortonEnumerator(cutoutVoxels, MyStorageDataTypeEnum.Content))
                    {
                        Dictionary<byte, float> RawMaterials = new Dictionary<byte, float>();
                        Vector3I VoxelInSphereCenter;
                        Vector3 CoordsSystemOutValue;
                        //MyVoxelCoordSystems.WorldPositionToLocalPosition(Cutout.Center, Voxel.PositionComp.WorldMatrix, Voxel.PositionComp.WorldMatrixInvScaled, Voxel.SizeInMetresHalf, out CoordsSystemOutValue);
                        //VoxelInSphereCenter = new Vector3I(CoordsSystemOutValue / 1f) + Voxel.StorageMin;
                        Vector3D sphereCenter = Cutout.Center;
                        MyVoxelCoordSystems.WorldPositionToLocalPosition(Voxel.PositionLeftBottomCorner, ref sphereCenter, out CoordsSystemOutValue);
                        VoxelInSphereCenter = new Vector3I(CoordsSystemOutValue / 1f) + Voxel.StorageMin;
                        Vector3D ReconstructedPosition = Voxel.PositionLeftBottomCorner + VoxelInSphereCenter;
                        //WriteToLog("ProcessVoxels", $"VoxelInSphereCenter is {Math.Round(Vector3D.Distance(hitpos, ReconstructedPosition), 2)}m away from hitpos");
                        double CutoutRadiusSquared = Cutout.Radius * Cutout.Radius;

                        int processedPoints = 0;
                        int skippedPoints = 0;
                        int linearIndex = -1;
                        for (bool move = true; move != false;)
                        {
                            try
                            {
                                move = VoxelLoop.MoveNext();
                            }
                            catch (Exception Scrap)
                            {
                                move = false;
                                LogError("ProcessVoxels", $"KeenSWH's voxel enumerator broke:", Scrap, EemRdx.SessionModules.LoggingLevelEnum.DebugLog);
                            }
                            if (move)
                            {
                                linearIndex++;
                                Vector3I voxelPoint;
                                cutoutVoxels.ComputePosition(linearIndex, out voxelPoint);
                                voxelPoint += refCorner;

                                if (Vector3D.DistanceSquared(ReconstructedPosition, Voxel.PositionLeftBottomCorner + voxelPoint) <= CutoutRadiusSquared)
                                {
                                    bool pointBlocked = true;
                                    lock (BlockedVoxels)
                                    {
                                        pointBlocked = BlockedVoxels[Voxel.EntityId].Contains(voxelPoint);
                                        if (!pointBlocked)
                                        {
                                            BlockedVoxels[Voxel.EntityId].Add(voxelPoint);
                                        }
                                    }
                                    if (!pointBlocked)
                                    {
                                        byte ContentFillLevel = cutoutVoxels.Content(linearIndex);
                                        byte MaterialId = cutoutVoxels.Material(linearIndex);
                                        float MaterialFillRatio = ContentFillLevel / MyVoxelConstants.VOXEL_CONTENT_FULL_FLOAT;

                                        if (!RawMaterials.ContainsKey(MaterialId))
                                            RawMaterials.Add(MaterialId, MaterialFillRatio);
                                        else
                                            RawMaterials[MaterialId] += MaterialFillRatio;

                                        processedPoints++;
                                    }
                                    else skippedPoints++;
                                }
                                //else
                                //{
                                //    WriteToLog("ProcessVoxels", $"Discarding voxel {linearIndex}: distance is {Math.Round(Vector3D.Distance(ReconstructedPosition, Voxel.PositionLeftBottomCorner + voxelPoint), 2)}m");
                                //}

                                int voxelIterationLimit = MyKernel.Session.Settings.SpeedMultiplierAcceleratesDrilling ? VoxelIterationLimit * MyKernel.TermControls.SpeedMultiplier : VoxelIterationLimit;
                                if (processedPoints > 0 && processedPoints % voxelIterationLimit == 0)
                                {
                                    MyKernel.ResponderModule.UpdateStatusReport($"Cutting out {Math.Round(Cutout.Radius * 2, 1)}m sphere:\r\n{linearIndex} voxels processed", append: false);
                                    yield return true;
                                }
                            }
                            totalPoints = linearIndex;
                        }
                        //WriteToLog("ProcessVoxels", $"Found {processedPoints} valid voxel points out of {totalPoints}", EemRdx.SessionModules.LoggingLevelEnum.DebugLog, true, 2000);

                        foreach (KeyValuePair<byte, float> kvp in RawMaterials)
                        {
                            MyVoxelMaterialDefinition material = MyDefinitionManager.Static.GetVoxelMaterialDefinition(kvp.Key);
                            if (material != null)
                            {
                                Materials.Add(material, kvp.Value * DrillingYield);
                            }
                            else
                            {
                                nullMaterials++;
                                continue;
                            }
                        }
                    }
                    //WriteToLog("ProcessVoxels", $"Found {VoxelPoints.Count} valid voxel points", EemRdx.SessionModules.LoggingLevelEnum.DebugLog, true, 2000);
                    //WriteToLog("ProcessVoxels", $"Found {Materials.Count} valid materials{(nullMaterials > 0 ? $" and {nullMaterials} null materials" : "")}", EemRdx.SessionModules.LoggingLevelEnum.DebugLog, true, 2000);
                    //foreach (var kvp in Materials)
                    //{
                    //    WriteToLog("ProcessVoxels", $"Material: {kvp.Key.MaterialTypeName} (mined ore: {(kvp.Key.MinedOre != null ? kvp.Key.MinedOre : "null")}), amount: {kvp.Value}");
                    //}

                    Dictionary<MyObjectBuilder_Ore, float> MaterialsToAdd = new Dictionary<MyObjectBuilder_Ore, float>();
                    int nullMinedOres = 0;
                    foreach (KeyValuePair<MyVoxelMaterialDefinition, float> kvp in Materials)
                    {
                        MyVoxelMaterialDefinition material = kvp.Key;
                        if (string.IsNullOrWhiteSpace(material.MinedOre))
                        {
                            nullMinedOres++;
                            continue;
                        }
                        try
                        {
                            if (MyKernel.TermControls.DumpStone && material.MinedOre == "Stone") continue;
                            MyObjectBuilder_Ore oreBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(material.MinedOre);
                            oreBuilder.MaterialTypeName = new MyStringHash?(material.Id.SubtypeId);
                            MyPhysicalItemDefinition oreDef = MyDefinitionManager.Static.GetPhysicalItemDefinition(oreBuilder);
                            float MinedAmountInKg = (kvp.Value / 1000 / oreDef.Volume) * oreDef.Mass * kvp.Key.MinedOreRatio * 32;
                            stopwatch.Stop();
                            //WriteToLog("ProcessVoxels", $"Found material {material.MaterialTypeName}, {Math.Round(kvp.Value)} points, mined amount would be {Math.Round(MinedAmountInKg)} kg");
                            stopwatch.Start();
                            MaterialsToAdd.Add(oreBuilder, MinedAmountInKg);
                        }
                        catch (Exception Scrap)
                        {
                            LogError("ProcessVoxels().dict_Materials.Iterate", $"Unknown error occurred on material {material.MinedOre}", Scrap);
                        }
                    }
                    
                    Stopwatch cutoutWatch = Stopwatch.StartNew();
                    Voxel.RootVoxel.PerformCutOutSphereFast(Cutout.Center, (float)Cutout.Radius, true);
                    cutoutWatch.Stop();
                    //WriteToLog("ProcessVoxels", $"Sphere was cut in {cutoutWatch.ElapsedTicks} ticks, which is {Math.Round(cutoutWatch.Elapsed.TotalMilliseconds, 4)} ms");

                    foreach (var kvp in MaterialsToAdd)
                    {
                        MyDefinitionId OreId = kvp.Key.GetId();
                        MyItemType oreItemType = new MyItemType(OreId.TypeId, OreId.SubtypeId);
                        float amountToAdd = kvp.Value;
                        while (amountToAdd > 0.1f)
                        {
                            while (((float)ToolCargo.CurrentVolume / (float)ToolCargo.MaxVolume) > 0.9f) yield return true;

                            MyInventoryItem oreItem = new MyInventoryItem(oreItemType, 0, (VRage.MyFixedPoint)amountToAdd);
                            float FittingAmount = (float)(ToolCargo as Sandbox.Game.MyInventory).ComputeAmountThatFits(OreId);
                            if (FittingAmount > amountToAdd) FittingAmount = amountToAdd;
                            ToolCargo.AddItems((VRage.MyFixedPoint)FittingAmount, kvp.Key);
                            amountToAdd -= FittingAmount;
                            //MyKernel.ResponderModule.UpdateStatusReport($"Adding {Math.Round(FittingAmount)}u of {OreId.SubtypeId}, {amountToAdd} remains", append: false);
                        }
                    }
                }
            }
            stopwatch.Stop();
            double ElapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            //WriteToLog("ProcessVoxels", $"Voxels processed, elapsed time: {stopwatch.ElapsedTicks} ticks, which is {Math.Round(ElapsedMs, 3)} ms");

            yield return false;
        }

        IEnumerator<bool> WeldAndPlaceBlocks(ICollection<IMySlimBlock> RealBlocks, ICollection<IMySlimBlock> ProjectedBlocks)
        {
            if (RealBlocks == null || ProjectedBlocks == null) yield return false;
            if (RealBlocks.Count == 0 && ProjectedBlocks.Count == 0) yield return false;
            if (MyKernel.TermControls.ToolMode == false) yield return false;
            if (!SafeZonesHelper.IsActionAllowed(MyKernel.Block.GetPosition(), MySafeZoneAction.Welding | MySafeZoneAction.Building, MyKernel.Block.EntityId)) yield return false;

            bool ShouldContinue = RealBlocks.Count > 0 || ProjectedBlocks.Count > 0;
            //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Entry Point: real blocks: {RealBlocks.Count}, projected blocks: {ProjectedBlocks.Count}, should continue: {ShouldContinue}");

            if (!ShouldContinue) yield return false;
            
            if (MyKernel.TermControls.DistanceMode && RealBlocks.Count > 0)
            {
                IMySlimBlock FarthestBlock = RealBlocks.OrderBy(x => Vector3D.DistanceSquared(x.CubeGrid.GridIntegerToWorld(x.Position), MyKernel.Block.GetPosition())).Last();
                RealBlocks.Clear();
                RealBlocks.Add(FarthestBlock);
                //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Selected block for distance mode");
            }

            float SpeedRatio = (VanillaToolConstants.WelderSpeed / RealBlocks.Count) * NominalWorkingFrequency * MyKernel.TermControls.SpeedMultiplier * WeldGrindSpeedMultiplier;
            float BoneFixSpeed = VanillaToolConstants.WelderBoneRepairSpeed * NominalWorkingFrequency;
            IMyProjector Projector = null;
            if (ProjectedBlocks.Count > 0)
                Projector = ((ProjectedBlocks.First().CubeGrid as MyCubeGrid).Projector as IMyProjector);

            //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Checked for projectors");

            if (!MyAPIGateway.Session.CreativeMode)
            {
                Dictionary<string, int> RequiredComponentNames = new Dictionary<string, int>();

                {
                    Action worker = () =>
                    {
                        foreach (IMySlimBlock Block in RealBlocks)
                        {
                            Block.GetMissingComponents(RequiredComponentNames);
                        }
                    };
                    bool completed = false;
                    Action callback = () => completed = true;
                    MyAPIGateway.Parallel.StartBackground(worker, callback);
                    while (!completed) yield return true;
                }

                //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Got missing components");

                foreach (IMySlimBlock Block in ProjectedBlocks)
                {
                    var FirstItem = ((MyCubeBlockDefinition)Block.BlockDefinition).Components[0].Definition.Id;
                    if (RequiredComponentNames.ContainsKey(FirstItem.SubtypeName))
                        RequiredComponentNames[FirstItem.SubtypeName] += 1;
                    else
                        RequiredComponentNames.Add(FirstItem.SubtypeName, 1);
                }

                //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Got missing components for projected blocks");

                Dictionary<MyItemType, float> RequiredComponents = new Dictionary<MyItemType, float>();
                var list = RequiredComponentNames.ToList();

                foreach (KeyValuePair<string, int> kvp in list)
                {
                    //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Trying to generate itemtype...");
                    //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"for {kvp.Key}");
                    MyItemType itemtype = new MyItemType("MyObjectBuilder_Component", kvp.Key);
                    //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Generated item value");
                    //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"(item: {itemtype.SubtypeId}, amount: {kvp.Value})");
                    RequiredComponents.Add(itemtype, kvp.Value);
                }

                //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Reassembled missing dictionary");

                List<IMyTerminalBlock> InventoryOwners = new List<IMyTerminalBlock>(MyKernel.Inventory.InventoryOwners);
                InventoryOwners.Add(MyKernel.Tool);

                InventoryModule.CalculateMissingItems(RequiredComponents, MyKernel.Inventory.GetAggregateItemsFor(InventoryOwners));

                //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Calculated aggregate missing components");

                if (MyKernel.Tool.UseConveyorSystem)
                {
                    Dictionary<MyItemType, float> _RqComponents = new Dictionary<MyItemType, float>(RequiredComponents);
                    foreach (var RequiredComponent in RequiredComponents)
                    {
                        float pulledAmount = MyKernel.Inventory.TryTransferTo(ToolCargo, RequiredComponent.Key, RequiredComponent.Value, InventoryOwners);
                        _RqComponents[RequiredComponent.Key] -= pulledAmount;
                    }
                    RequiredComponents = _RqComponents;
                    //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Pulled missing components");
                }

                MyKernel.ResponderModule.UpdateMissing(RequiredComponents);

                yield return true;

                int weldedBlocks = 0;
                List<IMySlimBlock> UnweldedBlocks = new List<IMySlimBlock>();
                foreach (IMySlimBlock Block in RealBlocks)
                {
                    if (Block.CanContinueBuild(ToolCargo) || Block.HasDeformation)
                    {
                        if (MyKernel.Session.Settings.AllowAsyncWelding)
                        {
                            if (CanWeldInAsyncMode(Block))
                                MyAPIGateway.Parallel.StartBackground(() => Weld(Block, SpeedRatio, BoneFixSpeed));
                            else
                                Weld(Block, SpeedRatio, BoneFixSpeed);
                        }
                        else
                        {
                            Weld(Block, SpeedRatio, BoneFixSpeed);
                        }
                        weldedBlocks++;
                        if (!MyKernel.Session.Settings.AllowAsyncWelding && weldedBlocks != RealBlocks.Count - 1) yield return true;
                    }
                    else
                    {
                        UnweldedBlocks.Add(Block);
                    }
                }

                //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Welded real blocks");

                if (weldedBlocks > 0) yield return true;

                int placedBlocks = 0;
                List<IMySlimBlock> NonplacedBlocksByLimit = new List<IMySlimBlock>();
                List<IMySlimBlock> NonplacedBlocksByResources = new List<IMySlimBlock>();
                List<IMySlimBlock> PlaceableProjectedBlocks = new List<IMySlimBlock>();

                foreach (IMySlimBlock Block in ProjectedBlocks)
                {
                    MyCubeBlockDefinition blockDef = Block.BlockDefinition as MyCubeBlockDefinition;
                    if (MyKernel.Tool.IsWithinWorldLimits(Projector, blockDef.BlockPairName, blockDef.PCU))
                    {
                        var FirstItem = blockDef.Components[0].Definition.Id;
                        if (ToolCargo.GetItemAmount(FirstItem) >= 1)
                        {
                            PlaceableProjectedBlocks.Add(Block);
                        }
                        else
                        {
                            NonplacedBlocksByResources.Add(Block);
                        }
                    }
                    else
                    {
                        NonplacedBlocksByLimit.Add(Block);
                    }
                }

                foreach (IMySlimBlock Block in PlaceableProjectedBlocks)
                {
                    var FirstItem = ((MyCubeBlockDefinition)Block.BlockDefinition).Components[0].Definition.Id;
                    Projector.Build(Block, MyKernel.Tool.OwnerId, MyKernel.Tool.EntityId, false);
                    ToolCargo.RemoveItemsOfType(1, FirstItem);
                    placedBlocks++;
                    if (placedBlocks != ProjectedBlocks.Count - 1) yield return true;
                }

                if (UnweldedBlocks.Count > 0)
                {
                    StringBuilder UnweldedReport = new StringBuilder();
                    UnweldedReport.AppendLine($"Failed to weld {UnweldedBlocks.Count} blocks:");
                    List<IGrouping<MyObjectBuilderType, IMySlimBlock>> unweldedByType = UnweldedBlocks.GroupBy(x => x.BlockDefinition.Id.TypeId).ToList();
                    foreach (IGrouping<MyObjectBuilderType, IMySlimBlock> grouping in unweldedByType)
                    {
                        UnweldedReport.AppendLine($"{grouping.Key.ToString().Replace("MyObjectBuilder_", "")} ({grouping.Count()})");
                    }

                    if (NonplacedBlocksByLimit.Count > 0 || NonplacedBlocksByResources.Count > 0)
                        UnweldedReport.AppendLine();

                    MyKernel.ResponderModule.UpdateStatusReport(UnweldedReport, append: true);
                }

                if (NonplacedBlocksByLimit.Count > 0)
                {
                    StringBuilder NonplacedReport = new StringBuilder();
                    NonplacedReport.AppendLine($"Failed to place {UnweldedBlocks.Count} blocks,");
                    NonplacedReport.AppendLine($"reached the PCU/block limit:");
                    List<IGrouping<MyObjectBuilderType, IMySlimBlock>> unplacedByType = UnweldedBlocks.GroupBy(x => x.BlockDefinition.Id.TypeId).ToList();
                    foreach (IGrouping<MyObjectBuilderType, IMySlimBlock> grouping in unplacedByType)
                    {
                        NonplacedReport.AppendLine($"{grouping.Key.ToString().Replace("MyObjectBuilder_", "")} ({grouping.Count()})");
                    }
                    
                    MyKernel.ResponderModule.UpdateStatusReport(NonplacedReport, append: true);
                }

                if (NonplacedBlocksByResources.Count > 0)
                {
                    StringBuilder NonplacedReport = new StringBuilder();
                    NonplacedReport.AppendLine($"Failed to place {UnweldedBlocks.Count} blocks,");
                    NonplacedReport.AppendLine($"out of resources:");
                    List<IGrouping<MyObjectBuilderType, IMySlimBlock>> unplacedByType = UnweldedBlocks.GroupBy(x => x.BlockDefinition.Id.TypeId).ToList();
                    foreach (IGrouping<MyObjectBuilderType, IMySlimBlock> grouping in unplacedByType)
                    {
                        NonplacedReport.AppendLine($"{grouping.Key.ToString().Replace("MyObjectBuilder_", "")} ({grouping.Count()})");
                    }

                    MyKernel.ResponderModule.UpdateStatusReport(NonplacedReport, append: true);
                }
                //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Placed projected blocks");
            }
            else if (MyAPIGateway.Session.CreativeMode)
            {
                foreach (IMySlimBlock Block in RealBlocks)
                {
                    if (MyKernel.Session.Settings.AllowAsyncWelding)
                    {
                        if (CanWeldInAsyncMode(Block))
                            MyAPIGateway.Parallel.StartBackground(() => Weld(Block, SpeedRatio, BoneFixSpeed));
                        else
                            Weld(Block, SpeedRatio, BoneFixSpeed);
                    }
                    else
                    {
                        Weld(Block, SpeedRatio, BoneFixSpeed);
                    }
                }

                foreach (IMySlimBlock Block in ProjectedBlocks)
                    Projector.Build(Block, MyKernel.Tool.OwnerId, MyKernel.Tool.EntityId, false);
            }

            //MyKernel.SessionBase.Log.DebugLog.WriteToLog($"WeldAndPlaceBlocks", $"Exit Point");
            yield return false;
        }

        IEnumerator<bool> GrindBlocks(ICollection<IMySlimBlock> Blocks)
        {
            if (Blocks.Count == 0) yield return false;
            if (MyKernel.TermControls.ToolMode == true) yield return false;
            
            float SpeedRatio = VanillaToolConstants.GrinderSpeed / (MyKernel.TermControls.DistanceMode ? 1 : Blocks.Count) * NominalWorkingFrequency * MyKernel.TermControls.SpeedMultiplier * WeldGrindSpeedMultiplier;
            if (MyKernel.TermControls.DistanceMode)
            {
                IMySlimBlock ClosestBlock = Blocks.OrderBy(x => Vector3D.DistanceSquared(x.CubeGrid.GridIntegerToWorld(x.Position), MyKernel.Block.GetPosition())).First();
                Blocks.Clear();
                Blocks.Add(ClosestBlock);
            }

            foreach (IMySlimBlock Block in Blocks)
                Block.DoDamage(0, MyStringHash.GetOrCompute("Grind"), true, null, MyKernel.Block.EntityId);

            int grindedBlocks = 0;
            if (MyKernel.Session.Settings.AllowAsyncWelding)
            {
                Action worker = () =>
                {
                    foreach (IMySlimBlock Block in Blocks)
                    {
                        grindedBlocks++;
                        MyAPIGateway.Parallel.StartBackground(() => Grind(Block, SpeedRatio, Raze: false));
                    }
                };
                bool completed = false;
                Action callback = () => completed = true;
                MyAPIGateway.Parallel.StartBackground(worker, callback);
                while (!completed) yield return true;

                List<IMySlimBlock> blocksToRaze = Blocks.Where(x => x.IsFullyDismounted).ToList();
                foreach (IMySlimBlock Block in blocksToRaze)
                    Block.CubeGrid.RazeBlock(Block.Position);
            }
            else
            {
                Stopwatch watch = new Stopwatch();
                foreach (IMySlimBlock Block in Blocks)
                {
                    grindedBlocks++;
                    //WriteToLog($"GrindBlocks", $"Block pre-grind", EemRdx.SessionModules.LoggingLevelEnum.DebugLog);
                    watch.Start();
                    Grind(Block, SpeedRatio);
                    //WriteToLog($"GrindBlocks", $"Block post-grind", EemRdx.SessionModules.LoggingLevelEnum.DebugLog);
                    watch.Stop();
                    if ((grindedBlocks + 1) < Blocks.Count) yield return true;
                }
                watch.Stop();
                if (MyAPIGateway.Session.IsServer && MyKernel.Session.Settings.Debug)
                    WriteToLog($"GrindBlocks[{MyKernel.Block.CustomName}]", $"Grinding in total took {Math.Round(watch.Elapsed.TotalMilliseconds, 3)}ms", EemRdx.SessionModules.LoggingLevelEnum.ProfilingLog);
            }
            
            //WriteToLog($"GrindBlocks", $"Exiting GrindBlocks", EemRdx.SessionModules.LoggingLevelEnum.DebugLog);
            yield return false;
        }

        void Weld(IMySlimBlock Block, float SpeedRatio, float BoneFixRatio)
        {
            if (Block == null) return;
            try
            {
                Block.MoveItemsToConstructionStockpile(ToolCargo);
                Block.IncreaseMountLevel(SpeedRatio, MyKernel.Tool.OwnerId, ToolCargo, BoneFixRatio, MyKernel.Tool.HelpOthers);
            }
            catch (Exception Scrap)
            {
                LogError(nameof(Weld), $"Welding of the block {Extensions.GeneralExtensions.GetTypeName(Block)} failed", Scrap);
            }
        }
        
        void Grind(IMySlimBlock Block, float SpeedRatio, bool Raze = true)
        {
            if (Block == null) return;
            try
            {
                Stopwatch watch = Stopwatch.StartNew();
                Block.DecreaseMountLevel(SpeedRatio, ToolCargo);
                watch.Stop();
                //WriteToLog($"GrindBlocks[{MyKernel.Block.CustomName}]", $"Decreasing mount level took {Math.Round(watch.Elapsed.TotalMilliseconds, 3)}ms", EemRdx.SessionModules.LoggingLevelEnum.ProfilingLog);
                watch.Restart();
                Block.MoveItemsFromConstructionStockpile(ToolCargo);
                watch.Stop();
                //WriteToLog($"GrindBlocks[{MyKernel.Block.CustomName}]", $"Moving items took {Math.Round(watch.Elapsed.TotalMilliseconds, 3)}ms", EemRdx.SessionModules.LoggingLevelEnum.ProfilingLog);
                watch.Restart();
                // This is necessary for compatibility with EEM and other mods which react to damage to their grids
                Block.DoDamage(0, MyStringHash.GetOrCompute("Grind"), true, null, MyKernel.Block.EntityId);
                watch.Stop();
                //WriteToLog($"GrindBlocks[{MyKernel.Block.CustomName}]", $"Doing damage took {Math.Round(watch.Elapsed.TotalMilliseconds, 3)}ms", EemRdx.SessionModules.LoggingLevelEnum.ProfilingLog);
                watch.Restart();

                if (Block.FatBlock?.IsFunctional == false && Block.FatBlock?.HasInventory == true)
                {
                    foreach (IMyInventory Inventory in Block.FatBlock.GetInventories())
                    {
                        if (Inventory.CurrentVolume == VRage.MyFixedPoint.Zero) continue;
                        List<MyInventoryItem> Items = new List<MyInventoryItem>();
                        Inventory.GetItems(Items);

                        foreach (MyInventoryItem Item in Items)
                        {
                            VRage.MyFixedPoint Amount = (VRage.MyFixedPoint)Math.Min((float)(Inventory as Sandbox.Game.MyInventory).ComputeAmountThatFits(Item.Type), (float)Item.Amount);
                            if ((float)Amount > 0)
                                ToolCargo.TransferItemFrom(Inventory, (int)Item.ItemId, null, null, Amount, false);
                        }
                    }
                    watch.Stop();
                    //WriteToLog($"GrindBlocks[{MyKernel.Block.CustomName}]", $"Block has an inventory; cleaning took {Math.Round(watch.Elapsed.TotalMilliseconds, 3)}ms", EemRdx.SessionModules.LoggingLevelEnum.ProfilingLog);
                    watch.Reset();
                }
                
                if (Raze && Block.IsFullyDismounted)
                {
                    watch.Restart();
                    Block.CubeGrid.RazeBlock(Block.Position);
                    watch.Stop();
                    //WriteToLog($"GrindBlocks[{MyKernel.Block.CustomName}]", $"Razing the block took {Math.Round(watch.Elapsed.TotalMilliseconds, 3)}ms", EemRdx.SessionModules.LoggingLevelEnum.ProfilingLog);
                }
            }
            catch (Exception Scrap)
            {
                LogError($"GrindBlocks[{MyKernel.Block.CustomName}]", $"Grinding of the block {Extensions.GeneralExtensions.GetTypeName(Block)} failed", Scrap);
            }
        }

        static bool CanWeldInAsyncMode(IMySlimBlock Block)
        {
            return true;
        }

        static bool CanGrindInAsyncMode(IMySlimBlock Block)
        {
            if (Block is IMyLargeTurretBase) return false;
            return true;
        }

        List<LineD> BuildLineGrid(int HalfHeight, int HalfWidth)
        {
            Vector3D CenterStart = MyKernel.BeamDrawer.BeamStart;
            Vector3D CenterEnd = MyKernel.BeamDrawer.BeamEnd;

            Vector3D UpOffset = Vector3D.Normalize(MyKernel.Block.WorldMatrix.Up) * 0.5;
            Vector3D RightOffset = Vector3D.Normalize(MyKernel.Tool.WorldMatrix.Right) * 0.5;

            return VectorHelpers.BuildLineGrid(HalfHeight, HalfWidth, CenterStart, CenterEnd, UpOffset, RightOffset);
        }

        #region Block separators
        bool IsBlockWeldable(IMySlimBlock Block)
        {
            if (Block.CubeGrid.Physics == null || Block.CubeGrid.Physics.Enabled == false) return false;
            if (Block.IsDestroyed || Block.IsFullyDismounted) return false;
            return !Block.IsFullIntegrity || Block.BuildLevelRatio < 1 || Block.CurrentDamage > 0.1f || Block.HasDeformation;
        }

        bool IsBlockPlaceable(IMySlimBlock Block)
        {
            MyCubeGrid Grid = Block.CubeGrid as MyCubeGrid;
            if (Grid.Projector == null) return false;

            BuildCheckResult CheckResult = (Grid.Projector as IMyProjector).CanBuild(Block, true);
            return CheckResult == BuildCheckResult.OK;
        }

        bool IsBlockGrindable(IMySlimBlock Block)
        {
            MyCubeGrid Grid = Block.CubeGrid as MyCubeGrid;
            if (!Grid.Editable) return false;
            return Grid.Physics?.Enabled == true;
        }
        #endregion

    }
}
