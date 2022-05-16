namespace AdvancedSubclassingRedux.EventHandlers
{
    public class Server
    {
        public static void OnRestartingRound()
        {
            foreach (Exiled.API.Features.Player player in Tracking.PlayersWithClasses.Keys)
            {
                Tracking.PlayersWithClasses[player].CleanUp(player);
            }

            Tracking.PlayersWithClasses.Clear();
            Tracking.PlayersJustLostClass.Clear();
            Tracking.PlayerSnapshots.Clear();
            Tracking.PlayerAbilityCooldowns.Clear();
            Tracking.PlayerAbilityUses.Clear();
            Tracking.SubclassesGiven.Clear();
        }
    }
}
