using AdvancedSubclassingRedux.Managers;
using Exiled.API.Enums;
using Exiled.API.Features;
using MEC;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedSubclassingRedux
{
    public class Subclass
    {
        public string Name { get; set; }

        public Dictionary<RoleType, float> AffectsRoles { get; set; } = new Dictionary<RoleType, float>();
        public Dictionary<string, float> AffectsUsers { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> Permissions { get; set; } = new Dictionary<string, float>();
        public List<SubclassSpawnParameter> SpawnParameters { get; set; } = new List<SubclassSpawnParameter>();

        public Dictionary<string, string> StringOptions { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, bool> BoolOptions { get; set; } = new Dictionary<string, bool>();
        public Dictionary<string, int> IntOptions { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, float> FloatOptions { get; set; } = new Dictionary<string, float>();

        public List<Dictionary<ItemType, float>> SpawnItems { get; set; } = new List<Dictionary<ItemType, float>>();
        public Dictionary<AmmoType, ushort> SpawnAmmo { get; set; } = new Dictionary<AmmoType, ushort>();
        public Dictionary<RoomType, float> SpawnLocations { get; set; } = new Dictionary<RoomType, float>();

        public List<string> Abilities { get; set; } = new List<string>();
        public List<Ability> AbilitiesList { get; set; } = new List<Ability>();

        public Dictionary<string, double> AbilityCooldowns { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> InitialAbilityCooldowns { get; set; } = new Dictionary<string, double>();

        public List<Dictionary<string, object>> OnGiven { get; set; } = new List<Dictionary<string, object>>();

        public List<Dictionary<string, object>> OnRemoved { get; set; } = new List<Dictionary<string, object>>();

        public void Unload()
        {

        }

        public void CleanUp(Player player)
        {
            if (Tracking.PlayerSnapshots.TryGetValue(player, out PlayerSnapshot snapshot))
            {
                player.RankName = snapshot.Badge;
                player.RankColor = snapshot.BadgeColor;
                player.DisplayNickname = snapshot.Nickname;
                Tracking.PlayerSnapshots.Remove(player);
            }

            if (player.GameObject.TryGetComponent(out SubclassBehaviour subclassBehaviour))
            {
                subclassBehaviour.Destroy();
            }

            if (StringOptions.TryGetValue("CassieDeathMessage", out string cassieMessage))
            {
                if (StringOptions.TryGetValue("CassieDeathMessageSubtitles", out string cassieSubtitles))
                    Cassie.MessageTranslated(cassieMessage, cassieSubtitles);
                else
                    Cassie.Message(cassieMessage);
            }

            if (OnRemoved != null && OnRemoved.Count > 0)
            {
                Timing.RunCoroutine(Helpers.Eval(typeof(SubclassOnData), new SubclassOnData(player, this), OnRemoved));
            }

            Tracking.PlayersWithClasses.Remove(player);
            Tracking.PlayerAbilityCooldowns.Remove(player);
        }

        public void OnGive(Player player)
        {
            Tracking.PlayerSnapshots.Add(player, new PlayerSnapshot(player));
            Tracking.PlayerAbilityCooldowns.Add(player, new Dictionary<Ability, DateTime>());
            if (StringOptions.TryGetValue("GotClassMessage", out string classMessage))
            {
                player.Broadcast(IntOptions.TryGetValue("GotClassMessageDuration", out int duration) ? (ushort)duration : (ushort)5, classMessage, Broadcast.BroadcastFlags.Normal);
            }

            if (StringOptions.TryGetValue("CassieMessage", out string cassieMessage))
            {
                if (StringOptions.TryGetValue("CassieMessageSubtitles", out string cassieSubtitles))
                    Cassie.MessageTranslated(cassieMessage, cassieSubtitles);
                else
                    Cassie.Message(cassieMessage);
            }

            if (InitialAbilityCooldowns != null)
            {
                foreach (KeyValuePair<string, double> ability in InitialAbilityCooldowns)
                {
                    Ability abilityValue = Ability.Get(ability.Key);
                    if (abilityValue == null) continue;
                    Tracking.PlayerAbilityCooldowns[player][abilityValue] = DateTime.Now.AddSeconds(ability.Value);
                }
            }

            bool giveBehaviour = false;
            foreach (Ability ability in AbilitiesList)
            {
                if (ability.Update != null && ability.Update.Count > 0)
                {
                    giveBehaviour = true;
                    break;
                }
            }

            if (giveBehaviour)
            {
                player.GameObject.AddComponent<SubclassBehaviour>();
            }

            if (OnGiven != null && OnGiven.Count > 0)
            {
                Timing.RunCoroutine(Helpers.Eval(typeof(SubclassOnData), new SubclassOnData(player, this), OnGiven));
            }

            Timing.CallDelayed(0.4f, () =>
            {
                if (SpawnLocations != null && SpawnLocations.Count > 0)
                {
                    float chanceSoFar = 0;
                    float rng = UnityEngine.Random.Range(0f, 100f);
                    foreach (KeyValuePair<RoomType, float> possibleRoom in SpawnLocations)
                    {
                        if (possibleRoom.Value + chanceSoFar >= rng)
                        {
                            if (possibleRoom.Key == RoomType.Unknown) break;
                            Vector3 pos = player.Position;
                            Room room = Room.List.FirstOrDefault(r => r.Type == possibleRoom.Key);
                            if (room != null)
                                pos = room.Position + Vector3.up * 3;
                            player.Position = pos;
                            break;
                        }
                        chanceSoFar += possibleRoom.Value;
                    }
                }
            });

            Timing.CallDelayed(0.1f, () =>
            {
                if (IntOptions.TryGetValue("MaxHealth", out int maxHealth))
                {
                    player.MaxHealth = maxHealth;
                }

                if (IntOptions.TryGetValue("HealthOnSpawn", out int healthOnSpawn))
                {
                    player.Health = healthOnSpawn;
                }

                if (IntOptions.TryGetValue("MaxArmor", out int maxArmor))
                {
                    player.MaxArtificialHealth = maxArmor;
                }

                if (IntOptions.TryGetValue("ArmorOnSpawn", out int armorOnSpawn))
                {
                    player.ArtificialHealth = armorOnSpawn;
                }

                if (BoolOptions.TryGetValue("RemoveDefaultSpawnItems", out bool removeDefaultSpawnItems))
                {
                    if (removeDefaultSpawnItems)
                    {
                        player.ClearInventory();
                    }
                }

                if (SpawnItems != null && SpawnItems.Count > 0)
                {
                    foreach (Dictionary<ItemType, float> possibleItems in SpawnItems)
                    {
                        float chanceSoFar = 0;
                        float rng = UnityEngine.Random.Range(0f, 100f);
                        foreach (KeyValuePair<ItemType, float> item in possibleItems)
                        {
                            if (item.Value + chanceSoFar >= rng)
                            {
                                if (item.Key == ItemType.None) break;
                                player.AddItem(item.Key);
                                break;
                            }
                            chanceSoFar += item.Value;
                        }
                    }
                }

                if (SpawnAmmo != null)
                {
                    foreach (KeyValuePair<AmmoType, ushort> ammo in SpawnAmmo)
                    {
                        if (ammo.Value >= 0)
                        {
                            player.SetAmmo(ammo.Key, ammo.Value);
                        }
                    }
                }

                Vector3 scale = new Vector3(player.Scale.x, player.Scale.y, player.Scale.z);

                if (FloatOptions.TryGetValue("ScaleX", out float scaleX)) scale.x = scaleX;
                if (FloatOptions.TryGetValue("ScaleY", out float scaleY)) scale.y = scaleY;
                if (FloatOptions.TryGetValue("ScaleZ", out float scaleZ)) scale.z = scaleZ;

                player.Scale = scale;

            });
        }
    }

    public class SubclassOnData
    {
        public Player Player { get; set; }

        public Subclass Subclass { get; set; }

        public SubclassOnData(Player player, Subclass subclass)
        {
            Player = player;
            Subclass = subclass;
        }
    }

    public class SubclassSpawnParameter
    {
    }

    public class SubclassBehaviour : MonoBehaviour
    {
        public bool Enabled { get; set; } = true;
        public Player Player { get; set; }
        public List<Ability> Abilities { get; set; }

        private void Awake()
        {
            Player = Player.Get(gameObject);
            Abilities = Tracking.PlayersWithClasses[Player].AbilitiesList.Where(a => a.Update != null && a.Update.Count > 0).ToList();
        }


        private void Update()
        {
            if (Enabled)
            {
                foreach (Ability ability in Abilities)
                {
                    Timing.RunCoroutine(Helpers.Eval(this.GetType(), this, ability.Update));
                }
            }
        }

        public void Destroy()
        {
            Enabled = false;
            DestroyImmediate(this, true);
        }
    }
}
