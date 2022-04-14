using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedSubclassingRedux
{
	public class Ability
	{
		public string Name { get; set; }
		
		public Dictionary<string, Dictionary<string, object>> Events { get; set; } = new Dictionary<string, Dictionary<string, object>>();

		public void Unload()
		{
			
		}
	}
}
