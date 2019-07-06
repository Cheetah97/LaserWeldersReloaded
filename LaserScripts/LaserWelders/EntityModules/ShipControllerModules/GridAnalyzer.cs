using Sandbox.ModAPI;
using System;
using System.Text;
using EemRdx.EntityModules;
using VRage.ModAPI;
using Draygo.API;
using System.Linq;
using System.Collections.Generic;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRageMath;
using VRage.Game.ModAPI;
using EemRdx.Extensions;

namespace EemRdx.LaserWelders.EntityModules.ShipControllerModules
{
    public class GridAnalyzerModule : EntityModuleBase<ISCKernel>, InitializableModule, UpdatableModule
    {
        public GridAnalyzerModule(ISCKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(GridAnalyzerModule);
        public bool Inited { get; private set; }
        public static readonly int MaxBlockGps = 10;
        public static readonly int BlockHUDUpdateFrequency = 3 * 60;
        bool UpdatableModule.RequiresOperable { get; } = false;
        MyEntityUpdateEnum UpdatableModule.UpdateFrequency { get; } = MyEntityUpdateEnum.EACH_FRAME;
        private int Ticker => MyKernel.Session.Clock.Ticker;
        private readonly HashSet<IMySlimBlock> BlocksThatShouldBeTracked = new HashSet<IMySlimBlock>();
        private readonly Dictionary<IMySlimBlock, HudAPIv2.SpaceMessage> BlockGPSes = new Dictionary<IMySlimBlock, HudAPIv2.SpaceMessage>();

        void InitializableModule.Init()
        {
            if (Inited) return;

            Inited = true;
        }

        void UpdatableModule.Update()
        {
            if (Ticker % BlockHUDUpdateFrequency == 0) AssessUnbuilt();
            UpdateGPSes();
        }

        private static bool ShouldBlockBeShown(IMySlimBlock Block)
        {
            if (Block.CubeGrid.Physics?.Enabled == true)
            {
                return !Block.IsFullIntegrity || Block.HasDeformation;
            }
            else
            {
                return true;
            }
        }

        private void AssessUnbuilt()
        {
            BlocksThatShouldBeTracked.Clear();
            bool CanContinue = MyKernel.TermControls.ShowBlocksOnHud && MyKernel.HUDModule.SCControlledByLocalPlayer;
            if (!CanContinue) return;
            List<IMyCubeGrid> GridsToShow = GetGridsToShow();
            if (GridsToShow.Count == 0) return;
            List<IMySlimBlock> BlocksToShow = new List<IMySlimBlock>(2000);
            foreach (IMyCubeGrid Grid in GridsToShow)
            {
                Grid.GetBlocks(BlocksToShow, ShouldBlockBeShown);
            }
            
            if (BlocksToShow.Count == 0) return;
            var _BlocksToShow = BlocksToShow.OrderBy(GetBlockIntegrity).ToList();

            for (int i = 0; i < MaxBlockGps && i < BlocksToShow.Count; i++)
            {
                IMySlimBlock Block = _BlocksToShow[i];
                BlocksThatShouldBeTracked.Add(Block);
            }
        }

        private void UpdateGPSes()
        {
            Cleanup();
            bool CanContinue = MyKernel.TermControls.ShowBlocksOnHud && MyKernel.HUDModule.SCControlledByLocalPlayer;
            if (!CanContinue) return;
            CreateNewInstances();
            UpdateInstances();
        }

        private void CreateNewInstances()
        {
            List<IMySlimBlock> BlocksWithoutLabels = BlocksThatShouldBeTracked.Where(x => !BlockGPSes.ContainsKey(x)).ToList();
            foreach (IMySlimBlock Block in BlocksWithoutLabels)
            {
                HudAPIv2.SpaceMessage BlockSpaceMessage = new HudAPIv2.SpaceMessage(new StringBuilder(), Block.CubeGrid.GridIntegerToWorld(Block.Position), MyKernel.SC.WorldMatrix.Up, MyKernel.SC.WorldMatrix.Left, Scale: 0.5, Offset: new Vector2D(-1, 0), Blend: BlendTypeEnum.AdditiveTop);
                BlockGPSes.Add(Block, BlockSpaceMessage);
            }
        }

        private void UpdateInstances()
        {
            foreach (var kvp in BlockGPSes)
            {
                IMySlimBlock Block = kvp.Key;
                HudAPIv2.SpaceMessage BlockSpaceMessage = kvp.Value;
                StringBuilder SpaceMessageText = BlockSpaceMessage.Message;

                SpaceMessageText.Clear();
                int Integrity = (int)Math.Round(GetBlockIntegrity(Block) * 100);
                string blockName = Block.CubeGrid.Physics?.Enabled == true ? Block.BlockDefinition.DisplayNameText : $"[{Block.BlockDefinition.DisplayNameText}]";
                SpaceMessageText.Append($"{GetIntegrityColoration(Block)}{blockName}: {Integrity}%");
                if (Block.HasDeformation) SpaceMessageText.Append(" (deformed)");

                BlockSpaceMessage.WorldPosition = Block.CubeGrid.GridIntegerToWorld(Block.Position);
                BlockSpaceMessage.Up = MyAPIGateway.Session.Camera.WorldMatrix.Up;
                BlockSpaceMessage.Left = MyAPIGateway.Session.Camera.WorldMatrix.Left;
                BlockSpaceMessage.Visible = true;
            }
        }

        private static string GetIntegrityColoration(IMySlimBlock Block)
        {
            Color color;
            if (Block.CubeGrid.Physics?.Enabled == true)
            {
                float Integrity = GetBlockIntegrity(Block);
                float R = 255 - (255 * Integrity);
                float G = 255 * Integrity;
                color = new Color(R, G, 0);
            }
            else
            {
                color = Color.LightSkyBlue;
            }
            return $"<color={Math.Round((float)color.R)},{Math.Round((float)color.G)},{(float)color.B}>";
        }

        private static float GetBlockIntegrity(IMySlimBlock Block)
        {
            if (Block.CubeGrid.Physics?.Enabled != true) return 0;
            return (Block.BuildIntegrity - Block.CurrentDamage) / Block.MaxIntegrity;
        }

        private void Cleanup()
        {
            bool CanContinue = MyKernel.TermControls.ShowBlocksOnHud && MyKernel.HUDModule.SCControlledByLocalPlayer;
            List<IMySlimBlock> BlocksThatShouldBeCleaned = null;
            if (CanContinue)
                BlocksThatShouldBeCleaned = BlockGPSes.Keys.Where(x => BlocksThatShouldBeTracked.Contains(x) == false || ShouldBlockBeShown(x)).ToList();
            else
                BlocksThatShouldBeCleaned = BlockGPSes.Keys.ToList();

            foreach (IMySlimBlock Block in BlocksThatShouldBeCleaned)
            {
                var BlockSpaceMessage = BlockGPSes[Block];
                BlockSpaceMessage.DeleteMessage();
                BlockGPSes.Remove(Block);
            }
        }

        private List<IMyCubeGrid> GetGridsToShow()
        {
            Dictionary<long, IMyCubeGrid> GridsOfTools = new Dictionary<long, IMyCubeGrid>();
            foreach (ILaserToolKernel LaserTool in MyKernel.ToolListProvider.Tools)
            {
                var OpGrid = LaserTool.Responder.LastOperatedGrid;
                var OpProjectedGrid = LaserTool.Responder.LastOperatedProjectedGrid;
                if (OpGrid != null && !GridsOfTools.ContainsKey(OpGrid.EntityId)) GridsOfTools.Add(OpGrid.EntityId, OpGrid);
                if (OpProjectedGrid != null && !GridsOfTools.ContainsKey(OpProjectedGrid.EntityId)) GridsOfTools.Add(OpProjectedGrid.EntityId, OpProjectedGrid);
            }
            return GridsOfTools.Values.ToList();
        }
    }
}
