﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class Inventory
{
    [SerializeField] List<ItemAmountPair> content = new List<ItemAmountPair>();

    [field: NonSerialized]
    public event System.Action<bool, ItemAmountPair> InventoryChanged;

    public ItemAmountPair this[int index]
    {
        get
        {
            if (index < 0 || index >= content.Count)
                return ItemAmountPair.Nothing;

            return content[index];
        }
    }

    public void Add(ItemType type, int amount)
    {
        bool isReadable = ItemsData.GetItemInfo(type).AmountIsUniqueID;

        if (content.Count > 0 && !isReadable)
        {
            for (int i = 0; i < content.Count; i++)
            {
                var item = content[i];

                if (item.type == type)
                {
                    content[i] = new ItemAmountPair(item.type, item.amount + amount);
                    InventoryChanged?.Invoke(true, new ItemAmountPair(type,amount));
                    return;
                }
            }
        }

        ItemAmountPair pair = new ItemAmountPair(type, amount);

        content.Add(pair);
        InventoryChanged?.Invoke(true, pair);
    }

    public void Add(ItemAmountPair pair)
    {
        Add(pair.type, pair.amount);
    }

    public bool Contains(ItemAmountPair pair)
    {
        int id = GetStackIdFor(pair.type);

        if (id >= 0)
        {
            if (content[id].amount >= pair.amount)
            {
                return true;
            }
        }

        return false;
    }

    public bool TryRemove(ItemAmountPair pair)
    {
        var info = ItemsData.GetItemInfo(pair.type);

        if (info.AmountIsUniqueID)
        {
            int i = content.FindIndex(0,(x) => x == pair);
            if(i >= 0 && i < content.Count)
            {
                content.RemoveAt(i);
                InventoryChanged?.Invoke(false,pair);
                return true;
            }
        }
        else
        {
            int id = GetStackIdFor(pair.type);

            if (id >= 0)
            {
                if (content[id].amount > pair.amount)
                {
                    var newPair = new ItemAmountPair(pair.type, content[id].amount - pair.amount);
                    content[id] = newPair;

                    InventoryChanged?.Invoke(false, pair);
                    return true;
                }
                else if (content[id].amount == pair.amount)
                {
                    content.RemoveAt(id);

                    InventoryChanged?.Invoke(false, pair);
                    return true;
                }
            }
        }

        return false;
    }

    private int GetStackIdFor(ItemType type)
    {
        return content.FindIndex((x) => x.type == type);
    }

    public ItemAmountPair RemoveStack(int index)
    {
        if (index < 0 || index >= content.Count)
            return ItemAmountPair.Nothing;

        var c = content[index];

        if (c.IsNull())
        {
            content.RemoveAt(index);
            InventoryChanged?.Invoke(false, c);
            return c;
        }

        return ItemAmountPair.Nothing;
    }

    public int GetTotalWeight()
    {
        return content.Sum((x) => x.GetTotalWeight());
    }


    public ItemAmountPair[] GetContent()
    {
        return content.ToArray();
    }
}

[System.Serializable]
public struct ItemAmountPair
{
    public ItemType type;
    public int amount;

    public ItemAmountPair(ItemType itemType, int itemAmount)
    {
        type = itemType;
        amount = itemAmount;
    }

    public bool IsNull()
    {
        return amount <= 0 || type == ItemType.None;
    }

    public bool IsValid()
    {
        return !IsNull();
    }

    public static ItemAmountPair Nothing
    {
        get => new ItemAmountPair(ItemType.None, -1);
    }

    public int GetTotalWeight()
    {
        return amount * ItemsData.GetItemInfo(type).Weight;
    }

    public static bool operator ==(ItemAmountPair i1, ItemAmountPair i2)
    {
        return i1.type == i2.type && i1.amount == i2.amount;
    }

    public static bool operator !=(ItemAmountPair i1, ItemAmountPair i2)
    {
        return i1.type != i2.type || i1.amount != i2.amount;
    }

}
