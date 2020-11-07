﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Tile
{
    public TileType Type;
    public byte NeighbourBitmask;
    public float Damage;

    public static Tile Air
    {
        get
        {
            var t = new Tile();
            t.Type = TileType.Air;
            t.NeighbourBitmask = 0;
            t.Damage = 0;
            return t;
        }
    }

    public static Tile Stone
    {
        get
        {
            var t = Air;
            t.Type = TileType.Stone;
            return t;
        }
    }

    public void TakeDamage(float amount)
    {
        if (Type == TileType.Air)
            return;
        Damage += amount;
    }
}

public enum TileType
{
    Air = 0,
    Stone = 1
}