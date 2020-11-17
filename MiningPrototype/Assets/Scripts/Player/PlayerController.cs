﻿using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngineInternal;

public enum PlayerState
{
    Normal,
    Locked,
    Climbing
}

public class PlayerController : InventoryOwner, IEntity
{
    [SerializeField] PlayerSettings settings;

    [SerializeField] Transform feet;

    [SerializeField] GameObject pickaxe;
    [SerializeField] Transform mouseHighlight;


    [SerializeField] AudioSource breakBlock, startMining, walking;
    [SerializeField] DirectionBasedAnimator pickaxeAnimator;

    [SerializeField] EventSystem eventSystem;
    [SerializeField] ParticleSystem miningParticles;
    [SerializeField] SpriteRenderer heldItemPreview;


    Rigidbody2D rigidbody;
    SpriteAnimator spriteAnimator;
    float lastGroundedTimeStamp;
    float lastJumpTimeStamp;

    private bool isGrounded;
    Vector2 rightWalkVector = Vector3.right;
    Camera camera;
    SpriteRenderer spriteRenderer;
    Vector2Int? gridDigTarget;
    IInteractable currentInteractable;
    IMinableNonGrid nonGridDigTarget;

    [ReadOnly]
    [SerializeField] bool inMining;

    private PlayerState playerState;
    private bool isVisible = true;
    private Ladder currentLadder;
    private float gravityScale;
    private bool heldIsPickaxe = true;


    private bool InFrontOfLadder { get => currentLadder != null; }
    private bool IsLocked { get => playerState != PlayerState.Locked; }

    public override bool IsFlipped { get => spriteRenderer.flipX; }

    protected override void Start()
    {
        base.Start();
        camera = Camera.main;
        rigidbody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteAnimator = GetComponent<SpriteAnimator>();
        gravityScale = rigidbody.gravityScale;
    }

    private void Update()
    {
        if (!IsLocked || !isVisible)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }

        if (Vector2Int.Distance(GetPositionInGrid(), GetClickCoordinate()) <= settings.maxDigDistance)
        {
            Debug.DrawLine(GetPositionInGridV3(), GetClickPositionV3(), Color.yellow, Time.deltaTime);
            UpdateDigTarget();
            UpdateNonGridDigTarget();

            if (Input.GetMouseButton(0))
            {
                if (eventSystem.currentSelectedGameObject == null)
                    TryDig();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                if (Vector3.Distance(GetPositionInGridV3(), GetClickPositionV3()) <= settings.inventoryOpenDistance && isGrounded)
                {
                    ToggleInventory();
                }
                else
                {
                    if (currentInteractable == null)
                        TryInteract();
                    else
                    {
                        if (eventSystem.IsPointerOverGameObject() == false)
                            TryStopInteracting();
                    }
                }
            }
            else
            {
                TryDisableMiningVisuals();
            }
        }
        else
        {
            gridDigTarget = null;
            TryDisableMiningVisuals();
        }

