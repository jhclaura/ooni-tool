/// <summary>
/// Vive interactive object, referece: Unity's VR
/// Should be added to gameObject in the scene
/// It contains events that can be subscribed to by classes that
/// need to know about input specifics to this gameobject
/// </summary>
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRInteractiveObject : MonoBehaviour {

	public event Action<GameObject> OnOver;
	public event Action<GameObject> OnOut;
	public event Action<GameObject> OnClick;
	public event Action<GameObject> OnDown;
	public event Action<GameObject> OnUp;
	public event Action<GameObject> OnTouch;
	public event Action<GameObject> OnPadDown;
	// Laser
	public event Action<Ooni_LaserPointer> OnLaserDown;
	public event Action<Ooni_LaserPointer> OnLaserUp;

	public bool usePhysics = false;
	//public GameObject scaleTarget;
	[HideInInspector]
	public GameObject theThingGrabMe = null;
	[HideInInspector]
	public Vector3 grabbedPoint;

	private Rigidbody grabber;
	private Rigidbody m_rigidbody;
	private List<GameObject> touchingObjects = new List<GameObject>();
	private FixedJoint grabJoint;

	protected bool m_IsGrabbing = false;
	protected bool m_IsTouching = false;
	private bool m_IsShooting = false;
	private bool m_IsSpringing = false;
	private bool m_IsHammered = false;
	private bool m_IsDropping = false;
	// Laser
	private bool m_IsLasering = false;
	private Ooni_LaserPointer currLaser;

	private bool readyToDie = false;
	private float m_tapeWidth = 0f;

	private bool useRigidbody = false;
	private bool rigidbodyIsKinematic = false;

	private PhysicMaterial artPhyMat;
	private ParticleSystem particleEffect;

	public bool IsTouching
	{
		get { return m_IsTouching; }		// Is the controller currently over this object?
	}

	public bool IsGrabbing
	{
		get { return m_IsGrabbing; }
	}

	public bool IsShooting
	{
		get { return m_IsShooting; }
		set { m_IsShooting = value; }
	}

	public bool IsSpringing
	{
		get { return m_IsSpringing; }
		set { m_IsSpringing = value; }
	}

	public bool IsHammered
	{
		get { return m_IsHammered; }
		set { m_IsHammered = value; }
	}

	public bool IsLasering
	{
		get { return m_IsLasering; }
		set { m_IsLasering = value; }
	}

	public Vector3 GrabbedPos
	{
		get { return grabber.position; }
	}

	public Rigidbody Grabber
	{
		get { return grabber; }
	}

	public FixedJoint Joint
	{
		get { return grabJoint; }
		set { grabJoint = value; }
	}

	public bool IsKinematic
	{
		get {
			if (TheRigidbody)
				return TheRigidbody.isKinematic;
			else
				return false;
		}
		set {
			if (TheRigidbody)
				TheRigidbody.isKinematic = value;
		}
	}

	public bool HasRigidbody
	{
		get { return useRigidbody; }
	}

	public Rigidbody TheRigidbody
	{
		get { 
			if (m_rigidbody == null) {
				m_rigidbody = GetComponent<Rigidbody> ();
			}
			return m_rigidbody;
		}
	}
	private float _mass = 2f;

	public float Mass
	{
		get { return _mass; }
		set { _mass = value; }
	}

	public ParticleSystem Particles
	{
		get { return particleEffect; }
		set { particleEffect = value; }
	}

	public float TapeWidth
	{
		get { return m_tapeWidth; }
		set { m_tapeWidth = value; }
	}

	///------------------------------------------------------------------
	/// FUNCTIONSSS
	///------------------------------------------------------------------
	// void Awake???
	void Start() {
		m_rigidbody = GetComponent<Rigidbody> ();

		if (usePhysics && m_rigidbody==null)
		{
			// Get the material
			artPhyMat = GameObject.Instantiate(
				Resources.Load("Materials/artPhyMat", typeof(PhysicMaterial)) as PhysicMaterial
			) as PhysicMaterial;
			m_rigidbody = gameObject.AddComponent<Rigidbody> ();
			m_rigidbody.mass = Mass;
			m_rigidbody.drag = 0.01f;
			m_rigidbody.angularDrag = 0.05f;
			if(GetComponent<Collider> ())
			{
				GetComponent<Collider> ().material = artPhyMat;
			}
			else
			{
				GetComponentInChildren<Collider> ().material = artPhyMat;
			}
		}

		if (m_rigidbody)
		{
			useRigidbody = true;

			if (m_rigidbody.isKinematic)
				rigidbodyIsKinematic = true;
		}
	}

	void OnCollisionEnter(Collision collision)
	{
		if (collision.collider.CompareTag("Wall") || collision.collider.CompareTag("Thing"))
		{
			if (m_IsShooting)
			{
				//v.1
				// AddSpring (collision.rigidbody);

				//v.2
				RemoveRigidbody();
				//usePhysics = false;
				useRigidbody = false;
				m_IsShooting = false;
				// Invoke ("RemoveRigidbody", 3f);
			}
			else if (m_IsSpringing)
			{
				Invoke ("RemoveSpring", 3f);
			}
			else if (m_IsDropping)
			{
				Invoke ("RemoveRigidbody", 2f);
				//usePhysics = false;
				useRigidbody = false;
				m_IsDropping = false;
			}
		}

		if (collision.collider.CompareTag ("Sticker"))
		{
			if (m_IsShooting || m_IsDropping)
			{
				Invoke ("RemoveRigidbody", 2f);
				//usePhysics = false;
				useRigidbody = false;
				m_IsDropping = false;
			}
		}

		if (IsHammered)
		{
			// Particle effect start
			//v.1
			/*
			var i_particle = Instantiate(Particles, collision.contacts[0].point, Quaternion.identity, transform);

			if (TapeWidth != 0)
			{
				//Mapping(float x, float in_min, float in_max, float out_min, float out_max)
				float newSize = Mapping(TapeWidth, 0.05f, 4f, 0.2f, 4f);
				i_particle.transform.localScale *= newSize;
			}

			i_particle.Play ();
			*/
			var emitParams = new ParticleSystem.EmitParams();
			if (TapeWidth != 0)
			{
				//Mapping(float x, float in_min, float in_max, float out_min, float out_max)
				float newSize = Mapping(TapeWidth, 0.05f, 4f, 0.01f, 0.35f);
				emitParams.startSize = newSize;
			}
			emitParams.position = collision.contacts [0].point;
			Particles.Emit(emitParams, 1);

			if (!readyToDie)
			{
				Invoke ("DoDestroy", 1f);
				readyToDie = true;
			}
		}
	}

	void DoDestroy()
	{
		Destroy (gameObject);
	}

	void RemoveSpring()
	{
		var s_joint = GetComponent<SpringJoint> ();
		Destroy (s_joint);
		Destroy (m_rigidbody);
	}

	void RemoveRigidbody()
	{
		if (m_rigidbody == null)
			return;
		
		if (grabJoint)
			RemoveJoint ();

		m_rigidbody.isKinematic = true;
		Destroy (m_rigidbody);
		RemovePhyMat ();
	}

	public Rigidbody AddRigidbody()
	{
		AssignPhyMat ();

		//usePhysics = true;
		useRigidbody = true;
		m_IsDropping = true;

		if (!m_rigidbody)
			m_rigidbody = gameObject.AddComponent<Rigidbody> ();

		return m_rigidbody;
	}

	void AssignPhyMat()
	{
		var b_c = GetComponent<BoxCollider> ();
		var m_c = GetComponent<MeshCollider> ();
		if (b_c) {
			b_c.material = artPhyMat;
		} else {
			m_c.convex = true;
			m_c.material = artPhyMat;
		}
	}

	void RemovePhyMat()
	{
		var b_c = GetComponent<BoxCollider> ();
		var m_c = GetComponent<MeshCollider> ();
		if (b_c) {
			b_c.material = null;
		} else {
			m_c.material = null;
			m_c.convex = false;
		}
	}

	#region Controller Events
	// Functions are called by the controller when the physical input is detected
	// Functions in turn call the appropriate events should they have subscribers
	public void StartTouching(GameObject touchingObj)
	{
		if(!touchingObjects.Contains(touchingObj))
		{
			touchingObjects.Add (touchingObj);
		}

		m_IsTouching = true;

		if (OnOver != null)
			OnOver (touchingObj);
	}

	public void StopTouching(GameObject touchingObj)
	{
		if(touchingObjects.Contains(touchingObj))
		{
			touchingObjects.Remove (touchingObj);
		}

		if (touchingObjects.Count == 0)
		{
			m_IsTouching = false;
		}

		if (OnOut != null)
			OnOut (touchingObj);
	}

	public void Click(GameObject grabbingObj)
	{
		if (OnClick != null)
			OnClick (grabbingObj);
	}

	public void Down(GameObject grabbingObj)
	{
		if (m_IsGrabbing)
			return;

		theThingGrabMe = grabbingObj;
		grabber = theThingGrabMe.GetComponent<GrabnStretch> ().attachPoint;
		m_IsGrabbing = true;

		if (OnDown != null)
			OnDown (grabbingObj);
	}

	public void Up(GameObject grabbingObj)
	{
		theThingGrabMe = null;
		grabber = null;
		m_IsGrabbing = false;

		if (OnUp != null)
			OnUp (grabbingObj);
	}

	public void Touch(GameObject touchingObj)
	{
		if (OnTouch != null)
			OnTouch (touchingObj);
	}

	public void PadDown (GameObject touchingObj)
	{
		if (OnPadDown != null)
			OnPadDown (touchingObj);
	}

	public void LaserDown(Ooni_LaserPointer laser)
	{
		if (m_IsLasering)
			return;

		m_IsLasering = true;
		currLaser = laser;

		if (OnLaserDown != null)
			OnLaserDown (laser);
	}

	public void LaserUp(Ooni_LaserPointer laser)
	{
		m_IsLasering = false;
		currLaser = null;

		if (OnLaserUp != null)
			OnLaserUp (laser);
	}
	#endregion

	public Vector3 CurrentGrabbedPoint()
	{
		return theThingGrabMe.GetComponent<ViveSimpleController> ().attachPoint.position;
	}

	public void RemoveJoint()
	{
		UnityEngine.Object.Destroy (grabJoint);
		grabJoint = null;
	}

	public void AddJoint(Rigidbody connectBody)
	{
		if (grabJoint != null)
		{
			UnityEngine.Object.Destroy (grabJoint);
		}
		grabJoint =  gameObject.AddComponent<FixedJoint> ();
		grabJoint.connectedBody = connectBody;
	}

	public void AddSpring(Rigidbody connectBody)
	{
		var s_joint = gameObject.AddComponent<SpringJoint> ();
		s_joint.connectedBody = connectBody;
		s_joint.spring = 50f;
		s_joint.damper = 3f;
		s_joint.enableCollision = true;
	}

	public void AddSpringJoint(Rigidbody connectBody)
	{
		var s_joint = gameObject.AddComponent<SpringJoint> ();
		s_joint.connectedBody = connectBody;
		s_joint.autoConfigureConnectedAnchor = false;
		//var anchor = gameObject.transform.position - connectBody.position;
		s_joint.connectedAnchor = new Vector3();
		s_joint.spring = 50f;
		s_joint.damper = 3f;
		s_joint.enableCollision = true;
	}

	public void AddSpringJoint(Rigidbody connectBody, Vector3 anchor, float force)
	{
		var s_joint = gameObject.AddComponent<SpringJoint> ();
		s_joint.connectedBody = connectBody;
		s_joint.autoConfigureConnectedAnchor = false;
		s_joint.connectedAnchor = anchor;
		s_joint.spring = force;
		s_joint.damper = force/15f;
		s_joint.enableCollision = true;
	}

	public void PrepColliderForHammer()
	{
		MeshCollider m_c;
		if(m_c = GetComponent<MeshCollider>())
		{
			m_c.convex = true;
			//m_c.material = artPhyMat;
		}
	}

	public float Mapping(float x, float in_min, float in_max, float out_min, float out_max)
	{
		return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
	}
}
