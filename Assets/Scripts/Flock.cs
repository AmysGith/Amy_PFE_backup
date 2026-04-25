using UnityEngine;

public class Flock : MonoBehaviour
{
    public float speed;
    float nextRandomRotationTime;
    Vector3 randomDirection;

    [Header("Height Settings")]
    public float minHeight = 1f;
    public float maxHeight = 10f;

    void OnEnable()
    {
        InitializeFish();
    }

    void InitializeFish()
    {
        if (FlockManager.FM == null)
        {
            Debug.LogError("FlockManager.FM n'est pas initialisé !");
            return;
        }

        speed = Random.Range(FlockManager.FM.minSpeed, FlockManager.FM.maxSpeed);
        nextRandomRotationTime = Time.time + Random.Range(1f, 10f);
        randomDirection = GenerateRandomDirection();

        float initialHeight = Random.Range(minHeight, maxHeight);
        transform.position = new Vector3(transform.position.x, initialHeight, transform.position.z);
    }

    void Update()
    {
        if (IsOutOfBounds())
        {
            Vector3 directionToCenter = (FlockManager.FM.transform.position - transform.position).normalized;
            directionToCenter.y = 0;
            RotateTowards(directionToCenter);
        }
        else
        {
            if (Time.time >= nextRandomRotationTime)
            {
                randomDirection = GenerateRandomDirection();
                nextRandomRotationTime = Time.time + Random.Range(1f, 10f);
            }
            RotateTowards(randomDirection);
        }

        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (transform.position.y < minHeight || transform.position.y > maxHeight)
        {
            float correctedHeight = Mathf.Clamp(transform.position.y, minHeight, maxHeight);
            transform.position = new Vector3(transform.position.x, correctedHeight, transform.position.z);
        }
    }

    bool IsOutOfBounds()
    {
        Vector3 relativePos = transform.position - FlockManager.FM.transform.position;
        return Mathf.Abs(relativePos.x) > FlockManager.FM.swimLimits.x ||
               Mathf.Abs(relativePos.z) > FlockManager.FM.swimLimits.z;
    }

    Vector3 GenerateRandomDirection()
    {
        return new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(0.5f, 1f)
        ).normalized;
    }

    void RotateTowards(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                FlockManager.FM.rotationSpeed * Time.deltaTime);
        }
    }
}