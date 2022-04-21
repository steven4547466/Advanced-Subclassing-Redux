using Exiled.API.Features;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AdvancedSubclassingRedux.Managers
{
    public class SubclassManager
    {
        public static Dictionary<string, Subclass> Subclasses { get; private set; } = new Dictionary<string, Subclass>();
        public static Dictionary<RoleType, List<Subclass>> RolesForClass { get; private set; } = new Dictionary<RoleType, List<Subclass>>();

        public static void ReloadSubclasses()
        {
            Log.Info("Loading subclasses");
            foreach (Subclass subclass in Subclasses.Values)
            {
                subclass.Unload();
            }
            Subclasses.Clear();

            List<string> classPaths = new List<string>();

            if (Directory.Exists(Path.Combine(Paths.Configs, "Subclasses", "global", "classes")))
            {
                classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", "global", "classes")));
                foreach (string directory in Directory.GetDirectories(Path.Combine(Paths.Configs, "Subclasses", "global", "classes")))
                {
                    classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", "global", "classes", directory), "*.yml"));
                }
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses", "global", "classes"));
            }

            if (Directory.Exists(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "classes")))
            {
                classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "classes")));
                foreach (string directory in Directory.GetDirectories(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "classes")))
                {
                    classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "classes", directory), "*.yml"));
                }
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "classes"));
            }

            Log.Debug("Classes in config: " + classPaths.Count, Plugin.Instance.Config.Debug);
            foreach (string path in classPaths)
            {
                Log.Debug("Loading subclass from " + path, Plugin.Instance.Config.Debug);
                string file = Helpers.Read(Path.Combine(Paths.Configs, "Subclasses", path));
                if (file == string.Empty)
                {
                    Log.Warn("Subclass at path " + path + " doesn't exist..?");
                }
                else
                {
                    Subclass subclass = Helpers.Deserializer.Deserialize<Subclass>(file);
                    if (subclass.BoolOptions.TryGetValue("enabled", out bool enabled))
                    {
                        if (!enabled)
                        {
                            Log.Debug("Subclass " + subclass.Name + " is disabled, skipping", Plugin.Instance.Config.Debug);
                            continue;
                        }
                    }
                    Subclasses.Add(subclass.Name, subclass);
                    foreach (RoleType roleType in subclass.AffectsRoles.Keys)
                    {
                        if (!RolesForClass.ContainsKey(roleType))
                            RolesForClass.Add(roleType, new List<Subclass>());
                        RolesForClass[roleType].Add(subclass);
                    }
                }
            }

            WeightChances();
            ConvertAbilityNames();
            Log.Info("Subclasses loaded");
        }

        public static void WeightChances()
        {
            foreach (RoleType roleType in RolesForClass.Keys)
            {
                float totalChance = 0;
                foreach (Subclass subclass in RolesForClass[roleType])
                {
                    totalChance += subclass.AffectsRoles[roleType];
                }

                float chanceTilNow = 0;
                int classesPassed = 1;

                foreach (Subclass subclass in RolesForClass[roleType])
                {
                    subclass.AffectsRoles[roleType] = chanceTilNow + (subclass.AffectsRoles[roleType] / totalChance);
                    chanceTilNow += subclass.AffectsRoles[roleType] / classesPassed;
                    classesPassed++;
                }
            }
        }

        public static void ConvertAbilityNames()
        {
            foreach (Subclass subclass in Subclasses.Values)
            {
                foreach (string abilityName in subclass.Abilities)
                {
                    if (AbilityManager.Abilities.TryGetValue(abilityName, out Ability ability))
                    {
                        subclass.AbilitiesList.Add(ability);
                    }
                    else
                    {
                        Log.Warn("Attempt to add non-existent ability, " + abilityName + ", to class " + subclass.Name + " this is an error on your end. Don't even think about reporting this as a bug.");
                    }
                }
            }
        }

        public static void RemoveClass(Player player)
        {
            if (!Tracking.PlayersWithClasses.TryGetValue(player, out Subclass subclass)) return;

            subclass.CleanUp(player);
        }

        public static void GiveClass(Player player, Subclass subclass)
        {
            Tracking.PlayersWithClasses.Add(player, subclass);

            subclass.OnGive(player);
        }

        public static void MaybeAddClasses(Player player)
        {
            if (!Round.IsStarted) return;
            RemoveClass(player);

            if (RolesForClass.TryGetValue(player.Role, out List<Subclass> subclassPossibilities))
            {
                float rng = Random.Range(0f, 1f);
                foreach (Subclass potentialClass in subclassPossibilities)
                {
                    if (rng <= potentialClass.AffectsRoles[player.Role])
                    {
                        if (potentialClass.IntOptions.TryGetValue("MaxSpawnPerRound", out int maxPerRound))
                        {
                            if (maxPerRound <= 0 || (Tracking.SubclassesGiven.TryGetValue(potentialClass, out int numGiven) && numGiven >= maxPerRound))
                                continue;
                        }

                        if (potentialClass.IntOptions.TryGetValue("MaxAlive", out int maxAlive))
                        {
                            int playersWithThisSubclass = 0;

                            foreach (Subclass subclass in Tracking.PlayersWithClasses.Values)
                            {
                                if (subclass == potentialClass)
                                    playersWithThisSubclass++;
                            }

                            if (playersWithThisSubclass >= maxAlive)
                                continue;
                        }

                        GiveClass(player, potentialClass);
                        break;
                    }
                }
            }
        }
    }
}
