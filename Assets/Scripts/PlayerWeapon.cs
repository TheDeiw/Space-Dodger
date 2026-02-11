using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] GameObject laser;
    [SerializeField] Transform spawnPoint;
    

    // OnAttack records the input, but doesn't spawn yet
    void OnAttack(InputValue value)
    {
        GameObject newLaser = Instantiate(laser, spawnPoint.position, spawnPoint.rotation);
        newLaser.transform.SetParent(spawnPoint); 
    }
    
}
