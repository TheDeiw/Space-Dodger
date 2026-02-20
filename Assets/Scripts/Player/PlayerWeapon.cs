using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] GameObject laser;
    [SerializeField] Transform spawnPoint;
    [SerializeField] float fireRate = 0.3f; // Prevents the AI from spamming lasers
    
    private float nextFireTime = 0f;

    public void FireWeapon()
    {
        if (Time.time >= nextFireTime)
        {
            GameObject newLaser = Instantiate(laser, spawnPoint.position, spawnPoint.rotation);
            newLaser.transform.SetParent(spawnPoint); 
            nextFireTime = Time.time + fireRate;
        }
    }
    
    void OnAttack(InputValue value)
    {
        FireWeapon();
    }
    
}
