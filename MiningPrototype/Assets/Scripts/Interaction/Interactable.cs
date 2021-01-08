﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable : IBaseInteractable
{
    void EndInteracting(GameObject interactor);

    void SubscribeToForceQuit(System.Action<IInteractable> action);
    void UnsubscribeToForceQuit(System.Action<IInteractable> action);

    GameObject gameObject { get; }
}

public interface IBaseInteractable
{
    void BeginInteracting(GameObject interactor);
}