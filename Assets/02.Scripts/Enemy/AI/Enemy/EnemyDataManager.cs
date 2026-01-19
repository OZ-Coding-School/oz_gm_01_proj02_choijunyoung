using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyDataManager : NetworkBehaviour
{
    [SerializeField] public EnemyData _enemyData;
    [SerializeField] public List<ParticleSystem> muzzleFlashVFX = new List<ParticleSystem>();

}
