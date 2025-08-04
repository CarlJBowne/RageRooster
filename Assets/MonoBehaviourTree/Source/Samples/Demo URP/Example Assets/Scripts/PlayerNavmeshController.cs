using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MBTExample
{
    public class PlayerNavmeshController : MonoBehaviour
    {
        public Camera cam;
        public NavMeshAgent agent;

        void Reset()
        {
            cam = Camera.main;
            agent = GetComponent<NavMeshAgent>();
        }

        // Update is called once per frame
        void Update()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(UnityEngine.Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    agent.SetDestination(hit.point);
                }
            }
        }
    }
}
