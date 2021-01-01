﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KernelIterator : StateListenerBehaviour
{
    [SerializeField] int sizeX, sizeY;
    [SerializeField] bool debug;

    [Zenject.Inject] KernelParser kernelParser;
    [Zenject.Inject] RuntimeProceduralMap map;
    [Zenject.Inject] PlayerStateMachine player;

    Kernel[] kernels;

    int currentStartX;
    int currentStartY;
    int currentIndex;
    protected override void OnRealStart()
    {
        kernels = kernelParser.GetAllKernels();
        StartCoroutine(KernelRoutine());
    }

    private IEnumerator KernelRoutine()
    {
        currentIndex = 0;
        currentStartX = player.transform.position.ToGridPosition().x - sizeX / 2;
        currentStartY = player.transform.position.ToGridPosition().y - sizeY / 2;
        int x = currentStartX;
        int y = currentStartY;

        while (true)
        {
            Kernel currentKernel = kernels[currentIndex];

            if (currentKernel.MatchesWith(map, x, y))
            {
                if (debug)
                    Debug.Log(currentKernel.Name + " matched at: (" + x + "/" + y + ")");
                ApplyKernel(currentKernel, x, y);
            }

            x++;
            if (x >= map.SizeX - currentKernel.Width)
            {
                x = currentStartX;
                y++;

                if (y >= map.SizeY - currentKernel.Height)
                {
                    y = currentStartY;
                    yield return null; //1 kernel per frame
                    currentIndex++;

                    if (currentIndex >= kernels.Length) //finished all kernels
                    {
                        currentIndex = 0;
                        currentStartX = player.transform.position.ToGridPosition().x - sizeX / 2;
                        currentStartY = player.transform.position.ToGridPosition().y - sizeY / 2;
                        x = currentStartX;
                        y = currentStartY;
                    }
                }
            }
        }
    }

    private void ApplyKernel(Kernel k, int px, int py)
    {
        for (int y = 0; y < k.Height; y++)
        {
            for (int x = 0; x < k.Width; x++)
            {
                Vector2Int loc = new Vector2Int(px + x, py + y);
                var info = map.GetTileInfoAt(loc);
                if (info.CanBeUnstable)
                {
                    if ((k[x, y] & CrumbleType.Crumble) != CrumbleType.Null)
                    {
                        map.MakeTileUnstable(loc, info.UnstableTimeBeforeCrumble);
                    }
                    else if ((k[x, y] & CrumbleType.CrumbleInstant) != CrumbleType.Null)
                    {
                        map.MakeTileUnstable(loc, 0);
                    }
                }
            }
        }
    }



#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (player != null)
        {
            Vector3 pos = player.transform.position.ToGridPosition().AsV3();
            Gizmos.DrawWireCube(pos, new Vector3(sizeX, sizeY, 0));
            UnityEditor.Handles.Label(pos + new Vector3(-sizeX / 2, sizeY / 2, 0), "KernelIterator #" + currentIndex);

            if (debug)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        Vector2Int p = new Vector2Int(currentStartX + x, currentStartY + y);
                        var i = map.GetTileInfoAt(p);

                        Gizmos.color = i.GetCrumbleTypeColor();
                        Gizmos.DrawCube(p.AsV3() + new Vector3(0.5f, 0.5f), Vector2.one);
                    }
                }
            }
        }
    }
#endif
}
