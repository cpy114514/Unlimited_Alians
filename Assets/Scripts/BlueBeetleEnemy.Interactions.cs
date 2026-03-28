using UnityEngine;

public partial class BlueBeetleEnemy
{
    void EnsureHitboxes()
    {
        backHitbox = EnsureHitbox("BackHitbox", backHitbox, backHitboxOffset, backHitboxSize);
        hurtHitbox = EnsureHitbox("BodyHitbox", hurtHitbox, bodyHitboxOffset, bodyHitboxSize);
        shellKickLeftHitbox = EnsureHitbox(
            "ShellKickLeftHitbox",
            shellKickLeftHitbox,
            shellKickLeftOffset,
            shellKickHitboxSize
        );
        shellKickRightHitbox = EnsureHitbox(
            "ShellKickRightHitbox",
            shellKickRightHitbox,
            shellKickRightOffset,
            shellKickHitboxSize
        );
        shellTopKickHitbox = EnsureHitbox(
            "ShellTopKickHitbox",
            shellTopKickHitbox,
            shellTopKickOffset,
            shellTopKickHitboxSize
        );
    }

    BoxCollider2D EnsureHitbox(
        string childName,
        BoxCollider2D existingCollider,
        Vector2 defaultOffset,
        Vector2 defaultSize
    )
    {
        if (existingCollider != null)
        {
            existingCollider.isTrigger = true;
            existingCollider.gameObject.layer = gameObject.layer;
            return existingCollider;
        }

        Transform child = transform.Find(childName);
        bool createdObject = false;
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            child = childObject.transform;
            createdObject = true;
        }

        BoxCollider2D collider = child.GetComponent<BoxCollider2D>();
        bool createdCollider = false;
        if (collider == null)
        {
            collider = child.gameObject.AddComponent<BoxCollider2D>();
            createdCollider = true;
        }

        RemoveDuplicateChildren(childName, child);
        collider.isTrigger = true;
        child.gameObject.layer = gameObject.layer;

        if (createdObject || createdCollider)
        {
            collider.offset = defaultOffset;
            collider.size = defaultSize;
        }

