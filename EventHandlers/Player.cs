using AdvancedSubclassingRedux.Managers;

namespace AdvancedSubclassingRedux.EventHandlers
{
    public class Player
    {
        public static void OnPlayerSpawned(ReferenceHub hub)
        {
            Exiled.API.Features.Player player = Exiled.API.Features.Player.Get(hub);
            if (player != null) SubclassManager.MaybeAddClasses(player);
        }
    }
}
