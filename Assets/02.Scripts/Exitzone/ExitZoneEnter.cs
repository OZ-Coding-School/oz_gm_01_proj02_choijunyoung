using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ExitZoneEnter : NetworkBehaviour
{
    [SerializeField] GameObject exitZoneCanvas;
    [SerializeField] private TextMeshProUGUI timerTxt;

    private NetworkVariable<float> time = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private bool timerActive = false;
    private readonly List<ulong> playersInZone = new List<ulong>();

    private void Awake()
    {
        if (timerTxt == null) timerTxt = exitZoneCanvas.GetComponentInChildren<TextMeshProUGUI>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        time.OnValueChanged += OnTimeChanged;
    }

    public override void OnNetworkDespawn()
    {
        time.OnValueChanged -= OnTimeChanged;
        base.OnNetworkDespawn();
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var networkObj = other.GetComponentInParent<NetworkObject>();
            if (networkObj != null)
            {
                ulong clinetId = networkObj.OwnerClientId;
                if (!playersInZone.Contains(clinetId))
                {
                    playersInZone.Add(clinetId);
                    Debug.Log($"[ExitZone] player {clinetId} 진입.(현재 : {playersInZone.Count} 명)");

                    if (!timerActive && playersInZone.Count > 0) StartTimerServerRpc();
                }
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var networkObj = other.GetComponentInParent<NetworkObject>();
            if (networkObj != null)
            {
                ulong clinetId = networkObj.OwnerClientId;
                if (playersInZone.Contains(clinetId))
                {
                    playersInZone.Remove(clinetId);
                    Debug.Log($"[ExitZone] player {clinetId} 퇴장.(현재 : {playersInZone.Count} 명)");
                    if (playersInZone.Count == 0)
                    {
                        StopTimerServerRpc();
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StartTimerServerRpc()
    {
        if (timerActive) return;

        timerActive = true;
        time.Value = 40f;
        exitZoneCanvas.SetActive(true);
        Debug.Log("[ExitZone] 타이머 시작");
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void StopTimerServerRpc()
    {
        if (!timerActive) return;

        timerActive= false;
        exitZoneCanvas.SetActive(false);
        Debug.Log("[ExitZone] 타이머 중지 (모두 퇴장)");
    }

    private void Update()
    {
        if (!timerActive) return;

        time.Value -= Time.deltaTime;

        if(time.Value <= 0f)
        {
            time.Value = 0f;
            timerActive = false;
            PlayerExit();
        }
    }

    private void OnTimeChanged(float prev, float current)
    {
        if(timerTxt != null)
        {
            int seconds = Mathf.CeilToInt(current);
            timerTxt.text = $"{seconds:D2}s";
        }
        if (current <= 0f && exitZoneCanvas.activeSelf)
        {
            exitZoneCanvas.SetActive(false);
        }
    }

    private void PlayerExit()
    {
        Debug.Log("[ExitZone] 타이머 종료. 탈출 시퀀스 시작.");

        foreach(var clientId in playersInZone)
        {
            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) continue;

            var playerobj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if(playerobj == null) continue;

            var playerDamage = playerobj.GetComponent<PlayerDamage>(); 
            if(playerDamage != null && !playerDamage.isDead.Value)
            {
                playerDamage.ExitSession();
                Debug.Log($"[ExitZone] {clientId} 탈출 성공");
            }
            else
            {
                Debug.Log($"[ExitZone] {clientId} 탈출 실패(사망)");
            }
        }
        exitZoneCanvas.SetActive(false);
    }

}