        return collider;
    }

    void UpdateHitboxes()
    {
        RefreshHitbox(backHitbox);
        RefreshHitbox(hurtHitbox);
        RefreshHitbox(shellKickLeftHitbox);
        RefreshHitbox(shellKickRightHitbox);
        RefreshHitbox(shellTopKickHitbox);
    }

    void RefreshHitbox(BoxCollider2D hitbox)
    {
        if (hitbox == null)
        {
            return;
        }

        hitbox.isTrigger = true;
        hitbox.gameObject.layer = gameObject.layer;
    }

    void ProcessPlayerInteractions()
    {
        if (state == BeetleState.Walking)
        {
            if (TryHandleWalkingStomp())
            {
                return;
            }

            TryHandleWalkingDamage();
            return;
        }

        if (TryGetShellTopKick(out PlayerController topPlayer, out Collider2D topCollider, out bool topKickToRight))
        {
            if (!CanProcessPlayer(topPlayer))
            {
                return;
            }

            MarkPlayerProcessed(topPlayer);
            topPlayer.Bounce(stompBounceForce);

            if (state == BeetleState.ShellIdle)
            {
                KickShell(topKickToRight, topCollider);
                return;
            }

            StopShell();
            return;
        }

        if (TryGetShellPlayerOverlap(
                out PlayerController player,
                out Collider2D collider,
                out bool kickToRight))
        {
            if (!CanProcessPlayer(player))
            {
                return;
            }

            MarkPlayerProcessed(player);

            if (state == BeetleState.ShellIdle)
            {
                KickShell(kickToRight, collider);
                return;
            }

            StopShell();
        }
    }

    bool TryHandleWalkingStomp()
    {
        int count = OverlapPlayers(backHitbox, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = overlapBuffer[i];
            PlayerController player = collider != null ? collider.GetComponentInParent<PlayerController>() : null;
            if (player == null || !CanProcessPlayer(player))
            {
                continue;
            }

            if (!CanStompPlayer(player, collider))
            {
                continue;
            }

            MarkPlayerProcessed(player);
            EnterShell();
            player.Bounce(stompBounceForce);
            return true;
        }

        return false;
    }

    bool TryGetShellTopKick(out PlayerController player, out Collider2D collider, out bool kickToRight)
    {
        player = null;
        collider = null;
        kickToRight = false;

        int count = OverlapPlayers(shellTopKickHitbox, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D candidate = overlapBuffer[i];
            PlayerController foundPlayer = candidate != null
                ? candidate.GetComponentInParent<PlayerController>()
                : null;
            if (foundPlayer == null || !CanKickShellFromTop(foundPlayer, candidate))
            {
                continue;
            }

            player = foundPlayer;
            collider = candidate;
            kickToRight = foundPlayer.transform.position.x < transform.position.x;
            return true;
        }

        return false;
    }

    bool TryGetShellPlayerOverlap(out PlayerController player, out Collider2D collider, out bool kickToRight)
    {
        player = null;
        collider = null;
        kickToRight = false;

        int count = OverlapPlayers(shellKickLeftHitbox, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            PlayerController foundPlayer = overlapBuffer[i] != null
                ? overlapBuffer[i].GetComponentInParent<PlayerController>()
                : null;
            if (foundPlayer == null)
            {
                continue;
            }

            player = foundPlayer;
            collider = overlapBuffer[i];
            kickToRight = true;
            return true;
        }

        count = OverlapPlayers(shellKickRightHitbox, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            PlayerController foundPlayer = overlapBuffer[i] != null
                ? overlapBuffer[i].GetComponentInParent<PlayerController>()
                : null;
            if (foundPlayer == null)
            {
                continue;
            }

            player = foundPlayer;
            collider = overlapBuffer[i];
            kickToRight = false;
            return true;
        }

        return false;
    }

    void TryHandleWalkingDamage()
    {
        int count = OverlapPlayers(hurtHitbox, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D collider = overlapBuffer[i];
            PlayerController player = collider != null ? collider.GetComponentInParent<PlayerController>() : null;
            if (player == null || !CanProcessPlayer(player))
            {
                continue;
            }

            if (RoundManager.Instance != null &&
                RoundManager.Instance.IsPlayerResolved(player.controlType))
            {
                continue;
            }

            MarkPlayerProcessed(player);
            RoundManager.Instance?.PlayerDied(player.controlType);
            Destroy(player.gameObject);
            return;
        }
    }

    bool TryGetAnyPlayerOverlap(out PlayerController player, out Collider2D collider)
    {
        player = null;
        collider = null;

        int count = OverlapPlayers(backHitbox, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            PlayerController foundPlayer = overlapBuffer[i] != null
                ? overlapBuffer[i].GetComponentInParent<PlayerController>()
                : null;
            if (foundPlayer == null)
            {
                continue;
            }

            player = foundPlayer;
            collider = overlapBuffer[i];
            return true;
        }

        count = OverlapPlayers(hurtHitbox, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            PlayerController foundPlayer = overlapBuffer[i] != null
                ? overlapBuffer[i].GetComponentInParent<PlayerController>()
                : null;
            if (foundPlayer == null)
            {
                continue;
            }

            player = foundPlayer;
            collider = overlapBuffer[i];
            return true;
        }

        return false;
    }

    int OverlapPlayers(BoxCollider2D sourceCollider, Collider2D[] results)
    {
        if (sourceCollider == null || results == null)
        {
            return 0;
        }

        Bounds bounds = sourceCollider.bounds;
        Collider2D[] hits = Physics2D.OverlapBoxAll(bounds.center, bounds.size, 0f);
        int count = 0;

        for (int i = 0; i < hits.Length && count < results.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || hit.transform.root == transform)
            {
                continue;
            }

            if (hit.GetComponentInParent<PlayerController>() == null)
            {
                continue;
            }

            bool duplicate = false;
            for (int j = 0; j < count; j++)
            {
                if (results[j] != null &&
                    results[j].GetComponentInParent<PlayerController>() ==
                    hit.GetComponentInParent<PlayerController>())
                {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate)
            {
                continue;
            }

            results[count] = hit;
            count++;
        }

        for (int i = count; i < results.Length; i++)
        {
            results[i] = null;
        }

        return count;
    }

    bool CanProcessPlayer(PlayerController player)
    {
        if (player == null)
        {
            return false;
        }

        if (RoundManager.Instance != null &&
            RoundManager.Instance.IsPlayerResolved(player.controlType))
        {
            return false;
        }

        int playerId = player.GetInstanceID();
        if (interactionCooldownUntil.TryGetValue(playerId, out float cooldownUntil) &&
            Time.time < cooldownUntil)
        {
            return false;
        }

        return true;
    }

    void MarkPlayerProcessed(PlayerController player)
    {
        if (player == null)
        {
            return;
        }

        interactionCooldownUntil[player.GetInstanceID()] = Time.time + playerInteractionCooldown;
    }

    bool CanStompPlayer(PlayerController player, Collider2D playerCollider)
    {
        if (player == null || playerCollider == null || backHitbox == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Bounds backBounds = backHitbox.bounds;

        bool descending = player.VerticalVelocity <= stompMaxVerticalVelocity;
        bool feetAboveBackCenter = playerBounds.min.y >= backBounds.center.y - 0.01f;
        bool overlapWidth =
            playerBounds.max.x > backBounds.min.x + 0.01f &&
            playerBounds.min.x < backBounds.max.x - 0.01f;

        return descending && feetAboveBackCenter && overlapWidth;
    }

    bool CanKickShellFromTop(PlayerController player, Collider2D playerCollider)
    {
        if (player == null || playerCollider == null || shellTopKickHitbox == null)
        {
            return false;
        }

        Bounds playerBounds = playerCollider.bounds;
        Bounds topBounds = shellTopKickHitbox.bounds;

        bool descending = player.VerticalVelocity <= stompMaxVerticalVelocity;
        bool feetAboveTop = playerBounds.min.y >= topBounds.center.y - 0.01f;
        bool overlapWidth =
            playerBounds.max.x > topBounds.min.x + 0.01f &&
            playerBounds.min.x < topBounds.max.x - 0.01f;

        return descending && feetAboveTop && overlapWidth;
    }

    void EnterShell()
    {
        state = BeetleState.ShellIdle;
        animationTimer = 0f;

        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        ApplyColliderForState();
        ApplySprite(0f);
    }

    void StopShell()
    {
        state = BeetleState.ShellIdle;

        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        ApplyColliderForState();
    }

    void KickShell(bool kickToRight, Collider2D kickerCollider)
    {
        state = BeetleState.ShellMoving;
        movingRight = kickToRight;
        ApplyColliderForState();

        float direction = movingRight ? 1f : -1f;
        transform.position += new Vector3(direction * shellKickNudge, 0f, 0f);

        if (rb != null)
        {
            rb.position = transform.position;
            rb.velocity = new Vector2(direction * shellMoveSpeed, Mathf.Max(0f, rb.velocity.y));
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = movingRight;
        }

        if (kickerCollider != null && shellKickIgnoreTime > 0f && bodyCollider != null)
        {
            StartCoroutine(TemporarilyIgnoreCollision(kickerCollider));
        }
    }

    System.Collections.IEnumerator TemporarilyIgnoreCollision(Collider2D otherCollider)
    {
        if (otherCollider == null || bodyCollider == null)
        {
            yield break;
        }

        Physics2D.IgnoreCollision(bodyCollider, otherCollider, true);
        yield return new WaitForSeconds(shellKickIgnoreTime);

        if (otherCollider != null && bodyCollider != null)
        {
            Physics2D.IgnoreCollision(bodyCollider, otherCollider, false);
        }
    }
}
