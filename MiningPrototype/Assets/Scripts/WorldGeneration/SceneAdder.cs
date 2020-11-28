﻿using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneAdder : StateListenerBehaviour
{
    [SerializeField] List<MapAddition> addition;
    [SerializeField] SceneReference altarScene;

    [Zenject.Inject] DiContainer diContainer;


    bool loaded = false;
    MapAddition current;

    private void Start()
    {
        gameState.ChangeStateTo(GameState.State.Entry);
    }

    protected override void OnStateChanged(GameState.State newState)
    {
        if (newState == GameState.State.PreLoadScenes)
        {
            if (addition.Count > 0)
                StartCoroutine(LoadAdditive(addition, transitionState: true));
        }
    }

    private IEnumerator LoadAdditive(List<MapAddition> maps, bool transitionState)
    {
        int i = 0;
        SceneManager.sceneLoaded += OnSceneLoaded;

        while (i < maps.Count)
        {
            current = maps[i];

            SceneManager.LoadScene(current.SceneToAdd, LoadSceneMode.Additive);
            loaded = false;

            while (!loaded)
                yield return null;

            i++;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (transitionState)
            gameState.ChangeStateTo(GameState.State.PostLoadScenes);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (loadMode != LoadSceneMode.Additive)
            return;

        int x = Mathf.FloorToInt(RuntimeProceduralMap.Instance.SizeX * Util.RandomInV2(current.XOffsetRange));
        int y = Mathf.FloorToInt(RuntimeProceduralMap.Instance.SizeY * current.YOffset);

        Vector2Int offset = new Vector2Int(x, y);
        Debug.Log("Loaded: " + scene.name + " " + scene.rootCount + " at " + offset);

        foreach (var obj in scene.GetRootGameObjects())
        {
            if (obj.TryGetComponent(out EditorMap map))
            {
                RuntimeProceduralMap.Instance.LoadFromMap(map.SaveAsset, offset.x, offset.y);
                DestroyImmediate(obj);
            }
            else
            {
                obj.transform.position += offset.AsV3();
                diContainer.InjectGameObject(obj);
            }
        }
        loaded = true;
    }

    public void LoadAltarAt(Vector2Int value)
    {
        Debug.Log("Loading altar at " + value);
        MapAddition addition;
        addition.XOffsetRange = new Vector2((float) value.x / RuntimeProceduralMap.Instance.SizeX, (float)value.x / RuntimeProceduralMap.Instance.SizeX);
        addition.YOffset = (float)value.y / RuntimeProceduralMap.Instance.SizeY;
        addition.SceneToAdd = altarScene;
        StartCoroutine(LoadAdditive(new List<MapAddition>() { addition }, transitionState: false));
    }
}

[System.Serializable]
public struct MapAddition
{
    public Vector2 XOffsetRange;
    public float YOffset;
    public SceneReference SceneToAdd;
}