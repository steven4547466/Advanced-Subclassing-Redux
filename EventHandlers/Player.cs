using AdvancedSubclassingRedux.Managers;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using UnityEngine;

namespace AdvancedSubclassingRedux.EventHandlers
{
    public class Player
    {
        
        public static void OnDied(DiedEventArgs ev)
        {
            if (Tracking.PlayersJustLostClass.TryGetValue(ev.Target, out Subclass subclass))
            {
                foreach (Ability ability in subclass.AbilitiesList)
                {
                    if (ability.OnDied != null && ability.OnDied.Count > 0)
                    {
                        Timing.RunCoroutine(Helpers.Eval(typeof(SubclassOnData), new AbilityOnDiedData(ev.Target, ev.Killer, ev.Handler, subclass), ability.OnDied));
                    }
                }
                Tracking.PlayersJustLostClass.Remove(ev.Target);
            }
        }

        public static void OnSpawned(SpawnedEventArgs ev)
        {
            if (Tracking.PlayersWithClasses.TryGetValue(ev.Player, out Subclass subclass))
                subclass.OnSpawned(ev.Player);
        }

        public static void OnSpawning(SpawningEventArgs ev)
        {
            if (Tracking.PlayersWithClasses.TryGetValue(ev.Player, out Subclass subclass))
            {
                SubclassSpawnData spawnData = subclass.GetSpawnData(ev.Player);
                ev.Player.ReferenceHub.playerMovementSync.AddSafeTime(5f);
                ev.Position = spawnData.SpawnPosition == Vector3.zero ? ev.Position : spawnData.SpawnPosition;
                subclass.OnSpawning(ev.Player);
            }
        }

        public static void OnChangingRole(ChangingRoleEventArgs ev)
        {
            SubclassManager.MaybeAddClasses(ev.Player, ev.NewRole);
            if (Tracking.PlayersWithClasses.TryGetValue(ev.Player, out Subclass subclass))
            {
                SubclassSpawnData spawnData = subclass.GetSpawnData(ev.Player);
                if (subclass.BoolOptions.TryGetValue("RemoveDefaultSpawnItems", out bool removeDefaultItems))
                {
                    if (removeDefaultItems)
                        ev.Items.Clear();
                }
                
                if (spawnData.SpawnItems != null)
                {
                    ev.Items.AddRange(spawnData.SpawnItems);
                }

                if (spawnData.SpawnAmmo != null)
                {
                    if (subclass.BoolOptions.TryGetValue("RemoveDefaultSpawnAmmo", out bool removeDefaultAmmo))
                    {
                        if (removeDefaultAmmo)
                            ev.Ammo.Clear();
                    }
                    
                    foreach (var ammo in spawnData.SpawnAmmo)
                    {
                        if (ev.Ammo.ContainsKey(ammo.Key.GetItemType()))
                            ev.Ammo.Remove(ammo.Key.GetItemType());
                        ev.Ammo.Add(ammo.Key.GetItemType(), ammo.Value);
                    }
                }
            }
        }            
    }
}
