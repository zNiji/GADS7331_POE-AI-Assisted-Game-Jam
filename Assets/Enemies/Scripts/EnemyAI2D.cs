using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyAI2D : MonoBehaviour
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

    [Header("Detection")]
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 4f;
    [SerializeField] private float loseTargetRange = 6f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private bool fallbackToDistanceIfLayerMaskMisconfigured = true;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3f;

    [Header("Contact Damage")]
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float contactDamageCooldown = 0.5f;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private Vector2 currentPatrolTarget;
    private EnemyState state;
    private float nextDamageTime;

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
    }

    private void FixedUpdate()
    {
        UpdateState();

        if (state == EnemyState.Chase && player != null)
        {
            MoveTowards(player.position, chaseSpeed);
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
            state = EnemyState.Chase;
            return;
        }

        if (state == EnemyState.Chase && player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= loseTargetRange)
            {
                return;
            }
        }

        state = EnemyState.Patrol;
    }

    private bool CanDetectPlayer()
    {
        if (player == null)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > detectionRange)
        {
            return false;
        }

        // Optional layer filtering; if no layer is selected, distance check is enough.
        if (playerLayer.value == 0)
        {
            return true;
        }
        int playerBit = 1 << player.gameObject.layer;
        bool included = (playerLayer.value & playerBit) != 0;
        if (included) return true;

        // If the mask is misconfigured, enemies would look like "aggro isn't working".
        // Fallback keeps gameplay robust.
        return fallbackToDistanceIfLayerMaskMisconfigured;
    }

    private void Patrol()
    {
        if (patrolPointA == null || patrolPointB == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        MoveTowards(currentPatrolTarget, patrolSpeed);

        float distanceToTarget = Vector2.Distance(transform.position, currentPatrolTarget);
        if (distanceToTarget <= patrolStopDistance)
        {
            currentPatrolTarget = IsNear(patrolPointA.position, currentPatrolTarget)
                ? patrolPointB.position
                : patrolPointA.position;
        }
    }

    private void MoveTowards(Vector2 targetPosition, float speed)
    {
        float direction = Mathf.Sign(targetPosition.x - transform.position.x);
        if (Mathf.Abs(targetPosition.x - transform.position.x) < 0.02f)
        {
            direction = 0f;
        }

        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);
        UpdateFacing(direction);
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

        float distanceToA = Vector2.Distance(transform.position, patrolPointA.position);
        float distanceToB = Vector2.Distance(transform.position, patrolPointB.position);
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
}
