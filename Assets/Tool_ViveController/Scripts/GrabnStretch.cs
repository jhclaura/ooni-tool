using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(SteamVR_TrackedController))]
public class GrabnStretch : MonoBehaviour {

	public event Action<float> OnScalingSelf;
	public Rigidbody attachPoint;

	private SteamVR_TrackedController controller;
	private ViveControllerExtra controllerExtra;

	//--- Grab n Stretch ---
	//----------------------
	private VRInteractiveObject m_CurrentInteractible;		// The current interactive object
	private GameObject touchedObj;
	private GameObject grabbedObj; // for grabbed obj (needs rigidBody)
	private GameObject stretchObj;

	private bool grabSomething = false;
	private bool inStretchMode = false;
	private float initialControllersDistance;
	private Vector3 originalScale;
	//----------------------

	private Vector3 m_TriggerClickPosition;
	private Vector3 m_TriggerDownPosition;
	private Vector3 m_TriggerUpPosition;
	private float m_LastUpTime;

	public StickerTool myTool;
	public Tool hand;
	private bool inUse;
	private Transform stickerParent;

	//--- Scale Self ---
	//----------------------
	// SCALE_CameraRig_Parent => Player
	[Header("Self Scaling")]
	public Transform cameraEye;
	public float minSelfScale = 0.05f;
	public float maxSelfScale = 20f;
	public Tool scaleHand;
	public bool enableSelfScale = true;

	private bool m_inSelfScalingMode = false;
	private bool m_inSelfScalingSupportMode = false;
	private GrabnStretch otherController;
	private Transform player;
	private float scaleWaitTime = 1f;
	private float firstTouchTime;
	private bool isBusySelfScaling = false;

	public float PlayerScale
	{
		get { return player.localScale.x; }
	}

	public bool InSelfScalingSupportMode
	{
		get { return m_inSelfScalingSupportMode; }
	}

	public bool InSelfScalingMode
	{
		get { return m_inSelfScalingMode; }
	}
	//----------------------

	public SteamVR_Controller.Device Device
	{
		get { return SteamVR_Controller.Input ((int)controller.controllerIndex); }
	}

	void OnEnable()
	{
		if(controller==null)
			controller = GetComponent<SteamVR_TrackedController>();
			
		controller.TriggerClicked += HandleTriggerDown;
		controller.TriggerUnclicked += HandleTriggerUp;
		controller.TriggerDowning += HandleTriggerTouch;

//		if(controllerExtra==null)
//			controllerExtra = GetComponent<ViveControllerExtra>();
//
//		controllerExtra.OnTriggerDown += HandleTriggerDown;
//		controllerExtra.OnTriggerUp += HandleTriggerUp;
//		controllerExtra.OnTriggerTouch += HandleTriggerTouch;

//		controller.PadClicked += HandlePadDown;
//		controller.PadUnclicked += HandlePadUp;

		hand.OnCollideEnter += HandleOver;
		hand.OnCollideStay += HandleStay;
		hand.OnCollideExit += HandleOut;

//		scaleHand.OnCollideEnter += HandleSelfOver;
//		scaleHand.OnCollideStay += HandleSelfStay;
//		scaleHand.OnCollideExit += HandleSelfExit;

		if (myTool != null)
			myTool.OnChangeToolStatus += OnToolStatusChange;
		else
			inUse = true;
	}

	void OnDisable()
	{
		controller.TriggerClicked -= HandleTriggerDown;
		controller.TriggerUnclicked -= HandleTriggerUp;
		controller.TriggerDowning -= HandleTriggerTouch;

//		controllerExtra.OnTriggerDown -= HandleTriggerDown;
//		controllerExtra.OnTriggerUp -= HandleTriggerUp;
//		controllerExtra.OnTriggerTouch -= HandleTriggerTouch;

//		controller.PadClicked -= HandlePadDown;
//		controller.PadUnclicked -= HandlePadUp;

		hand.OnCollideEnter -= HandleOver;
		hand.OnCollideStay -= HandleStay;
		hand.OnCollideExit -= HandleOut;

//		scaleHand.OnCollideEnter -= HandleSelfOver;
//		scaleHand.OnCollideStay -= HandleSelfStay;
//		scaleHand.OnCollideExit -= HandleSelfExit;

		if(myTool!=null)
			myTool.OnChangeToolStatus -= OnToolStatusChange;
	}
	
	void Start ()
	{
		player = transform.parent.parent;
	}

	private void OnToolStatusChange(bool _inUse, int toolIndex)
	{
		inUse = _inUse;

		if (!inUse)
			Reset ();
	}

