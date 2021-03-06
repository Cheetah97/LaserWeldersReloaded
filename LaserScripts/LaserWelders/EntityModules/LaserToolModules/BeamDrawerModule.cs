﻿using EemRdx.EntityModules;
using EemRdx.LaserWelders.Helpers;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace EemRdx.LaserWelders.EntityModules.LaserToolModules
{
    public interface IBeamDrawer : IEntityModule
    {
        Color InternalBeamColor { get; }
        Color ExternalWeldBeamColor { get; }
        Color ExternalGrindBeamColor { get; }

        Vector3D BeamEnd { get; }
        Vector3D BeamStart { get; }
        int MaxBeamLengthBlocks { get; }
        float MaxBeamLengthM { get; }
        int MinBeamLengthBlocks { get; }
        float MinBeamLengthM { get; }

        void DrawBeam(Vector4 Internal, Vector4 External);
    }

    public class BeamDrawerModule : EntityModuleBase<ILaserToolKernel>, InitializableModule, UpdatableModule, IBeamDrawer
    {
        public BeamDrawerModule(ILaserToolKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(BeamDrawerModule);
        IMyFunctionalBlock Tool => (MyKernel.Entity as IMyFunctionalBlock);
        protected float GridBlockSize => Tool.CubeGrid.GridSize;
        protected Vector3I BlockDimensions => (Tool.SlimBlock.BlockDefinition as MyCubeBlockDefinition).Size;
        protected Vector3D BlockPosition => Tool.GetPosition();
        public Color InternalBeamColor { get; private set; } = Color.Black;
        public Color ExternalWeldBeamColor { get; private set; } = Color.Black;
        public Color ExternalGrindBeamColor { get; private set; } = Color.Black;
        public int MinBeamLengthBlocks => 1;
        public int MaxBeamLengthBlocks
        {
            get
            {
                return (int)ToolCharacteristics.MaxBeamLengthBlocks;
            }
        }
        public float MinBeamLengthM => MinBeamLengthBlocks * GridBlockSize;
        public float MaxBeamLengthM => MaxBeamLengthBlocks * GridBlockSize;
        Vector3D BlockForwardEnd => Tool.WorldMatrix.Forward * GridBlockSize * (BlockDimensions.Z / 2);
        Vector3 LaserEmitterPosition
        {
            get
            {
                var EmitterDummy = Tool.Model.GetDummy("Laser_Emitter");
                return EmitterDummy != null ? EmitterDummy.Matrix.Translation : (Vector3)BlockForwardEnd;
            }
        }
        public Vector3D BeamStart => BlockPosition + LaserEmitterPosition;
        public Vector3D BeamEnd => BeamStart + Tool.WorldMatrix.Forward * (MyKernel.TermControls.LaserBeamLength + (BlockDimensions.Z / 2f)) * GridBlockSize;// * ToolComp.PowerModule.SuppliedPowerRatio;

        public bool RequiresOperable => true;

        public MyEntityUpdateEnum UpdateFrequency => MyEntityUpdateEnum.EACH_FRAME;

        protected ToolTierCharacteristics ToolCharacteristics => MyKernel.Session?.Settings?.ToolTiers?.ContainsKey(MyKernel.Block.BlockDefinition.SubtypeName) == true ? MyKernel.Session.Settings.ToolTiers[MyKernel.Block.BlockDefinition.SubtypeName] : default(ToolTierCharacteristics);

        public bool Inited { get; private set; }

        public void DrawBeam(Vector4 Internal, Vector4 External)
        {
            if (MyAPIGateway.Session.Player == null) return;
            Vector3 VelocityPerTick = GetGridVelocityAtBlock() / 60;
            Vector3D BeamStart = this.BeamStart + VelocityPerTick;
            Vector3D BeamEnd = this.BeamEnd + VelocityPerTick;
            
            // SDR blending is non-transparent and order-dependent, if the smaller beam is drawn before the larger one, it is hidden
            MySimpleObjectDraw.DrawLine(BeamStart, BeamEnd, MyStringId.GetOrCompute("WeaponLaser"), ref External, 0.2f, BlendTypeEnum.SDR);
            MySimpleObjectDraw.DrawLine(BeamStart, BeamEnd, MyStringId.GetOrCompute("WeaponLaser"), ref Internal, 0.05f, BlendTypeEnum.SDR);
        }

        private Vector3 GetGridVelocityAtBlock()
        {
            return MyKernel.Block.CubeGrid.Physics.GetVelocityAtPoint(MyKernel.Block.GetPosition());
        }

        void UpdatableModule.Update()
        {
            if (MyKernel.Session.Clock.Ticker % (15 * 60) == 0) SetBeamColors();

            if (MyKernel.Toggle.Toggled)
            {
                if (MyKernel.TermControls.ToolMode == LaserTerminalControlsModule.ToolModeWelder)
                {
                    DrawBeam(InternalBeamColor, ExternalWeldBeamColor);
                }
                else if (MyKernel.TermControls.ToolMode == LaserTerminalControlsModule.ToolModeGrinder)
                {
                    DrawBeam(InternalBeamColor, ExternalGrindBeamColor);
                }

                if (MyKernel.Session.Settings.Debug && !MyKernel.TermControls.DistanceMode)
                {
                    DrawDebugRayGrid();
                }
            }
        }

        private void DrawDebugRayGrid()
        {
            int RayGridSize = ToolCharacteristics.WelderGrinderWorkingZoneWidth;

            Vector4 _internalBeamColor = InternalBeamColor;
            Vector3D CenterStart = MyKernel.BeamDrawer.BeamStart;
            Vector3D CenterEnd = MyKernel.BeamDrawer.BeamEnd;

            Vector3D UpOffset = Vector3D.Normalize(MyKernel.Block.WorldMatrix.Up) * 0.5;
            Vector3D RightOffset = Vector3D.Normalize(MyKernel.Tool.WorldMatrix.Right) * 0.5;

            var VirtualLineGrid = VectorHelpers.BuildLineGrid(RayGridSize, RayGridSize, CenterStart, CenterEnd, UpOffset, RightOffset);
            VirtualLineGrid.Remove(new LineD(BeamStart, BeamEnd));
            foreach (LineD VirtualLine in VirtualLineGrid)
            {
                MySimpleObjectDraw.DrawLine(VirtualLine.From, VirtualLine.To, MyStringId.GetOrCompute("WeaponLaser"), ref _internalBeamColor, 0.1f);
            }
        }

        void InitializableModule.Init()
        {
            SetBeamColors();
        }

        private void SetBeamColors()
        {
            InternalBeamColor = ToolCharacteristics.InternalBeamColor;
            ExternalWeldBeamColor = ToolCharacteristics.ExternalWeldBeamColor;
            ExternalGrindBeamColor = ToolCharacteristics.ExternalGrindBeamColor;
        }
    }

    public static class ModelHelpers
    {
        public static IMyModelDummy GetDummy(this IMyModel Model, string DummyName)
        {
            Dictionary<string, IMyModelDummy> Dummies = new Dictionary<string, IMyModelDummy>();
            Model.GetDummies(Dummies);
            return Dummies.ContainsKey(DummyName) ? Dummies[DummyName] : null;
        }
    }
}