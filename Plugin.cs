using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Enumerations;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using Logger = Rocket.Core.Logging.Logger;
using System.Collections.Generic;
using UnityEngine;
using Rocket.Unturned;
using Steamworks;
using fr34kyn01535.Uconomy;
using System.Collections;

namespace SDTradePhone
{
	public class Plugin : RocketPlugin<Config>
	{
		public static Plugin Instance;

		public override TranslationList DefaultTranslations
		{
			get
			{
				return new TranslationList{
					{"success", "You success buy this item"},
					{"successr","You success remove this item"},
					{"blocked","This items not allow to sell"},
					{"successsell","You success add to SellList this item"},
					{"no_money",    "You don't have money"},
					{"nofind", "Item with this id don't exists"},
					{"noitems", "You don't have items for sell"},
					{"noitemnow",   "This item was removed or edited. Try again with new information..."},
					{"ui_nocost","Cost must be more 0"},
					{"max", "You have too many items on sell"},
					{"cantdelete", "You can't remove this item from shop!"},
					{"no_exists", "This item already buy someone..."},
					{"good_sell", "Wow! Someone bought your item! You get {0}$"},
					{"good_buy", "You buy item and spend {0}$"},
					{"int", "You need insert only integer cost"},
					{"ui_page", "{0}/{1}   {2} - {3} of {4} items"},
					{"ui_desc", "ID: {0}    Amount: {1}    Quality: {2}    Cost: {3}"},
					{"balance", "{0}$"},
					{"ui_name", "Trade Shop"},
					{"cost", "Cost must be more than 0"},
					{"fee", "You don't have money to pay fee {0}$"},
					{"amount", "Amount: {0}"},
					{"quality", "Quality: {0}"},
					{"type", "Type: {0}"},
					{"barrel", "Barrel: {0}"},
					{"grip", "Grip: {0}"},
					{"tactical", "Tactical: {0}"},
					{"scope", "Scope: {0}"},
					{"player", "Seller: {0}"},
					{"magazine", "Magazine: {0} (Ammo: {1})"},
					{"sellcount", "{0}/{1}"},
					{"None", "Nothing"},
					{"window", "Trade Shop - {0}"},
					{"pages", "{0}/{1} ({2} items)"},
					{"sellPH", "Insert cost"},
					{"filterPH", "Insert filter name"},
					{"category_all", "All Items"},
					{"category_mine", "Mine Items"},
				};
			}
		}

		public string GetTypeName(EItemType type)
		{
			return Instance.Configuration.Instance.ItemTypesGeneration.Find(x => x.TypeName == Enum.GetName(typeof(EItemType), type)).PublicName;
		}

		public int GetTypeID(EItemType type)
		{
			return Instance.Configuration.Instance.ItemTypesGeneration.Find(x => x.TypeName == Enum.GetName(typeof(EItemType), type)).ID;
		}

		public void ReloadShops()
        {
			foreach(SteamPlayer sp in Provider.clients)
            {
				UnturnedPlayer player = UnturnedPlayer.FromSteamPlayer(sp);
				PlayerComponent component = player.GetComponent<PlayerComponent>();
				if (!component.isOpen) continue;
				if (component.itemloader != null) StopCoroutine(component.itemloader);
				component.itemloader = StartCoroutine(LoadItems(player, false));
			}
        }

		public void GenerateTypes()
		{
			string[] names = Enum.GetNames(typeof(EItemType));
			int count = 0;
			for (int i = 0; i < names.Length; i++)
            {
				if (Instance.Configuration.Instance.ItemTypesGeneration.Exists(x => x.TypeName == names[i])) continue;
				Instance.Configuration.Instance.ItemTypesGeneration.Add(new Config.IType
				{
					PublicName = names[i],
					ID = i,
					TypeName = names[i],
				});
				count++;
			}
			Instance.Configuration.Save();
			Logger.Log($"{count} types not found! It was generated", ConsoleColor.Blue);
		}

