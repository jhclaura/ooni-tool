using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Tool : MonoBehaviour {

	public event Action<Collider> OnCollideEnter;
	public event Action<Collider> OnCollideExit;
	public event Action<Collider> OnCollideStay;

	private Animator animator;
	private ParticleSystem particle;
	private GvrAudioSource soundEffect;

	void Start()
	{
		animator = GetComponent<Animator> ();
		particle = GetComponent<ParticleSystem> ();
		soundEffect = GetComponent<GvrAudioSource> ();

		OnStart ();
	}

	public virtual void OnStart()
	{
		//
	}

	private void OnTriggerEnter(Collider _collider)
	{
		if (OnCollideEnter!=null)
			OnCollideEnter (_collider);
	}

	private void OnTriggerExit(Collider _collider)
	{
		if (OnCollideExit!=null)
			OnCollideExit (_collider);
	}

	private void OnTriggerStay(Collider _collider)
	{
		if (OnCollideStay!=null)
			OnCollideStay (_collider);
	}

	public void OnDown()
	{
		if(animator)
			animator.SetTrigger ("Down");

		if(particle)
			particle.Play();

		if (soundEffect)
			soundEffect.Play ();
	}

	public void OnUp()
	{
		if(animator)
			animator.SetTrigger ("Up");

		if(particle)
			particle.Stop();

		if (soundEffect)
			soundEffect.Stop ();
	}
}
