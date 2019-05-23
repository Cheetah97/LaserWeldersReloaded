using EemRdx.EntityModules;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.ModAPI;
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
        Vector3D BlockForwardEnd => Tool.WorldMatrix.Forward * GridBlockSize * (BlockDimensions.Z) / 2;
        Vector3 LaserEmitterPosition
        {
            get
            {
                var EmitterDummy = Tool.Model.GetDummy("Laser_Emitter");
                return EmitterDummy != null ? EmitterDummy.Matrix.Translation : (Vector3)BlockForwardEnd;
            }
        }
        public Vector3D BeamStart => BlockPosition + LaserEmitterPosition;
        public Vector3D BeamEnd => BeamStart + Tool.WorldMatrix.Forward * MyKernel.TermControls.LaserBeamLength * GridBlockSize;// * ToolComp.PowerModule.SuppliedPowerRatio;

        public bool RequiresOperable => true;

        public MyEntityUpdateEnum UpdateFrequency => MyEntityUpdateEnum.EACH_FRAME;

        protected ToolTierCharacteristics ToolCharacteristics => MyKernel.Session?.Settings?.ToolTiers?.ContainsKey(MyKernel.Block.BlockDefinition.SubtypeName) == true ? MyKernel.Session.Settings.ToolTiers[MyKernel.Block.BlockDefinition.SubtypeName] : default(ToolTierCharacteristics);

        public bool Inited { get; private set; }

        public void DrawBeam(Vector4 Internal, Vector4 External)
        {
            if (MyAPIGateway.Session.Player == null) return;
            Vector3D BeamStart = this.BeamStart;
            Vector3D BeamEnd = this.BeamEnd;
            MySimpleObjectDraw.DrawLine(BeamStart, BeamEnd, MyStringId.GetOrCompute("WeaponLaser"), ref Internal, 0.1f);
            MySimpleObjectDraw.DrawLine(BeamStart, BeamEnd, MyStringId.GetOrCompute("WeaponLaser"), ref External, 0.2f);
        }

        void UpdatableModule.Update()
        {
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
            }
        }

        public void Init()
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