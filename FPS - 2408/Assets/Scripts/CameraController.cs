using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private int sens;
    [SerializeField] private int lockVertMin, lockVertMax;
    [SerializeField] private bool invertY;

    private float rotX;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // input
        var mouseY = Input.GetAxis("Mouse Y") * sens * Time.deltaTime;
        var mouseX = Input.GetAxis("Mouse X") * sens * Time.deltaTime;

        if (invertY) rotX += mouseY;
        else rotX -= mouseY;

        // clamp x rotation on x
        rotX = Mathf.Clamp(rotX, lockVertMin, lockVertMax);

        // rotate camera on x
        transform.localRotation = Quaternion.Euler(rotX, 0, 0);

        // rotate the player on y
        transform.parent.Rotate(Vector3.up * mouseX);
    }
}