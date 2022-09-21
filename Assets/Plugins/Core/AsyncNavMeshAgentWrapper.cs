using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Core.Async.Wrappers
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class AsyncNavMeshAgentWrapper : AsyncUnityBehaviour
    {
        [SerializeField] NavMeshAgent navMeshAgent;
        public NavMeshAgent Main => navMeshAgent;

        private Vector3 cashedPoint;
        private bool requireUpdate = false;
        protected override bool RequireUpdate => requireUpdate;

        private void Reset()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        protected override void SyncUpdate()
        {
            navMeshAgent.SetDestination(cashedPoint);
            requireUpdate = false;
        }

        public void SetDestination(Vector3 target)
        {
            if ((cashedPoint == target) && requireUpdate)
                return;

            cashedPoint = target;
            requireUpdate = true;
        }
    }
}
