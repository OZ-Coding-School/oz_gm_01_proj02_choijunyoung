using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayerInputs : MonoBehaviour
{
    public bool aim;
    public bool attack;

    public void OnAim(InputValue value)
    {
        SetAim(value.isPressed);
    }
    public void OnShoot(InputValue value)
    {
        SetShoot(value.isPressed);
    }

    public void SetAim(bool newAim)
    {
        aim = newAim;
    }
    public void SetShoot(bool newShoot)
    {
        attack = newShoot;
    }
}