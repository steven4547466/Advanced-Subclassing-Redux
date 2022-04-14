using Exiled.API.Features;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AdvancedSubclassingRedux.Managers
{
	internal class Helpers
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
	}
}
