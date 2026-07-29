using UnityEngine;

public class BilboardCorruption : MonoBehaviour
{
    Transform cam;

    [SerializeField] private float minDistance = 3f;
    [SerializeField] private float maxDistance = 15f;
    void Start() => cam = Camera.main.transform;

    void LateUpdate()
    {
        Vector3 dir = transform.position - cam.position;
        dir.y = 0f;

        float distance = dir.magnitude;

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(dir);

            float amount = Mathf.InverseLerp(minDistance, maxDistance, distance);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                target,
                amount * Time.deltaTime * 5f
            );
        }
    }
}