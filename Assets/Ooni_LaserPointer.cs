//Reference: SteamVR_LaserPointer.cs
using UnityEngine;
using System.Collections;
using System;

public struct OoniPointerEventArgs
{
	public uint controllerIndex;
	public uint flags;
	public float distance;
	public Transform target;
	public Collider targetCollider;
	public Vector3 hitPoint;
}

public delegate void OoniPointerEventHandler(object sender, OoniPointerEventArgs e);

[RequireComponent(typeof(SteamVR_TrackedController))]
public class Ooni_LaserPointer : MonoBehaviour
{
	public Color color;
	public Material pointerMaterial;
	public float thickness = 0.002f;
	public GameObject holder;
	public GameObject pointer;
	public bool addRigidBody = false;
	//public GameObject testDot;

	public event OoniPointerEventHandler PointerIn;
	public event OoniPointerEventHandler PointerOut;
	public event OoniPointerEventHandler PointerOn;

	public event Action PointerOutWithTrigger;	// pointerOut due to triggerUp

	public event Action<Vector2> PadTouching;
	public event Action<Vector2> PadDown;
	public event Action<Vector2> PadUp;

	private bool isActive = false;
	private Transform previousContact = null;
	private SteamVR_TrackedController controller;
	private int wallLayer;
	private int noArtLayer;

	private bool m_onToArt;
	public bool OnToArt
	{
		get { return m_onToArt; }
		set { m_onToArt = value; }
	}

	private bool m_isTeleporting;
	public bool LaserTeleporting
	{
		get { return m_isTeleporting; }
		set { m_isTeleporting = value; }
	}

	public SteamVR_Controller.Device Device
	{
		get { return SteamVR_Controller.Input ((int)controller.controllerIndex); }
	}

	void OnEnable()
	{
		if (controller == null)
			controller = GetComponent<SteamVR_TrackedController> ();

		controller.PadClicked += HandleTriggerDown;
		controller.PadUnclicked += HandleTriggerUp;
		controller.PadTouching += HandleTriggerTouch;
	}

	void OnDisable()
	{
		controller.PadClicked -= HandleTriggerDown;
		controller.PadUnclicked -= HandleTriggerUp;
		controller.PadTouching -= HandleTriggerTouch;
	}

	void Start ()
	{
		holder = new GameObject();
		holder.name = "PointerHolder";
		holder.transform.parent = this.transform;
		holder.transform.localPosition = Vector3.zero;
		holder.transform.localRotation = Quaternion.identity;

		pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
		pointer.name = "Pointer";
		pointer.transform.parent = holder.transform;
		pointer.transform.localScale = new Vector3(thickness, thickness, 100f);
		pointer.transform.localPosition = new Vector3(0f, 0f, 50f);
		pointer.transform.localRotation = Quaternion.identity;
		BoxCollider collider = pointer.GetComponent<BoxCollider>();
		if (addRigidBody)
		{
			if (collider)
			{
				collider.isTrigger = true;
			}
			Rigidbody rigidBody = pointer.AddComponent<Rigidbody>();
			rigidBody.isKinematic = true;
		}
		else
		{
			if(collider)
			{
				UnityEngine.Object.Destroy(collider);
			}
		}

		if (pointerMaterial == null)
		{
			pointerMaterial = new Material (Shader.Find ("Unlit/Color"));
			pointerMaterial.SetColor ("_Color", color);
		}
		pointer.GetComponent<MeshRenderer>().material = pointerMaterial;

		wallLayer = 1 << 8;
		noArtLayer = 1 << 9;
		noArtLayer = ~noArtLayer;

		holder.SetActive (false);
	}

	public virtual void OnPointerIn(OoniPointerEventArgs e)
	{
		if (PointerIn != null)
			PointerIn(this, e);
	}

	public virtual void OnPointerOut(OoniPointerEventArgs e)
	{
		if (PointerOut != null)
			PointerOut(this, e);
	}

	public virtual void OnPointerOn(OoniPointerEventArgs e)
	{
		if (PointerOn != null)
			PointerOn(this, e);
	}

