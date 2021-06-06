using UnityEngine;

namespace UltimateReplay.Example
{
    public class CarFollowCam : MonoBehaviour
    {
        // Public
        public Transform target;
        public float height = 3;
        public float distance = 3;
        public float speed = 5;
        public float lookHeight = 1;

        // Methods
        public void Update()
        {
            Vector3 wantedPosition;
            //if (followBehind)
                wantedPosition = target.TransformPoint(0, height, -distance);
            //else
            //    wantedPosition = target.TransformPoint(0, height, distance);

            transform.position = Vector3.Lerp(transform.position, wantedPosition, Time.deltaTime * speed);

            //if (smoothRotation)
            //{
            //    Quaternion wantedRotation = Quaternion.LookRotation(target.position - transform.position, target.up);
            //    transform.rotation = Quaternion.Slerp(transform.rotation, wantedRotation, Time.deltaTime * rotationDamping);
            //}
            //else 
                transform.LookAt(target, target.up);
        }
    }
}
