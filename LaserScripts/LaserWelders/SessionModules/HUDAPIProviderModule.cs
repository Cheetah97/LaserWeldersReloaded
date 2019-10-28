using EemRdx.SessionModules;
using Draygo.API;
using Sandbox.ModAPI;
using System.Linq;
using System.Text;

namespace EemRdx.LaserWelders.SessionModules
{
    public interface IHUDAPIProvider : ISessionModule
    {
        HudAPIv2 HudAPI { get; }
    }

    public class HUDAPIProviderModule : SessionModuleBase<ILaserWeldersSessionKernel>, InitializableModule, IHUDAPIProvider
    {
        public HUDAPIProviderModule(ILaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel)
        {
            ScreenDescription = GenerateText();
        }

        public override string DebugModuleName => nameof(HUDAPIProviderModule);
        public HudAPIv2 HudAPI { get; private set; }
        private const ulong TextHudAPIModId = 758597413;

        private string ModName => "Text HUD API";
        private string DeveloperName => "DraygoKorvan and Midspace";
        private string ScreenTitle => $"Laser Welders";
        private string ScreenObjective => $"{ModName}";
        private string ScreenButtonCaption => "Ok, understood";
        private string ScreenDescription;

        void InitializableModule.Init()
        {
            HudAPI = new HudAPIv2();
            CheckForHudAPIInModlist();
        }

        private void CheckForHudAPIInModlist()
        {
            if (!MyAPIGateway.Session.Mods.Any(x => x.PublishedFileId == TextHudAPIModId))
            {
                if (MyAPIGateway.Session.LocalHumanPlayer != null)
                {
                    MyAPIGateway.Utilities.ShowMissionScreen(ScreenTitle, "Mod missing: ", ScreenObjective, ScreenDescription, null, ScreenButtonCaption);
                }
            }
            else
            {
                WriteToLog("CheckForHudAPIInModlist", "Found Text HUD API");
            }
        }

        private string GenerateText()
        {
            StringBuilder Text = new StringBuilder();
            Text.AppendLine($"Laser Welders depend on the Text HUD API mod by {DeveloperName} in order to work properly.");
            Text.AppendLine($"However, Laser Welders have detected that {ModName} is not loaded {(MyAPIGateway.Session.IsServer ? "in your world" : "on this server")}.");
            if (MyAPIGateway.Session.IsServer)
                Text.AppendLine($"Please add {ModName} in your modlist and reload the world.");
            else
                Text.AppendLine($"Please contact your server administrator so that he/she adds {ModName} in the server's modlist.");

            return Text.ToString();
        }
    }
}
