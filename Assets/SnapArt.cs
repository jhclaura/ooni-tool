using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ooni_LaserPointer))]
public class SnapArt : MonoBehaviour
{
	[Tooltip("For debug display")]
	public GameObject currArt;

	private GameObject prevArt;
	private Ooni_LaserPointer laserPointer;
	private Vector3 artTargetPos;
	private Quaternion artTargetRot = Quaternion.identity;

	private float posPercentage = 1f; // 1 ~ 0.2
	private Vector2 currTouchpadAxis;
	private Vector2 pastTouchpadAxis;

	private bool hittingWall = false;

	// Grab n Stretch
	private VRInteractiveObject m_CurrentInteractible;
	private GameObject grabbedObj;
	private GameObject stretchObj;
	private bool grabSomething = false;
	private bool inStretchMode = false;
	private float initialControllersDistance;
	private Vector3 originalScale;
	private Vector3 m_TriggerClickPosition;
	private Vector3 m_TriggerDownPosition;
	private Vector3 m_TriggerUpPosition;
	private float m_LastUpTime;

	//---------------------------------------------------------

	void OnEnable()
	{
		if (laserPointer == null)
			laserPointer = GetComponent<Ooni_LaserPointer> ();

		laserPointer.PointerIn += OnPointerIn;
		laserPointer.PointerOutWithTrigger += OnPointerOut;
		laserPointer.PointerOn += OnPointerOn;

		laserPointer.PadTouching += OnPadTouching;
		laserPointer.PadDown += OnPadDown;
		laserPointer.PadUp += OnPadUp;

	}

	void OnDisable()
	{
		laserPointer.PointerIn -= OnPointerIn;
		laserPointer.PointerOutWithTrigger -= OnPointerOut;
		laserPointer.PointerOn -= OnPointerOn;

		laserPointer.PadTouching -= OnPadTouching;
		laserPointer.PadDown -= OnPadDown;
		laserPointer.PadUp -= OnPadUp;
	}

	public void OnPointerIn(object sender, OoniPointerEventArgs e)
	{
		// prepare to grab

		// ignore if already grabbing something
		if (grabSomething)
			return;

		// ignore if already stretching something
		if (inStretchMode)
			return;
		
		if (e.targetCollider.CompareTag ("Art"))
		{
			if (currArt == null)
			{
				GameObject c_art = e.targetCollider.gameObject;
				m_CurrentInteractible = c_art.GetComponent<VRInteractiveObject> ();

				// if already been lasering
				if(m_CurrentInteractible.IsLasering)
				{
					// enter stretch mode!
					inStretchMode = true;
					stretchObj = c_art;
					originalScale = stretchObj.transform.localScale;

					initialControllersDistance = (e.hitPoint - e.target.position).sqrMagnitude;
					Debug.Log ("pointer onto art & stretch!");
				}
				else
				{
					// be grabbed!
					m_CurrentInteractible.LaserDown(laserPointer);

					// Let laser shoot through Art
					// v.1
					// e.targetCollider.enabled = false;
					//v.2
					laserPointer.OnToArt = true;

					currArt = e.target.gameObject;

					if (prevArt != currArt)
						posPercentage = 1f;

					grabSomething = true;
					Debug.Log ("pointer onto art & grab!");
				}

				laserPointer.DeviceVibrate ();
			}
		}
	}

	// Pointer On (anything)
	public void OnPointerOn(object sender, OoniPointerEventArgs e)
	{
		if (grabSomething)
		{
			if (!e.targetCollider.CompareTag ("Art"))
			{
				artTargetPos = e.hitPoint;
				artTargetRot = e.target.localRotation;
				hittingWall = true;
			}
		}

		if (inStretchMode)
		{
			// check if the object is still be lasered
			if(!m_CurrentInteractible.IsLasering)
			{
				//if not, exit stretch mode
				//ExitStretchMode();
			}
			else
			{
				if (stretchObj != null)
					ScaleAroundPoint (stretchObj, e.hitPoint);
			}
		}

	}

	// Pointer Out + Trigger Up => for sure Out
	public void OnPointerOut()
	{
		if (inStretchMode)
		{
			//Exit Stretch Mode
		}

		if (grabSomething && currArt)
		{
			//currArt.GetComponent<Collider>().enabled = true;
			laserPointer.OnToArt = true;

			prevArt = currArt;
			currArt = null;
			Debug.Log ("pointer left");
		}
	}

	public void OnPadDown(Vector2 touchAxis)
	{
		currTouchpadAxis = pastTouchpadAxis = touchAxis;
	}

	public void OnPadTouching(Vector2 touchAxis)
	{
		/*
		if(IfInBetween(touchAxis.y) && IfInBetween(touchAxis.x))
		{
			//Debug.Log ("pad center");
		}
		else if(touchAxis.y > 0.5f && IfInBetween(touchAxis.x))
		{
			if (posPercentage < 0.999f)
				posPercentage += 0.001f;
			//Debug.Log ("pad up");
		}
		else if(touchAxis.y < -0.5f && IfInBetween(touchAxis.x))
		{
			if (posPercentage > 0.2f)
				posPercentage -= 0.001f;
			//Debug.Log ("pad down");
		}*/

		//currTouchpadAxis = touchAxis;

		if (currArt==null)
			return;
		
		float distY = currTouchpadAxis.y - pastTouchpadAxis.y;
		float absDistY = Mathf.Abs (distY);

		if(absDistY > 0.01f)
		{
			if (distY > 0) {
				// go up
				if (posPercentage <= 0.9f)
				{
					posPercentage += 0.1f;
					Debug.Log ("go up");
				}
			} else {
				// go down
				if (posPercentage >= 0.2f)
				{
					posPercentage -= 0.1f;
					Debug.Log ("go down");
				}
			}
			currTouchpadAxis = pastTouchpadAxis = touchAxis;
		}
		else
		{
			currTouchpadAxis = touchAxis;
		}
	}

	public void OnPadUp(Vector2 touchAxis)
	{
		//
	}

	void Update()
	{
		if (grabSomething && currArt)
		{
			if(hittingWall)
				artTargetPos = ((artTargetPos - transform.position) * posPercentage) + transform.position;

			currArt.transform.position = Vector3.Lerp(currArt.transform.position, artTargetPos, 0.1f);
			currArt.transform.rotation = Quaternion.Slerp(currArt.transform.rotation, artTargetRot, 0.1f);
		}

		// reset, for OnPointerOn to set true
		hittingWall = false;
	}

	private void ScaleAroundPoint(GameObject target, Vector3 laserHit)
	{
		// compare current distance of two laser points, with the start distance, to stretch the object
		var pivot = target.transform.position;
		var mag = (laserHit - pivot).sqrMagnitude - initialControllersDistance;			
		var endScale = target.transform.localScale * (1f + mag*0.1f);

		// diff from obj pivot to desired pivot
		var diffP = target.transform.position - pivot;
		var finalPos = (diffP * (1f + mag*0.1f)) + pivot;

		target.transform.localScale = endScale;
		target.transform.position = finalPos;
	}

	//--------------------------------------------------------------
	private bool IfMightInBetween(float input)
	{
		return (input < 0.5f) || (input > -0.5f);
	}

	private bool IfInBetween(float input)
	{
		return (input < 0.5f) && (input > -0.5f);
	}
}
