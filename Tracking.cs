using Exiled.API.Features;
using System;
using System.Collections.Generic;

namespace AdvancedSubclassingRedux
{
    public class Tracking
    {
        public static Dictionary<Player, Subclass> PlayersWithClasses = new Dictionary<Player, Subclass>();
        public static Dictionary<Player, Subclass> PlayersJustLostClass = new Dictionary<Player, Subclass>();
        public static Dictionary<Player, PlayerSnapshot> PlayerSnapshots = new Dictionary<Player, PlayerSnapshot>();
        public static Dictionary<Player, Dictionary<Ability, DateTime>> PlayerAbilityCooldowns = new Dictionary<Player, Dictionary<Ability, DateTime>>();
        public static Dictionary<Player, Dictionary<Ability, int>> PlayerAbilityUses = new Dictionary<Player, Dictionary<Ability, int>>();
        public static Dictionary<Subclass, int> SubclassesGiven = new Dictionary<Subclass, int>();
    }
}
