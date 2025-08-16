using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float acceleration = 10f;
    public float rotationSpeed = 30f;

    private float currentSpeed;

    void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        // Only move/rotate when right mouse button is held
        if (mouse != null && mouse.rightButton.isPressed)
        {
            HandleRotation(mouse);
            HandleMovement(keyboard);
        }
    }

    void HandleRotation(Mouse mouse)
    {
        Vector2 mouseDelta = mouse.delta.ReadValue();
        transform.eulerAngles += new Vector3(-mouseDelta.y * rotationSpeed * Time.deltaTime, mouseDelta.x * rotationSpeed * Time.deltaTime, 0);
    }

    void HandleMovement(Keyboard keyboard)
    {
        // Determine speed
        currentSpeed = keyboard.leftShiftKey.isPressed ? acceleration : moveSpeed;

        // WASD movement
        float moveX = 0f;
        float moveZ = 0f;
        float moveY = 0f;
        if (keyboard.aKey.isPressed) moveX -= 1f;
        if (keyboard.dKey.isPressed) moveX += 1f;
        if (keyboard.wKey.isPressed) moveZ += 1f;
        if (keyboard.sKey.isPressed) moveZ -= 1f;

        // E/Q for height adjustment
        if (keyboard.eKey.isPressed) moveY += 1f;
        if (keyboard.qKey.isPressed) moveY -= 1f;

        Vector3 move = transform.right * moveX + transform.forward * moveZ + Vector3.up * moveY;

        // If space is held, restrict movement to XZ plane
        if (keyboard.spaceKey.isPressed)
        {
            move.y = 0;
        }

        transform.position += move.normalized * currentSpeed * Time.deltaTime;
    }
}