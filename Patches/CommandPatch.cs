using Exiled.API.Features;
using HarmonyLib;
using RemoteAdmin;
using System;
using System.Linq;

namespace AdvancedSubclassingRedux.Patches
{
    [HarmonyPatch(typeof(QueryProcessor), nameof(QueryProcessor.ProcessGameConsoleQuery))]
    public static class CommandPatch
    {
        public static bool Prefix(QueryProcessor __instance, string query)
        {
            Player player = Player.Get(__instance.gameObject);

            if (Tracking.PlayersWithClasses.TryGetValue(player, out Subclass subclass))
            {
                string[] array = query.Trim().Split(QueryProcessor.SpaceArray, 32, StringSplitOptions.RemoveEmptyEntries); // if you need more than 32 arguments, you're doing something wrong
                string command = array[0].ToLower();
                foreach (Ability ability in subclass.AbilitiesList)
                {
                    if (ability.Command == command || (ability.Aliases != null && ability.Aliases.Contains(command)))
                    {
                        if (ability.ExecuteCommand(player, array.Segment(1).ToList()))
                            return false;
                        else
                            return true;
                    }
                }
            }

            return true;
        }
    }
}
