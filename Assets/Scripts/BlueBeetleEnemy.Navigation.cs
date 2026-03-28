using UnityEngine;

public partial class BlueBeetleEnemy
{
    void Patrol()
    {
        float direction = movingRight ? 1f : -1f;
        bool grounded = IsGrounded();

        if (!grounded)
        {
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
            spriteRenderer.flipX = movingRight;
            return;
        }

        if (HasTrampolineAhead(direction))
        {
            movingRight = !movingRight;
            direction = movingRight ? 1f : -1f;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
            spriteRenderer.flipX = movingRight;
            return;
        }

        if (TryLeaveSmallPlatform(ref direction))
        {
            spriteRenderer.flipX = movingRight;
            return;
        }

        bool steppedUp = TryStepUp(direction);
        bool blockedAhead = !steppedUp && IsFrontBlocked(direction);

        if (blockedAhead && TryJumpUpTile(direction))
        {
            blockedAhead = false;
        }

        if (blockedAhead || !HasTraversableGroundAhead(direction))
        {
            movingRight = !movingRight;
            direction = movingRight ? 1f : -1f;
        }

        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        spriteRenderer.flipX = movingRight;
    }

    bool TryStepUp(float direction)
    {
        if (rb == null || bodyCollider == null || !IsGrounded())
        {
            return false;
        }

        if (Mathf.Abs(rb.velocity.y) > 0.2f)
        {
            return false;
        }

        Bounds bounds = bodyCollider.bounds;
        Vector2 lowerOrigin = new Vector2(
            direction > 0f ? bounds.max.x : bounds.min.x,
            bounds.min.y + 0.06f
        );
        LayerMask mask = ResolveGroundMask();

        RaycastHit2D lowerHit = Physics2D.Raycast(
            lowerOrigin,
            Vector2.right * direction,
            stepCheckDistance,
            mask
        );
        if (lowerHit.collider == null || lowerHit.collider.transform.root == transform)
        {
            return false;
        }

        Vector2 upperOrigin = lowerOrigin + Vector2.up * stepUpHeight;
        RaycastHit2D upperHit = Physics2D.Raycast(
            upperOrigin,
            Vector2.right * direction,
            stepCheckDistance,
            mask
        );
        if (upperHit.collider != null && upperHit.collider.transform.root != transform)
        {
            return false;
        }

        Vector2 landingProbeOrigin = upperOrigin + Vector2.right * direction * (stepCheckDistance + 0.04f);
        RaycastHit2D landingHit = Physics2D.Raycast(
            landingProbeOrigin,
            Vector2.down,
            stepUpHeight + 0.2f,
            mask
        );
        if (landingHit.collider == null || landingHit.collider.transform.root == transform)
        {
            return false;
        }

        float targetBottom = landingHit.point.y + 0.02f;
        float verticalLift = targetBottom - bounds.min.y;
        if (verticalLift <= 0.02f || verticalLift > stepUpHeight + 0.05f)
        {
            return false;
        }

        Vector2 stepOffset = new Vector2(direction * stepForwardNudge, verticalLift);
        Vector2 overlapCenter = (Vector2)bounds.center + stepOffset;
        Vector2 overlapSize = bounds.size - new Vector3(0.06f, 0.04f, 0f);
        Collider2D blockingCollider = Physics2D.OverlapBox(
            overlapCenter,
            overlapSize,
            0f,
            mask
        );

        if (blockingCollider != null && blockingCollider.transform.root != transform)
        {
            return false;
        }

        rb.position += stepOffset;
        rb.velocity = new Vector2(direction * moveSpeed, Mathf.Max(0f, rb.velocity.y));
        spriteRenderer.flipX = movingRight;
        return true;
    }

