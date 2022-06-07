using Exiled.API.Features;
using MEC;
using NCalc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AdvancedSubclassingRedux.Managers
{
    internal static class Helpers
    {
        public static IDeserializer Deserializer { get; set; } = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreFields()
            .IgnoreUnmatchedProperties()
            .Build();

        public static string Read(string fullPath)
        {
            try
            {
                if (File.Exists(fullPath))
                    return File.ReadAllText(fullPath);
            }
            catch (Exception exception)
            {
                Log.Error($"An error has occurred while reading class from {fullPath} path: {exception}\nBegin stack trace:\n{exception.StackTrace}");
            }

            return string.Empty;
        }

        internal static object GetValue(string name, object from)
        {
            Tuple<PropertyInfo, object> t1 = GetInfoWithFinalValue<PropertyInfo>(name, from);
            if (t1 != null)
                return t1.Item1.GetValue(t1.Item2);

            Tuple<FieldInfo, object> t2 = GetInfoWithFinalValue<FieldInfo>(name, from);
            if (t2 != null)
                return t2.Item1.GetValue(t2.Item2);

            return null;
        }

        internal static T GetInfo<T>(string name, object from) where T : class
        {
            MemberInfo memberInfo = null;
            object value = null;
            Type type = from.GetType();
            if (name.Contains('.'))
            {
                string[] split = name.Trim().Split('.');
                value = type.GetProperty(split[0]).GetValue(from);
                for (int i = 1; i < split.Length; i++)
                {
                    memberInfo = value.GetType().GetMember(split[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static).First();
                    if (i < split.Length - 1)
                    {
                        if (memberInfo.MemberType == MemberTypes.Property)
                        {
                            value = ((PropertyInfo)memberInfo).GetValue(value);
                        }
                        else if (memberInfo.MemberType == MemberTypes.Field)
                        {
                            value = ((FieldInfo)memberInfo).GetValue(value);
                        }
                    }
                }
            }
            else
            {
                value = from;
                memberInfo = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static).First();
            }

            if (memberInfo.MemberType == MemberTypes.Property && typeof(T) == typeof(PropertyInfo))
            {
                return memberInfo as T;
            }
            else if (memberInfo.MemberType == MemberTypes.Field && typeof(T) == typeof(FieldInfo))
            {
                return memberInfo as T;
            }
            else
            {
                return null;
            }
        }

        internal static Tuple<T, object> GetInfoWithFinalValue<T>(string name, object from) where T : class
        {
            MemberInfo memberInfo = null;
            object value = null;
            Type type = from.GetType();
            if (name.Contains('.'))
            {
                string[] split = name.Trim().Split('.');
                value = type.GetProperty(split[0]).GetValue(from);
                for (int i = 1; i < split.Length; i++)
                {
                    memberInfo = value.GetType().GetMember(split[i], BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static).First();
                    if (i < split.Length - 1)
                    {
                        if (memberInfo.MemberType == MemberTypes.Property)
                        {
                            value = ((PropertyInfo)memberInfo).GetValue(value);
                        }
                        else if (memberInfo.MemberType == MemberTypes.Field)
                        {
                            value = ((FieldInfo)memberInfo).GetValue(value);
                        }
                    }
                }
            }
            else
            {
                value = from;
                memberInfo = type.GetMember(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static).First();
            }

            if (memberInfo.MemberType == MemberTypes.Property && typeof(T) == typeof(PropertyInfo))
            {
                return new Tuple<T, object>((memberInfo as T), value);
            }
            else if (memberInfo.MemberType == MemberTypes.Field && typeof(T) == typeof(FieldInfo))
            {
                return new Tuple<T, object>((memberInfo as T), value);
            }
            else
            {
                return null;
            }
        }

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

        internal static IEnumerator<float> Eval<T>(Type type, T main, List<Dictionary<string, object>> toEval)
        {
            Dictionary<string, object> localValues = new Dictionary<string, object>();
            foreach (Dictionary<string, object> item in toEval)
            {
                string name = item.First().Key;
                object value = item.First().Value;

                if (name.StartsWith("set_"))
                {
                    string valName = name.Substring(4);
                    Tuple<PropertyInfo, object> prop = GetInfoWithFinalValue<PropertyInfo>(valName, main);
                    object valOrKey = value;
                    if (localValues.TryGetValue((string)valOrKey, out object val))
                        prop.Item1?.SetValue(prop.Item2, Convert.ChangeType(val, prop.Item1.PropertyType));
                    else
                        prop.Item1?.SetValue(prop.Item2, Convert.ChangeType(valOrKey, prop.Item1.PropertyType));
                }
                else if (name.StartsWith("get_"))
                {
                    if (((string)value).Contains("."))
                    {
                        Log.Error("Attempted to use '.' while saving local variable.\nI literally told you not to use '.' when saving local variables...");
                        throw new Exception($"Attempted to use '.' while saving local variable.\nI literally told you not to use '.' when saving local variables...");
                    }
                    string valName = name.Substring(4);
                    Tuple<PropertyInfo, object> prop = GetInfoWithFinalValue<PropertyInfo>(valName, main);
                    localValues[(string)value] = prop.Item1?.GetValue(prop.Item2);
                }
                else if (name.StartsWith("eval"))
                {
                    string[] split = ((string)value).Split('>');
                    string saveName = split[1].Trim();
                    if (saveName.Contains("."))
                    {
                        Log.Error("Attempted to use '.' while saving local variable.\nI literally told you not to use '.' when saving local variables...");
                        throw new Exception($"Attempted to use '.' while saving local variable.\nI literally told you not to use '.' when saving local variables...");
                    }
                    string expression = split[0].Trim();
                    var val = EvaluateMath(main, localValues, expression);
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
    }
}
