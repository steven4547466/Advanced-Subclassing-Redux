using Exiled.API.Features;
using HarmonyLib;
using PlayerStatsSystem;

namespace AdvancedSubclassingRedux.Patches
{
    [HarmonyPatch(typeof(NineTailedFoxAnnouncer), nameof(NineTailedFoxAnnouncer.AnnounceScpTermination))]
    internal class AnnounceScpTerminationPatch
    {
        public static bool Prefix(ReferenceHub scp, DamageHandlerBase hit)
        {
            Player player = Player.Get(scp);

            if (player != null)
            {
                if (Tracking.PlayersWithClasses.TryGetValue(player, out Subclass subclass))
                {
                    if (subclass.StringOptions.ContainsKey("CassieDeathMessage"))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
