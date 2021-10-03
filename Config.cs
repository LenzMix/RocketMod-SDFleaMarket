using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SDTradePhone
{
	public class Config : IRocketPluginConfiguration, IDefaultable
	{
		public void LoadDefaults()
		{
			this.UIID1 = 23112;
			this.UIKey = 2311;
			this.maxsell = 10;
			this.feepercent = 0.1f;
			this.isNoteMode = false;
			this.isUconomy = false;
			this.isLoadSteamAvatar = false;
			this.perms = new List<Config.perm>
			{
				new Config.perm
				{
					permission = "vip",
					maxitems = 15
				},
				new Config.perm
				{
					permission = "perm",
					maxitems = 200
				}
			};
			this.blockitems = new List<ushort>
			{
				519,
				520
			};
			this.Banknotes = new List<EcoMoney>
			{
				new EcoMoney
				{
					id = 1051,
					value = 1,
				},
				new EcoMoney
				{
					id = 1052,
					value = 5,
				},
			};
			this.ItemTypesGeneration = new List<IType>();
			this.Categories = new List<ICategory>
			{
				new ICategory
				{
					name = "Clothes",
					image = "",
					types = new List<int>{0, 1, 2, 3, 4, 5, 6}
				},
				new ICategory
				{
					name = "Weapons",
					image = "",
					types = new List<int>{7, 16}
				},
				new ICategory
				{
					name = "Modules",
					image = "",
					types = new List<int>{8, 9, 10, 11, 25}
				},
				new ICategory
				{
					name = "Ammo",
					image = "",
					types = new List<int>{12, 34}
				},
			};
			this.Images = new List<IItemImage>
			{
				new IItemImage
				{
					ID = 363,
					ImageURL = "",
				}
			};
			this.ImageStandart = "";
			this.ImageAllItems = "";
			this.ImageMineItems = "";
		}

		public List<Config.perm> perms;
		public List<ItemSell> SellingItems;
		public List<Sender> MoneySend;
		public List<EcoMoney> Banknotes;
		public List<ushort> blockitems;
		public List<IType> ItemTypesGeneration;
		public List<ICategory> Categories;
		public List<IItemImage> Images;
		public ushort UIID1;
		public short UIKey;
		public int maxsell;
		public float feepercent;
		public bool isUconomy;
		public bool isNoteMode;
		public bool isLoadSteamAvatar;
		public string ImageMineItems;
		public string ImageAllItems;
		public string ImageStandart;

		public class IType
		{
			public int ID;
			public string TypeName;
			public string PublicName;
		}

		public class IItemImage
		{
			public int ID;
			public string ImageURL;
		}

		public class ICategory
		{
			public string name;
			public List<int> types;
			public string image;
		}

		public class perm
		{
			[XmlAttribute]
			public string permission;
			[XmlAttribute]
			public int maxitems;
		}

		public class EcoMoney
		{
			[XmlAttribute]
			public ushort id;
			[XmlAttribute]
			public uint value;
		}

		public class Sender
		{
			[XmlAttribute]
			public ulong player;
			[XmlAttribute]
			public uint money;
        }

		public class ItemSell
		{
			[XmlAttribute]
			public int id;
			[XmlAttribute]
			public ushort itemid;
			[XmlAttribute]
			public string name;
			[XmlAttribute]
			public byte quality;
			[XmlAttribute]
			public byte amount;
			[XmlAttribute]
			public byte[] state;
			[XmlAttribute]
			public ulong owner;
			[XmlAttribute]
			public string ownername;
			[XmlAttribute]
			public uint cost;
		}
	}
}