	public virtual void OnPointerOutByTriggerUp()
	{
		if (PointerOutWithTrigger != null)
			PointerOutWithTrigger();
	}

	void Update ()
	{
		if (!isActive)
			return;

		if (m_isTeleporting)
			return;
		
		float dist = 100f;

		Ray raycast = new Ray(transform.position, transform.forward);
		RaycastHit hit;
		bool bHit;

		if(OnToArt)
			bHit = Physics.Raycast(raycast, out hit, dist, noArtLayer);
		else
			bHit = Physics.Raycast(raycast, out hit);

		// Pointer Out
		if(previousContact && previousContact != hit.transform)
		{
			OoniPointerEventArgs args = new OoniPointerEventArgs();
			args.controllerIndex = controller.controllerIndex;
			args.distance = 0f;
			args.flags = 0;
			args.target = previousContact;
			args.targetCollider = previousContact.GetComponent<Collider>();
			args.hitPoint = hit.point;
			OnPointerOut(args);

			// update
			previousContact = null;
		}

		// Pointer In
		if(bHit && previousContact != hit.transform)
		{
			OoniPointerEventArgs argsIn = new OoniPointerEventArgs();
			argsIn.controllerIndex = controller.controllerIndex;
			argsIn.distance = hit.distance;
			argsIn.flags = 0;
			argsIn.target = hit.transform;
			argsIn.hitPoint = hit.point;
			argsIn.targetCollider = hit.collider;
			OnPointerIn(argsIn);

			// update
			previousContact = hit.transform;
		}
		// Pointer On
		else if(bHit && previousContact == hit.transform)
		{
			OoniPointerEventArgs argsOn = new OoniPointerEventArgs();
			argsOn.controllerIndex = controller.controllerIndex;
			argsOn.distance = hit.distance;
			argsOn.flags = 0;
			argsOn.target = hit.transform;
			argsOn.hitPoint = hit.point;
			argsOn.targetCollider = hit.collider;
			OnPointerOn(argsOn);

			// update
//			if(testDot)
//				testDot.transform.position = hit.point;
			
			previousContact = hit.transform;
		}

		if(!bHit)
		{
			previousContact = null;
		}
		if (bHit && hit.distance < 100f)
		{
			dist = hit.distance;
		}

//		if (controller.triggerPressed)
//		{
//			pointer.transform.localScale = new Vector3(thickness * 5f, thickness * 5f, dist);
//		}
//		else
//		{
//			pointer.transform.localScale = new Vector3(thickness, thickness, dist);
//		}

		pointer.transform.localScale = new Vector3(thickness, thickness, dist);
		pointer.transform.localPosition = new Vector3(0f, 0f, dist/2f);
	}

	public void HandleTriggerDown(object sender, ClickedEventArgs e)
	{
		if (m_isTeleporting)
			return;

		isActive = true;
		holder.SetActive(true);

		if (PadDown != null)
			PadDown ( GetTouchpadAxis() );

	}

	public void HandleTriggerTouch(object sender, ClickedEventArgs e)
	{
		if (m_isTeleporting)
			return;
		
		if (PadTouching != null)
			PadTouching ( GetTouchpadAxis() );
	}

	public void HandleTriggerUp(object sender, ClickedEventArgs e)
	{
		if (m_isTeleporting)
			return;
		
		if (PadUp != null)
			PadUp ( GetTouchpadAxis() );
		
		// reset
		if(previousContact)
		{
			OoniPointerEventArgs args = new OoniPointerEventArgs();
			args.controllerIndex = controller.controllerIndex;
			args.distance = 0f;
			args.flags = 0;
			args.target = previousContact;
			args.targetCollider = previousContact.GetComponent<Collider>();

			OnPointerOut(args);

			// update
			previousContact = null;
		}

		OnPointerOutByTriggerUp();

		isActive = false;
		holder.SetActive(false);
	}

	//===========================================================================
	public Vector2 GetTouchpadAxis()
	{
		var device = SteamVR_Controller.Input((int)controller.controllerIndex);
		return device.GetAxis();
	}

	public void DeviceVibrate()
	{
		Device.TriggerHapticPulse (1000);
	}
}
