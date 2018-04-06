using UnityEngine;
using System.Linq;
using DG.Tweening;

public class AutoKillParticle : MonoBehaviour
{
	void Start ()
    {
        float time = GetComponentsInChildren<ParticleSystem>().OrderByDescending(p => p.duration).First().duration;
        Destroy(gameObject,time);
	}
}