    void MoveShell()
    {
        float direction = movingRight ? 1f : -1f;

        if (!IsGrounded())
        {
            rb.velocity = new Vector2(direction * shellMoveSpeed, rb.velocity.y);
            spriteRenderer.flipX = movingRight;
            return;
        }

        if (CastForGround(
                new Vector2(
                    direction > 0f ? bodyCollider.bounds.max.x + 0.02f : bodyCollider.bounds.min.x - 0.02f,
                    bodyCollider.bounds.center.y),
                Vector2.right * direction,
                wallCheckDistance))
        {
            movingRight = !movingRight;
            direction = movingRight ? 1f : -1f;
        }

        rb.velocity = new Vector2(direction * shellMoveSpeed, rb.velocity.y);
        spriteRenderer.flipX = movingRight;
    }

    bool IsFrontBlocked(float direction)
    {
        Vector2 wallProbe = GetProbeWorldPosition(frontWallProbe, direction, new Vector2(0.4f, -0.02f));
        if (!TryRaycastTraversal(wallProbe, Vector2.right * direction, wallCheckDistance, out RaycastHit2D hit))
        {
            return false;
        }

        if (bodyCollider != null && hit.point.y <= bodyCollider.bounds.min.y + 0.08f)
        {
            return false;
        }

        return true;
    }

    bool HasTraversableGroundAhead(float direction)
    {
        Bounds bounds = bodyCollider.bounds;
        float rayDistance = maxStepDownHeight + edgeCheckDistance + 0.18f;
        Vector2[] probeOrigins =
        {
            GetProbeWorldPosition(dropProbeNear, direction, new Vector2(0.34f, 1.05f)),
            GetProbeWorldPosition(dropProbeFar, direction, new Vector2(0.72f, 1.05f))
        };

        for (int i = 0; i < probeOrigins.Length; i++)
        {
            if (!TryRaycastTraversal(probeOrigins[i], Vector2.down, rayDistance, out RaycastHit2D hit))
            {
                continue;
            }

            float dropHeight = bounds.min.y - hit.point.y;
            if (dropHeight <= maxStepDownHeight + 0.05f)
            {
                return true;
            }
        }

        float[] fallbackForwardOffsets =
        {
            bounds.extents.x + 0.12f,
            bounds.extents.x + 0.45f,
            bounds.extents.x + 0.82f
        };

        for (int i = 0; i < fallbackForwardOffsets.Length; i++)
        {
            Vector2 origin = new Vector2(
                direction > 0f ? bounds.max.x + fallbackForwardOffsets[i] : bounds.min.x - fallbackForwardOffsets[i],
                bounds.min.y + 0.08f
            );

            if (!TryRaycastTraversal(
                origin,
                Vector2.down,
                maxStepDownHeight + 1.1f,
                out RaycastHit2D hit))
            {
                continue;
            }

            float dropHeight = bounds.min.y - hit.point.y;
            if (dropHeight <= maxStepDownHeight + 0.05f)
            {
                return true;
            }
        }

        return false;
    }

    bool TryLeaveSmallPlatform(ref float direction)
    {
        if (bodyCollider == null || !TryGetGroundHit(out RaycastHit2D groundHit))
        {
            return false;
        }

        float currentPlatformWidth = EstimatePlatformWidthAt(groundHit.point);
        if (currentPlatformWidth > smallPlatformWidthThreshold)
        {
            return false;
        }

        LandingOption leftLowerOption = FindLowerLandingOption(-1f, groundHit.point, currentPlatformWidth);
        LandingOption rightLowerOption = FindLowerLandingOption(1f, groundHit.point, currentPlatformWidth);

        if (leftLowerOption.valid || rightLowerOption.valid)
        {
            LandingOption chosenLowerOption = ChooseBetterLandingOption(leftLowerOption, rightLowerOption);
            movingRight = chosenLowerOption.direction > 0f;
            direction = chosenLowerOption.direction;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
            return true;
        }

        LandingOption leftOption = FindLandingOption(-1f, groundHit.point, currentPlatformWidth);
        LandingOption rightOption = FindLandingOption(1f, groundHit.point, currentPlatformWidth);

        if (!leftOption.valid && !rightOption.valid)
        {
            return false;
        }

        LandingOption chosenOption = ChooseBetterLandingOption(leftOption, rightOption);
        movingRight = chosenOption.direction > 0f;
        direction = chosenOption.direction;

        if (chosenOption.requiresJump)
        {
            if (Time.time - lastJumpTime < jumpCooldown)
            {
                return false;
            }

            float launchForce = CalculateJumpForce(chosenOption.jumpHeight);
            rb.velocity = new Vector2(direction * moveSpeed, launchForce);
            lastJumpTime = Time.time;
            return true;
        }

        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        return true;
    }

