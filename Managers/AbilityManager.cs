using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AdvancedSubclassingRedux.Managers
{
    public class AbilityManager
    {
        public static Dictionary<string, Ability> Abilities { get; set; } = new Dictionary<string, Ability>();

        internal static Dictionary<EventInfo, int> EventsConnected { get; set; } = new Dictionary<EventInfo, int>();
        internal static Dictionary<EventInfo, Delegate> Delegates { get; set; } = new Dictionary<EventInfo, Delegate>();
        internal static Dictionary<Type, List<Tuple<Ability, string>>> AbilitiesAndEvents { get; set; } = new Dictionary<Type, List<Tuple<Ability, string>>>();

        public static void Helper<T>(T eventArgs)
        {
            Type type = eventArgs.GetType();
            if (AbilitiesAndEvents.TryGetValue(type, out List<Tuple<Ability, string>> abilities))
            {
                foreach (Tuple<Ability, string> tuple in abilities)
                {
                    Ability ability = tuple.Item1;
                    string eventName = tuple.Item2;
                    Player player = null;
                    bool hasCheck = false;
                    bool skip = true;

                    foreach (Dictionary<string, object> item in ability.Events[eventName])
                    {
                        if (item.TryGetValue("check", out object check))
                        {
                            hasCheck = true;
                            string propToCheckString = (string)check;
                            string[] split = propToCheckString.Split(new string[] { " is ", " != ", " == " }, StringSplitOptions.None);

                            // Handle getting the player and ensuring they have a subclass with the ability.
                            if (split.Length == 1)
                            {
                                player = (Player)type.GetProperty(propToCheckString).GetValue(eventArgs);
                                if (Tracking.PlayersWithClasses.TryGetValue(player, out Subclass subclass))
                                {
                                    if (subclass.AbilitiesList.Contains(ability))
                                    {
                                        skip = false;
                                    }
                                    else
                                    {
                                        skip = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    skip = true;
                                    break;
                                }
                            }
                            else
                            {
                                // Handle other checks.
                                object value = null;
                                if (split[0].Contains('.'))
                                {
                                    string[] split2 = split[0].Trim().Split('.');
                                    value = type.GetProperty(split2[0]).GetValue(eventArgs);
                                    for (int i = 1; i < split2.Length; i++)
                                    {
                                        Type type1 = value.GetType();
                                        PropertyInfo propertyInfo = type1.GetProperty(split2[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
                                        if (propertyInfo == null)
                                        {
                                            value = type1.GetField(split2[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static).GetValue(value);
                                        }
                                        else
                                        {
                                            value = propertyInfo.GetValue(value);
                                        }

                                        if (value == null)
                                        {
                                            skip = true;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    value = type.GetProperty(split[0]).GetValue(eventArgs);
                                }

                                if (skip || value == null)
                                {
                                    skip = true;
                                    break;
                                }

                                if (propToCheckString.Contains(" is "))
                                {
                                    Type expectedType = value?.GetType();
                                    if (expectedType != Type.GetType(split[1].Trim() + ", " + expectedType?.Assembly?.FullName))
                                    {
                                        skip = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    Type expectedType = value?.GetType();
                                    Type t = null;
                                    object valToCheck = null;

                                    string trimmed = split[1].Trim();

                                    if (trimmed != "null")
                                    {
                                        if (split[1].Contains('.'))
                                            t = Type.GetType(split[1].Substring(0, split[1].LastIndexOf('.')) + ", " + expectedType.Assembly.FullName);
                                        else
                                        {
                                            valToCheck = Convert.ChangeType(trimmed, expectedType);
                                            t = valToCheck.GetType();
                                        }

                                        if (expectedType != t)
                                        {
                                            Log.Info("Type mismatch: " + expectedType.FullName + " != " + t.FullName);
                                            skip = true;
                                            break;
                                        }

                                        if (valToCheck == null)
                                        {
                                            if (expectedType.IsEnum)
                                            {
                                                valToCheck = Enum.Parse(expectedType, split[1].Substring(split[1].LastIndexOf('.') + 1).Trim());
                                            }
                                            else
                                            {
                                                valToCheck = Convert.ChangeType(split[1].Substring(split[1].LastIndexOf('.') + 1), expectedType);
                                            }
                                        }
                                    }

                                    if (propToCheckString.Contains(" != "))
                                    {
                                        if (valToCheck != null)
                                        {
                                            if (value.Equals(valToCheck))
                                            {
                                                skip = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (value == null)
                                            {
                                                skip = true;
                                                break;
                                            }
                                        }

                                    }
                                    else if (propToCheckString.Contains(" == "))
                                    {
                                        if (valToCheck != null)
                                        {
                                            if (!value.Equals(valToCheck))
                                            {
                                                skip = true;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            if (value != null)
                                            {
                                                skip = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (skip || !hasCheck)
                        continue;

                    Log.Info(ability.Name + " is being executed!");

                    if (!ability.Use(player))
                        return;

                    Timing.RunCoroutine(Helpers.Eval(type, eventArgs, ability.Events[eventName]));
                }
            }
        }

        internal static void DisconnectFromEvent(EventInfo eventInfo)
        {
            if (Delegates.TryGetValue(eventInfo, out Delegate d))
            {
                eventInfo.RemoveEventHandler(null, d);
                Delegates.Remove(eventInfo);
            }
        }

        public static void ReloadAbilities()
        {
            Log.Info("Loading abilities");
            foreach (Ability ability in Abilities.Values)
            {
                ability.Unload();

            }
            Abilities.Clear();
            AbilitiesAndEvents.Clear();

            List<string> classPaths = new List<string>();

            if (Directory.Exists(Path.Combine(Paths.Configs, "Subclasses", "global", "abilities")))
            {
                classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", "global", "abilities")));
                foreach (string directory in Directory.GetDirectories(Path.Combine(Paths.Configs, "Subclasses", "global", "abilities")))
                {
                    classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", "global", "abilities", directory), "*.yml"));
                }
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses", "global", "abilities"));
            }

            if (Directory.Exists(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "abilities")))
            {
                classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "abilities")));
                foreach (string directory in Directory.GetDirectories(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "abilities")))
                {
                    classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "abilities", directory), "*.yml"));
                }
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "abilities"));
            }

            Assembly exiledAssembly = Loader.GetPlugin("Exiled.Events").Assembly;

            Type[] eventTypes = exiledAssembly.GetTypes().Where(t => t.Namespace == "Exiled.Events.Handlers").ToArray();

            Log.Debug("Abilities in config: " + classPaths.Count, Plugin.Instance.Config.Debug);
            foreach (string path in classPaths)
            {
                Log.Debug("Loading ability from " + path, Plugin.Instance.Config.Debug);
                string file = Helpers.Read(Path.Combine(Paths.Configs, "Subclasses", path));
                if (file == string.Empty)
                {
                    Log.Warn("Ability at path " + path + " doesn't exist..?");
                }
                else
                {
                    Ability ability = Helpers.Deserializer.Deserialize<Ability>(file);
                    if (!ability.Enabed)
                    {
                        Log.Debug("Ability " + ability.Name + " is disabled, skipping", Plugin.Instance.Config.Debug);
                        continue;
                    }
                    foreach (string eventName in ability.Events.Keys)
                    {
                        foreach (Type eventType in eventTypes)
                        {
                            EventInfo eventInfo = eventType.GetEvent(eventName);
                            if (eventInfo != null)
                            {
                                List<Type> args = new List<Type>();

                                foreach (ParameterInfo p in eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters())
                                {
                                    args.Add(p.ParameterType);
                                }

                                if (!Delegates.ContainsKey(eventInfo))
                                {
                                    MethodInfo mi = typeof(AbilityManager).GetMethod("Helper");

                                    Delegate del = Delegate.CreateDelegate(eventInfo.EventHandlerType, mi.MakeGenericMethod(args.ToArray()));
                                    eventInfo.AddEventHandler(null, del);
                                    Delegates.Add(eventInfo, del);
                                }

                                if (!EventsConnected.ContainsKey(eventInfo))
                                    EventsConnected.Add(eventInfo, 0);
                                EventsConnected[eventInfo]++;

                                if (!AbilitiesAndEvents.ContainsKey(args[0]))
                                    AbilitiesAndEvents.Add(args[0], new List<Tuple<Ability, string>>());
                                AbilitiesAndEvents[args[0]].Add(new Tuple<Ability, string>(ability, eventName));
                                ability.EventInfos.Add(eventInfo);
                                break;
                            }
                        }
                    }
                    Abilities.Add(ability.Name, ability);
                }
            }
            Log.Info("Abilities loaded");
        }
    }
}