		protected override void Load()
		{
			Instance = this;
			Logger.Log("------------------------------------------------------------", ConsoleColor.Blue);
			Logger.Log("|                                                          |", ConsoleColor.Blue);
			Logger.Log("|                     Imperial Plugins                     |", ConsoleColor.Blue);
            Logger.Log("|                   SodaDevs: FleaMarket                   |", ConsoleColor.Blue);
			Logger.Log("|                                                          |", ConsoleColor.Blue);
			Logger.Log("------------------------------------------------------------", ConsoleColor.Blue);
			Logger.Log("Version: 1.0.0.0", ConsoleColor.Blue);
			EffectManager.onEffectButtonClicked += OnButton;
			EffectManager.onEffectTextCommitted += OnText;
			U.Events.OnPlayerConnected += OnConnect;
			DamageTool.damagePlayerRequested += DamagedOn;
			GenerateTypes();
		}

		public void CloseUI(CSteamID steamid)
        {
			UnturnedPlayer player = UnturnedPlayer.FromCSteamID(steamid);
			PlayerComponent component = player.GetComponent<PlayerComponent>();
			EffectManager.askEffectClearByID(Instance.Configuration.Instance.UIID1, Provider.findTransportConnection(steamid));
			if (component.pageloader != null) StopCoroutine(component.pageloader);
			component.pageloader = null;
			if (component.sellloader != null) StopCoroutine(component.sellloader);
			component.sellloader = null;
			if (component.itemloader != null) StopCoroutine(component.itemloader);
			component.itemloader = null;
			component.ItemPlayer = new List<InventoryHelper.InventoryItem>();
			component.ItemShop = new List<Config.ItemSell>();
			component.page = 0;
			component.prefilter = "";
			component.selecter = 0;
			component.filter = "";
			component.cost = 0;
			component.category = 0;
			component.isOpen = false;
			player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
			return;
		}

        private void DamagedOn(ref DamagePlayerParameters parameters, ref bool shouldAllow)
		{
			if (parameters.player.life.health - parameters.damage > 0) return;
			CloseUI(UnturnedPlayer.FromPlayer(parameters.player).CSteamID);
		}

        private void OnConnect(UnturnedPlayer player)
        {
			if (!Instance.Configuration.Instance.MoneySend.Exists(x => x.player == player.CSteamID.m_SteamID)) return;
			UpdateBalance(player, Instance.Configuration.Instance.MoneySend.Find(x => x.player == player.CSteamID.m_SteamID).money, true);
			Instance.Configuration.Instance.MoneySend.RemoveAll(x => x.player == player.CSteamID.m_SteamID);
			Instance.Configuration.Save();
		}

