﻿using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryVisualizer : ScalingUIElementBase
{
    [SerializeField] int widthInSlots, heightInSlots;

    [SerializeField] RectTransform boxTransform;
    [SerializeField] Vector2 basePadding;
    [SerializeField] Vector2 additonalSpacePerSlotNeeded;

    [SerializeField] InventorySlotVisualizer inventorySlotPrefab;
    [SerializeField] Transform gridBox;
    [SerializeField] Transform gridLayoutParent;
    [SerializeField] bool updateContinously = true;
    [SerializeField] ImageSpriteAnimator animator;
    [SerializeField] SpriteAnimation closeAnimation;
    SpriteRenderer spriteRendererToGetOrientatioFrom;

    Inventory inventory;
    List<InventorySlotVisualizer> slots = new List<InventorySlotVisualizer>();

    public void Init(Transform target, Inventory inventoryToVisualize)
    {
        transformToFollow = target;
        inventory = inventoryToVisualize;
        spriteRendererToGetOrientatioFrom = target.GetComponent<SpriteRenderer>();

        RefreshInventoryDisplay();
    }

    protected override void Update()
    {
        if (updateContinously)
        {
            bool flipX = (spriteRendererToGetOrientatioFrom != null && spriteRendererToGetOrientatioFrom.flipX) ? false : true;
            UpdatePosition(flipX);
            RecalculateUIOrientation(spriteRendererToGetOrientatioFrom);
        }
    }

    [Button]
    public void RefreshInventoryDisplay ()
    {
        Debug.Log("refresh inventorx display");

        foreach (Transform child in gridLayoutParent)
        {
            GameObject.Destroy(child.gameObject);
        }

        KeyValuePair<ItemType, int>[] content = inventory.GetContent();
        StopAllCoroutines();
        StartCoroutine(SpawnItemElements(content));
        RecalculateUISize(content.Length);
        RecalculateUIOrientation(spriteRendererToGetOrientatioFrom);
    }

    private void RecalculateUISize (int sizeCurrent)
    {
        int width = widthInSlots;
        int height = heightInSlots;

        //inventory smaller than one row
        if (sizeCurrent <= widthInSlots)
        {
            width = Mathf.Max(1,sizeCurrent);
            height = 1;
        } else
        {
            float h = sizeCurrent / width;

            if (h > width)
            {
                height = width;
                width = (int)h;
            }
        }

        boxTransform.sizeDelta = new Vector2(basePadding.x + additonalSpacePerSlotNeeded.x * width, basePadding.y + additonalSpacePerSlotNeeded.y * height);
    }

    public void RecalculateUIOrientation(SpriteRenderer spriteRendererToGetOrientatioFrom)
    {
        if (spriteRendererToGetOrientatioFrom == null)
            return;

        Vector3 flipX = new Vector3(spriteRendererToGetOrientatioFrom.flipX? 1 : -1, 1, 1);

        if (gridBox != null)
            gridBox.localScale = flipX;

        if(gridLayoutParent != null)
            gridLayoutParent.localScale = flipX;
    }

    private IEnumerator SpawnItemElements(KeyValuePair<ItemType, int>[] itemsToVisualize)
    {
        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < itemsToVisualize.Length; i++)
        {
            InventorySlotVisualizer newSlot = Instantiate(inventorySlotPrefab, gridLayoutParent);
            newSlot.Display(itemsToVisualize[i]);
            slots.Add(newSlot);
            yield return new WaitForSeconds(0.1f);
        }
    }

    internal void Close()
    {
        animator.Play(closeAnimation);
        float closeLength = closeAnimation.GetLength();
        StopAllCoroutines();
        StartCoroutine(DestroyItemElements(closeLength));
        Invoke("Selfdestroy", closeLength);
    }

    private IEnumerator DestroyItemElements(float length)
    {
        float delay = (length * (2/3)) / (float)slots.Count; 

        foreach (InventorySlotVisualizer slot in slots)
        {
            yield return new WaitForSeconds(delay);
            slot.CloseInventory();
        }
    }
    private void Selfdestroy()
    {
        Destroy(gameObject);
    }

}
