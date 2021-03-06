﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rock : FallingTilemapCarvingEntity
{
    [SerializeField] float destructionSpeed;
    [SerializeField] AudioSource rockSmashing;
    [SerializeField] float crumbleMinTime = 0.3f;
    [SerializeField] new SpriteRenderer renderer;
    [SerializeField] Sprite[] sprites;

    [Zenject.Inject] CameraController cameraController;

    float lastCrumbleStamp = -1000;

    protected override void Start()
    {
        base.Start();
        Carve();
        renderer.sprite = sprites[UnityEngine.Random.Range(0, sprites.Length)];
        renderer.sortingOrder = UnityEngine.Random.Range(0, 100);
    }


    public override void OnTileCrumbleNotified(int x, int y)
    {
        if (this == null)
            return;

        Debug.Log("Crumble " + (Time.time - lastCrumbleStamp));

        if (Time.time - lastCrumbleStamp < crumbleMinTime)
        {
            TryDestroyBelow(map); //<-dirty
            Debug.Log("Instant Crumble!");
        }

        lastCrumbleStamp = Time.time;

        base.OnTileCrumbleNotified(x, y);
    }


    public override void OnTileChanged(int x, int y, TileUpdateReason reason)
    {
        if (reason == TileUpdateReason.Destroy || reason == TileUpdateReason.Generation || reason == TileUpdateReason.MapLoad)
        {
            UncarveDestroy();
        }
    }

    public override void OnTileUpdated(int x, int y)
    {
        if (this == null)
            return;

        int minVis = 3;
        var tilemapPos = (transform.position + carvingOffset).ToGridPosition();
        foreach (var item in tilesToOccupy)
        {
            Vector2Int pos = tilemapPos + item.Offset;
            Tile t = map[pos];
            if (t.Visibility < minVis)
                minVis = t.Visibility;
        }

        if (minVis > 1)
            renderer.enabled = false;
        else
            renderer.enabled = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent(out IEntity entity))
        {
            float angle = Mathf.Acos(Vector2.Dot(collision.contacts[0].normal, Vector2.up)) * Mathf.Rad2Deg;

            //Debug.Log(angle + " " + speed);
            if (angle < 70 && !rigidbody.isKinematic)
            {
                entity.TakeDamage(DamageStrength.Strong);
                rigidbody.velocity = collision.relativeVelocity * -1;
                if (!rockSmashing.isPlaying)
                {
                    rockSmashing.Play();
                    cameraController.Shake(transform.position, CameraShakeType.explosion, 0.25f, 16f);
                }
            }
        }
        else if (collision.collider.TryGetComponent(out ITileMapElement gridElement))
        {
            if (gridElement.TileMap == null || collision.relativeVelocity.magnitude < destructionSpeed)
            {
                return;
            }

            TryDestroyBelow(gridElement.TileMap);
            if (!rockSmashing.isPlaying)
            {
                rockSmashing.Play();
                cameraController.Shake(transform.position, CameraShakeType.explosion, 0.25f, 16f);
            }
        }
    }

    private void TryDestroyBelow(BaseMap map)
    {
        Vector3[] offsets = { new Vector3(0, -1.2f), new Vector3(-0.55f, -1.2f), new Vector3(0.55f, -1.2f) };

        foreach (var offset in offsets)
        {
            var pos = (transform.position + offset).ToGridPosition();
            Util.DebugDrawTile(pos);
            bool block = map.IsBlockAt(pos.x, pos.y);
            if (block)
            {
                if (map[pos.x, pos.y].Type != TileType.Rock)
                {
                    map.DamageAt(pos.x, pos.y, 100, BaseMap.DamageType.Crush);
                }
            }
        }
    }

}

