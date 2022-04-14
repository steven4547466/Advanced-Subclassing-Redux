using AdvancedSubclassingRedux.Managers;
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

        public List<string> Abilities { get; set; } = new List<string>();
        public List<Ability> AbilitiesList { get; set; } = new List<Ability>();

        public Dictionary<string, double> AbilityCooldowns { get; set; } = new Dictionary<string, double>();

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
        }

        public void OnGive(Player player)
        {
            Tracking.PlayerSnapshots.Add(player, new PlayerSnapshot(player));
            Tracking.PlayerLastUsedAbilities.Add(player, new Dictionary<Ability, DateTime>());
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
                Log.Info("Giving behaviour");
                player.GameObject.AddComponent<SubclassBehaviour>();
            }

            Timing.CallDelayed(0.3f, () =>
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
            });
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
                    Timing.RunCoroutine(Helpers.Eval(ability, this.GetType(), this, ability.Update));
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
