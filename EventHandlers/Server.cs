namespace AdvancedSubclassingRedux.EventHandlers
{
    public class Server
    {
        public static void OnRestartingRound()
        {
            Tracking.PlayersWithClasses.Clear();
            Tracking.PlayersJustLostClass.Clear();
            Tracking.PlayerSnapshots.Clear();
            Tracking.PlayerAbilityCooldowns.Clear();
            Tracking.PlayerAbilityUses.Clear();
            Tracking.SubclassesGiven.Clear();
        }
    }
}
