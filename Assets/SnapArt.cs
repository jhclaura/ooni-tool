using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ooni_LaserPointer))]
public class SnapArt : MonoBehaviour
{
	public GameObject currArt;

	private GameObject prevArt;
	private Ooni_LaserPointer laserPointer;
	private Vector3 artTargetPos;
	private Quaternion artTargetRot = Quaternion.identity;

	private float posPercentage = 1f; // 1 ~ 0.2
	private Vector2 currTouchpadAxis;
	private Vector2 pastTouchpadAxis;

	private bool hittingWall = false;

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
		if (e.targetCollider.CompareTag ("Art") && currArt==null)
		{
			e.targetCollider.enabled = false;
			currArt = e.target.gameObject;

			if(prevArt!=currArt)
				posPercentage = 1f;
			
			Debug.Log ("pointer onto art");
		}
	}

	public void OnPointerOn(object sender, OoniPointerEventArgs e)
	{
		if (!e.targetCollider.CompareTag ("Art"))
		{
			artTargetPos = e.hitPoint;
			artTargetRot = e.target.localRotation;
			hittingWall = true;
		}
	}

	public void OnPointerOut()
	{
		if (currArt)
		{
			currArt.GetComponent<Collider>().enabled = true;
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

		if(absDistY > 0.02f)
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
		if (currArt)
		{
			if(hittingWall)
				artTargetPos = ((artTargetPos - transform.position) * posPercentage) + transform.position;

			currArt.transform.position = Vector3.Lerp(currArt.transform.position, artTargetPos, 0.1f);
			currArt.transform.rotation = Quaternion.Slerp(currArt.transform.rotation, artTargetRot, 0.1f);
		}
		hittingWall = false;
	}

	//=====================================================================
	private bool IfMightInBetween(float input)
	{
		return (input < 0.5f) || (input > -0.5f);
	}

	private bool IfInBetween(float input)
	{
		return (input < 0.5f) && (input > -0.5f);
	}
}
