using AdvancedSubclassingRedux.Managers;
using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AdvancedSubclassingRedux
{
    public class Ability
    {
        public string Name { get; set; }

        public bool Enabed { get; set; } = true;

        public Dictionary<string, List<Dictionary<string, object>>> Events { get; set; } = new Dictionary<string, List<Dictionary<string, object>>>();

        public List<Dictionary<string, object>> Update { get; set; } = new List<Dictionary<string, object>>();

        public List<EventInfo> EventInfos { get; set; } = new List<EventInfo>();

        public static Ability Get(string name)
        {
            if (AbilityManager.Abilities.TryGetValue(name, out Ability ability))
                return ability;
            return null;
        }

        public bool Use(Player player)
        {
            if (Tracking.PlayersWithClasses.TryGetValue(player, out Subclass subclass))
            {
                if (subclass.AbilitiesList.Contains(this))
                {
                    if (!subclass.AbilityCooldowns.TryGetValue(Name, out double cooldown))
                        return true;
                    if (Tracking.PlayerAbilityCooldowns[player].TryGetValue(this, out DateTime nextAvilable))
                    {
                        TimeSpan time = nextAvilable - DateTime.Now;
                        if (time > TimeSpan.FromSeconds(0))
                        {
                            if (subclass.StringOptions.TryGetValue("AbilityOnCooldownMessage", out string message))
                            {
                                player.Broadcast(3, message.Replace("{ability}", Name).Replace("{cooldown}", (time.TotalSeconds).ToString("0.0")), Broadcast.BroadcastFlags.Normal, true);
                            }
                            return false;
                        }
                        Tracking.PlayerAbilityCooldowns[player][this] = DateTime.Now.AddSeconds(cooldown);
                        return true;
                    }
                    else
                    {
                        Tracking.PlayerAbilityCooldowns[player][this] = DateTime.Now.AddSeconds(cooldown);
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        public void Unload()
        {
            foreach (EventInfo eventInfo in AbilityManager.EventsConnected.Keys)
            {
                AbilityManager.EventsConnected[eventInfo]--;
                if (AbilityManager.EventsConnected[eventInfo] == 0)
                {
                    AbilityManager.DisconnectFromEvent(eventInfo);
                }
            }
        }
    }
}