        private void OnButton(Player player, string buttonName)
		{
			try
			{
				UnturnedPlayer unturnedPlayer = UnturnedPlayer.FromPlayer(player);
				PlayerComponent component = unturnedPlayer.GetComponent<PlayerComponent>();
				if (component.isOpen)
				{
					if (buttonName == "close")
					{
						CloseUI(unturnedPlayer.CSteamID);
						return;
					}
					if (buttonName == "arrow_back")
					{
						component.page--;
						if (component.pageloader != null) StopCoroutine(component.pageloader);
						component.pageloader = StartCoroutine(OpenPage(unturnedPlayer));
						return;
					}
					if (buttonName == "arrow_next")
					{
						component.page++;
						if (component.pageloader != null) StopCoroutine(component.pageloader);
						component.pageloader = StartCoroutine(OpenPage(unturnedPlayer));
						return;
					}
					if (buttonName.Contains("shop_"))
					{
						int count = 0;
						for (int i = 5; i < buttonName.Length; i++)
							count = count * 10 + (buttonName[i] - '0');
						int pos = component.page * 10 + count;
						BuyItem(unturnedPlayer, pos);
						if (component.itemloader != null) StopCoroutine(component.itemloader);
						component.itemloader = StartCoroutine(LoadItems(unturnedPlayer, false));
						return;

					}
					if (buttonName.Contains("category_"))
					{
						int count = 0;
						for (int i = 9; i < buttonName.Length; i++)
							count = count * 10 + (buttonName[i] - '0');
						component.category = count;
						if (component.itemloader != null) StopCoroutine(component.itemloader);
						component.itemloader = StartCoroutine(LoadItems(unturnedPlayer, true));
						return;

					}
					if (buttonName == "filterbutton")
					{
						component.filter = component.prefilter;
						if (component.itemloader != null) StopCoroutine(component.itemloader);
						component.itemloader = StartCoroutine(LoadItems(unturnedPlayer, true));
						return;
					}
					if (buttonName == "filterclose")
					{
						component.filter = "";
						if (component.itemloader != null) StopCoroutine(component.itemloader);
						component.itemloader = StartCoroutine(LoadItems(unturnedPlayer, true));
						return;
					}
					if (buttonName.Contains("shopdelete_"))
					{
						int count = 0;
						for (int i = 11; i < buttonName.Length; i++)
							count = (count * 10) + (Convert.ToInt32(buttonName[i].ToString()));
						int pos = component.page * 10 + count;
						RemoveItem(unturnedPlayer, pos);
						if (component.itemloader != null) StopCoroutine(component.itemloader);
						component.itemloader = StartCoroutine(LoadItems(unturnedPlayer, false));
						return;
					}
					if (buttonName == "sellarrow_back")
					{
						component.selecter--;
						if (component.sellloader != null) StopCoroutine(component.sellloader);
						component.sellloader = StartCoroutine(LoadSell(unturnedPlayer));
						return;
					}
					if (buttonName == "sellarrow_next")
					{
						component.selecter++;
						if (component.sellloader != null) StopCoroutine(component.sellloader);
						component.sellloader = StartCoroutine(LoadSell(unturnedPlayer));
						return;
					}
					if (buttonName == "sellbutton")
					{
						SellItem(unturnedPlayer, component.selecter);
						if (component.itemloader != null) StopCoroutine(component.itemloader);
						component.itemloader = StartCoroutine(LoadItems(unturnedPlayer, true));
						return;
					}
				}
			}
            catch { }
		}

		public void SellItem(UnturnedPlayer player, int item)
		{
			PlayerComponent component = player.GetComponent<PlayerComponent>();
			InventoryHelper.InventoryItem ShopItem = component.ItemPlayer[item];
			if (InventoryHelper.GetItems(player).items.Count <= 0)
			{
				UnturnedChat.Say(player, Translate("noitemnow"), Color.yellow);
				return;
			}
			if (InventoryHelper.GetItems(player).items.Contains(ShopItem))
			{
				UnturnedChat.Say(player, Translate("noitemnow"), Color.yellow);
				return;
			}
			if (Plugin.Instance.Configuration.Instance.blockitems.Contains(ShopItem.item.id))
			{
				UnturnedChat.Say(player, Translate("blocked"), Color.yellow);
				return;
			}
			if (component.cost <= 0)
			{
				UnturnedChat.Say(player, Translate("cost"));
				return;
			}
			if (component.cost * Instance.Configuration.Instance.feepercent > GetBalance(player))
			{
				UnturnedChat.Say(player, Translate("fee", component.cost * Instance.Configuration.Instance.feepercent));
				return;
			}
			UpdateBalance(player, (uint)(component.cost * Instance.Configuration.Instance.feepercent), false);
			player.Inventory.removeItem(ShopItem.page, player.Inventory.getIndex(ShopItem.page, ShopItem.x, ShopItem.y));
			Instance.Configuration.Instance.SellingItems.Add(new Config.ItemSell
			{
				state = ShopItem.item.state,
				amount = ShopItem.item.amount,
				cost = component.cost,
				id = SellIDGenerate(),
				itemid = ShopItem.item.id,
				name = ((ItemAsset)Assets.find(EAssetType.ITEM, ShopItem.item.id)).itemName,
				owner = player.CSteamID.m_SteamID,
				ownername = player.DisplayName,
				quality = ShopItem.item.quality
			});
			Instance.Configuration.Save();
			UnturnedChat.Say(player, Translate("successsell"), Color.yellow);
			ReloadShops();
			return;
		}