	public void HandleSelfOver(Collider _collider)
	{
		if(!enableSelfScale)
			return;
		
		if (_collider.gameObject.tag == "GameController")
		{
			otherController = _collider.gameObject.GetComponentInParent<GrabnStretch> ();
			if(otherController.InSelfScalingMode)
			{
				m_inSelfScalingSupportMode = true;
				m_inSelfScalingMode = false;
//				Debug.Log (_collider.name + ": S_Scaling Support");
			}
			else
			{
				m_inSelfScalingSupportMode = false;
				m_inSelfScalingMode = true;
//				Debug.Log (_collider.name + ": S_Scaling");
			}

			if(m_inSelfScalingMode)
			{
				originalScale = player.localScale;
				initialControllersDistance = (attachPoint.position - otherController.attachPoint.position).sqrMagnitude;
				firstTouchTime = Time.time;
			}
			DeviceVibrate ();
		}
	}

	public void HandleSelfStay(Collider _collider)
	{
		if(!enableSelfScale)
			return;
		
		if (_collider.gameObject.tag == "GameController")
		{
			// just in case
			if (!m_inSelfScalingMode && !m_inSelfScalingSupportMode)
			{
				otherController = _collider.gameObject.GetComponentInParent<GrabnStretch> ();
				if(otherController.InSelfScalingMode)
				{
					m_inSelfScalingSupportMode = true;
				}
				else
				{
					m_inSelfScalingMode = true;
					firstTouchTime = Time.time;
				}
			}

			if( m_inSelfScalingMode && otherController.InSelfScalingSupportMode )
			{
				float threshold = firstTouchTime + scaleWaitTime;
				if (Time.time > threshold)
				{
					isBusySelfScaling = true;
					ScaleSelf (player);
				}
			}
		}
	}

	public void HandleSelfExit(Collider _collider)
	{
		if(!enableSelfScale)
			return;
		
		if (_collider.gameObject.tag == "GameController")
		{
			if(m_inSelfScalingMode || m_inSelfScalingSupportMode)
			{
				m_inSelfScalingMode = false;
				m_inSelfScalingSupportMode = false;
				otherController = null;
				firstTouchTime = 0f;
				isBusySelfScaling = false;

//				Debug.Log (_collider.name + ": handle exit");
			}
		}
	}

	public void HandleOver(Collider _collider)
	{
		if (_collider.CompareTag("GameController"))
			return;
						
		if (!inUse)
			return;
		
		// ignore if in self-stretching mode
//		if (m_inSelfScalingMode || m_inSelfScalingSupportMode)
		if (isBusySelfScaling)
			return;

		// ignore if already grabbing something
		if (grabSomething)
			return;

		if (inStretchMode)
			return;

		// If we hit an interactive item
		if (_collider.gameObject.GetComponent<VRInteractiveObject> ())
		{
			DeviceVibrate();

			touchedObj = _collider.gameObject;
			m_CurrentInteractible = touchedObj.GetComponent<VRInteractiveObject> ();
			m_CurrentInteractible.StartTouching (gameObject);
		}
	}

	public void HandleOut(Collider _collider)
	{
		if (_collider.gameObject.tag == "GameController")
			return;

		if (!inUse)
			return;
		
		if (touchedObj == null)
			return;

		// comment out???
		if (inStretchMode)
			return;

		if (_collider == touchedObj.GetComponent<Collider> ())
		{
			// checking cuz the parenting(aka non-physics) method will trigger this event for some reason :/
			if (!m_CurrentInteractible.HasRigidbody && grabSomething)
				return;

			// what if it's still grabbing?
			if (grabSomething)
				ExitGrabMode (false);

			//Debug.Log (gameObject.name + " exit touch " + collider.name);
			m_CurrentInteractible.StopTouching (gameObject);
			m_CurrentInteractible = null;
			touchedObj = null;
		}
	}

	public void HandleStay(Collider _collider)
	{
		if (_collider.gameObject.tag == "GameController")
			return;

		// double check if after triggerExit, but still try to grab but not being able to do triggerEnter
		if (!inUse)
			return;

		// ignore if in self-stretching mode
//		if (m_inSelfScalingMode || m_inSelfScalingSupportMode)
		if (isBusySelfScaling)
			return;

		// ignore if already grabbing something
		if (grabSomething || touchedObj)
			return;

		// If we hit an interactive item
		if (_collider.gameObject.GetComponent<VRInteractiveObject> ())
		{
			DeviceVibrate();

			touchedObj = _collider.gameObject;
			m_CurrentInteractible = touchedObj.GetComponent<VRInteractiveObject> ();
			m_CurrentInteractible.StartTouching (gameObject);
		}
	}

