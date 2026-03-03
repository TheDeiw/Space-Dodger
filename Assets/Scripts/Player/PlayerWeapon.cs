using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [SerializeField] GameObject laser;
    [SerializeField] Transform spawnPoint;
    [SerializeField] float fireRate = 0.3f;

    private float nextFireTime = 0f;
    private bool _isDisabled = false;

    /// <summary>
    /// Disables the weapon entirely (used during training).
    /// </summary>
    public void SetDisabled(bool disabled)
    {
        _isDisabled = disabled;
    }

    public void FireWeapon()
    {
        if (_isDisabled) return;

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
