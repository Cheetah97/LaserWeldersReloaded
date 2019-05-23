using System;
using EemRdx.SessionModules;

namespace EemRdx
{
    public interface ISessionKernel
    {
        bool DebugEnabled { get; }
        ILogProvider Log { get; }
        uint ModID { get; }
        INetworker Networker { get; }
        ISaveProvider SaveProvider { get; }
        Guid StorageGuid { get; }
        SessionStateEnum SessionState { get; }

        T GetModule<T>() where T : ISessionModule;
    }

    public enum SessionStateEnum
    {
        Unknown,
        Loading,
        Running,
        Unloading
    }
}