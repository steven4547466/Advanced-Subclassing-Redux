using AdvancedSubclassingRedux.Managers;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedSubclassingRedux.EventHandlers
{
	public class Player
	{
		System.Random rnd = new System.Random();
		
		public static void OnChangingRole(ChangingRoleEventArgs ev)
		{
			SubclassManager.MaybeAddClasses(ev.Player, ev);
		}

		public static void OnPlayerHurt(HurtingEventArgs ev)
		{
			if (Tracking.PlayersWithClasses.TryGetValue(ev.Target, out Subclass subclass))
			{
				foreach (Ability ability in subclass.AbilitiesList)
				{
					if (ability.Events.ContainsKey("OnPlayerHurt"))
					{
						Type type = ev.GetType();
						
						foreach (string name in ability.Events["OnPlayerHurt"].Keys)
						{
							if (name.StartsWith("set_"))
							{
								string valName = name.Substring(4);
								PropertyInfo prop = type.GetProperty(valName, BindingFlags.Public | BindingFlags.Instance);
								prop?.SetValue(ev, Convert.ChangeType(ability.Events["OnPlayerHurt"][name], prop.PropertyType));
							}
						}
					}
				}
			}
		}
	}
}
