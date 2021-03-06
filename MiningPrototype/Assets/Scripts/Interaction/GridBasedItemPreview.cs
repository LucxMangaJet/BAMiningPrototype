﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridBasedItemPreview : MonoBehaviour, IItemPreview
{
    [SerializeField] Vector3 offset;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] bool underworldOnly = false;
    bool couldPlace;

    [Zenject.Inject] RuntimeProceduralMap map;

    public Vector3 GetPlacePosition(Vector3 pointPosition)
    {
        return pointPosition.ToGridPosition().AsV3() + offset;
    }
    
    public void UpdatePreview(Vector3 position)
    {
        var gridPos = position.ToGridPosition();
        transform.position = gridPos.AsV3() + offset;
    
        if (map.IsAirAt(gridPos.x, gridPos.y) && (!underworldOnly || !Util.InOverworld(gridPos.y)))
        {
            spriteRenderer.color = Color.green;
            couldPlace = true;
        }
        else
        {
            spriteRenderer.color = Color.red;
            couldPlace = false;
        }
    }
    
    public bool WouldPlaceSuccessfully()
    {
        return couldPlace;
    }
}
