using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedSubclassingRedux.EventHandlers
{
	public class Server
	{
		public static void OnRestartingRound()
		{
			foreach(Exiled.API.Features.Player player in Tracking.PlayersWithClasses.Keys)
			{
				Tracking.PlayersWithClasses[player].CleanUp(player);
			}

			Tracking.PlayersWithClasses.Clear();
			Tracking.PlayerLastUsedAbilities.Clear();
		}
	}
}
