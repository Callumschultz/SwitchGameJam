using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles the player's melee attack:
/// - listens for left mouse click using the new Input System
/// - plays the Attack animation via Animator bool "isAttacking"
/// - damages any enemies in range using an OverlapSphere at an AttackPoint transform
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Empty child transform positioned in front of the player. If left null, a fallback AttackPoint will be created.")]
    [SerializeField] private Transform attackPoint;

    [Header("Attack Timing")]
    [Tooltip("How long the animator bool stays true (used to drive the Attack -> Idle transition).")]
    [SerializeField] private float attackDuration = 0.5f;

    [Header("Hit Detection")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask enemyLayer = default;

    private Animator animator;
    private Coroutine attackRoutine;

    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");

    private void Awake()
    {
        animator = GetComponent<Animator>();

        // Default to the "Enemy" layer if it exists; otherwise the field will be empty.
        if (enemyLayer.value == 0)
        {
            int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
            if (enemyLayerIndex >= 0)
            {
                enemyLayer = 1 << enemyLayerIndex;
            }
        }

        // If AttackPoint wasn't assigned in the inspector, create one as a child.
        // The spec says "1 unit in front ... on the X axis", so we place it along local +X.
        if (attackPoint == null)
        {
            attackPoint = new GameObject("AttackPoint").transform;
            attackPoint.SetParent(transform, false);
            attackPoint.localPosition = new Vector3(1f, 0f, 0f);
        }
    }

    private void Update()
    {
        // Left mouse click (new Input System)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartAttack();
        }
    }

    private void TryStartAttack()
    {
        if (attackRoutine != null)
        {
            return; // Prevent overlapping attacks.
        }

        animator.SetBool(IsAttackingHash, true);
        DoAttackDamage();

        attackRoutine = StartCoroutine(AttackCooldownRoutine());
    }

    private IEnumerator AttackCooldownRoutine()
    {
        yield return new WaitForSeconds(attackDuration);
        animator.SetBool(IsAttackingHash, false);
        attackRoutine = null;
    }

    private void DoAttackDamage()
    {
        if (attackPoint == null)
        {
            return;
        }

        // Find any enemy colliders within the attack range.
        Collider[] hits = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        // Avoid double-damaging the same enemy (multiple colliders).
        HashSet<EnemyAI> damaged = new HashSet<EnemyAI>();

        foreach (Collider hit in hits)
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null || damaged.Contains(enemy))
            {
                continue;
            }

            damaged.Add(enemy);
            enemy.TakeDamage();
        }
    }
}

