using UnityEngine;

public struct FirstPersonInputFrame
{
    public Vector2 Move;
    public Vector2 Look;
    public bool JumpPressed;
    public bool SprintHeld;
}

public interface IFirstPersonInputSource
{
    FirstPersonInputFrame ReadInput();
}
