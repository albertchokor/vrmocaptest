using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace UltimateReplay.Demo
{
    public class SimpleEnemy : MonoBehaviour
    {
        // Private
        private NavMeshAgent agent = null;
        private Animator anim = null;
        private float lastShootTime = 0;
        private Vector3 startPosition = Vector3.zero;
        private Quaternion startRotation = Quaternion.identity;

        // Public
        public Transform playerTarget;
        public Transform weaponRoot;
        public Collider mainCollider;
        public SimpleDamage damage;
        public SimpleRagdoll ragdoll;
        public float viewRange = 10;
        public float viewField = 0.6f;
        public float shootRange = 8;

        public ParticleSystem muzzleFlash;

        public SimpleProjectile bulletPrefab;
        public SimpleKillcamPlayback killcam;
        public Transform bulletSpawn;

        // Methods
        public void Start()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;

            agent = GetComponent<NavMeshAgent>();
            anim = GetComponent<Animator>();

            damage.OnKilled += OnKilled;
        }

        public void Update()
        {
            if (damage.IsDead == true)
                return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            Vector3 directionToPlayer = (playerTarget.position - transform.position);

            bool isInSight = false;

            // Check distance
            if (distanceToPlayer <= viewRange)
            {
                if (Vector3.Dot(transform.forward, directionToPlayer.normalized) > 0.6f)
                {
                    // Perform line of sight raycast
                    RaycastHit hit;
                    if (Physics.Raycast(new Ray(transform.position, directionToPlayer.normalized), out hit) == true)
                    {
                        // Check for matching hit object
                        if (hit.transform == playerTarget)
                            isInSight = true;
                    }
                }
            }



            // Check for not in sight and move to player
            if (isInSight == false)
            {
                agent.SetDestination(playerTarget.transform.position);
                agent.updateRotation = true;
            }

            // Update animator
            anim.SetBool("PlayerInSight", isInSight);
            anim.SetFloat("Speed", agent.velocity.magnitude);
                       
            // Aim at player
            if(distanceToPlayer < shootRange)
            {
                // Set the current destination to match the current position
                agent.SetDestination(transform.position);

                if(Vector3.Dot(transform.forward, directionToPlayer.normalized) > 0.98f)
                {
                    SimpleDamage playerDamage = playerTarget.GetComponent<SimpleDamage>();

                    if (playerDamage.IsDead == false && Time.time > lastShootTime + 0.7f)
                    {
                        Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity).Shoot(gameObject, bulletSpawn.forward);

                        // Shoot the player
                        //playerDamage.TakeDamage(0.6f, gameObject);

                        // Play muzzle flash
                        muzzleFlash.Play();

                        // Reset shoot timer
                        lastShootTime = Time.time;
                    }
                }
                else
                {
                    Vector3 lookDirection = directionToPlayer;
                    lookDirection.y = 0;

                    Quaternion look = Quaternion.LookRotation(lookDirection);

                    agent.updateRotation = false;

                    float lookRotationSpeed = agent.angularSpeed * Time.deltaTime * 0.2f;

                    if (lookRotationSpeed < 0.3f)
                        lookRotationSpeed = 0.3f;

                    if (distanceToPlayer < 4)
                        lookRotationSpeed = 1f;

                    transform.rotation = Quaternion.Slerp(transform.rotation, look, lookRotationSpeed);
                }
            }
        }

        private void OnKilled(GameObject damagingObject)
        {
            killcam.ResetKillcam();

            mainCollider.enabled = false;
            anim.enabled = false;
            agent.enabled = false;

            // Hide weapon
            weaponRoot.gameObject.SetActive(false);
            
            ragdoll.ActivateRagDoll();

            // Respawn enemy
            StartCoroutine(Respawn());
        }

        private IEnumerator Respawn()
        {
            yield return new WaitForSeconds(3f);

            // Reset character
            ragdoll.DeactivateRagdoll();

            mainCollider.enabled = true;
            anim.enabled = true;
            agent.enabled = true;

            weaponRoot.gameObject.SetActive(true);

            transform.position = startPosition;
            transform.rotation = startRotation;
            agent.Warp(startPosition);

            // Give back full health
            damage.RestoreHealth();
        }
    }
}
