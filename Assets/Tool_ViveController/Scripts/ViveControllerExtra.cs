/// <summary>
/// Vive simple controller, only pass down the controller action
/// </summary>
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SteamVR_TrackedObject))]
public class ViveControllerExtra : MonoBehaviour {

	public event Action<GameObject> OnTriggerDown;
	public event Action<GameObject> OnTriggerUp;
	public event Action<GameObject> OnTriggerTouch;

	private SteamVR_TrackedObject trackedObj;
	private Vector3 m_TriggerDownPosition;
	private Vector3 m_TriggerUpPosition;

	public SteamVR_Controller.Device Device
	{
		get { return SteamVR_Controller.Input ((int)trackedObj.index); }
	}

	public SteamVR_TrackedObject TrackedObj
	{
		get { return trackedObj; }
	}
	///------------------------------------------------------------------
	private void Awake()
	{
		trackedObj = GetComponent<SteamVR_TrackedObject> ();
	}

	private void Update()
	{
		CheckInput ();
	}

	private void CheckInput()
	{
		var device = SteamVR_Controller.Input ((int)trackedObj.index);

		if (device.GetTouchDown (SteamVR_Controller.ButtonMask.Trigger))
		{
			if (OnTriggerDown != null)
				OnTriggerDown (gameObject);
		}

		if(device.GetTouchUp(SteamVR_Controller.ButtonMask.Trigger))
		{
			if (OnTriggerUp != null)
				OnTriggerUp (gameObject);
		}
			
		if(device.GetTouch(SteamVR_Controller.ButtonMask.Trigger))
		{
			if (OnTriggerTouch != null)
				OnTriggerTouch (gameObject);
		}
	}
}
