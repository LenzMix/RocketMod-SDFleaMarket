using Rocket.API;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDTradePhone
{
    class CommandOpenTrade : IRocketCommand
	{
		public AllowedCaller AllowedCaller
		{
			get
			{
				return AllowedCaller.Player;
			}
		}

		public string Name
		{
			get
			{
				return "opentrade";
			}
		}

		public string Help
		{
			get
			{
				return "Open Marketplace";
			}
		}

		public string Syntax
		{
			get
			{
				return "Usage: /opentrade";
			}
		}

		public List<string> Aliases
		{
			get
			{
				return new List<string>
				{
					"marketplace",
					"ot"
				};
			}
		}

		public List<string> Permissions
		{
			get
			{
				return new List<string>
				{
					"opentrade"
				};
			}
		}

		public void Execute(IRocketPlayer caller, string[] command)
		{
			Plugin.Instance.StartCoroutine(Plugin.Instance.StartShop((UnturnedPlayer)caller));
		}
	}
}
