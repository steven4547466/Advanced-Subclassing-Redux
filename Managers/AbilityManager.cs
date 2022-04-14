using Exiled.API.Features;
using Exiled.Loader;
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

		internal static Dictionary<Type, List<Tuple<Ability, string>>> Delegates { get; set; } = new Dictionary<Type, List<Tuple<Ability, string>>>();

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
				//if (expression.Contains(property.Name))
				//	expression = expression.Replace(property.Name, property.GetValue(eventArgs).ToString());
			}

			foreach (KeyValuePair<string, object> keyValuePair in localValues)
			{
				mathExpression.Parameters[keyValuePair.Key] = keyValuePair.Value;
				//if (expression.Contains(keyValuePair.Key))
				//	expression = expression.Replace(keyValuePair.Key, keyValuePair.Value.ToString());
			}

			return mathExpression.Evaluate();
		}

		public static void Helper<T>(T eventArgs)
		{
			Type type = eventArgs.GetType();
			if (Delegates.TryGetValue(type, out List<Tuple<Ability, string>> abilities))
			{
				foreach(Tuple<Ability, string> tuple in abilities)
				{
					Ability ability = tuple.Item1;
					string eventName = tuple.Item2;
					Dictionary<string, object> localValues = new Dictionary<string, object>();
					foreach (string name in ability.Events[eventName].Keys)
					{
						if (name.StartsWith("set_"))
						{
							string valName = name.Substring(4);
							PropertyInfo prop = type.GetProperty(valName, BindingFlags.Public | BindingFlags.Instance);
							object valOrKey = ability.Events[eventName][name];
							if (localValues.TryGetValue((string)valOrKey, out object val))
								prop?.SetValue(eventArgs, Convert.ChangeType(val, prop.PropertyType));
							else
								prop?.SetValue(eventArgs, Convert.ChangeType(valOrKey, prop.PropertyType));
						}
						else if (name.StartsWith("get_"))
						{
							if (((string)ability.Events[eventName][name]).Contains("."))
							{
								throw new Exception($"Attempted to use '.' while saving local variable in ability: {ability.Name}.\nI literally told you not to use '.' when saving local variables...");
							}	
							string valName = name.Substring(4);
							PropertyInfo prop = type.GetProperty(valName, BindingFlags.Public | BindingFlags.Instance);
							localValues[(string)ability.Events[eventName][name]] = prop?.GetValue(eventArgs);
						}
						else if (name == "eval")
						{
							string saveName = ((string)ability.Events[eventName][name]).Split('>')[1].Trim();
							if (saveName.Contains("."))
							{
								throw new Exception($"Attempted to use '.' while saving local variable in ability: {ability.Name}.\nI literally told you not to use '.' when saving local variables...");
							}
							string expression = ((string)ability.Events[eventName][name]).Split('>')[0].Trim();
							var val = EvaluateMath(eventArgs, localValues, expression);
							localValues[saveName] = val;
						}
					}
				}
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

								MethodInfo mi = typeof(AbilityManager).GetMethod("Helper");

								foreach (ParameterInfo p in mi.GetParameters())
								{
									Log.Info(p.ParameterType);
								}

								Delegate del = Delegate.CreateDelegate(eventInfo.EventHandlerType, mi.MakeGenericMethod(args.ToArray()));
								eventInfo.AddEventHandler(null, del);
								if (!Delegates.ContainsKey(args[0]))
									Delegates.Add(args[0], new List<Tuple<Ability, string>>());
								Delegates[args[0]].Add(new Tuple<Ability, string>(ability, eventName));
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
