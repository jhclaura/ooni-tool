/// <summary>
/// Vive simple controller, only pass down the controller action
/// </summary>
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SteamVR_TrackedObject))]
public class ViveSimpleController : MonoBehaviour {
	[Tooltip("The attach point for grabbing.")]
	public Rigidbody attachPoint;

	public event Action<Collider> OnHover;
	public event Action<Collider> OnHoverLeave;

	public event Action<GameObject> OnTriggerClick;
	public event Action<GameObject> OnTriggerDown;
	public event Action<GameObject> OnTriggerUp;
	public event Action<GameObject> OnTriggerTouch;

	public event Action<GameObject> OnTouchpadDown;
	public event Action<GameObject, SteamVR_Controller.Device> OnTouchpadUp;

	private SteamVR_TrackedObject trackedObj;
	private GameObject touchedObj;
	private GameObject grabbedObj; // for grabbed obj (needs rigidBody)
	private GameObject stretchObj;

	private bool objHasRigidbody = false;
	private bool objIsKinematic = false;
	private FixedJoint joint; // for grabbed obj
	private bool grabSomething = false;

	private Vector3 m_TriggerClickPosition;
	private Vector3 m_TriggerDownPosition;
	private Vector3 m_TriggerUpPosition;
	private float m_LastUpTime;

	///------------------------------------------------------------------
	/// UTILITY for other classes to get private values
	///------------------------------------------------------------------
	public Vector3 TriggerDownPos
	{
		get { return m_TriggerDownPosition; }
	}

	public Vector3 TriggerUpPos
	{
		get { return m_TriggerUpPosition; }
	}

	public float LastTriggerUpTime
	{
		get { return m_LastUpTime; }
	}

	public SteamVR_Controller.Device Device
	{
		get { return SteamVR_Controller.Input ((int)trackedObj.index); }
	}

	public SteamVR_TrackedObject TrackedObj
	{
		get { return trackedObj; }
	}
	///------------------------------------------------------------------
	/// FUNCTIONSSS
	///------------------------------------------------------------------
	private void Awake()
	{
		trackedObj = GetComponent<SteamVR_TrackedObject> ();
	}

	private void Update()
	{
		CheckInput ();
	}

	private void OnTriggerEnter(Collider collider)
	{
		if (OnHover != null)
			OnHover (collider);
	}

	private void OnTriggerExit(Collider collider)
	{
		if (OnHoverLeave != null)
			OnHoverLeave (collider);
	}

	private void CheckInput()
	{
		var device = SteamVR_Controller.Input ((int)trackedObj.index);

		if (device.GetTouchDown (SteamVR_Controller.ButtonMask.Trigger))
		{
			m_TriggerDownPosition = attachPoint.position;

			//HandleDown (gameObject);

			if (OnTriggerDown != null)
				OnTriggerDown (gameObject);
		}

		if(device.GetTouchUp(SteamVR_Controller.ButtonMask.Trigger))
		{
			m_TriggerUpPosition = attachPoint.position;

			//HandleUp (gameObject);

			if (OnTriggerUp != null)
				OnTriggerUp (gameObject);
		}
			
		if(device.GetTouch(SteamVR_Controller.ButtonMask.Trigger))
		{
			//HandleTouch (gameObject);

			if (OnTriggerTouch != null)
				OnTriggerTouch (gameObject);
		}

		if(device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
		{
			m_TriggerClickPosition = attachPoint.position;

			//HandleClick (gameObject);

			if (OnTriggerClick != null)
				OnTriggerClick (gameObject);
		}

		if(device.GetTouchDown(SteamVR_Controller.ButtonMask.Touchpad))
		{
			/*
			if (OnTouchpadDown != null)
				OnTouchpadDown (gameObject);*/
		}

		if(device.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
		{
			//HandlePadDown (gameObject);

			if (OnTouchpadDown != null)
				OnTouchpadDown (gameObject);
		}

		if(device.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
		{
			//HandlePadUp (gameObject);

			if (OnTouchpadUp != null)
				OnTouchpadUp (gameObject, device);
		}
	}

	public void DeviceVibrate()
	{
		var device = SteamVR_Controller.Input ((int)trackedObj.index);
		device.TriggerHapticPulse (1000);
	}
}
