using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UltimateReplay.Demo
{
    public class SimpleProjectile : MonoBehaviour
    {
        // Private
        private Rigidbody body = null;
        private GameObject owner = null;

        // Public
        public float speed = 10;

        // Methods
        public void Shoot(GameObject owner, Vector3 direction)
        {
            this.owner = owner;
            body.velocity = direction * speed;            
        }

        public void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void OnCollisionEnter(Collision collision)
        {
            SimpleDamage damage = collision.collider.GetComponent<SimpleDamage>();

            if(damage != null)
            {
                damage.TakeDamage(0.6f, owner);
            }
        }
    }
}
