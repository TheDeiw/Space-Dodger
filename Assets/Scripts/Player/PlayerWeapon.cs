using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] GameObject laser;
    [SerializeField] Transform spawnPoint;
    [SerializeField] float fireRate = 0.3f; // Prevents the AI from spamming lasers
    [SerializeField] private SpaceshipAgent localAgent;
    
    private float nextFireTime = 0f;

    public void FireWeapon()
    {
        if (Time.time >= nextFireTime)
        {
            GameObject newLaser = Instantiate(laser, spawnPoint.position, spawnPoint.rotation);
            newLaser.transform.SetParent(spawnPoint); 
            
            // NEW: Give the laser the reference to this specific ship's agent
            CollisionHandler handler = newLaser.GetComponent<CollisionHandler>();
            if (handler != null)
            {
                handler.SetShooter(localAgent);
            }
            
            nextFireTime = Time.time + fireRate;
        }
    }
    
    void OnAttack(InputValue value)
    {
        FireWeapon();
    }
    
}