    LandingOption ChooseBetterLandingOption(LandingOption leftOption, LandingOption rightOption)
    {
        if (!leftOption.valid)
        {
            return rightOption;
        }

        if (!rightOption.valid)
        {
            return leftOption;
        }

        return rightOption.score > leftOption.score ? rightOption : leftOption;
    }

    LandingOption FindLandingOption(float direction, Vector2 currentGroundPoint, float currentPlatformWidth)
    {
        LandingOption bestOption = default;
        if (bodyCollider == null)
        {
            return bestOption;
        }

        Bounds bounds = bodyCollider.bounds;
        float startDistance = bounds.extents.x + 0.3f;
        float searchStartY = currentGroundPoint.y + maxJumpableHeight + 0.45f;

        for (float distance = startDistance; distance <= smallPlatformSearchDistance; distance += smallPlatformProbeStep)
        {
            Vector2 origin = new Vector2(
                bounds.center.x + direction * distance,
                searchStartY
            );

            if (!TryRaycastTraversal(
                    origin,
                    Vector2.down,
                    maxJumpableHeight + maxStepDownHeight + 1.2f,
                    out RaycastHit2D hit))
            {
                continue;
            }

            float heightDelta = hit.point.y - currentGroundPoint.y;
            if (heightDelta > maxJumpableHeight + 0.05f)
            {
                continue;
            }

            if (heightDelta < -(maxStepDownHeight + 0.55f))
            {
                continue;
            }

            float landingWidth = EstimatePlatformWidthAt(hit.point);
            if (landingWidth <= currentPlatformWidth + 0.2f)
            {
                continue;
            }

            bool requiresJump = heightDelta > 0.15f || distance > bounds.extents.x + 0.8f;
            float score =
                landingWidth * 2f -
                distance * 0.35f -
                Mathf.Abs(heightDelta) * 0.5f;

            if (!bestOption.valid || score > bestOption.score)
            {
                bestOption = new LandingOption
                {
                    valid = true,
                    direction = direction,
                    requiresJump = requiresJump,
                    jumpHeight = Mathf.Max(0.25f, heightDelta),
                    distance = distance,
                    landingWidth = landingWidth,
                    score = score
                };
            }
        }

        return bestOption;
    }

    LandingOption FindLowerLandingOption(float direction, Vector2 currentGroundPoint, float currentPlatformWidth)
    {
        LandingOption bestOption = default;
        if (bodyCollider == null)
        {
            return bestOption;
        }

        Bounds bounds = bodyCollider.bounds;
        float startDistance = bounds.extents.x + 0.3f;
        float searchStartY = currentGroundPoint.y + 0.5f;

        for (float distance = startDistance; distance <= smallPlatformSearchDistance; distance += smallPlatformProbeStep)
        {
            Vector2 origin = new Vector2(
                bounds.center.x + direction * distance,
                searchStartY
            );

            if (!TryRaycastTraversal(
                    origin,
                    Vector2.down,
                    smallPlatformMaxDropHeight + 1.2f,
                    out RaycastHit2D hit))
            {
                continue;
            }

            float heightDelta = hit.point.y - currentGroundPoint.y;
            if (heightDelta > -0.15f || heightDelta < -smallPlatformMaxDropHeight)
            {
                continue;
            }

            float landingWidth = EstimatePlatformWidthAt(hit.point);
            if (landingWidth <= currentPlatformWidth + 0.2f)
            {
                continue;
            }

            float score =
                landingWidth * 3f -
                distance * 0.25f +
                Mathf.Abs(heightDelta) * 0.2f;

            if (!bestOption.valid || score > bestOption.score)
            {
                bestOption = new LandingOption
                {
                    valid = true,
                    direction = direction,
                    requiresJump = false,
                    jumpHeight = 0f,
                    distance = distance,
                    landingWidth = landingWidth,
                    score = score
                };
            }
        }

        return bestOption;
    }