        private int SellIDGenerate()
        {
			int i = 0;
			if (Instance.Configuration.Instance.SellingItems.Count <= 0) return i;
			while (true)
            {
				if (!Instance.Configuration.Instance.SellingItems.Exists(x => x.id == i)) return i;
				i++;
            }
        }

        private void RemoveItem(UnturnedPlayer player, int item)
		{
			PlayerComponent component = player.GetComponent<PlayerComponent>();
			if (!Instance.Configuration.Instance.SellingItems.Contains(component.ItemShop[item]) || component.ItemShop[item].owner != player.CSteamID.m_SteamID)
			{
				UnturnedChat.Say(player, Translate("no_exists"));
				return;
			}
			Instance.Configuration.Instance.SellingItems.Remove(component.ItemShop[item]);
			Instance.Configuration.Save();
			Config.ItemSell itemSell = component.ItemShop[item];
			player.Inventory.forceAddItem(new Item(itemSell.itemid, itemSell.amount, itemSell.quality, itemSell.state), true);
			ReloadShops();
			return;
		}

		private void BuyItem(UnturnedPlayer player, int item)
		{
			PlayerComponent component = player.GetComponent<PlayerComponent>();
			Config.ItemSell ShopItem = component.ItemShop[item];
			if (!Instance.Configuration.Instance.SellingItems.Contains(ShopItem))
            {
				UnturnedChat.Say(player, Translate("no_exists"));
				return;
            }
			if (ShopItem.cost > GetBalance(player))
			{
				UnturnedChat.Say(player, Translate("no_money"));
				return;
			}
			Instance.Configuration.Instance.SellingItems.Remove(component.ItemShop[item]);
			Instance.Configuration.Save();
			UpdateBalance(player, ShopItem.cost, false);
			player.Inventory.tryAddItem(new Item(ShopItem.itemid, ShopItem.amount, ShopItem.quality, ShopItem.state), true);
			var p = PlayerTool.getPlayer(new CSteamID(ShopItem.owner));
			if (p) 
			{
				UpdateBalance(UnturnedPlayer.FromPlayer(p), ShopItem.cost, true);
				UnturnedChat.Say(UnturnedPlayer.FromPlayer(p), Translate("good_sell", ShopItem.cost.ToString()), Color.yellow);
				UnturnedChat.Say(player, Translate("good_buy", ShopItem.cost.ToString()), Color.yellow);
			}
			else
			{
				if (Instance.Configuration.Instance.MoneySend.Exists(x => x.player == ShopItem.owner)) Instance.Configuration.Instance.MoneySend.Find(x => x.player == ShopItem.owner).money += ShopItem.cost;
				else Instance.Configuration.Instance.MoneySend.Add(new Config.Sender { player = ShopItem.owner, money = ShopItem.cost });
				Instance.Configuration.Save();
				UnturnedChat.Say(player, Translate("good_buy", ShopItem.cost.ToString()), Color.yellow);
			}
			ReloadShops();
			return;
		}

		private uint GetBalance(UnturnedPlayer player)
		{
			if (Instance.Configuration.Instance.isNoteMode)
			{
				List<Config.EcoMoney> money = Instance.Configuration.Instance.Banknotes;
				uint moneysumm = 0;
				for (byte b = 0; b < 7; b += 1)
				{
					if (player.Inventory.items[b].getItemCount() <= 0) continue;
					foreach (ItemJar itemJar in player.Inventory.items[b].items)
					{
						if (itemJar == null) continue;
						if (money.Exists(x => x.id == itemJar.item.id))
						{
							moneysumm += money.Find(x => x.id == itemJar.item.id).value;
						}
					}
				}
				return moneysumm;
			}
			if (Instance.Configuration.Instance.isUconomy)
            {
				return Convert.ToUInt32(Uconomy.Instance.Database.GetBalance(player.CSteamID.m_SteamID.ToString()));
            }
			else
            {
				return player.Experience;
            }
		}

