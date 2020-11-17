﻿using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 generalOffset = new Vector3(0, 0, -10);
    [SerializeField] float constantSpeedMultiplyer;
    [SerializeField] CameraShaker cameraShaker;
    Vector3 offsetToTarget;
    Transform defaultTarget;

    public Camera Camera { get; private set; }
    private void Start()
    {
        defaultTarget = target;
        offsetToTarget = generalOffset;
        Camera = GetComponent<Camera>();
    }

    private void OnPreRender()
    {
        transform.position = target.position + offsetToTarget + cameraShaker.GetShakeAmount();
    }

    public void FollowNewTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void TransitionToDefault(bool constantSpeed= true, float time = 0)
    {
        TransitionToNewTarget(defaultTarget, constantSpeed, time);
    }

    public void TransitionToNewTarget(Transform newTarget, bool constantSpeed = true, float time = 0)
    {
        StopAllCoroutines();

        if (constantSpeed)
        {
            time = Vector2.Distance(target.position, newTarget.position) / constantSpeedMultiplyer;
        }

        StartCoroutine(TransitionTo(target.position, newTarget, time));
    }

    private IEnumerator TransitionTo(Vector3 start, Transform end, float totalTime)
    {
        Transform temporary = new GameObject("TEMP_TRANSITION").transform;
        temporary.position = start;
        FollowNewTarget(temporary);

        float currentTime = totalTime;
        while (currentTime > 0)
        {
            temporary.position = Vector3.Lerp(end.position, start, currentTime / totalTime);

            yield return null;
            currentTime -= Time.deltaTime;
        }

        FollowNewTarget(end);
        Destroy(temporary.gameObject);
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(2))
            Shake(Util.MouseToWorld());
    }
    public CameraShake Shake(Vector2 location, CameraShakeType shakeType = CameraShakeType.hill, float duration = 1f, float range = 10f)
    {
        return cameraShaker.StartShake(shakeType, duration, location, range);
    }
    public void StopShake(CameraShake shake)
    {
        cameraShaker.StopShake(shake);
    }
}
