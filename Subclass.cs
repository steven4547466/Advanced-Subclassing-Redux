using AdvancedSubclassingRedux.Managers;
using Exiled.API.Enums;
using Exiled.API.Features;
using Interactables.Interobjects.DoorUtils;
using MEC;
using PlayableScps.Interfaces;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AdvancedSubclassingRedux
{
    public class Subclass
    {
        private static Dictionary<Player, SubclassSpawnData> SpawnDataPerPlayer { get; set; } = new Dictionary<Player, SubclassSpawnData>();
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
        public Dictionary<string, float> CustomSpawnLocations { get; set; } = new Dictionary<string, float>();

        public List<string> Abilities { get; set; } = new List<string>();
        public List<Ability> AbilitiesList { get; set; } = new List<Ability>();

        public Dictionary<string, double> AbilityCooldowns { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> InitialAbilityCooldowns { get; set; } = new Dictionary<string, double>();

        public Dictionary<string, int> MaxAbilityUses { get; set; } = new Dictionary<string, int>();

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
                player.BadgeHidden = snapshot.BadgeHidden;
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

            player.Scale = Vector3.one;

            Tracking.PlayersWithClasses.Remove(player);
            Tracking.PlayerAbilityCooldowns.Remove(player);
            SpawnDataPerPlayer.Remove(player);
        }

        public void OnSpawned(Player player)
        {
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
                if (!giveBehaviour && ability.Update != null && ability.Update.Count > 0)
                {
                    giveBehaviour = true;
                }

                if (ability.OnGiven != null && ability.OnGiven.Count > 0)
                {
                    Timing.RunCoroutine(Helpers.Eval(typeof(SubclassOnData), new SubclassOnData(player, this), ability.OnGiven));
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

            if (IntOptions.TryGetValue("ArmorOnSpawn", out int armorOnSpawn))
            {
                var module = player.ReferenceHub.playerStats.GetModule<AhpStat>();
                if (player.CurrentScp is IShielded shielded)
                {
                    var shield = shielded.Shield;
                    int maxArmor = IntOptions.ContainsKey("MaxArmor") ? IntOptions["MaxArmor"] : (int)player.MaxArtificialHealth;
                    float armorDecay = FloatOptions.ContainsKey("ArmorDecay") ? FloatOptions["ArmorDecay"] : 1.2f;
                    float armorEfficacy = FloatOptions.ContainsKey("ArmorEfficacy") ? FloatOptions["ArmorEfficacy"] : 0.7f;
                    float armorSustain = FloatOptions.ContainsKey("ArmorSustain") ? FloatOptions["ArmorSustain"] : 0f;
                    bool persistent = BoolOptions.ContainsKey("ArmorPersists") ? BoolOptions["ArmorPersists"] : false;
                    shield.Limit = maxArmor;
                    shield.CurrentAmount = armorOnSpawn;
                    shield.DecayRate = armorDecay;
                    shield.Efficacy = armorEfficacy;
                    shield.SustainTime = armorSustain;
                    shield.GetType().GetField("Persistant", BindingFlags.Instance | BindingFlags.Public).SetValue(shield, persistent);
                }
                else
                {
                    foreach (var process in player.ActiveArtificialHealthProcesses)
                    {
                        module.ServerKillProcess(process.KillCode);
                    }
                    int maxArmor = IntOptions.ContainsKey("MaxArmor") ? IntOptions["MaxArmor"] : (int)player.MaxArtificialHealth;
                    float armorDecay = FloatOptions.ContainsKey("ArmorDecay") ? FloatOptions["ArmorDecay"] : 1.2f;
                    float armorEfficacy = FloatOptions.ContainsKey("ArmorEfficacy") ? FloatOptions["ArmorEfficacy"] : 0.7f;
                    float armorSustain = FloatOptions.ContainsKey("ArmorSustain") ? FloatOptions["ArmorSustain"] : 0f;
                    bool persistent = BoolOptions.ContainsKey("ArmorPersists") ? BoolOptions["ArmorPersists"] : false;

                    player.AddAhp(armorOnSpawn, maxArmor, armorDecay, armorEfficacy, armorSustain, persistent);
                }
            }

            if (StringOptions.TryGetValue("Badge", out string badge))
            {
                player.BadgeHidden = false;
                player.RankName = badge;
                if (StringOptions.TryGetValue("BadgeColor", out string badgeColor))
                {
                    player.RankColor = badgeColor;
                }
            }

            if (StringOptions.TryGetValue("Nickname", out string nickname))
            {
                player.DisplayNickname = nickname.Replace("{name}", player.Nickname);
            }

            if (StringOptions.TryGetValue("HelpMessage", out string helpMessage))
            {
                player.SendConsoleMessage(helpMessage, "white");
            }

            if (IntOptions.TryGetValue("MaxHealth", out int maxHealth))
            {
                player.MaxHealth = maxHealth;
            }

            if (IntOptions.TryGetValue("HealthOnSpawn", out int healthOnSpawn))
            {
                player.Health = healthOnSpawn;
            }

            Vector3 scale = new Vector3(player.Scale.x, player.Scale.y, player.Scale.z);

            if (FloatOptions.TryGetValue("ScaleX", out float scaleX)) scale.x = scaleX;
            if (FloatOptions.TryGetValue("ScaleY", out float scaleY)) scale.y = scaleY;
            if (FloatOptions.TryGetValue("ScaleZ", out float scaleZ)) scale.z = scaleZ;

            player.Scale = scale;
        }

        public void OnSpawning(Player player)
        {
            
        }

        public void OnGive(Player player)
        {
            Tracking.PlayerSnapshots.Add(player, new PlayerSnapshot(player));
            Tracking.PlayerAbilityCooldowns.Add(player, new Dictionary<Ability, DateTime>());

            if (!Tracking.SubclassesGiven.ContainsKey(this))
                Tracking.SubclassesGiven.Add(this, 1);
            else
                Tracking.SubclassesGiven[this]++;
        }

        public SubclassSpawnData GetSpawnData(Player player)
        {
            if (SpawnDataPerPlayer.TryGetValue(player, out SubclassSpawnData spawnData))
            {
                return spawnData;
            }
            else
            {
                SubclassSpawnData data = new SubclassSpawnData(this);
                SpawnDataPerPlayer.Add(player, data);
                return data;
            }
        }
    }

    public class SubclassSpawnData
    {
        public Vector3 SpawnPosition { get; set; } = Vector3.zero;

        public List<ItemType> SpawnItems { get; set; } = null;

        public Dictionary<AmmoType, ushort> SpawnAmmo { get; set; } = null;

        public SubclassSpawnData(Subclass subclass)
        {
            if (subclass.FloatOptions.TryGetValue("SpawnLocationX", out float spawnX) && subclass.FloatOptions.TryGetValue("SpawnLocationY", out float spawnY) && subclass.FloatOptions.TryGetValue("SpawnLocationZ", out float spawnZ))
            {
                SpawnPosition = new Vector3(spawnX, spawnY, spawnZ);
            }
            else if (subclass.SpawnLocations != null && subclass.SpawnLocations.Count > 0)
            {
                float chanceSoFar = 0;
                float rng = UnityEngine.Random.Range(0f, 100f);
                bool selected = false;
                foreach (KeyValuePair<RoomType, float> possibleRoom in subclass.SpawnLocations)
                {
                    if (possibleRoom.Value + chanceSoFar >= rng)
                    {
                        if (possibleRoom.Key == RoomType.Unknown) break;
                        Room room = Room.List.FirstOrDefault(r => r.Type == possibleRoom.Key);
                        if (room != null)
                        {
                            SpawnPosition = room.Position + Vector3.up * 3;
                            selected = true;
                        }
                        else
                            continue;
                        break;
                    }
                    chanceSoFar += possibleRoom.Value;
                }

                if (!selected && subclass.CustomSpawnLocations != null && subclass.CustomSpawnLocations.Count > 0)
                {
                    foreach (KeyValuePair<string, float> possibleLocation in subclass.CustomSpawnLocations)
                    {
                        if (possibleLocation.Value + chanceSoFar >= rng)
                        {
                            Vector3 pos = GetCustomLocation(possibleLocation.Key);
                            if (pos != Vector3.zero)
                            {
                                SpawnPosition = pos;
                                selected = true;
                            }
                            else
                                continue;
                        }
                        chanceSoFar += possibleLocation.Value;
                    }
                }
            }

            if (subclass.SpawnItems != null && subclass.SpawnItems.Count > 0)
            {
                SpawnItems = new List<ItemType>();
                foreach (Dictionary<ItemType, float> possibleItems in subclass.SpawnItems)
                {
                    float chanceSoFar = 0;
                    float rng = UnityEngine.Random.Range(0f, 100f);
                    foreach (KeyValuePair<ItemType, float> item in possibleItems)
                    {
                        if (item.Value + chanceSoFar >= rng)
                        {
                            if (item.Key == ItemType.None) break;
                            SpawnItems.Add(item.Key);
                            break;
                        }
                        chanceSoFar += item.Value;
                    }
                }
            }
            
            SpawnAmmo = subclass.SpawnAmmo;
        }
        private Vector3 GetCustomLocation(string name)
        {
            switch(name)
            {
                case "173Armory":
                    DoorVariant door = DoorNametagExtension.NamedDoors["173_ARMORY"].TargetDoor;
                    return door.transform.position + new Vector3(1f, 0, 1f);
            }
            return Vector3.zero;
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

    public class PlayerSnapshot
    {
        public string Badge { get; set; }
        public string BadgeColor { get; set; }
        public bool BadgeHidden { get; set; }
        public string Nickname { get; set; }

        public PlayerSnapshot(Player player)
        {
            Badge = player.RankName;
            BadgeColor = player.RankColor;
            BadgeHidden = player.BadgeHidden;
            Nickname = player.DisplayNickname;
        }
    }
}
