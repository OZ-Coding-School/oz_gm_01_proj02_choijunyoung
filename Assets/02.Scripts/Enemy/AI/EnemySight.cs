using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySight : NetworkBehaviour
{
    [Header("Sight Settings")]
    public Transform eyeTransform;
    [Range(0, 360)]
    public float viewAngle = 120f;

    [Header("Obstacle Settings")]
    public LayerMask obstacleMask;

    // 플레이어 리스트
    private List<Transform> allPlayers = new List<Transform>();

    public Transform CurrentTarget { get; private set; }

    EnemyDataManager _dataManager;
    EnemyData EnemyData;

    private float _scanTimer = 0f;
    private float _scanInterval = 1.0f; // 1초마다 플레이어 목록 갱신

    void Start()
    {
        _dataManager = GetComponent<EnemyDataManager>();
        if (_dataManager != null) EnemyData = _dataManager._enemyData;

        // 시작 시 한 번 찾기
        RefreshPlayerList();
    }

    // 주기적으로 플레이어 목록을 최신화하는 함수
    void RefreshPlayerList()
    {
        allPlayers.Clear();
        var playerObjs = FindObjectsByType<PlayerMove>(FindObjectsSortMode.None);
        foreach (var p in playerObjs)
        {
            allPlayers.Add(p.transform);
        }
    }

    public bool IsPlayerInRange()
    {
        // 매 프레임 타이머를 돌려서 1초마다 리스트를 강제 갱신
        // (플레이어가 죽어서 새로 생성되거나, 난입했을 때를 대비)
        _scanTimer += Time.deltaTime;
        if (_scanTimer > _scanInterval)
        {
            RefreshPlayerList();
            foreach (var p in allPlayers) Debug.Log("플레이어들 : "+p);
            _scanTimer = 0f;
        }

        if (_dataManager == null || EnemyData == null) return false;

        Vector3 eyePos = eyeTransform != null ? eyeTransform.position : transform.position;
        float sightRange = EnemyData.SightRange;

        Transform nearestTarget = null;
        float minDist = float.MaxValue;

        // 리스트 검사
        foreach (Transform target in allPlayers)
        {
            // 리스트에 null이 섞여있으면 무시 (죽은 객체 등)
            if (target == null) continue;
            Vector3 targetCenter = target.position + Vector3.up * 1.2f;

            float dist = Vector3.Distance(targetCenter, eyePos);
            if (dist > sightRange) continue;

            Vector3 dirToTarget = (target.position - eyePos).normalized;

            // 시야각 체크
            if (Vector3.Angle(transform.forward, dirToTarget) > viewAngle * 0.5f)
            {
                continue;
            }

            // 벽 체크
            if (Physics.Raycast(eyePos, dirToTarget, dist, obstacleMask))
            {
                continue;
            }

            // 가장 가까운 대상 선정
            if (dist < minDist)
            {
                minDist = dist;
                nearestTarget = target;
            }
        }

        CurrentTarget = nearestTarget;
        return CurrentTarget != null;
    }

    private void OnDrawGizmosSelected()
    {
        float range = (_dataManager != null && _dataManager._enemyData != null) ? _dataManager._enemyData.SightRange : 10f;
        Vector3 eyePos = eyeTransform != null ? eyeTransform.position : transform.position;

        Vector3 leftDir = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(eyePos, leftDir * range);
        Gizmos.DrawRay(eyePos, rightDir * range);

        if (CurrentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(eyePos, CurrentTarget.position);
            Gizmos.DrawWireSphere(CurrentTarget.position, 1.0f);
        }
    }
}