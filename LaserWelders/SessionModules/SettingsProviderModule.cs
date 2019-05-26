using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EemRdx;
using EemRdx.Networking;
using EemRdx.SessionModules;
using Sandbox.ModAPI;

namespace EemRdx.LaserWelders.SessionModules
{
    public interface ISettingsProvider : ISessionModule
    {
        LaserSettings Settings { get; }
    }

    public class SettingsProviderModule : SessionModuleBase<LaserWeldersSessionKernel>, InitializableModule, UpdatableModule, SaveableModule, UnloadableModule
    {
        public override string DebugModuleName { get; } = nameof(SettingsProviderModule);
        public LaserSettings Settings
        {
            get
            {
                return Syncer != null ? Syncer.Data : LaserSettings.Default;
            }
            private set
            {
                if (Syncer != null) Syncer.Set(value);
            }
        }
        private Sync<LaserSettings> Syncer;
        private const string CommChannelTag = "LaserSettingsComm";

        void InitializableModule.Init()
        {
            Syncer = new Sync<LaserSettings>(MySessionKernel.Networker, "LaserWelders SessionSettings", LaserSettings.Default);
			Syncer.Register();
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                LoadSettings();
                MySessionKernel.Networker.RegisterHandler(CommChannelTag, MessageHandler_Server);
            }
            else
            {
                Syncer.Ask();
                MySessionKernel.Networker.RegisterHandler(CommChannelTag, MessageHandler_Client);
            }
            MyAPIGateway.Utilities.MessageEntered += Utilities_MessageEntered;
            WriteToLog("Init", "SettingsProvider initialized");
        }

        private void MessageHandler_Server(NetworkerMessage data)
        {
            if (data.DataTag == CommChannelTag && data.DataDescription == "reloadsettings")
            {
                LoadSettings();
                MySessionKernel.Networker.SendTo(data.SenderID, CommChannelTag, "reloaded", null);
            }
        }

        private void MessageHandler_Client(NetworkerMessage data)
        {
            if (data.DataTag == CommChannelTag && data.DataDescription == "reloaded")
            {
                MyAPIGateway.Utilities.ShowMessage("Laser Tools", "Reloaded settings");
            }
        }

        private void Utilities_MessageEntered(string messageText, ref bool sendToOthers)
        {
            if (MyAPIGateway.Session.LocalHumanPlayer == null) return;
            if ((int)MyAPIGateway.Session.LocalHumanPlayer.PromoteLevel < 4) return;
            if (!messageText.StartsWith("/laserwelders") && !messageText.StartsWith("/lw")) return;
            sendToOthers = false;
            List<string> subArguments = messageText.Split(' ').ToList();
            if (subArguments.Count == 2)
            {
                if (subArguments[1] == "reloadsettings")
                {
                    if (MyAPIGateway.Session.IsServer)
                    {
                        LoadSettings();
                        MyAPIGateway.Utilities.ShowMessage("Laser Tools", "Reloaded settings");
                    }
                    else
                    {
                        MySessionKernel.Networker.SendToServer(CommChannelTag, "reloadsettings", null);
                    }
                }
            }
        }

        void UpdatableModule.Update()
        {
            if (MySessionKernel.Clock.Ticker % 60 * 10 == 0)
            {
                if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
                    Syncer.Ask();
            }
        }

        void LoadSettings()
        {
            bool foundSettings = false;
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("laserweldersv2.sbc", typeof(SettingsProviderModule)))
            {
                using (var Reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("laserweldersv2.sbc", typeof(SettingsProviderModule)))
                {
                    string buffer = Reader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(buffer))
                    {
                        var settings = MyAPIGateway.Utilities.SerializeFromXML<LaserSettings>(buffer);
                        if (settings != null && settings.IsValid())
                        {
                            Settings = settings;
                            foundSettings = true;
                        }
                        else if (settings != null && !settings.IsValid())
                        {
                            WriteToLog(nameof(LoadSettings), $"Settings.ToolTiers: {(settings.ToolTiers != null ? settings.ToolTiers.Count.ToString() : "null")}");//, showOnHud: true, duration: 10000, color: "Red");
                        }
                        else if (settings == null)
                        {
                            WriteToLog(nameof(LoadSettings), $"Settings is null!");//, showOnHud: true, duration: 10000, color: "Red");
                        }
                    }
                }
            }
            
            if (!foundSettings) SaveSettings();
        }

        void SaveSettings()
        {
            if (MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Multiplayer.IsServer)
                return;
            using (var Writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("laserweldersv2.sbc", typeof(SettingsProviderModule)))
            {
                string buffer = MyAPIGateway.Utilities.SerializeToXML(Settings);
                Writer.Write(buffer);
            }
        }

        void SaveableModule.Save()
        {
            SaveSettings();
        }

        void UnloadableModule.UnloadData()
        {
            MyAPIGateway.Utilities.MessageEntered -= Utilities_MessageEntered;
        }

        public SettingsProviderModule(LaserWeldersSessionKernel MySessionKernel) : base(MySessionKernel) { }
    }
}
