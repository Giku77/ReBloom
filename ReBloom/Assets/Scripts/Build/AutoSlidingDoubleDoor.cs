using UnityEngine;

public class AutoSlidingDoubleDoor : MonoBehaviour
{
    [Header("Door Panels")]
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [Header("Player")]
    [SerializeField] private Transform player;

    [Header("Move Settings")]
    [SerializeField] private float slideDistance = 1.2f; // 좌우 이동
    [SerializeField] private float recessDepth = 0.15f;  // 뒤로 숨기는 깊이(로컬 -Z 방향 가정)
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Auto Open/Close Distance")]
    [SerializeField] private float openDistance = 2.5f;
    [SerializeField] private float closeDistance = 3.0f;
    [SerializeField] private float closeDelay = 0.0f;

    private Vector3 leftClosedLocalPos, rightClosedLocalPos;
    private Vector3 leftOpenLocalPos, rightOpenLocalPos;

    private bool isOpenRequested;
    private float closeTimer;

    private void Awake()
    {
        if (!leftDoor || !rightDoor) { enabled = false; return; }

        leftClosedLocalPos = leftDoor.localPosition;
        rightClosedLocalPos = rightDoor.localPosition;

        Vector3 leftOffset  = Vector3.left  * slideDistance + Vector3.back * recessDepth;
        Vector3 rightOffset = Vector3.right * slideDistance + Vector3.back * recessDepth;

        leftOpenLocalPos  = leftClosedLocalPos  + leftOffset;
        rightOpenLocalPos = rightClosedLocalPos + rightOffset;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    private void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(player.position, transform.position);

        if (!isOpenRequested)
        {
            if (dist <= openDistance) { isOpenRequested = true; closeTimer = 0f; }
        }
        else
        {
            if (dist >= closeDistance)
            {
                closeTimer += Time.deltaTime;
                if (closeTimer >= closeDelay) isOpenRequested = false;
            }
            else closeTimer = 0f;
        }

        Vector3 targetLeft  = isOpenRequested ? leftOpenLocalPos  : leftClosedLocalPos;
        Vector3 targetRight = isOpenRequested ? rightOpenLocalPos : rightClosedLocalPos;

        float step = moveSpeed * Time.deltaTime;
        leftDoor.localPosition  = Vector3.MoveTowards(leftDoor.localPosition,  targetLeft,  step);
        rightDoor.localPosition = Vector3.MoveTowards(rightDoor.localPosition, targetRight, step);
    }
}
