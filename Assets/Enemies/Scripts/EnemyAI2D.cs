using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyAI2D : MonoBehaviour, IRunResettable
{
    private enum EnemyState
    {
        Patrol,
        Chase
    }

    [Header("Patrol")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolStopDistance = 0.1f;

    [Header("Patrol Safety")]
    [SerializeField] private bool preventPatrolFallingOffLedges = true;
    [SerializeField] private float ledgeCheckAhead = 0.55f;
    [SerializeField] private float ledgeCheckDownDistance = 2f;
    [SerializeField] private float ledgeCheckOriginYOffset = -0.25f;

    [SerializeField] private bool avoidOtherEnemies = false;
    [SerializeField] private float enemyAvoidRadius = 0.35f;
    [SerializeField] private float enemyAvoidAhead = 0.4f;
    [SerializeField] private float enemyAvoidVerticalTolerance = 0.35f;
    [SerializeField] private float enemyAvoidDecisionCooldown = 0.2f;

    [Header("Detection")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 12f;
    [SerializeField] private float loseTargetRange = 16f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool fallbackToDistanceIfLayerMaskMisconfigured = true;
    [SerializeField] private float minDetectionRange = 12f;
    [SerializeField] private float minLoseTargetRange = 16f;
    [Header("Aggro LOS (platform blocking)")]
    [SerializeField] private bool requireLineOfSightForAggro = false;
    [SerializeField] private float minHorizontalDistanceToConsiderLOS = 0.35f;
    [SerializeField] private float losEndpointEpsilon = 0.05f;
    [SerializeField] private float aggroSameLayerVerticalTolerance = 0.3f;
    [SerializeField] private float aggroHorizontalLOSYOffset = 0.15f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3f;

    [Header("Chase Feel")]
    [SerializeField] private bool resumePatrolIfPlayerStationary = false;
    [SerializeField] private float chaseAlignmentDistanceToPatrol = 0.25f;
    [SerializeField] private float playerStationaryVelocityThresholdX = 0.05f;

    [SerializeField] private bool chaseContinueEvenIfAligned = true;
    [SerializeField] private float chaseAlignedDeadzoneX = 0.001f;
    [SerializeField] private float chaseSearchIfVerticalDiffAtLeast = 0.6f;

    [Header("Contact Damage")]
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float contactDamageCooldown = 0.5f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private Collider2D myCollider;
    private Vector2 currentPatrolTarget;
    private EnemyState state;
    private float nextDamageTime;
    private float nextPlayerAcquireTime;
    private Vector3 initialPosition;
    private float lastChaseDirectionX = 1f;
    private float nextEnemyAvoidDecisionTime;
    private bool swappedPatrolTargetThisFixedUpdate;
    private bool ignoredEnemyEnemyCollisionPairsThisEnable;
    private static bool s_enemyLayerCollisionIgnored;

    [Header("Player Reference")]
    [SerializeField] private float playerAcquireRetrySeconds = 0.5f;

    private int ObstacleMask
    {
        get
        {
            int mask = 0;
            int ground = LayerMask.NameToLayer("Ground");
            if (ground >= 0) mask |= (1 << ground);

            int platform = LayerMask.NameToLayer("Platform");
            if (platform >= 0) mask |= (1 << platform);

            return mask;
        }
    }

    // Called by the level spawner to scale enemy difficulty based on distance.
    public void ApplyDifficulty(float speedMultiplier, float damageMultiplier)
    {
        ApplyDifficulty(speedMultiplier, damageMultiplier, 1f);
    }

    // Overload for also scaling aggro/detection feel with distance.
    public void ApplyDifficulty(float speedMultiplier, float damageMultiplier, float detectionRangeMultiplier)
    {
        speedMultiplier = Mathf.Max(0.01f, speedMultiplier);
        damageMultiplier = Mathf.Max(0.01f, damageMultiplier);
        detectionRangeMultiplier = Mathf.Max(0.01f, detectionRangeMultiplier);

        patrolSpeed *= speedMultiplier;
        chaseSpeed *= speedMultiplier;
        contactDamage *= damageMultiplier;

        detectionRange *= detectionRangeMultiplier;

        // Keep loseTargetRange meaningfully larger than detectionRange.
        float loseMult = Mathf.Lerp(1.1f, 1.6f, Mathf.Clamp01((detectionRangeMultiplier - 1f) / 1.5f));
        loseTargetRange *= loseMult;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        initialPosition = rb.position;
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (player == null)
        {
            PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                player = playerStats.transform;
            }
        }
    }

    private void Start()
    {
        state = EnemyState.Patrol;
        currentPatrolTarget = GetInitialPatrolTarget();

        // Ensure prefab-serialized older values can't make aggro feel inconsistent.
        detectionRange = Mathf.Max(minDetectionRange, detectionRange);
        loseTargetRange = Mathf.Max(minLoseTargetRange, loseTargetRange);
        EnsureEnemyPassThrough();
    }

    private void OnEnable()
    {
        // New enemies can spawn during gameplay; we must re-run pass-through setup
        // for each instance, not only once globally.
        ignoredEnemyEnemyCollisionPairsThisEnable = false;
        EnsureEnemyPassThrough();
    }

    private void EnsureEnemyPassThrough()
    {
        if (ignoredEnemyEnemyCollisionPairsThisEnable) return;
        ignoredEnemyEnemyCollisionPairsThisEnable = true;

        // Prefer the Physics2D layer collision matrix when possible (fast + global).
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            gameObject.layer = enemyLayer;
            if (!s_enemyLayerCollisionIgnored)
            {
                s_enemyLayerCollisionIgnored = true;
                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
            }
            return;
        }

        // Fallback: ignore collisions against all existing enemies.
        // Use *all* collider pairs (root + children) to avoid cases where a child collider
        // is the one actually blocking movement.
        IgnoreEnemyEnemyCollisionsWithExisting();
    }

    private void IgnoreEnemyEnemyCollisionsWithExisting()
    {
        EnemyAI2D[] all = FindObjectsByType<EnemyAI2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            EnemyAI2D other = all[i];
            if (other == null || other == this) continue;
            IgnoreCollisionWithEnemy(other);
        }
    }

    private void FixedUpdate()
    {
        // Some enemies can fall asleep after being idle for a while.
        // If they're meant to patrol/chase, force wake so movement stays consistent.
        if (rb != null && rb.IsSleeping())
        {
            rb.WakeUp();
        }

        UpdateState();

        if (state == EnemyState.Chase && player != null)
        {
            MoveTowardsHoriz(player.position, chaseSpeed);
        }
        else
        {
            Patrol();
        }
    }

    private void UpdateState()
    {
        if (CanDetectPlayer())
        {
            if (resumePatrolIfPlayerStationary && IsPlayerStationaryX() && Mathf.Abs(transform.position.x - player.position.x) <= chaseAlignmentDistanceToPatrol)
            {
                state = EnemyState.Patrol;
                return;
            }

            state = EnemyState.Chase;
            return;
        }

        if (state == EnemyState.Chase && player != null)
        {
            float horizontalDistanceToPlayer = Mathf.Abs(transform.position.x - player.position.x);
            if (horizontalDistanceToPlayer <= loseTargetRange)
            {
                if (!requireLineOfSightForAggro || HasLineOfSightToPlayer())
                {
                    return;
                }
            }
        }

        state = EnemyState.Patrol;
    }

    // Used by other enemy components (e.g. shooters) to decide if they should act.
    public bool IsPlayerDetected()
    {
        return CanDetectPlayer();
    }

    private bool CanDetectPlayer()
    {
        AcquirePlayerIfNeeded();

        if (player == null)
        {
            return false;
        }

        // Use horizontal distance for “aggro radius” so being above/below
        // doesn't break detection due to 2D distance including Y.
        float horizontalDistance = Mathf.Abs(transform.position.x - player.position.x);
        if (horizontalDistance > detectionRange)
        {
            return false;
        }

        // Optional layer filtering: we only “allow” detection if the player layer
        // is in the mask (unless we allow the fallback).
        bool playerInMask;
        if (playerLayer.value == 0)
        {
            playerInMask = true;
        }
        else
        {
            int playerBit = 1 << player.gameObject.layer;
            playerInMask = (playerLayer.value & playerBit) != 0;
        }

        // If the mask is misconfigured, enemies would look like "aggro isn't working".
        // Fallback keeps gameplay robust.
        if (!playerInMask)
        {
            if (!fallbackToDistanceIfLayerMaskMisconfigured) return false;
        }

        if (!requireLineOfSightForAggro) return true;

        // If they're extremely close horizontally, treat LOS as clear to avoid
        // “ignore until bump” edge cases on platform edges.
        if (horizontalDistance <= minHorizontalDistanceToConsiderLOS) return true;

        return HasLineOfSightToPlayer();
    }

    private bool IsPlayerStationaryX()
    {
        if (player == null) return false;
        Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
        if (prb == null) return false;
        return Mathf.Abs(prb.linearVelocity.x) <= playerStationaryVelocityThresholdX;
    }

    private void AcquirePlayerIfNeeded()
    {
        if (player != null) return;
        if (Time.time < nextPlayerAcquireTime) return;

        nextPlayerAcquireTime = Time.time + Mathf.Max(0.01f, playerAcquireRetrySeconds);
        PlayerStats ps = FindAnyObjectByType<PlayerStats>();
        if (ps != null) player = ps.transform;
    }

    public bool HasLineOfSightToPlayer()
    {
        int mask = ObstacleMask;
        if (mask == 0)
        {
            // If we can't identify obstacles, fall back to aggro rules only.
            return true;
        }

        float verticalDiff = Mathf.Abs(transform.position.y - player.position.y);

        // If they are on roughly the same “layer height”, do an *horizontal* LOS check.
        // This avoids false negatives from diagonal linecasts that clip platform colliders
        // at corners even when both are on the same platform strip.
        if (verticalDiff <= aggroSameLayerVerticalTolerance)
        {
            Vector2 start = new Vector2(transform.position.x, transform.position.y + aggroHorizontalLOSYOffset);
            Vector2 end = new Vector2(player.position.x, transform.position.y + aggroHorizontalLOSYOffset);

            Vector2 delta = end - start;
            float dist = delta.magnitude;
            if (dist <= 0.001f) return true;

            Vector2 dir = delta / dist;
            start += dir * losEndpointEpsilon;
            end -= dir * losEndpointEpsilon;

            RaycastHit2D hit = Physics2D.Linecast(start, end, mask);
            return hit.collider == null;
        }

        // Otherwise use diagonal LOS.
        Vector2 diagStart = transform.position;
        Vector2 diagEnd = player.position;

        Vector2 diagDelta = diagEnd - diagStart;
        float diagDist = diagDelta.magnitude;
        if (diagDist <= 0.001f) return true;

        Vector2 diagDir = diagDelta / diagDist;
        diagStart += diagDir * losEndpointEpsilon;
        diagEnd -= diagDir * losEndpointEpsilon;

        RaycastHit2D diagHit = Physics2D.Linecast(diagStart, diagEnd, mask);
        return diagHit.collider == null;
    }

    private void Patrol()
    {
        if (patrolPointA == null || patrolPointB == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        swappedPatrolTargetThisFixedUpdate = false;

        // Patrol movement should NOT actively control Y.
        // Otherwise enemies can “float” / get stuck mid-air when they drop a layer.
        // Let gravity + collisions handle falling/jumping naturally.
        MoveTowardsHoriz(currentPatrolTarget, patrolSpeed, isChasing: false);

        // Only swap when close on X.
        // If the enemy fell to another layer due to aggro, it will continue patrolling on X at that Y.
        float closeX = Mathf.Abs(transform.position.x - currentPatrolTarget.x);
        if (!swappedPatrolTargetThisFixedUpdate && closeX <= patrolStopDistance)
        {
            currentPatrolTarget = IsNear(patrolPointA.position, currentPatrolTarget)
                ? patrolPointB.position
                : patrolPointA.position;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider == null) return;

        // Prevent enemy-enemy physics blocking. This lets them pass through each other
        // instead of “detecting but not moving”.
        EnemyAI2D otherAI = collision.collider.GetComponentInParent<EnemyAI2D>();
        if (otherAI == null || otherAI == this) return;

        IgnoreCollisionWithEnemy(otherAI);
    }

    private void IgnoreCollisionWithEnemy(EnemyAI2D other)
    {
        // Ignore all our colliders against all of theirs.
        Collider2D[] mine = GetComponentsInChildren<Collider2D>(true);
        Collider2D[] theirs = other.GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < mine.Length; i++)
        {
            Collider2D a = mine[i];
            if (a == null) continue;

            for (int j = 0; j < theirs.Length; j++)
            {
                Collider2D b = theirs[j];
                if (b == null) continue;
                Physics2D.IgnoreCollision(a, b, true);
            }
        }
    }

    private void MoveTowardsHoriz(Vector2 targetPosition, float speed)
    {
        MoveTowardsHoriz(targetPosition, speed, isChasing: true);
    }

    private void MoveTowardsHoriz(Vector2 targetPosition, float speed, bool isChasing)
    {
        float dx = targetPosition.x - transform.position.x;
        float deadzoneX = isChasing ? chaseAlignedDeadzoneX : 0.02f;
        float directionX = Mathf.Abs(dx) < deadzoneX ? 0f : Mathf.Sign(dx);

        // If we're aligned to the player, don't instantly stop the enemy's movement
        // (reduces “mimic player when they stop” feel).
        if (isChasing && directionX == 0f && chaseContinueEvenIfAligned)
        {
            // If the player is directly above/below us (dx≈0), don't freeze in place.
            // Keep moving in the last chase direction to “search” for a path/edge.
            float verticalDiff = player != null ? Mathf.Abs(transform.position.y - player.position.y) : 0f;
            if (verticalDiff >= chaseSearchIfVerticalDiffAtLeast)
            {
                directionX = Mathf.Abs(lastChaseDirectionX) < 0.01f ? 1f : Mathf.Sign(lastChaseDirectionX);
            }
        }

        if (directionX != 0f) lastChaseDirectionX = directionX;

        if (!isChasing)
        {
            // Prevent patrol from pushing enemies off ledges.
            if (preventPatrolFallingOffLedges && directionX != 0f && !HasGroundAhead(directionX))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                SwapPatrolTargetX();
                swappedPatrolTargetThisFixedUpdate = true;
                UpdateFacing(-directionX);
                return;
            }

            // Keep enemies from getting jammed by each other.
            if (avoidOtherEnemies && directionX != 0f && Time.time >= nextEnemyAvoidDecisionTime && IsEnemyBlockingAhead(directionX))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                SwapPatrolTargetX();
                swappedPatrolTargetThisFixedUpdate = true;
                UpdateFacing(-directionX);
                nextEnemyAvoidDecisionTime = Time.time + enemyAvoidDecisionCooldown;
                return;
            }
        }
        else
        {
            // In chase, we still allow blocking avoidance, but we don't swap patrol targets.
            if (avoidOtherEnemies && directionX != 0f && IsEnemyBlockingAhead(directionX))
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                UpdateFacing(directionX);
                return;
            }
        }

        rb.linearVelocity = new Vector2(directionX * speed, rb.linearVelocity.y);
        UpdateFacing(directionX);
    }

    private void SwapPatrolTargetX()
    {
        if (patrolPointA == null || patrolPointB == null) return;

        float distToA = Mathf.Abs(currentPatrolTarget.x - patrolPointA.position.x);
        float distToB = Mathf.Abs(currentPatrolTarget.x - patrolPointB.position.x);
        bool currentlyCloserToA = distToA <= distToB;
        currentPatrolTarget = currentlyCloserToA ? (Vector2)patrolPointB.position : (Vector2)patrolPointA.position;
    }

    private bool HasGroundAhead(float directionX)
    {
        // If we can't identify ground/platform blockers, don't prevent movement.
        int mask = ObstacleMask;
        if (mask == 0) return true;

        Vector2 origin = (Vector2)transform.position + new Vector2(directionX * ledgeCheckAhead, ledgeCheckOriginYOffset);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, ledgeCheckDownDistance, mask);
        return hit.collider != null;
    }

    private bool IsEnemyBlockingAhead(float directionX)
    {
        // Avoid expensive raycasts; a small overlap check is enough because we only move on X.
        Vector2 center = (Vector2)transform.position + new Vector2(directionX * enemyAvoidAhead, 0f);
        Collider2D[] cols = Physics2D.OverlapCircleAll(center, enemyAvoidRadius);
        for (int i = 0; i < cols.Length; i++)
        {
            Collider2D c = cols[i];
            if (c == null) continue;
            EnemyAI2D other = c.GetComponentInParent<EnemyAI2D>();
            if (other == null || other == this) continue;

            if (Mathf.Abs(other.transform.position.y - transform.position.y) <= enemyAvoidVerticalTolerance)
            {
                return true;
            }
        }

        return false;
    }

    private void MoveTowardsAxis(Vector2 targetPosition, float speed)
    {
        // Legacy method: patrol ignores Y-axis entirely to avoid “floating” mid-air.
        MoveTowardsHoriz(targetPosition, speed, isChasing: false);
    }

    private void UpdateFacing(float direction)
    {
        if (spriteRenderer == null || Mathf.Abs(direction) < 0.01f)
        {
            return;
        }

        spriteRenderer.flipX = direction < 0f;
    }

    private Vector2 GetInitialPatrolTarget()
    {
        if (patrolPointA == null || patrolPointB == null)
        {
            return transform.position;
        }

        // Choose based on X so vertical differences (falling layers) don't cause inconsistent target selection.
        float distanceToA = Mathf.Abs(transform.position.x - patrolPointA.position.x);
        float distanceToB = Mathf.Abs(transform.position.x - patrolPointB.position.x);
        return distanceToA <= distanceToB ? patrolPointB.position : patrolPointA.position;
    }

    private static bool IsNear(Vector2 a, Vector2 b)
    {
        return Vector2.Distance(a, b) <= 0.05f;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < nextDamageTime)
        {
            return;
        }

        PlayerStats hitPlayer = collision.collider.GetComponentInParent<PlayerStats>();
        if (hitPlayer == null)
        {
            return;
        }

        hitPlayer.TakeDamage(contactDamage);
        nextDamageTime = Time.time + contactDamageCooldown;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }

    public void ResetForNewRun()
    {
        // Reset enemy position every run so they don't “drift” between sessions.
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = initialPosition;
        }
        else
        {
            transform.position = initialPosition;
        }

        state = EnemyState.Patrol;
        nextDamageTime = 0f;
        nextPlayerAcquireTime = 0f;
        currentPatrolTarget = GetInitialPatrolTarget();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }
    }
}
