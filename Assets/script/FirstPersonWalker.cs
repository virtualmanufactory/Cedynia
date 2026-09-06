using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonWalker : MonoBehaviour
{
    public float walkSpeed = 3.6f;
    public float runSpeed = 6.4f;
    public float lookSensitivity = 2.1f;
    public float gravity = 22f;
    public float jumpSpeed = 5.2f;
    public float maxPitch = 85f;

    CharacterController controller;
    Transform eyes;
    float pitch;
    float verticalVelocity;
    Vector3 spawnPoint;
    bool showHelp = true;
    float helpUntil;

    public void BindEyes(Transform cameraTransform)
    {
        eyes = cameraTransform;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        spawnPoint = transform.position;
        helpUntil = Time.time + 10f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        if (Cursor.lockState == CursorLockMode.Locked && eyes != null)
        {
            float mx = Input.GetAxis("Mouse X") * lookSensitivity;
            float my = Input.GetAxis("Mouse Y") * lookSensitivity;
            transform.Rotate(0f, mx, 0f);
            pitch = Mathf.Clamp(pitch - my, -maxPitch, maxPitch);
            eyes.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Vector3 input = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 move = transform.TransformDirection(input) * speed;

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
            if (Input.GetButtonDown("Jump"))
                verticalVelocity = jumpSpeed;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);

        if (transform.position.y < -25f)
        {
            controller.enabled = false;
            transform.position = spawnPoint;
            verticalVelocity = 0f;
            controller.enabled = true;
        }

        if (showHelp && Time.time > helpUntil)
            showHelp = false;
    }

    public void RememberSpawn(Vector3 point)
    {
        spawnPoint = point;
    }

    void OnGUI()
    {
        if (!showHelp)
            return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(18, 16, 640, 70),
            "WASD — chodzenie    Mysz — rozglądanie\nShift — bieg    Spacja — skok    Esc — kursor",
            style);
    }
}
