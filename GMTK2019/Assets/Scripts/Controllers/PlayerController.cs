﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    #region fields
    public enum PlayerState
    {
        Running,
        Jumping,
        WallRide,
        Dead
    }

    public enum WallAttatchedState
    {
        None,
        Left,
        Right
    }

    PlayerState playerState;
    WallAttatchedState wallAttatchedState = WallAttatchedState.None;

    [SerializeField] Rigidbody2D rb;

    int maxHealth = 100;

    float currentHealth;

    [SerializeField] float jumpForce;
    [SerializeField] float movementSpeed;
    [SerializeField] float damageDoneByPlatform;
    [SerializeField] float passiveHealing;

    [SerializeField] Vector2 wallRideRaycastVector;

    bool canJump;
    bool isLosingHealth;
    bool isFacingRight = true;

    [SerializeField] Animator animator;

    [SerializeField] Transform topRaycastOrigin;
    [SerializeField] Transform midRaycastOrigin;
    [SerializeField] Transform bottomRaycastOrigin;

    public static event Action<float> OnPlayerLosingHealth = delegate { };
    public static event Action<float> OnPlayerHealing = delegate { };
    public static event Action OnPlayerFinishLevel = delegate { };

    #endregion

    #region PrivateMethods

    void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (wallAttatchedState == WallAttatchedState.Left)
        {
            if (!isFacingRight)
                Flip();
        }
        else if (wallAttatchedState == WallAttatchedState.Right)
        {
            if (isFacingRight)
                Flip();
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            Jump();
        }

        if (isLosingHealth)
        {
            if (currentHealth > 0)
            {
                currentHealth -= damageDoneByPlatform;
                OnPlayerLosingHealth(damageDoneByPlatform);
            }
        }
        else
        {
            if (currentHealth < maxHealth)
            {
                currentHealth += passiveHealing;
                OnPlayerHealing(passiveHealing);
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void FixedUpdate()
    {

        if (playerState == PlayerState.Running)
        {
            if (isFacingRight)
            {
                rb.velocity = new Vector2(movementSpeed, rb.velocity.y);
            }
            else
            {
                rb.velocity = new Vector2(-movementSpeed, rb.velocity.y);
            }
        }
    }

    void ChangePlayerState(PlayerState stateToApply)
    {
        playerState = stateToApply;

        switch (stateToApply)
        {
            case PlayerState.Running:
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsRunning", true);
                animator.SetBool("IsWallRiding", false);
                break;

            case PlayerState.Jumping:
                animator.SetBool("IsJumping", true);
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsWallRiding", false);
                break;

            case PlayerState.WallRide:
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsWallRiding", true);
                break;

            case PlayerState.Dead:
                animator.SetBool("IsDead", true);
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsWallRiding", false);
                break;
            default:
                break;
        }
    }

    void ChangeWallAttatchedState(WallAttatchedState wall)
    {
        wallAttatchedState = wall;
    }

    void Jump()
    {
        if (!canJump)
            return;

        transform.parent = null;

        ChangePlayerState(PlayerState.Jumping);
        canJump = false;

        var directionToJump = Vector2.zero;
        switch (wallAttatchedState)
        {
            case WallAttatchedState.None:
                directionToJump = new Vector2(rb.velocity.x, jumpForce);
                break;

            case WallAttatchedState.Left:
                directionToJump = new Vector2(10, jumpForce);
                break;

            case WallAttatchedState.Right:
                directionToJump = new Vector2(-10, jumpForce);
                break;

            default:
                break;
        }

        rb.velocity = directionToJump;
    }

    void Die()
    {
        ChangePlayerState(PlayerState.Dead);
        Destroy(gameObject, 1f);
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        var scale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        transform.localScale = scale;
    }

    void DrawRaycasts()
    {
        var vectorDirectionToRaycast = isFacingRight ? wallRideRaycastVector : -wallRideRaycastVector;

        var topHit = Physics2D.Raycast(topRaycastOrigin.position, wallRideRaycastVector, vectorDirectionToRaycast.x);
        var midHit = Physics2D.Raycast(midRaycastOrigin.position, wallRideRaycastVector, vectorDirectionToRaycast.x);
        var bottomHit = Physics2D.Raycast(bottomRaycastOrigin.position, wallRideRaycastVector, vectorDirectionToRaycast.x);

        if ((topHit || midHit || bottomHit)
            && (IsValidWallRideRaycast(topHit)
                || IsValidWallRideRaycast(midHit)
                || IsValidWallRideRaycast(bottomHit)))
        {
            if (isFacingRight)
            {
                OnAttatchToWall(WallAttatchedState.Right);
            }
            else
            {
                OnAttatchToWall(WallAttatchedState.Left);
            }

            if (midHit.transform != null
                && midHit.transform.CompareTag("MousePlatform"))
            {
                transform.parent = midHit.transform;
            }
            else if (topHit.transform != null
                && topHit.transform.CompareTag("MousePlatform"))
            {
                transform.parent = topHit.transform;
            }
            else if (bottomHit.transform != null
                && bottomHit.transform.CompareTag("MousePlatform"))
            {
                transform.parent = bottomHit.transform;
            }
        }
    }

    void OnAttatchToWall(WallAttatchedState wall)
    {
        canJump = true;
        ChangeWallAttatchedState(wall);
        ChangePlayerState(PlayerState.WallRide);

        if (wall == WallAttatchedState.Right)
        {
            Flip();
        }
        else if (wall == WallAttatchedState.Left)
        {
            Flip();
        }
    }

    bool IsValidWallRideRaycast(RaycastHit2D ray)
    {
        if (ray.transform != null)
        {
            return ray.transform.CompareTag("VerticalWall")
                || ray.transform.CompareTag("MousePlatform");
        }

        return false;
    }

    void FinishLevel()
    {
        OnPlayerFinishLevel();
    }

    #endregion

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var collisionObject = collision.gameObject;

        if (collisionObject.CompareTag("Ground")
            || collision.gameObject.CompareTag("MousePlatform"))
        {
            canJump = true;

            if (wallAttatchedState == WallAttatchedState.None)
            {
                ChangePlayerState(PlayerState.Running);
            }

            if (collisionObject.CompareTag("MousePlatform"))
            {
                isLosingHealth = true;
            }
        }


        if (collisionObject.CompareTag("VerticalWall")
            || collisionObject.CompareTag("MousePlatform"))
        {
            DrawRaycasts();

            if (collisionObject.CompareTag("VerticalWall") && playerState == PlayerState.Running)
            {
                Die();
            }
        }

        if (collisionObject.CompareTag("Obstacle"))
        {
            Die();
        }

        if (collisionObject.CompareTag("LevelGoal") && playerState != PlayerState.Dead)
        {
            FinishLevel();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        var collisionObject = collision.gameObject;

        if (collisionObject.CompareTag("Ground")
            || collisionObject.CompareTag("MousePlatform"))
        {
            canJump = false;

            if (collisionObject.CompareTag("MousePlatform"))
            {
                isLosingHealth = false;
                transform.parent = null;
            }
        }

        if (collisionObject.CompareTag("VerticalWall"))
        {
            ChangeWallAttatchedState(WallAttatchedState.None);
            ChangePlayerState(PlayerState.Jumping);
        }
    }
}
