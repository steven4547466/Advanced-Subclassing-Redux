using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdvancedSubclassingRedux.Managers;
using Exiled.API.Features;
using HarmonyLib;

namespace AdvancedSubclassingRedux.Patches
{
    [HarmonyPatch(typeof(CharacterClassManager), nameof(CharacterClassManager.SetClassIDAdv))]
    public static class RoleChanged
    {
        internal static Dictionary<Player, DateTime> Cooldowns = new Dictionary<Player, DateTime>();
        public static void Postfix(CharacterClassManager __instance)
        {
            Player player = Player.Get(__instance._hub);
            if (Cooldowns.TryGetValue(player, out DateTime cooldown))
            {
                if (cooldown > DateTime.Now)
                    return;
                Cooldowns[player] = DateTime.Now.AddSeconds(0.5);
            } 
            else
            {
                Cooldowns.Add(player, DateTime.Now.AddSeconds(0.5));
            }
            
            SubclassManager.MaybeAddClasses(player);
        }
    }
}
