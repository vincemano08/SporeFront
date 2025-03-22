using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour {
    [SerializeField] CinemachineFollow cameraFollow;

    [SerializeField] private float movementSpeed;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float zoomSpeed;

    [SerializeField] private float minZoom;
    [SerializeField] private float maxZoom;

    void Update() {
        HandlePosition();
        HandleRotation();
        HandleZoom();
    }

    private void HandlePosition() {
        Vector3 inputDir = new(0, 0, 0);
        if (Input.GetKey(KeyCode.W)) inputDir += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) inputDir += Vector3.back;
        if (Input.GetKey(KeyCode.A)) inputDir += Vector3.left;
        if (Input.GetKey(KeyCode.D)) inputDir += Vector3.right;

        if (inputDir != Vector3.zero) {
            Vector3 moveDir = transform.TransformDirection(inputDir);
            moveDir.y = 0;
            moveDir.Normalize();
            transform.position += moveDir * movementSpeed * Time.deltaTime;
        }
    }

    private void HandleRotation() {
        float rotateDir = 0;

        if (Input.GetKey(KeyCode.Q)) rotateDir = -1;
        if (Input.GetKey(KeyCode.E)) rotateDir = 1;

        if (rotateDir != 0) {
            transform.Rotate(Vector3.up, rotateDir * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void HandleZoom() {
        float zoomAmount = 3f;
        Vector3 followOffset = cameraFollow.FollowOffset;
        bool zoomed = false;

        if (Input.mouseScrollDelta.y > 0) {
            followOffset.y -= zoomAmount;
            zoomed = true;
        } else if (Input.mouseScrollDelta.y < 0) {
            followOffset.y += zoomAmount;
            zoomed = true;
        }
        if (zoomed) {
            followOffset.y = Mathf.Clamp(followOffset.y, minZoom, maxZoom);
            cameraFollow.FollowOffset = Vector3.Lerp(cameraFollow.FollowOffset, followOffset, zoomSpeed * Time.deltaTime);
        }
    }
}
