using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetPlayBackSpeedParticle : MonoBehaviour
{
    private ParticleSystem ps;

    public float speed;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        var psMain = ps.main;
        psMain.simulationSpeed = speed;
    }
}