    float EstimatePlatformWidthAt(Vector2 groundPoint)
    {
        if (bodyCollider == null)
        {
            return 0f;
        }

        Bounds bounds = bodyCollider.bounds;
        float baseWidth = bounds.size.x;
        float leftSupport = MeasurePlatformReach(groundPoint, -1f, bounds.extents.x);
        float rightSupport = MeasurePlatformReach(groundPoint, 1f, bounds.extents.x);
        return baseWidth + leftSupport + rightSupport;
    }

    float MeasurePlatformReach(Vector2 groundPoint, float direction, float startOffset)
    {
        float furthestSupport = 0f;
        float probeHeight = groundPoint.y + 0.35f;

        for (float distance = startOffset + smallPlatformProbeStep;
             distance <= smallPlatformSearchDistance;
             distance += smallPlatformProbeStep)
        {
            Vector2 origin = new Vector2(
                groundPoint.x + direction * distance,
                probeHeight
            );

            if (!TryRaycastTraversal(origin, Vector2.down, 0.75f, out RaycastHit2D hit))
            {
                break;
            }

            if (Mathf.Abs(hit.point.y - groundPoint.y) > platformHeightTolerance)
            {
                break;
            }

            furthestSupport = distance - startOffset;
        }

        return furthestSupport;
    }

    bool TryGetGroundHit(out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(
            GetProbeWorldPosition(groundProbe, 1f, new Vector2(0f, -0.34f)),
            Vector2.down,
            groundCheckDistance,
            ResolveGroundMask()
        );

        return IsTraversalCollider(hit.collider);
    }

    bool CastForGround(Vector2 origin, Vector2 direction, float distance)
    {
        return TryRaycastTraversal(origin, direction, distance, out _);
    }

    LayerMask ResolveGroundMask()
    {
        if (groundMask.value != 0)
        {
            return groundMask;
        }

        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            groundMask = player.groundLayer;
            return groundMask;
        }