		private void UpdateBalance(UnturnedPlayer player, uint cost, bool plus)
		{
			if (Instance.Configuration.Instance.isNoteMode)
			{
				PlayerComponent component = player.GetComponent<PlayerComponent>();
				List<Config.EcoMoney> money = Instance.Configuration.Instance.Banknotes;
				if (!plus)
				{
					uint moneysumm = 0;
					for (byte b = 0; b < 7; b += 1)
					{
						if (player.Inventory.items[b].getItemCount() <= 0) continue;
						foreach (Config.EcoMoney m in money)
						{
							while (player.Inventory.items[b].items.Exists(x => x.item.id == m.id))
							{
								ItemJar itemJar = player.Inventory.items[b].items.Find(x => x.item.id == m.id);
								player.Inventory.items[b].removeItem(player.Inventory.items[b].getIndex(itemJar.x, itemJar.y));
								moneysumm += m.value;
							}
						}
					}
					moneysumm = moneysumm - cost;
					for (int i = money.Count - 1; i >= 0; i--)
					{
						uint needmoney = moneysumm / money[i].value;
						moneysumm -= needmoney * money[i].value;
						if (needmoney > 0)
							player.GiveItem(money[i].id, Convert.ToByte(needmoney));
					}
				}
				else
				{
					for (int i = money.Count - 1; i >= 0; i--)
					{
						uint needmoney = cost / money[i].value;
						cost -= needmoney * money[i].value;
						if (needmoney > 0)
							player.GiveItem(money[i].id, Convert.ToByte(needmoney));
					}
				}
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "balance", Translate("balance", GetBalance(player).ToString()));
				return;
			}
			if (Instance.Configuration.Instance.isUconomy)
			{
				if (!plus) Uconomy.Instance.Database.IncreaseBalance(player.CSteamID.m_SteamID.ToString(), -cost);
				else Uconomy.Instance.Database.IncreaseBalance(player.CSteamID.m_SteamID.ToString(), cost);
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "balance", Translate("balance", GetBalance(player).ToString()));
				return;
			}
			else
			{
				if (!plus) player.Experience -= cost;
				else player.Experience += cost;
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "balance", Translate("balance", GetBalance(player).ToString()));
				return;
			}
		}

		private void OnText(Player player, string buttonName, string text)
		{
			UnturnedPlayer unturnedPlayer = UnturnedPlayer.FromPlayer(player);
			PlayerComponent component = unturnedPlayer.GetComponent<PlayerComponent>();
			if (buttonName == "sellinput")
			{
				try
				{
					component.cost = Convert.ToUInt32(text);
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(unturnedPlayer.CSteamID), true, "sellbutton", true);
				}
				catch
				{
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(unturnedPlayer.CSteamID), true, "sellbutton", false);
				}
			}
			if (buttonName == "filterinput")
			{
				component.prefilter = text;
			}
		}

		public IEnumerator StartShop(UnturnedPlayer player)
		{
			EffectManager.sendUIEffect(Instance.Configuration.Instance.UIID1, Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true);
			PlayerComponent component = player.GetComponent<PlayerComponent>();
			component.cost = 0;
			component.filter = "";
			component.prefilter = "";
			component.category = Instance.Configuration.Instance.Categories.Count;
			component.ItemPlayer = new List<InventoryHelper.InventoryItem>();
			component.ItemShop = new List<Config.ItemSell>();
			component.page = 0;
			component.selecter = 0;
			component.isOpen = true;

			int i = 0;

			foreach (Config.ICategory category in Instance.Configuration.Instance.Categories)
			{
				EffectManager.sendUIEffectImageURL(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "categoryimage_" + i.ToString(), category.image);
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "category_" + i.ToString(), true);
				i++;
			}
			EffectManager.sendUIEffectImageURL(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "categoryimage_" + i.ToString(), Instance.Configuration.Instance.ImageAllItems);
			EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "category_" + i.ToString(), true);
			i++;
			EffectManager.sendUIEffectImageURL(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "categoryimage_" + i.ToString(), Instance.Configuration.Instance.ImageMineItems);
			EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "category_" + i.ToString(), true);
			EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "filterinputph", Translate("filterPH"));
			EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellinputph", Translate("sellPH"));
			EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "steam_name", player.CharacterName);
			EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "balance", Translate("balance",GetBalance(player).ToString()));
			if (Plugin.Instance.Configuration.Instance.isLoadSteamAvatar) EffectManager.sendUIEffectImageURL(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "steam_profile", player.SteamProfile.AvatarMedium.OriginalString);

			player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
			if (component.itemloader != null) StopCoroutine(component.itemloader);
			component.itemloader = StartCoroutine(LoadItems(player, true));
			yield return null;
			yield break;
		}

        IEnumerator OpenPage(UnturnedPlayer player)
		{
			PlayerComponent component = player.GetComponent<PlayerComponent>();
			if (component.ItemShop.Count == 0)
            {
				for (int i = 0; i < 10; i++)
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shop_" + i.ToString(), false);
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "pages", Translate("pages", 0, 0, component.ItemShop.Count));
				component.pageloader = null;
				yield return null;
				yield break;
			}
			int maxpages = component.ItemShop.Count / 10;
			if (component.ItemShop.Count % 10 == 0) maxpages--;
			if (component.page > maxpages)
				component.page = maxpages;
			if (component.page < 0)
				component.page = 0;
			for (int i = 0; i < 10; i++)
            {
				if (component.page * 10 + i > component.ItemShop.Count - 1)
				{
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shop_" + i.ToString(), false);
					continue;
				}
				int index = component.page * 10 + i;
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopname_" + i.ToString(), component.ItemShop[index].name + " (" + Translate("balance", component.ItemShop[index].cost) + ")" );
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shoptype_" + i.ToString(), Translate("type", GetTypeName(((ItemAsset)Assets.find(EAssetType.ITEM, component.ItemShop[index].itemid)).type)));
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopamount_" + i.ToString(), Translate("amount", component.ItemShop[index].amount.ToString()));
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopquality_" + i.ToString(), Translate("quality", component.ItemShop[index].quality.ToString()));
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopplayer_" + i.ToString(), Translate("player", component.ItemShop[index].ownername.ToString()));
				if (component.ItemShop[index].owner == player.CSteamID.m_SteamID) EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopdelete_" + i.ToString(), true);
				else EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopdelete_" + i.ToString(), false);
				if (((ItemAsset)Assets.find(EAssetType.ITEM, component.ItemShop[index].itemid)).type == EItemType.GUN)
				{
					ushort scopeId = BitConverter.ToUInt16(component.ItemShop[index].state, 0);
					
					ushort tacticalId = BitConverter.ToUInt16(component.ItemShop[index].state, 2);
					ushort gripId = BitConverter.ToUInt16(component.ItemShop[index].state, 4);
					ushort barrelId = BitConverter.ToUInt16(component.ItemShop[index].state, 6);
					ushort magId = BitConverter.ToUInt16(component.ItemShop[index].state, 8);
					byte ammo = component.ItemShop[index].state[10];
					if (barrelId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopbarrel_" + i.ToString(), Translate("barrel", ((ItemAsset)Assets.find(EAssetType.ITEM, barrelId)).itemName));
					else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopbarrel_" + i.ToString(), Translate("barrel", Translate("None")));
					if (magId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopmagazine_" + i.ToString(), Translate("magazine", ((ItemAsset)Assets.find(EAssetType.ITEM, magId)).itemName, ammo));
					else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopmagazine_" + i.ToString(), Translate("magazine", Translate("None"), 0));
					if (scopeId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopscope_" + i.ToString(), Translate("scope", ((ItemAsset)Assets.find(EAssetType.ITEM, scopeId)).itemName));
					else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopscope_" + i.ToString(), Translate("scope", Translate("None")));
					if (tacticalId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shoptactical_" + i.ToString(), Translate("tactical", ((ItemAsset)Assets.find(EAssetType.ITEM, tacticalId)).itemName));
					else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shoptactical_" + i.ToString(), Translate("tactical", Translate("None")));
					if (gripId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopgrip_" + i.ToString(), Translate("grip", ((ItemAsset)Assets.find(EAssetType.ITEM, gripId)).itemName));
					else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopgrip_" + i.ToString(), Translate("grip", Translate("None")));
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopbarrel_" + i.ToString(), true);
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopscope_" + i.ToString(), true);
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopgrip_" + i.ToString(), true);
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shoptactical_" + i.ToString(), true);
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopmagazine_" + i.ToString(), true);
				}
				else
                {
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopbarrel_" + i.ToString(), false);
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopscope_" + i.ToString(), false);
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopgrip_" + i.ToString(), false);
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shoptactical_" + i.ToString(), false);
					EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopmagazine_" + i.ToString(), false);
				}
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shop_" + i.ToString(), true);
				if (Instance.Configuration.Instance.Images.Exists(x => x.ID == component.ItemShop[index].itemid)) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopimage_" + i.ToString(), Instance.Configuration.Instance.Images.Find(x => x.ID == component.ItemShop[index].itemid).ImageURL);
				else EffectManager.sendUIEffectImageURL(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "shopimage_" + i.ToString(), Instance.Configuration.Instance.ImageStandart);
			}
			EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "pages", Translate("pages", component.page + 1, maxpages + 1, component.ItemShop.Count));
			component.itemloader = null;
			yield return null;
			yield break;
		}

        IEnumerator LoadItems(UnturnedPlayer player, bool isNull)
		{
			PlayerComponent component = player.GetComponent<PlayerComponent>();
			if (component.category < Plugin.Instance.Configuration.Instance.Categories.Count) component.ItemShop = Instance.Configuration.Instance.SellingItems.FindAll(x => Plugin.Instance.Configuration.Instance.Categories[component.category].types.Contains(Plugin.Instance.GetTypeID(((ItemAsset)Assets.find(EAssetType.ITEM, x.itemid)).type)) && x.name.ToLower().Contains(component.filter.ToLower()));
			if (component.category == Plugin.Instance.Configuration.Instance.Categories.Count) component.ItemShop = Instance.Configuration.Instance.SellingItems.FindAll(x => x.name.ToLower().Contains(component.filter.ToLower()));
			if (component.category > Plugin.Instance.Configuration.Instance.Categories.Count) component.ItemShop = Instance.Configuration.Instance.SellingItems.FindAll(x => x.owner == player.CSteamID.m_SteamID && x.name.ToLower().Contains(component.filter.ToLower()));
			if (isNull) component.page = 0;
			component.ItemPlayer = InventoryHelper.GetItemsCategory(player, component.category);
			if (component.pageloader != null) StopCoroutine(component.pageloader);
			component.pageloader = StartCoroutine(OpenPage(player));
			if (component.sellloader != null) StopCoroutine(component.sellloader);
			component.sellloader = StartCoroutine(LoadSell(player));
			component.itemloader = null;
			if (component.category < Instance.Configuration.Instance.Categories.Count)EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "window", Translate("window", Instance.Configuration.Instance.Categories[component.category].name));
			if (component.category == Instance.Configuration.Instance.Categories.Count)EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "window", Translate("window",Translate("category_all")));
			if (component.category > Instance.Configuration.Instance.Categories.Count)EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "window", Translate("window",Translate("category_mine")));
			yield return null;
			yield break;
		}

		IEnumerator LoadSell(UnturnedPlayer player)
		{
			PlayerComponent component = player.GetComponent<PlayerComponent>();
			if (component.ItemPlayer.Count == 0)
            {
				component.sellloader = null;
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellitem", Translate("None"));
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellamount", "");
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellquality", "");
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellgrip", "");
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "selltactical", "");
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellscope", "");
				EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellbarrel", "");
				yield return null;
				yield break;
			}
			if (component.selecter < 0)
				component.selecter = component.ItemPlayer.Count - 1;
			if (component.selecter >= component.ItemPlayer.Count)
				component.selecter = 0;
			EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellitem", ((ItemAsset)Assets.find(EAssetType.ITEM, component.ItemPlayer[component.selecter].item.id)).itemName);
			EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellamount", Translate("amount", component.ItemPlayer[component.selecter].item.amount));
			EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellquality", Translate("quality", component.ItemPlayer[component.selecter].item.quality));
			if (((ItemAsset)Assets.find(EAssetType.ITEM, component.ItemPlayer[component.selecter].item.id)).type == EItemType.GUN)
			{
				ushort scopeId = component.ItemPlayer[component.selecter].item.state[0];
				ushort tacticalId = component.ItemPlayer[component.selecter].item.state[2];
				ushort gripId = component.ItemPlayer[component.selecter].item.state[4];
				ushort barrelId = component.ItemPlayer[component.selecter].item.state[6];
				ushort magId = component.ItemPlayer[component.selecter].item.state[8];
				byte ammo = component.ItemPlayer[component.selecter].item.state[10];
				if (barrelId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellbarrel", Translate("barrel", ((ItemAsset)Assets.find(EAssetType.ITEM, barrelId)).itemName));
				else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellbarrel", Translate("barrel", Translate("None")));
				if (magId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellamount", Translate("amount", ammo));
				else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellamount", Translate("amount", 0));
				if (scopeId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellscope", Translate("scope", ((ItemAsset)Assets.find(EAssetType.ITEM, scopeId)).itemName));
				else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellscope", Translate("scope", Translate("None")));
				if (tacticalId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "selltactical", Translate("tactical", ((ItemAsset)Assets.find(EAssetType.ITEM, tacticalId)).itemName));
				else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "selltactical", Translate("tactical", Translate("None")));
				if (gripId != 0) EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellgrip", Translate("grip", ((ItemAsset)Assets.find(EAssetType.ITEM, gripId)).itemName));
				else EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellgrip", Translate("grip", Translate("None")));
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellbarrel", true);
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "selltactical", true);
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellscope", true);
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellgrip", true);
			}
			else
			{
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellbarrel", false);
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "selltactical", false);
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellscope", false);
				EffectManager.sendUIEffectVisibility(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellgrip", false);
			}
			EffectManager.sendUIEffectText(Instance.Configuration.Instance.UIKey, Provider.findTransportConnection(player.CSteamID), true, "sellpage", Translate("sellcount", component.selecter+1, component.ItemPlayer.Count));
			component.sellloader = null;
			yield return null;
			yield break;
		}
	}

	public class PlayerComponent : UnturnedPlayerComponent
	{
		protected override void Load()
		{
			this.ItemShop = new List<Config.ItemSell>();
			this.category = 0;
			this.selecter = 0;
			this.page = 0;
			this.pageloader = null;
			this.itemloader = null;
			this.sellloader = null;

			this.prefilter = "";
			this.filter = "";

			this.ItemPlayer = new List<InventoryHelper.InventoryItem>();
			this.selecter = 0;
			this.cost = 0;
			this.isOpen = false;
		}

		public List<InventoryHelper.InventoryItem> ItemPlayer;
		public List<Config.ItemSell> ItemShop;
		public int category;
		public int selecter;
		public int page;
		public uint cost;
		public string prefilter;
		public string filter;
		public bool isOpen;
		public Coroutine pageloader;
		public Coroutine itemloader;
		public Coroutine sellloader;
	}
}
