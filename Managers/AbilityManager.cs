using Exiled.API.Features;
using Exiled.Loader;
using MEC;
using NCalc;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace AdvancedSubclassingRedux.Managers
{
	public class AbilityManager
	{
		public static Dictionary<string, Ability> Abilities { get; set; } = new Dictionary<string, Ability>();

		internal static Dictionary<EventInfo, int> EventsConnected { get; set; } = new Dictionary<EventInfo, int>();
		internal static Dictionary<EventInfo, Delegate> Delegates { get; set; } = new Dictionary<EventInfo, Delegate>();
		internal static Dictionary<Type, List<Tuple<Ability, string>>> AbilitiesAndEvents { get; set; } = new Dictionary<Type, List<Tuple<Ability, string>>>();

		internal static object EvaluateMath<T>(T eventArgs, Dictionary<string, object> localValues, string expression)
		{
			Expression mathExpression = new Expression(expression);
			PropertyInfo[] props = eventArgs.GetType().GetProperties();

			mathExpression.EvaluateParameter += delegate (string name, ParameterArgs args)
			{
				if (name.Contains("."))
				{
					object curValue = 0;
					string[] split = name.Split('.');
					if (localValues.ContainsKey(split[0]))
					{
						curValue = localValues[split[0]];
						for (int i = 1; i < split.Length; i++)
						{
							curValue = curValue.GetType().GetProperty(split[i]).GetValue(curValue);
						}
					}
					else
					{
						foreach (PropertyInfo prop in props)
						{
							if (prop.Name == split[0])
							{
								curValue = prop.GetValue(eventArgs);
								for (int i = 1; i < split.Length; i++)
								{
									curValue = curValue.GetType().GetProperty(split[i]).GetValue(curValue);
								}
								break;
							}
						}
					}

					args.Result = curValue;
				}
			};
			
			foreach (PropertyInfo property in props)
			{
				mathExpression.Parameters[property.Name] = property.GetValue(eventArgs);
			}

			foreach (KeyValuePair<string, object> keyValuePair in localValues)
			{
				mathExpression.Parameters[keyValuePair.Key] = keyValuePair.Value;
			}

			return mathExpression.Evaluate();
		}

		internal static IEnumerator<float> Eval<T>(Ability ability, string eventName, Type type, T eventArgs)
		{
			Dictionary<string, object> localValues = new Dictionary<string, object>();
			foreach (Dictionary<string, object> item in ability.Events[eventName])
			{
				string name = item.First().Key;
				object value = item.First().Value;

				if (name.StartsWith("set_"))
				{
					string valName = name.Substring(4);
					PropertyInfo prop = type.GetProperty(valName, BindingFlags.Public | BindingFlags.Instance);
					object valOrKey = value;
					if (localValues.TryGetValue((string)valOrKey, out object val))
						prop?.SetValue(eventArgs, Convert.ChangeType(val, prop.PropertyType));
					else
						prop?.SetValue(eventArgs, Convert.ChangeType(valOrKey, prop.PropertyType));
				}
				else if (name.StartsWith("get_"))
				{
					if (((string)value).Contains("."))
					{
						throw new Exception($"Attempted to use '.' while saving local variable in ability: {ability.Name}.\nI literally told you not to use '.' when saving local variables...");
					}
					string valName = name.Substring(4);
					PropertyInfo prop = type.GetProperty(valName, BindingFlags.Public | BindingFlags.Instance);
					localValues[(string)value] = prop?.GetValue(eventArgs);
				}
				else if (name.StartsWith("eval"))
				{
					string saveName = ((string)value).Split('>')[1].Trim();
					if (saveName.Contains("."))
					{
						throw new Exception($"Attempted to use '.' while saving local variable in ability: {ability.Name}.\nI literally told you not to use '.' when saving local variables...");
					}
					string expression = ((string)value).Split('>')[0].Trim();
					var val = EvaluateMath(eventArgs, localValues, expression);
					localValues[saveName] = val;
				}
				else if (name.StartsWith("call"))
				{
					string[] split = ((string)value).Split('>');
					string valname = split[0].Trim();
					string methodName = split[1].Trim();

					if (localValues.TryGetValue(valname, out object val))
					{
						MethodInfo method = val.GetType().GetMethod(methodName);
						if (method != null)
						{
							method.Invoke(val, new object[] { });
						}
						else
						{
							Log.Warn($"Method {methodName} not found in {val.GetType().Name}!");
						}
					}
					else
					{
						Log.Warn($"Local variable {valname} not found!");
					}
				}
				else if (name.StartsWith("wait"))
				{
					yield return Timing.WaitForSeconds((float)Convert.ToDouble(value));
				}
			}

			yield return 0;
		}

		public static void Helper<T>(T eventArgs)
		{
			Type type = eventArgs.GetType();
			if (AbilitiesAndEvents.TryGetValue(type, out List<Tuple<Ability, string>> abilities))
			{
				foreach(Tuple<Ability, string> tuple in abilities)
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
								object value = null;
								if (split[0].Contains('.'))
								{
									string[] split2 = split[0].Trim().Split('.');
									value = type.GetProperty(split2[0]).GetValue(eventArgs);
									for (int i = 1; i < split2.Length; i++)
									{
										PropertyInfo propertyInfo = value.GetType().GetProperty(split2[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
										if (propertyInfo == null)
										{
											value = value.GetType().GetField(split2[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static).GetValue(value);
										}
										else
										{
											value = propertyInfo.GetValue(value);
										}
									}
								}
								else
								{
									value = type.GetProperty(split[0]).GetValue(eventArgs);
								}

								if (propToCheckString.Contains(" is "))
								{
									if (value.GetType() != Type.GetType(split[1].Trim() + ", " + value.GetType().Assembly.FullName))
									{
										skip = true;
										break;
									}
								}
								else
								{
									Type expectedType = value.GetType();
									Type t = Type.GetType(split[1].Substring(0, split[1].LastIndexOf('.')) + ", " + expectedType.Assembly.FullName);
									if (expectedType != t)
									{
										Log.Info("Type mismatch: " + expectedType.FullName + " != " + t.FullName);
										skip = true;
										break;
									}

									object valToCheck = null;
									if (expectedType.IsEnum)
									{
										valToCheck = Enum.Parse(expectedType, split[1].Substring(split[1].LastIndexOf('.') + 1).Trim());
									}
									else
									{
										valToCheck = Convert.ChangeType(split[1].Substring(split[1].LastIndexOf('.') + 1), expectedType);
									}

									if (propToCheckString.Contains(" != "))
									{
										if (value.Equals(valToCheck))
										{
											skip = true;
											break;
										}
									} 
									else if (propToCheckString.Contains(" == "))
									{
										if (value != valToCheck)
										{
											skip = true;
											break;
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

					Timing.RunCoroutine(Eval(ability, eventName, type, eventArgs));
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
					classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", "global", "abilities", directory)));
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
					classPaths.AddRange(Directory.GetFiles(Path.Combine(Paths.Configs, "Subclasses", Server.Port.ToString(), "abilities", directory)));
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
					foreach(string eventName in ability.Events.Keys)
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
								
								if(!EventsConnected.ContainsKey(eventInfo))
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
