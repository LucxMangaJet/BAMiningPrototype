﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReadableItemHandler : Singleton<ReadableItemHandler>
{
    [SerializeField] Sprite iconClosed, iconOpen;
    [SerializeField] ReadableItemVisualizer textDisplayPrefab;
    [SerializeField] AudioSource ReadLetterSound;
    ReadableItemVisualizer current;
    [SerializeField] Canvas canvas;
    List<ReadableItem> readableItems = new List<ReadableItem>();

    private void Start()
    {
        readableItems.Add(new ReadableItem("I can't write."));
        readableItems.Add(new ReadableItem("Hello, I'm your wife and a like to write letters"));
    }

    public Sprite GetSpriteOfLetter(int letterId)
    {
        return (readableItems.Count - 1 >= letterId && readableItems[letterId].hasRead) ? iconOpen : iconClosed;
    }

    internal void Display(int id)
    {
        ReadableItem itemToDisplay = (readableItems.Count - 1 < id) ? null : readableItems[id];

        if (itemToDisplay == null)
        {
            Debug.LogWarning("no letter with id " + id + " found");
            return;
        }

        if (current != null)
            return;

        itemToDisplay.hasRead = true;

        ReadLetterSound.pitch = 1;
        ReadLetterSound.Play();

        current = Instantiate(textDisplayPrefab, canvas.transform);
        current.DisplayText(transform, itemToDisplay);

        Debug.Log("display letter with id:" + id);
    }

    public void Hide()
    {
        if (current != null)
        {
            Destroy(current.gameObject);

            ReadLetterSound.pitch = 0.66f;
            ReadLetterSound.Play();
        }
    }

    public static int AddNewReadable(List<ItemAmountPair> itemAmountPairs)
    {
        string str = "New Order:\n";

        foreach (ItemAmountPair pair in itemAmountPairs)
        {
            str += ItemsData.GetItemInfo(pair.type).DisplayName + " x " + pair.amount + "\n";
        }

        Instance.readableItems.Add(new ReadableItem(str));
        return Instance.readableItems.Count - 1;
    }

    public static int AddNewReadable(string str)
    {
        Instance.readableItems.Add(new ReadableItem(str));
        return Instance.readableItems.Count - 1;
    }
}

public class ReadableItem
{
    public bool hasRead;
    public string text;

    public ReadableItem(string _text)
    {
        text = _text;
    }
}