        groundMask = Physics2D.AllLayers;
        return groundMask;
    }

    LayerMask ResolveTraversalMask()
    {
        if (traversalMask.value != 0)
        {
            return traversalMask;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
        {
            traversalMask = 1 << groundLayer;
            return traversalMask;
        }

        traversalMask = ResolveGroundMask();
        return traversalMask;
    }

    bool TryRaycastTraversal(Vector2 origin, Vector2 direction, float distance, out RaycastHit2D validHit)
    {
        LayerMask mask = ResolveTraversalMask();
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction.normalized, distance, mask);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (IsTraversalCollider(hit.collider))
            {
                validHit = hit;
                return true;
            }
        }

        validHit = default;
        return false;
    }

    bool HasTrampolineAhead(float direction)
    {
        if (bodyCollider == null)
        {
            return false;
        }

        Bounds bounds = bodyCollider.bounds;
        float[] forwardOffsets =
        {
            bounds.extents.x + trampolineAvoidNearDistance,
            bounds.extents.x + trampolineAvoidFarDistance
        };

        LayerMask mask = ResolveGroundMask();
        float rayDistance = trampolineAvoidProbeHeight + maxStepDownHeight + 0.8f;

        for (int i = 0; i < forwardOffsets.Length; i++)
        {
            Vector2 origin = new Vector2(
                direction > 0f ? bounds.center.x + forwardOffsets[i] : bounds.center.x - forwardOffsets[i],
                bounds.min.y + trampolineAvoidProbeHeight
            );

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, rayDistance, mask);
            for (int j = 0; j < hits.Length; j++)
            {
                Collider2D hitCollider = hits[j].collider;
                if (hitCollider == null || hitCollider.transform.root == transform || hitCollider.isTrigger)
                {
                    continue;
                }

                if (hitCollider.GetComponentInParent<Trampoline>() != null)
                {
                    return true;
                }

                if (IsTraversalCollider(hitCollider))
                {
                    break;
                }
            }
        }

        return false;
    }

    bool IsTraversalCollider(Collider2D collider)
    {
        if (collider == null || collider.transform.root == transform || collider.isTrigger)
        {
            return false;
        }

        if (collider.GetComponentInParent<PlayerController>() != null)
        {
            return false;
        }

        if (collider.GetComponentInParent<BlueBeetleEnemy>() != null)
        {
            return false;
        }

        if (collider.GetComponentInParent<Trampoline>() != null)
        {
            return false;
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0 && collider.gameObject.layer == groundLayer)
        {
            return true;
        }
        return false;
    }

    bool TryJumpUpTile(float direction)
    {
        if (rb == null || bodyCollider == null || Time.time - lastJumpTime < jumpCooldown)
        {
            return false;
        }

        if (!IsGrounded())
        {
            return false;
        }

        float launchForce = jumpForce;
        int maxJumpCells = 1;

        Vector2 baseProbe = GetProbeWorldPosition(jumpBlockProbeLow, direction, new Vector2(0.36f, -0.12f));
        int targetCellHeight = -1;

        for (int cellHeight = 1; cellHeight <= maxJumpCells; cellHeight++)
        {
            Vector2 clearanceProbe = baseProbe + Vector2.up * cellHeight;
            if (IsSpaceClear(clearanceProbe))
            {
                targetCellHeight = cellHeight;
                break;
            }
        }

        if (targetCellHeight < 0)
        {
            return false;
        }

        float targetRise = targetCellHeight;
        launchForce = Mathf.Max(launchForce, CalculateJumpForce(targetRise));
        rb.velocity = new Vector2(direction * moveSpeed, launchForce);
        spriteRenderer.flipX = movingRight;
        lastJumpTime = Time.time;
        return true;
    }

    bool IsSpaceClear(Vector2 position)
    {
        LayerMask mask = ResolveTraversalMask();
        Collider2D[] hits = Physics2D.OverlapBoxAll(position, new Vector2(0.18f, 0.18f), 0f, mask);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (IsTraversalCollider(hit))
            {
                return false;
            }
        }

        return true;
    }

    float CalculateJumpForce(float targetRise)
    {
        if (rb == null)
        {
            return jumpForce;
        }

        float gravityMagnitude = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        if (gravityMagnitude <= 0.001f)
        {
            return jumpForce;
        }

        float requiredForce = Mathf.Sqrt(2f * gravityMagnitude * Mathf.Max(0.1f, targetRise + 0.08f));
        return Mathf.Max(jumpForce, requiredForce);
    }

    bool IsGrounded()
    {
        if (bodyCollider == null)
        {
            return false;
        }

        Vector2 origin = GetProbeWorldPosition(groundProbe, 1f, new Vector2(0f, -0.34f));
        return CastForGround(origin, Vector2.down, groundCheckDistance);
    }

    bool TryGetGroundedTrampoline(out Trampoline trampoline)
    {
        trampoline = null;
        if (bodyCollider == null)
        {
            return false;
        }

        RaycastHit2D hit = Physics2D.Raycast(
            GetProbeWorldPosition(groundProbe, 1f, new Vector2(0f, -0.34f)),
            Vector2.down,
            groundCheckDistance,
            ResolveGroundMask()
        );

        if (hit.collider == null)
        {
            return false;
        }

        trampoline = hit.collider.GetComponentInParent<Trampoline>();
        return trampoline != null;
    }

    Vector2 GetProbeWorldPosition(Transform probe, float direction, Vector2 fallbackLocalPosition)
    {
        Vector3 localPosition = probe != null ? probe.localPosition : (Vector3)fallbackLocalPosition;
        if (direction < 0f)
        {
            localPosition.x = -localPosition.x;
        }

        return transform.TransformPoint(localPosition);
    }
}
