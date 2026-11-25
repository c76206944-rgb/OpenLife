using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace OpenLife
{
    /// <summary>
    /// Optional bridge from the existing Sim AI to a prefab Animator.
    /// Add matching parameters to a Humanoid Animator Controller:
    /// Speed (float), IsMoving (bool), IsSleeping (bool).
    /// Missing parameters are safely ignored.
    /// </summary>
    public sealed class SimAnimationDriver : MonoBehaviour
    {
        [SerializeField] string speedParameter = "Speed";
        [SerializeField] string movingParameter = "IsMoving";
        [SerializeField] string sleepingParameter = "IsSleeping";

        Animator animator;
        NavMeshAgent agent;
        SimCharacter sim;
        int speedHash, movingHash, sleepingHash;
        bool hasSpeed, hasMoving, hasSleeping;

        public void Init(Transform visual, NavMeshAgent navAgent, SimCharacter character)
        {
            agent = navAgent;
            sim = character;
            animator = visual != null ? visual.GetComponentInChildren<Animator>() : null;
            if (animator == null) return;

            animator.applyRootMotion = false;
            speedHash = Animator.StringToHash(speedParameter);
            movingHash = Animator.StringToHash(movingParameter);
            sleepingHash = Animator.StringToHash(sleepingParameter);

            var parameters = new HashSet<int>();
            foreach (var p in animator.parameters) parameters.Add(p.nameHash);
            hasSpeed = parameters.Contains(speedHash);
            hasMoving = parameters.Contains(movingHash);
            hasSleeping = parameters.Contains(sleepingHash);
        }

        void Update()
        {
            if (animator == null || !animator.isActiveAndEnabled || sim == null) return;
            float speed = agent != null && agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
            bool moving = sim.Current == SimCharacter.State.Walking && speed > 0.08f;
            bool sleeping = sim.StatusText != null && sim.StatusText.ToLowerInvariant().Contains("sleep");

            if (hasSpeed) animator.SetFloat(speedHash, speed);
            if (hasMoving) animator.SetBool(movingHash, moving);
            if (hasSleeping) animator.SetBool(sleepingHash, sleeping);
        }
    }
}
