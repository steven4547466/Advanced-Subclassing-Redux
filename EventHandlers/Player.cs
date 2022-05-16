using AdvancedSubclassingRedux.Managers;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using UnityEngine;

namespace AdvancedSubclassingRedux.EventHandlers
{
    public class Player
    {
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
                ev.Position = spawnData.SpawnPosition != Vector3.negativeInfinity ? spawnData.SpawnPosition : ev.Position;
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
                            ev.Items.Clear();
                    }
                    
                    foreach (var ammo in spawnData.SpawnAmmo)
                        ev.Ammo.Add(ammo.Key.GetItemType(), ammo.Value);
                }
            }
        }            
    }
}
