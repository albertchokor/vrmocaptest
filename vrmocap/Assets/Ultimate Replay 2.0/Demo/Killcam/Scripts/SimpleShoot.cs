using UnityEngine;
using UnityEngine.UI;

namespace UltimateReplay.Demo
{
    /// <summary>
    /// A simple shooting script used in the demo scene.
    /// </summary>
    public class SimpleShoot : MonoBehaviour
    {
        // Private
        private Camera activeCam = null;
        private int currentClip = 0;
        private int currentAmmo = 0;

        // Public
        /// <summary>
        /// The layermask to use for all shooting raycasts.
        /// </summary>
        public LayerMask raycastMask = 1;
        /// <summary>
        /// The maximum amount of ammo.
        /// </summary>
        public int maxAmmoCount = 128;
        /// <summary>
        /// The maximum amount of ammo per clip.
        /// </summary>
        public int maxClipCount = 32;
        /// <summary>
        /// The amount of damage that will be applied to hit items.
        /// </summary>
        public float damageAmount = 0.6f;

        /// <summary>
        /// A UI reload hint that will be displayed when out of ammo.
        /// </summary>
        public GameObject reloadHint;
        /// <summary>
        /// A UI enemy counter to display the number of enemies remaining.
        /// </summary>
        public Text enemyHint;
        /// <summary>
        /// A UI ammot counter to display the amount of ammo remaining.
        /// </summary>
        public Text ammoHint;

        public SimpleProjectile bulletPrefab;

        /// <summary>
        /// The audio source for all shooting sound effects.
        /// </summary>
        [Header("SFX")]        
        public AudioSource source;
        /// <summary>
        /// The sound to play when the weapon is fired.
        /// </summary>
        public AudioClip shootClip;
        /// <summary>
        /// The sound to play when the weapon is fired but the clip is empty.
        /// </summary>
        public AudioClip emptyClip;
        /// <summary>
        /// The sound to play when the weapon is reloaded.
        /// </summary>
        public AudioClip reloadClip;

        /// <summary>
        /// Cheat to allow the ammo supply to never run out.
        /// </summary>
        [Header("Cheats")]
        public bool infiniteAmmo = false;

        public ParticleSystem muzzleFlash;

        public Transform bulletSpawn;

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Start()
        {
            activeCam = Component.FindObjectOfType<Camera>();

            currentAmmo = maxAmmoCount;
            currentClip = maxClipCount;

            // Update hud values
            HideReloadHint();
            UpdateAmmoHint();
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Update()
        {
            // Check for shoot button
            if(Input.GetButtonDown("Fire1") == true)
            {
                Shoot();
            }

            if(Input.GetButtonDown("Submit") == true)
            {
                Reload();
            }

        }

        /// <summary>
        /// Shoot the weapon.
        /// </summary>
        public void Shoot()
        {
            // Check for no ammo
            if(currentClip <= 0)
            {
                source.PlayOneShot(emptyClip);
                return;
            }
            
            // Decrease ammo
            currentClip--;

            // Fire raycast
            //Ray ray = activeCam.ScreenPointToRay(new Vector2(Screen.width / 2, Screen.height / 2));

            //RaycastHit hit;
            //if(Physics.Raycast(ray, out hit, 100, 1, QueryTriggerInteraction.Ignore) == true)
            //{
            //    Collider other = hit.collider;

            //    // Apply damage
            //    if (other.GetComponent<SimpleDamage>() != null)
            //        other.GetComponent<SimpleDamage>().TakeDamage(damageAmount);
            //}

            Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity).Shoot(gameObject, bulletSpawn.forward);

            muzzleFlash.Play();

            // Play sound effect
            source.PlayOneShot(shootClip);

            // Update ammo
            UpdateAmmoHint();

            if (currentAmmo > 0 && currentClip <= 0)
            {
                // Update hud
                ShowReloadHint();
            }
        }

        /// <summary>
        /// Reload the weapon.
        /// </summary>
        public void Reload()
        {
            // Hide hint
            HideReloadHint();

            // Check for no ammo
            if(currentAmmo <= 0)
                return;

            // Check for no reload needed
            if (currentClip >= maxClipCount)
                return;

            // Reload ammo
            int amountRequired = maxClipCount - currentClip;

            // Check for not enough for full reload
            if (currentAmmo < amountRequired)
                amountRequired = currentAmmo;

            currentClip += amountRequired;
            currentAmmo -= amountRequired;

            // Play sfx
            source.PlayOneShot(reloadClip);

            // Update hud
            UpdateAmmoHint();
        }

        /// <summary>
        /// Award maximum ammo.
        /// </summary>
        public void AwardMaxAmmo()
        {
            currentAmmo = maxAmmoCount;

            // Update hud
            UpdateAmmoHint();
        }

        /// <summary>
        /// Causes the reload UI hint to be displayed.
        /// </summary>
        public void ShowReloadHint()
        {
            if (reloadHint != null)
                reloadHint.SetActive(true);
        }

        /// <summary>
        /// Causes the reload UI hint to be hidden.
        /// </summary>
        public void HideReloadHint()
        {
            if (reloadHint != null)
                reloadHint.SetActive(false);
        }

        /// <summary>
        /// Causes the ammot UI text to be refreshed.
        /// </summary>
        public void UpdateAmmoHint()
        {
            if (ammoHint != null)
                ammoHint.text = string.Format("{0}/{1}", currentClip, currentAmmo);
        }
    }
}
