using AdvancedSubclassingRedux.Managers;
using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedSubclassingRedux
{
	public class Tracking
	{
		public static Dictionary<Player, Subclass> PlayersWithClasses = new Dictionary<Player, Subclass>();
		public static Dictionary<Player, PlayerSnapshot> PlayerSnapshots = new Dictionary<Player, PlayerSnapshot>();
	}
}
