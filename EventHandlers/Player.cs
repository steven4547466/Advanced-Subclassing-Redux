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
	}
}
