using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SDTradePhone
{
    public class InventoryHelper
    {
        public class InventoryItem
        {
            public byte x;
            public byte y;
            public byte rot;
            public byte page;
            public SDG.Unturned.Item item;
        }
        public class ListInventory
        {
            public byte[] glassesState;
            public byte[] maskState;
            public byte[] vestState;
            public byte[] hatState;
            public byte[] pantsState;
            public byte[] shirtState;
            public byte[] backpackState;
            public byte glassesQuality;
            public byte maskQuality;
            public byte vestQuality;
            public byte backpackQuality;
            public byte pantsQuality;
            public byte shirtQuality;
            public byte hatQuality;
            public ushort shirt = 0;
            public ushort pants = 0;
            public ushort hat = 0;
            public ushort backpack = 0;
            public ushort vest = 0;
            public ushort glasses = 0;
            public ushort mask = 0;
            public byte health = 0;
            public byte food = 0;
            public byte water = 0;
            public byte virus = 0;
            public List<InventoryItem> items = new List<InventoryItem>();
        }

        public static List<InventoryItem> GetItemsCategory(UnturnedPlayer UP, int category)
        {
            List<InventoryItem> list = new List<InventoryItem>();
            if (category < Plugin.Instance.Configuration.Instance.Categories.Count)
            {
                for (byte page = 0; page < 7; page++)
                {
                    if (UP.Inventory.items[page].getItemCount() > 0)
                        foreach (ItemJar item in UP.Inventory.items[page].items)
                        {
                            if (item != null && !Plugin.Instance.Configuration.Instance.blockitems.Contains(item.item.id) && Plugin.Instance.Configuration.Instance.Categories[category].types.Contains(Plugin.Instance.GetTypeID(((ItemAsset)Assets.find(EAssetType.ITEM, item.item.id)).type)))
                            {
                                list.Add(new InventoryItem
                                {
                                    x = item.x,
                                    y = item.y,
                                    rot = item.rot,
                                    item = item.item,
                                    page = page,
                                });
                            }
                        }
                }
            }
            if (category >= Plugin.Instance.Configuration.Instance.Categories.Count)
            {
                for (byte page = 0; page < 7; page++)
                {
                    if (UP.Inventory.items[page].getItemCount() > 0)
                        foreach (ItemJar item in UP.Inventory.items[page].items)
                        {
                            if (item != null && !Plugin.Instance.Configuration.Instance.blockitems.Contains(item.item.id))
                            {
                                list.Add(new InventoryItem
                                {
                                    x = item.x,
                                    y = item.y,
                                    rot = item.rot,
                                    item = item.item,
                                    page = page,
                                });
                            }
                        }
                }
            }
            return list;
        }

        public static ListInventory GetItems(UnturnedPlayer UP)
        {
            List<InventoryItem> list = new List<InventoryItem>();
            for (byte page = 0; page < 7; page++)
            {
                if (UP.Inventory.items[page].getItemCount() > 0)
                    foreach (ItemJar item in UP.Inventory.items[page].items)
                    {
                        if (item != null)
                        {
                            list.Add(new InventoryItem
                            {
                                x = item.x,
                                y = item.y,
                                rot = item.rot,
                                item = item.item,
                                page = page,
                            });
                        }
                    }
            }
            PlayerClothing clothing = UP.Player.clothing;
            return new ListInventory
            {
                glasses = clothing.glasses,
                glassesState = clothing.glassesState,
                glassesQuality = clothing.glassesQuality,
                mask = clothing.mask,
                maskState = clothing.maskState,
                maskQuality = clothing.maskQuality,
                vest = clothing.vest,
                vestState = clothing.vestState,
                vestQuality = clothing.vestQuality,
                backpack = clothing.backpack,
                backpackState = clothing.backpackState,
                backpackQuality = clothing.backpackQuality,
                shirt = clothing.shirt,
                shirtState = clothing.shirtState,
                shirtQuality = clothing.shirtQuality,
                pants = clothing.pants,
                pantsState = clothing.pantsState,
                pantsQuality = clothing.pantsQuality,
                hat = clothing.hat,
                hatState = clothing.hatState,
                hatQuality = clothing.hatQuality,
                items = list,
                health = UP.Health,
                food = UP.Player.life.food,
                virus = UP.Player.life.virus,
                water = UP.Player.life.water,
            };
        }

        public static void ClearItems(UnturnedPlayer UP)
        {
            Items[] items = UP.Player.inventory.items;
            for (byte b = 0; b < PlayerInventory.PAGES - 2; b++)
            {
                if (items[b]?.items?.Count == 0)
                {
                    continue;
                }

                for (int l = items[b].getItemCount() - 1; l >= 0; l--)
                {
                    items[b].removeItem((byte)l);
                }

                if (b < PlayerInventory.SLOTS)
                {
                    UP.Player.equipment.sendSlot(b);
                }
            }

            PlayerClothing clothing = UP.Player.clothing;
            HumanClothes clothes = clothing.thirdClothes;
            if (clothing.backpack != 0)
            {
                clothes.backpack = 0;
                clothing.askWearBackpack(0, 0, new byte[0], true);
            }

            if (clothing.glasses != 0)
            {
                clothes.glasses = 0;
                clothing.askWearGlasses(0, 0, new byte[0], true);
            }

            if (clothing.hat != 0)
            {
                clothes.hat = 0;
                clothing.askWearHat(0, 0, new byte[0], true);
            }

            if (clothing.mask != 0)
            {
                clothes.mask = 0;
                clothing.askWearMask(0, 0, new byte[0], true);
            }

            if (clothing.pants != 0)
            {
                clothes.pants = 0;
                clothing.askWearPants(0, 0, new byte[0], true);
            }

            if (clothing.shirt != 0)
            {
                clothes.shirt = 0;
                clothing.askWearShirt(0, 0, new byte[0], true);
            }

            if (clothing.vest != 0)
            {
                clothes.vest = 0;
                clothing.askWearVest(0, 0, new byte[0], true);
            }
        }

        public static void GiveItems(UnturnedPlayer UP, ListInventory inventory)
        {
            UP.Player.clothing.askWearShirt(inventory.shirt, inventory.shirtQuality, inventory.shirtState, false);
            UP.Player.clothing.thirdClothes.shirt = inventory.shirt;
            UP.Player.clothing.askWearPants(inventory.pants, inventory.pantsQuality, inventory.pantsState, false);
            UP.Player.clothing.thirdClothes.pants = inventory.pants;
            UP.Player.clothing.askWearVest(inventory.vest, inventory.vestQuality, inventory.vestState, false);
            UP.Player.clothing.thirdClothes.vest = inventory.vest;
            UP.Player.clothing.askWearBackpack(inventory.backpack, inventory.backpackQuality, inventory.backpackState, false);
            UP.Player.clothing.thirdClothes.backpack = inventory.backpack;
            UP.Player.clothing.askWearHat(inventory.hat, inventory.hatQuality, inventory.hatState, false);
            UP.Player.clothing.thirdClothes.hat = inventory.hat;
            UP.Player.clothing.askWearMask(inventory.mask, inventory.maskQuality, inventory.maskState, false);
            UP.Player.clothing.thirdClothes.mask = inventory.mask;
            UP.Player.clothing.askWearGlasses(inventory.glasses, inventory.glassesQuality, inventory.glassesState, false);
            UP.Player.clothing.thirdClothes.glasses = inventory.glasses;
            try
            {
                if (UP.Player.life.health > inventory.health) UP.Player.life.askDamage((byte)(UP.Player.life.health - inventory.health), new Vector3(), EDeathCause.PUNCH, ELimb.SPINE, new Steamworks.CSteamID(), out EPlayerKill kill);
                else UP.Player.life.askHeal((byte)(inventory.health - UP.Player.life.health), true, true);
                if (UP.Player.life.food > inventory.food) UP.Player.life.askStarve((byte)(UP.Player.life.food - inventory.food));
                else UP.Player.life.askEat((byte)(inventory.food - UP.Player.life.food));
                if (UP.Player.life.water > inventory.water) UP.Player.life.askDehydrate((byte)(UP.Player.life.water - inventory.water));
                else UP.Player.life.askDrink((byte)(inventory.water - UP.Player.life.water));
                if (UP.Player.life.virus > inventory.virus) UP.Player.life.askInfect((byte)(UP.Player.life.virus - inventory.virus));
                else UP.Player.life.askDisinfect((byte)(inventory.virus - UP.Player.life.virus));
            }
            catch { }
            foreach (InventoryItem item in inventory.items)
            {
                UP.Inventory.items[item.page].addItem(item.x, item.y, item.rot, item.item);
            }
        }
    }
}