	public void HandleTriggerDown(object sender, ClickedEventArgs e)
	//public void HandleTriggerDown(GameObject sender)
	{
//		if(m_inSelfScalingMode && otherController.InSelfScalingSupportMode)
//		{
//			originalScale = player.localScale;
//			initialControllersDistance = (attachPoint.position - otherController.attachPoint.position).sqrMagnitude;
//			return;
//		}

		if (!inUse)
			return;
		else
			hand.OnDown ();

		if (touchedObj == null)
			return;			// not possible but just double check

		// if haven't grab anything && not in stretch mode
		if(!grabSomething && !inStretchMode)
		{
			// if already been grabbed
			if (m_CurrentInteractible.IsGrabbing)
			{
				// enter stretch mode!
				inStretchMode = true;
				stretchObj = touchedObj;
				originalScale = stretchObj.transform.localScale;

				initialControllersDistance = (attachPoint.position - m_CurrentInteractible.GrabbedPos).sqrMagnitude;
				//Debug.Log (gameObject.name + "starts stretching!");
				DeviceVibrate ();

				// remote the joint attached to other controller
				if(m_CurrentInteractible.Joint)
				{
					stretchObj.GetComponent<Rigidbody> ().isKinematic = true;
					m_CurrentInteractible.RemoveJoint ();
				}
			}
			else
			{
				// be grabbed!
				m_CurrentInteractible.Down(gameObject);
				m_CurrentInteractible.grabbedPoint = attachPoint.position;

				// Grab in PHYSICS way if has rigidbody
				if (m_CurrentInteractible.TheRigidbody)  //if (m_CurrentInteractible.HasRigidbody)
				{
					Debug.Log ("to grab r_body");
					// set the kinematic false (if it is) so can be controlled by joint
					if (m_CurrentInteractible.IsKinematic)
					{
						Debug.Log ("to grab IsKinematic");
						m_CurrentInteractible.IsKinematic = false;
					}
					Debug.Log ("add joint");
					// add fixed joint
					m_CurrentInteractible.AddJoint(attachPoint);
				}
				else // or NON-PHYSICS(hierarchy way)
				{
					stickerParent = touchedObj.transform.parent;
					touchedObj.transform.parent = gameObject.transform;
				}

				grabSomething = true;
				DeviceVibrate();
			}
		}
	}

	public void HandleTriggerUp(object sender, ClickedEventArgs e)
	//public void HandleTriggerUp(GameObject sender)
	{
//		if(m_inSelfScalingMode || m_inSelfScalingSupportMode)
//		{
//			m_inSelfScalingMode = false;
//			m_inSelfScalingSupportMode = false;
//			otherController = null;
//		}

		if (!inUse)
			return;

		hand.OnUp ();

		if (grabSomething)
		{
			ExitGrabMode (true);	
		}

		if (inStretchMode)
		{
			ExitStretchMode ();
		}
	}

	public void HandleTriggerTouch(object sender, ClickedEventArgs e)
	//public void HandleTriggerTouch(GameObject sender)
	{
//		if(m_inSelfScalingMode && otherController.InSelfScalingSupportMode)
//		{
//			ScaleSelf (player);
//		}

		if (!inUse)
			return;

		if(m_CurrentInteractible)
			m_CurrentInteractible.Touch(gameObject);

		if (inStretchMode)
		{
			// check if the object is still be grabbed
			if(!m_CurrentInteractible.IsGrabbing)
			{
				//if not, exit stretch mode
				ExitStretchMode();
			}
			else
			{
				if (stretchObj != null)
					ScaleAroundPoint (stretchObj);
			}
		}
	}

	public void HandlePadDown(object sender, ClickedEventArgs e)
	{
		if (!inUse)
			return;
		
		if(m_CurrentInteractible)
			m_CurrentInteractible.PadDown (gameObject);

		// SHOOT_OUT
		if (grabSomething)
		{
			ExitGrabMode (true);
		}

		if (inStretchMode)
		{
			ExitStretchMode ();
		}
	}

	public void HandlePadUp(object sender, ClickedEventArgs e)
	{

	}

