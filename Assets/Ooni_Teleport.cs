using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Ooni_LaserPointer))]
public class Ooni_Teleport : MonoBehaviour {

	public GameObject dot;
	public Transform cameraEye;

	private Ooni_LaserPointer laserPointer;
	private bool teleportModeOn = false;
	private Transform cameraRig;

	void OnEnable()
	{
		if (laserPointer == null)
			laserPointer = GetComponent<Ooni_LaserPointer> ();

		laserPointer.PointerIn += OnPointerIn;
		laserPointer.PointerOn += OnPointerOn;
		laserPointer.PointerOut += OnPointerOut;

//		laserPointer.PadTouching += OnPadTouching;
//		laserPointer.PadDown += OnPadDown;
		laserPointer.PadUp += OnPadUp;

	}

	void OnDisable()
	{
		laserPointer.PointerIn -= OnPointerIn;
		laserPointer.PointerOn -= OnPointerOn;
		laserPointer.PointerOut -= OnPointerOut;

//		laserPointer.PadTouching -= OnPadTouching;
//		laserPointer.PadDown -= OnPadDown;
		laserPointer.PadUp -= OnPadUp;
	}

	void Start ()
	{
		if (dot.activeSelf)
			dot.SetActive (false);

		cameraRig = transform.parent;
	}

	// -------------------- Laser Pointer Callback --------------------
	public void OnPointerIn(object sender, OoniPointerEventArgs e)
	{
		if (laserPointer.OnToArt)
			return;

		// if other laser are doing teleporting
		if (dot.activeInHierarchy)
			return;
		
		if (e.targetCollider.CompareTag ("Floor"))
		{
			teleportModeOn = true;
			dot.transform.position = e.hitPoint;
			dot.SetActive (true);
			//Debug.Log ("teleport mode on");
		}
	}

	public void OnPointerOn(object sender, OoniPointerEventArgs e)
	{
		if (!teleportModeOn)
			return;

		if (e.targetCollider.CompareTag ("Floor"))
			dot.transform.position = e.hitPoint;
	}

	public void OnPointerOut(object sender, OoniPointerEventArgs e)
	{
		if (laserPointer.OnToArt)
			return;

		if (!teleportModeOn)
			return;

		if (e.targetCollider.CompareTag ("Floor"))
		{
			teleportModeOn = false;
			dot.SetActive (false);
			//Debug.Log ("teleport mode off");
		}
	}

	public void OnPadUp(Vector2 touchAxis)
	{
		if (!teleportModeOn)
			return;

		teleportModeOn = false;
		DoFadeTeleport ();
		Debug.Log ("teleport!");
	}

	public void DoFadeTeleport()
	{
		laserPointer.LaserTeleporting = true;
		SteamVR_Fade.Start(Color.grey, 0.3f);
		Invoke ("Teleport", 0.3f);
	}

	void Teleport()
	{
		Vector3 eyePos = cameraEye.localPosition;
		eyePos.y = 0;
		cameraRig.position = dot.transform.position - eyePos;

		dot.SetActive (false);

		SteamVR_Fade.Start(Color.clear, 0.3f);
		Invoke ("Reset", 0.3f);
	}

	void Reset()
	{
		laserPointer.LaserTeleporting = false;
	}
}
