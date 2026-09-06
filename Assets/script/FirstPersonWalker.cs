using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonWalker : MonoBehaviour
{
    public float walkSpeed = 3.6f;
    public float runSpeed = 6.4f;
    public float flySpeed = 12f;
    public float flyFastSpeed = 28f;
    public float lookSensitivity = 2.1f;
    public float gravity = 22f;
    public float jumpSpeed = 5.2f;
    public float maxPitch = 89f;

    CharacterController controller;
    Transform eyes;
    float pitch;
    float verticalVelocity;
    Vector3 spawnPoint;
    bool flying;
    float helpUntil;

    public void BindEyes(Transform cameraTransform)
    {
        eyes = cameraTransform;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        spawnPoint = transform.position;
        helpUntil = Time.time + 16f;
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

        if (Input.GetKeyDown(KeyCode.F))
            SetFlying(!flying);

        if (Cursor.lockState == CursorLockMode.Locked && eyes != null)
        {
            float mx = Input.GetAxis("Mouse X") * lookSensitivity;
            float my = Input.GetAxis("Mouse Y") * lookSensitivity;
            transform.Rotate(0f, mx, 0f);
            pitch = Mathf.Clamp(pitch - my, -maxPitch, maxPitch);
            eyes.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        if (flying)
            FlyMove();
        else
            WalkMove();

        ClampToWorld();

        if (!flying && transform.position.y < -25f)
        {
            controller.enabled = false;
            transform.position = spawnPoint;
            verticalVelocity = 0f;
            controller.enabled = true;
        }
    }

    void ClampToWorld()
    {
        Vector3 p = transform.position;
        float lim = CedyniaWorld.WorldLimit;
        p.x = Mathf.Clamp(p.x, -lim, lim);
        p.z = Mathf.Clamp(p.z, -lim, lim);
        p.y = Mathf.Clamp(p.y, -1.2f, CedyniaWorld.FlyMaxY);
        if ((p - transform.position).sqrMagnitude < 0.000001f)
            return;

        bool was = controller.enabled;
        controller.enabled = false;
        transform.position = p;
        controller.enabled = was;
    }

    void SetFlying(bool on)
    {
        flying = on;
        verticalVelocity = 0f;
        if (!controller.enabled)
            controller.enabled = true;
    }

    void FlyMove()
    {
        if (!controller.enabled)
            controller.enabled = true;

        Vector3 dir = Vector3.zero;
        if (eyes != null)
        {
            dir += eyes.forward * Input.GetAxis("Vertical");
            dir += eyes.right * Input.GetAxis("Horizontal");
        }
        else
        {
            dir += transform.forward * Input.GetAxis("Vertical");
            dir += transform.right * Input.GetAxis("Horizontal");
        }

        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E))
            dir += Vector3.up;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.C))
            dir += Vector3.down;

        if (dir.sqrMagnitude > 1f)
            dir.Normalize();

        float speed = Input.GetKey(KeyCode.LeftShift) ? flyFastSpeed : flySpeed;
        controller.Move(dir * speed * Time.deltaTime);
    }

    void WalkMove()
    {
        if (!controller.enabled)
            controller.enabled = true;

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
    }

    public void RememberSpawn(Vector3 point)
    {
        spawnPoint = point;
    }

    void OnGUI()
    {
        var hint = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
        hint.normal.textColor = Color.white;

        string flyState = flying ? "latanie WŁĄCZONE" : "latanie wyłączone";
        GUI.Label(new Rect(18, 14, 720, 28), "F — " + flyState, hint);

        if (Time.time > helpUntil)
            return;

        GUI.Label(new Rect(18, 42, 720, 90),
            "WASD — ruch    Mysz — rozglądanie    Shift — szybciej\n" +
            "Spacja / E — góra (lot)    Ctrl / Q — dół    Esc — kursor\n" +
            "Wyjdź z grodu przez bramę (most) albo leć nad palisadą (F).\n" +
            "Latanie nie przechodzi przez ściany, chaty i teren.",
            hint);
    }
}
