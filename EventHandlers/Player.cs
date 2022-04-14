using AdvancedSubclassingRedux.Managers;
using Exiled.Events.EventArgs;

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
