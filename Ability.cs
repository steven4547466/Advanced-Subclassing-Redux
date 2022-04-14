using AdvancedSubclassingRedux.Managers;
using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedSubclassingRedux
{
	public class Ability
	{
		public string Name { get; set; }

		public Dictionary<string, Dictionary<string, object>> Events { get; set; } = new Dictionary<string, Dictionary<string, object>>();

		public List<EventInfo> EventInfos { get; set; } = new List<EventInfo>();

		public bool Use(Player player)
		{
			if (Tracking.PlayersWithClasses.TryGetValue(player, out Subclass subclass))
			{
				if (subclass.AbilitiesList.Contains(this))
				{
					if (!subclass.AbilityCooldowns.TryGetValue(Name, out double cooldown))
						return true;
					if (Tracking.PlayerLastUsedAbilities[player].TryGetValue(this, out DateTime lastUsed))
					{
						TimeSpan time = DateTime.Now - lastUsed;
						if (time < TimeSpan.FromSeconds(cooldown))
						{
							if (subclass.StringOptions.TryGetValue("AbilityOnCooldownMessage", out string message))
							{
								player.Broadcast(3, message.Replace("{ability}", Name).Replace("{cooldown}", (cooldown - time.TotalSeconds).ToString("0.0")), Broadcast.BroadcastFlags.Normal, true);
							}
							return false;
						}
						Tracking.PlayerLastUsedAbilities[player][this] = DateTime.Now;
						return true;
					} 
					else
					{
						Tracking.PlayerLastUsedAbilities[player][this] = DateTime.Now;
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
