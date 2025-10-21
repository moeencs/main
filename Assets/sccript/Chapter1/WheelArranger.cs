using System.Collections;
using UnityEngine;

public class WheelArranger : MonoBehaviour
{  // Assign your 9 blocks in the Inspector
    public float radius = 2f;    // distance from center
    public float spinSpeed = 30f;
    public float moveSpeed = 2f;

    [SerializeField]
    GameObject wheelParent;
    private bool spinning = false;

    public Transform blocksPTemp;

    private void Start()
    {
       // StartCoroutine(MoveSequence());
    }
    private IEnumerator MoveSequence()
    {
        yield return new WaitForSeconds(2f);
        // CreateWheelAndSpin();

        for (int i = 0; i < blocksPTemp.childCount; i++)
        {
            float angle = i * Mathf.PI * 2f / blocksPTemp.childCount;
            Vector3 pos = new Vector3(0, Mathf.Sin(angle), Mathf.Cos(angle)) * radius;

            blocksPTemp.GetChild(i).SetParent(wheelParent.transform, false);
            blocksPTemp.GetChild(i).localPosition = pos;
        }

        // Start spinning
        spinning = true;
    }

    public void CreateWheelAndSpin()
    {
        // Put the blocks around in a circle
        StartCoroutine(MoveSequence());
    }

    void Update()
    {
        if (spinning && wheelParent != null)
        {
            // Rotate on X axis
            wheelParent.transform.Rotate(Vector3.left * spinSpeed * Time.deltaTime);
            wheelParent.transform.position += Vector3.back * moveSpeed * Time.deltaTime;


            // Clamp rotation between 3 and -26

        }
    }
}
