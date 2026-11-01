using UnityEngine;

public class CarCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 8f, -10f);
    public float followSmoothTime = 0.2f;
    public float lookAtHeight = 0.75f;

    private Vector3 followVelocity;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.TransformPoint(offset);
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            followSmoothTime);

        Vector3 lookDirection = target.position + Vector3.up * lookAtHeight - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
    }
}
