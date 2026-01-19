using Unity.Netcode;
using UnityEngine;

public class EnemyRotation : NetworkBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("초당 회전 속도 (높을수록 빨리 돕니다)")]
    public float turnSpeed = 360f;

    public Transform eyeTransform;

    public void LookAtDirection(Vector3 moveDirection)
    {
        // 이동하지 않을 때는 회전하지 않음
        if (moveDirection.sqrMagnitude < 0.01f) return;

        // Y축(높이) 값 제거 (적이 하늘/땅을 보며 기울어지는 것 방지)
        moveDirection.y = 0;
        moveDirection.Normalize();

        // 해당 방향을 바라보는 회전값(Quaternion) 계산
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

        // 현재 회전값에서 목표 회전값으로 부드럽게 회전
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    public void LookAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        LookAtDirection(direction);
    }
}