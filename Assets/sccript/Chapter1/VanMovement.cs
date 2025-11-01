using UnityEngine;
using System.Collections;

public class VanMovement : MonoBehaviour
{
    public float moveSpeed = 5f;       // Forward speed
    public float turnSpeed = 100f;     // Turning rotation speed
    public float turnRadius = 2f;      // Curve radius for more natural turn
    public Transform[] wheels;         // Assign your wheel transforms in Inspector
    public float wheelSpinSpeed = 500f; // Spin speed of wheels

    private bool isMoving = false;

    [SerializeField] Animator vanAnimator;
    [SerializeField] AudioSource soundSourceVan;
    

    public void StartVanMovement()
    {
        if (!isMoving)
        {
            soundSourceVan.Play();
            StartCoroutine(MoveSequence());
            vanAnimator.SetTrigger("go");
        }
    }

    private IEnumerator MoveSequence()
    {
        isMoving = true;

        // 1. Move forward 10 units
        yield return StartCoroutine(MoveForward(8f));

        // 2. Smooth right turn while moving forward
        yield return StartCoroutine(SmoothTurnRight(90f));

        // 3. Move forward 15 units
        yield return StartCoroutine(MoveForward(30f));

        isMoving = false;
        VanMovementDone();
        soundSourceVan.Stop();
    }

    private IEnumerator MoveForward(float distance)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + transform.forward * distance;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

            // Spin the wheels while moving
            SpinWheels();

            yield return null;
        }

    }

    private IEnumerator SmoothTurnRight(float angle)
    {
        float rotated = 0f;

        while (rotated < angle)
        {
            float step = turnSpeed * Time.deltaTime;

            // Move forward slightly while rotating
            transform.position += transform.forward * (moveSpeed * 0.5f * Time.deltaTime);

            // Rotate gradually
            transform.Rotate(0, step, 0);

            // Spin the wheels during the turn
            SpinWheels();

            rotated += step;
            yield return null;
        }
    }

    /// <summary>
    /// Spins all wheels to simulate motion
    /// </summary>
    private void SpinWheels()
    {
        if (wheels == null || wheels.Length == 0) return;

        foreach (Transform wheel in wheels)
        {
            wheel.Rotate(Vector3.right, wheelSpinSpeed * Time.deltaTime, Space.Self);
        }
    }

    public void VanMovementDone()
    {
        Debug.Log("start convo again");
        FindObjectOfType<Chapter1Mannager>().TaskDone();
    }
}
