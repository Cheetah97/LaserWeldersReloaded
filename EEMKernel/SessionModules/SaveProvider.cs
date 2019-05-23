using System;
using System.Collections.Generic;

namespace EemRdx.SessionModules
{
    public interface ISaveProvider : ISessionModule
    {
        Guid StorageGuid { get; }

        void Subscribe(Action SaveAction);
        void Unsubscribe(Action SaveAction);
    }

    /// <summary>
    /// This is for entity modules, as they can't determine by themselves when to save
    /// </summary>
    public class SaveProvider : SessionModuleBase<ISessionKernel>, SaveableModule, ISaveProvider
    {
        private static HashSet<Action> SaveActions = new HashSet<Action>();
        public override string DebugModuleName { get; } = "SaveProvider";
        public Guid StorageGuid => MySessionKernel.StorageGuid;
        public SaveProvider(ISessionKernel MySessionKernel) : base(MySessionKernel) { }

        public void Subscribe(Action SaveAction)
        {
            SaveActions.Add(SaveAction);
        }

        public void Unsubscribe(Action SaveAction)
        {
            SaveActions.Remove(SaveAction);
        }

        public void Save()
        {
            foreach (Action SaveAction in SaveActions)
                SaveAction?.Invoke();
        }
    }
}