	private void ScaleSelf(Transform target)
	{
		// v.1
		/*
		// compare current distance of two controllers, with the start distance, to stretch the object
		var pivot = cameraEye.transform.position;
		pivot.y = target.transform.position.y; // set pivot to be on the floor
		var mag = (attachPoint.position - otherController.attachPoint.position).sqrMagnitude - initialControllersDistance;			
		var endScale = target.transform.localScale * (1f + mag*0.01f);

		// diff from obj pivot to desired pivot
		var diffP = target.transform.position - pivot;
		var finalPos = (diffP * (1f + mag*0.01f)) + pivot;

		target.transform.localScale = endScale;
		target.transform.position = finalPos;
		*/

		float scaleFactor;
		if(transform.position.y > cameraEye.position.y)
		{
			// scale up
			scaleFactor = 1f + 0.01f;// * PlayerScale;
		}
		else {
			// scale down
			scaleFactor = 1f - 0.01f;// * PlayerScale;
		}
		var endScale = target.transform.localScale * scaleFactor;
		var idealScaleValue = Mathf.Clamp (endScale.x, minSelfScale, maxSelfScale);
		endScale.Set (idealScaleValue, idealScaleValue, idealScaleValue);

		if (Mathf.Approximately (target.transform.localScale.x, endScale.x))
			return;

		var pivot = cameraEye.transform.position;
		pivot.y = target.transform.position.y; // set pivot to be on the floor

		var diffP = target.transform.position - pivot;
		var finalPos = (diffP * scaleFactor) + pivot;

		target.transform.localScale = endScale;
		target.transform.position = finalPos;

		DeviceVibrate (500);

		if (OnScalingSelf != null)
			OnScalingSelf (PlayerScale);
	}

	private void ScaleAroundPoint(GameObject target)
	{
		// compare current distance of two controllers, with the start distance, to stretch the object
		var pivot = m_CurrentInteractible.GrabbedPos;
		var mag = (attachPoint.position - pivot).sqrMagnitude - initialControllersDistance;			
		var endScale = target.transform.localScale * (1f + mag*0.1f);

		// diff from obj pivot to desired pivot
		var diffP = target.transform.position - pivot;
		var finalPos = (diffP * (1f + mag*0.1f)) + pivot;

		target.transform.localScale = endScale;
		target.transform.position = finalPos;
	}

	private void ExitStretchMode()
	{
		// if it's physics object 
		if(m_CurrentInteractible.HasRigidbody)
		{
			// and be grabbed
			if(m_CurrentInteractible.IsGrabbing)
			{
				m_CurrentInteractible.AddJoint (m_CurrentInteractible.Grabber);
				stretchObj.GetComponent<Rigidbody> ().isKinematic = false;
			}

			if(!m_CurrentInteractible.IsKinematic)
			{
				stretchObj.GetComponent<Rigidbody> ().isKinematic = false;
			}
		}
						
		inStretchMode = false;
		stretchObj = null;
		//DeviceVibrate();
		//Debug.Log ("m_CurrentInteractible.IsGrabbing: " + m_CurrentInteractible.IsGrabbing + ", " + gameObject.name + " exit Stretch Mode");
	}

	private void ExitGrabMode(bool destroyImmediate)
	{
		//Debug.Log (gameObject.name + " exits grab!");
		//Debug.Log ("exits grab, device velocity: " + Device.velocity.sqrMagnitude);

		m_CurrentInteractible.Up (gameObject);

		if (m_CurrentInteractible.HasRigidbody)
		{
			Debug.Log ("m_CurrentInteractible.HasRigidbody");
			// remote joint!
			if(m_CurrentInteractible.Joint!=null)
			{
				var device = Device;
				var rigidbody = m_CurrentInteractible.Joint.GetComponent<Rigidbody> ();

				// destroy the fixed joint
				m_CurrentInteractible.RemoveJoint ();

				// Apply force
				rigidbody.velocity = device.velocity;
				rigidbody.angularVelocity = device.angularVelocity;
				rigidbody.maxAngularVelocity = rigidbody.angularVelocity.magnitude;
			}
				
			// Reset kinematic status
			if (m_CurrentInteractible.IsKinematic) {
				touchedObj.GetComponent<Rigidbody> ().isKinematic = true;
			}
		}
		else
		{
			if (touchedObj)
			{
				//touchedObj.transform.parent = null;
				touchedObj.transform.parent = stickerParent;

				if(m_CurrentInteractible.usePhysics && Device.velocity.sqrMagnitude > 0.2f)
				{
					Rigidbody r_b = m_CurrentInteractible.AddRigidbody ();
					r_b.velocity = Device.velocity;
					r_b.angularVelocity = Device.angularVelocity;
				}
			}
		}

		grabSomething = false;
		//DeviceVibrate();
	}

	private void Reset()
	{
		if (grabSomething)
			ExitGrabMode (false);

		if (inStretchMode)
			ExitStretchMode ();

		if(m_CurrentInteractible)
			m_CurrentInteractible.StopTouching (gameObject);
		m_CurrentInteractible = null;
		touchedObj = null;
		//glove.OnUp ();
	}

	public void DeviceVibrate()
	{
		Device.TriggerHapticPulse (1000);
	}

	public void DeviceVibrate(int strength)
	{
		Device.TriggerHapticPulse ((ushort)strength);
	}
}
