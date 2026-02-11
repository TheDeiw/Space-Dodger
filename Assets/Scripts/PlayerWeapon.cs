using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    bool isFiring = false;
    [SerializeField] GameObject laser;

    void Start()
    {
        //Cursor.visible = false;
    }

    void Update()
    {
        FiringProcess();
    }

    void FiringProcess()
    {
        var particleEmission = laser.GetComponent<ParticleSystem>().emission;
        particleEmission.enabled = isFiring;
    }

    void OnAttack(InputValue value)
    {
        isFiring = value.isPressed;
    }
    
}
