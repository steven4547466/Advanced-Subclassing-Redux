using AdvancedSubclassingRedux.Managers;
using Exiled.API.Features;
using System;
using PlayerEvents = Exiled.Events.Handlers.Player;
using ServerEvents = Exiled.Events.Handlers.Server;

namespace AdvancedSubclassingRedux
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance;

        public override string Author => "Steven4547466";
        public override string Name => "Advanced Subclassing Redux";
        public override string Prefix => "ASR";
        public override Version Version => new Version(1, 0, 0);
        public override Version RequiredExiledVersion => new Version(5, 0, 0, 0);

        public HarmonyLib.Harmony Harmony { get; private set; }

        public override void OnEnabled()
        {
            Instance = this;

            Harmony = new HarmonyLib.Harmony("steven4547466.AdvancedSubclassingRedux-" + DateTime.Now.Ticks.ToString());
            Harmony.PatchAll();

            AbilityManager.ReloadAbilities();
            SubclassManager.ReloadSubclasses();

            PlayerEvents.ChangingRole += EventHandlers.Player.OnChangingRole;

            ServerEvents.RestartingRound += EventHandlers.Server.OnRestartingRound;

            if(!Server.IsHeavilyModded)
              Server.IsHeavilyModded = true;

            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            PlayerEvents.ChangingRole -= EventHandlers.Player.OnChangingRole;

            ServerEvents.RestartingRound -= EventHandlers.Server.OnRestartingRound;

            Harmony.UnpatchAll(Harmony.Id);
            Instance = null;
            base.OnDisabled();
        }
    }
}
