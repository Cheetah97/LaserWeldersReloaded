using EemRdx.EntityModules;
using VRage.ModAPI;

namespace EemRdx
{
    public interface IEntityKernel
    {
        IMyEntity Entity { get; }
        string DebugFullName { get; }
        string DebugKernelName { get; }
        ISessionKernel SessionBase { get; }
        /// <summary>
        /// Be aware that this can be null.
        /// </summary>
        IOperabilityProvider OperabilityProvider { get; }

        T GetModule<T>() where T : IEntityModule;
        void Shutdown();
    }

}
