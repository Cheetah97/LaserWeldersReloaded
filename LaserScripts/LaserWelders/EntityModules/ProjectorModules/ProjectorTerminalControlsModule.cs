using EemRdx.EntityModules;
using EemRdx.LaserWelders.SessionModules;
using EemRdx.Networking;
using Sandbox.ModAPI;
using System;

namespace EemRdx.LaserWelders.EntityModules.ProjectorModules
{
    public class ProjectorTermHelperModule : TerminalControlsHelperModuleBase<IProjectorKernel, ProjectorTermControlsGeneratorModule>
    {
        public ProjectorTermHelperModule(IProjectorKernel MyKernel) : base(MyKernel) { }

        public override string DebugModuleName { get; } = nameof(ProjectorTermHelperModule);
    }
}
