using EemRdx.EntityModules;
using Sandbox.ModAPI;

namespace EemRdx.LaserWelders.EntityModules.PyroboltModules
{
    public interface IPyroboltModule : IEntityModule
    {
        void TryDetonate();
    }

    public class PyroboltModule : EntityModuleBase<IPyroboltKernel>, InitializableModule, IPyroboltModule
    {
        public PyroboltModule(IPyroboltKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(PyroboltModule);

        public bool Inited { get; private set; }
        private string NetworkTag => $"Pyrobolt {MyKernel.Block.EntityId}";
        public void Init()
        {
            if (Inited) return;
            if (MyAPIGateway.Session.IsServer)
                MyKernel.Session.Networker.RegisterHandler(NetworkTag, HandleMessageServer);
            else
                MyKernel.Session.Networker.RegisterHandler(NetworkTag, HandleMessageClient);
            Inited = true;
        }

        private void HandleMessageServer(Networking.NetworkerMessage message)
        {
            if (message.DataDescription == "Detonate") Detonate();
        }

        private void HandleMessageClient(Networking.NetworkerMessage message)
        {

        }

        public void TryDetonate()
        {
            if (!MyKernel.Sorter.Enabled) return;
            if (MyAPIGateway.Session.IsServer)
                Detonate();
            else
                MyKernel.Session.Networker.SendToServer(NetworkTag, "Detonate", null);
        }

        private void Detonate()
        {
            MyKernel.Block.CubeGrid.RazeBlock(MyKernel.Block.Position);
        }
    }
}