        UpdateDigHighlight();
    }

    private void ToggleInventory()
    {
        if (InventoryDisplayState == InventoryState.Closed)
            OpenInventory();
        else
            CloseInventory();
    }

    private void TryInteract()
    {
        var hits = Util.RaycastFromMouse();

        currentInteractable = null;
        foreach (var hit in hits)
        {
            if (hit.transform == transform)
                continue;

            if (hit.transform.TryGetComponent(out IInteractable interactable))
            {
                Debug.Log(hit.transform.name);
                currentInteractable = interactable;
                currentInteractable.SubscribeToForceQuit(OnInteractableForceQuit);
                currentInteractable.BeginInteracting(gameObject);
                Debug.DrawLine(GetPositionInGridV3(), hit.point, Color.green, 1f);
                break;
            }
        }
    }

    private void OnInteractableForceQuit()
    {
        TryStopInteracting();
    }

    private bool CanJump()
    {
        return Time.time - lastGroundedTimeStamp < settings.timeAfterGroundedToJump && Time.time - lastJumpTimeStamp > settings.jumpCooldown;
    }

    private void UpdateDigTarget()
    {
        gridDigTarget = TileMapHelper.GetClosestSolidBlock(TileMap.Instance, GetPositionInGrid(), GetClickCoordinate());
        if (!TileMap.Instance.CanTarget(gridDigTarget.Value.x, gridDigTarget.Value.y))
        {
            gridDigTarget = null;
        }
    }

    private void UpdateNonGridDigTarget()
    {
        var hits = Util.RaycastFromMouse();

        IMinableNonGrid newTarget = null;
        foreach (var hit in hits)
        {
            if (hit.transform == transform)
                continue;

            if (hit.transform.TryGetComponent(out IMinableNonGrid minable))
            {
                //Debug.Log(hit.transform.name);
                newTarget = minable;
                break;
            }
        }

        //Debug.Log("new target: " + (newTarget != null).ToString() + " / old target:" + (nonGridDigTarget != null).ToString());

        if (newTarget != nonGridDigTarget)
        {
            if (nonGridDigTarget != null)
                nonGridDigTarget.MouseLeave();

            if (newTarget != null)
                newTarget.MouseEnter();

            nonGridDigTarget = newTarget;
        }
    }

    private void UpdateDigHighlight()
    {

        if (gridDigTarget == null)
        {
            if (nonGridDigTarget == null)
                mouseHighlight.position = new Vector3(-1000, -1000);
            else
                mouseHighlight.position = new Vector3(nonGridDigTarget.GetPosition().x, nonGridDigTarget.GetPosition().y, 0);
        }
        else
            mouseHighlight.position = new Vector3(gridDigTarget.Value.x, gridDigTarget.Value.y, 0) + new Vector3(0.5f, 0.5f, 0);
    }

    private void TryPlace()
    {
        Vector2Int clickPos = GetClickCoordinate();
        if (TileMapHelper.HasLineOfSight(TileMap.Instance, GetPositionInGrid(), clickPos, debugVisualize: true))
            TileMap.Instance.PlaceAt(clickPos.x, clickPos.y, Tile.Make(TileType.Stone));
    }

    private void TryDig()
    {
        CloseInventory();

        if (gridDigTarget.HasValue)
        {
            bool broken = TileMap.Instance.DamageAt(gridDigTarget.Value.x, gridDigTarget.Value.y, Time.deltaTime * settings.digSpeed * ProgressionHandler.Instance.DigSpeedMultiplyer, playerCaused: true);

            if (broken)
            {
                miningParticles.transform.position = (Vector3Int)gridDigTarget + new Vector3(0.5f, 0.5f);
                miningParticles.Emit(settings.miningBreakParticlesCount);
                breakBlock.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                breakBlock.Play();
                TryDisableMiningVisuals();
            }
            else
            {
                UpdateMiningParticlesPositions();
            }

            TryEnableMiningVisuals();
        }
        else
        {
            if (nonGridDigTarget != null)
            {
                nonGridDigTarget.Damage(Time.deltaTime * settings.digSpeed);
                miningParticles.transform.position = nonGridDigTarget.GetPosition();
                TryEnableMiningVisuals();
            }
            else
            {
                TryDisableMiningVisuals();
            }
        }
    }


    private void UpdateMiningParticlesPositions()
    {
        miningParticles.transform.position = TileMapHelper.GetWorldLocationOfFreeFaceFromSource(TileMap.Instance, gridDigTarget.Value, GetPositionInGrid());
        Debug.DrawLine((Vector3Int)GetPositionInGrid(), miningParticles.transform.position, Color.yellow, 0.1f);
    }

    private void TryDisableMiningVisuals()
    {
        if (inMining)
        {
            inMining = false;
            var emission = miningParticles.emission;
            emission.rateOverTimeMultiplier = 0;
            startMining.Stop();
            pickaxeAnimator.Stop();
        }
    }

    private void TryStopInteracting()
    {
        if (currentInteractable != null)
        {
            currentInteractable.EndInteracting(gameObject);
            currentInteractable.UnsubscribeToForceQuit(OnInteractableForceQuit);
            currentInteractable = null;
        }
    }

    private void TryEnableMiningVisuals()
    {
        if (!inMining)
        {
            var emission = miningParticles.emission;
            emission.rateOverTimeMultiplier = settings.miningParticlesRateOverTime;
            inMining = true;
            startMining.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            startMining.Play();
            pickaxeAnimator.Play();
        }
    }

    public Vector2Int GetPositionInGrid()
    {
        return transform.position.ToGridPosition() + new Vector2Int(0, 1); //+1 to be at center of player
    }

    /// <summary>
    /// Just +1 in Y compared to transform.position
    /// </summary>
    public Vector3 GetPositionInGridV3()
    {
        return new Vector3(transform.position.x, transform.position.y + 1); //+1 to be at center of player
    }

    private Vector2Int GetClickCoordinate()
    {
        Vector3 clickPos = GetClickPositionV3();
        return new Vector2Int((int)clickPos.x, (int)clickPos.y);
    }

    private Vector3 GetClickPositionV3()
    {
        Vector3 position = Input.mousePosition + Vector3.back * camera.transform.position.z;
        return camera.ScreenToWorldPoint(position);
    }

    private void FixedUpdate()
    {
        if (!isVisible)
            return;

        switch (playerState)
        {
            case PlayerState.Normal:
                UpdateWalk();
                if (InFrontOfLadder)
                    TryStartClimb();
                else
                    UpdateJump();
                break;

            case PlayerState.Climbing:
                UpdateClimb();
                break;
        }
    }

    private void UpdateClimb()
    {
        if (InFrontOfLadder)
        {
            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");

            Vector2 climbVelocity = new Vector2(horizontal * settings.climbPanSpeed, vertical * settings.climbSpeed);
            rigidbody.velocity = climbVelocity;

            if (climbVelocity.magnitude > settings.climbIdleThreshold)
            {
                spriteAnimator.Play(settings.an_Climb, false);
            }
            else
            {
                spriteAnimator.Play(settings.an_ClimbIdle, false);
            }

            if (vertical > 0)
                currentLadder.NotifyGoingUp();
            else
                currentLadder.NotifyGoingDown();

        }
        else
        {
            ChangeStateTo(PlayerState.Normal);
        }


    }

    private void TryStartClimb()
    {
        var vertical = Input.GetAxis("Vertical");

        if (Mathf.Abs(vertical) > 0.75f)
        {
            ChangeStateTo(PlayerState.Climbing);
        }
    }

    private void UpdateWalk()
    {
        var horizontal = Input.GetAxis("Horizontal");

        if (currentInteractable != null)
        {
            if (Vector3.Distance(GetPositionInGridV3(), currentInteractable.gameObject.transform.position) > settings.maxInteractableDistance)
                TryStopInteracting();
        }

        rigidbody.position += horizontal * rightWalkVector * settings.moveSpeed * Time.fixedDeltaTime * ProgressionHandler.Instance.SpeedMultiplyer;
        rigidbody.velocity = new Vector2(0, rigidbody.velocity.y);

        if (Mathf.Abs(horizontal) > 0.2f)
            spriteRenderer.flipX = horizontal < 0;

        if (isGrounded)
        {
            if (horizontal == 0)
            {
                if (InventoryDisplayState == InventoryState.Open)
                {
                    spriteAnimator.Play(settings.an_Inventory, false);
                    SetHeldVisible(false);
                }
                else
                {
                    spriteAnimator.Play(settings.an_Idle, false);
                    SetHeldVisible(true);
                }
            }
            else
            {
                spriteAnimator.Play(settings.an_Walk, false);
                SetHeldVisible(true);
            }
        }
        else
        {
            spriteAnimator.Play(settings.an_Fall);
            SetHeldVisible(true);
        }

        UpdateWalkingSound(horizontal);
    }

    public void SetHeldVisible(bool isVisible = true)
    {
        if (heldIsPickaxe)
        {
            if (isVisible != pickaxe.activeSelf)
            {
                pickaxe.SetActive(isVisible);
            }
            heldItemPreview.enabled = false;
        }
        else
        {
            if (isVisible != heldItemPreview.enabled)
            {
                heldItemPreview.enabled = true;
            }

            if (pickaxe.activeSelf)
                pickaxe.SetActive(false);
        }
    }

    public void SetHeldItem(bool setToPickaxe)
    {
        heldIsPickaxe = setToPickaxe;
    }

    public void SetHeldItemSprite(Sprite sprite)
    {
        heldItemPreview.sprite = sprite;
    }

    private void UpdateJump()
    {
        var vertical = Input.GetAxis("Vertical");
        Collider2D[] colliders = Physics2D.OverlapCircleAll(feet.position, settings.feetRadius);
        isGrounded = colliders != null && colliders.Length > 1;

        if (isGrounded)
        {
            lastGroundedTimeStamp = Time.time;
        }

        if (CanJump() && vertical > 0)
        {
            Jump();
        }
    }

    private void Jump()
    {
        rigidbody.velocity = new Vector2(rigidbody.velocity.x, settings.jumpVelocity);
        lastJumpTimeStamp = Time.time;
    }

    private void UpdateWalkingSound(float horizontal)
    {
        if (isGrounded && Mathf.Abs(horizontal) > 0.01f)
        {
            if (!walking.isPlaying)
            {

                walking.Play();
            }
        }
        else
        {
            if (walking.isPlaying)
            {

                walking.Pause();
            }
        }
    }

    private void ChangeStateTo(PlayerState newState)
    {
        if (playerState == newState)
            return;

        LeaveState(playerState);
        playerState = newState;
        EnterState(newState);
    }

    private void LeaveState(PlayerState stateLeft)
    {
        switch (stateLeft)
        {
            case PlayerState.Climbing:
                rigidbody.gravityScale = gravityScale;
                SetHeldVisible(true);
                break;
        }
    }

    private void EnterState(PlayerState stateEntered)
    {
        switch (stateEntered)
        {
            case PlayerState.Climbing:
                SetHeldVisible(false);
                rigidbody.gravityScale = 0;
                break;
        }
    }

    [Button]
    public void Hide()
    {
        isVisible = false;
        SetHeldVisible(false);
        spriteRenderer.enabled = false;
        walking.Pause();
    }

    [Button]
    public void Show()
    {
        isVisible = true;
        SetHeldVisible(false);
        spriteRenderer.enabled = true;
    }

    [Button]
    public void Freeze()
    {
        ChangeStateTo(PlayerState.Locked);
        walking.Pause();
    }

    [Button]
    public void Defreeze()
    {
        ChangeStateTo(PlayerState.Normal);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        UpdateWalkVector(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        UpdateWalkVector(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        rightWalkVector = Vector2.right;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Ladder ladder))
        {
            currentLadder = ladder;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Ladder ladder))
        {
            currentLadder = null;
        }
    }

    private void UpdateWalkVector(Collision2D collision)
    {
        var contact = collision.contacts[0];
        float angle = Mathf.Acos(Vector3.Dot(contact.normal, Vector3.up)) * Mathf.Rad2Deg;

        Debug.DrawLine(transform.position, transform.position + (Vector3)contact.normal);

        if (angle < settings.groundedAngle)
        {
            rightWalkVector = Vector3.Cross(contact.normal, Vector3.forward).normalized;
        }
        else
        {
            rightWalkVector = Vector3.right;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (feet != null)
            Gizmos.DrawWireSphere(feet.position, settings.feetRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)rightWalkVector);

        Gizmos.DrawWireSphere((Vector3Int)GetPositionInGrid(), settings.maxDigDistance);
        Gizmos.DrawWireSphere(GetPositionInGridV3(), settings.inventoryOpenDistance);
    }
}
