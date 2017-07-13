using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ToolHub : MonoBehaviour {

	public event Action<bool> OnTouchpadClick;
	public event Action<bool> OnTouchpadClickCenter;

	public SteamVR_TrackedController controller;
	public SteamVR_Controller.Device Device
	{
		get { return SteamVR_Controller.Input ((int)controller.controllerIndex); }
	}
	public float rotDegreePerStep = 5f;

	private bool isTouching = false;
	private List<GameObject> toolObjects = new List<GameObject> ();
	private Vector2 currTouchpadAxis;
	private Vector2 pastTouchpadAxis;
	private float pastTouchValue;

	private List<float> toolRotateZones = new List<float> ();
	private int toolLayer;
	public int currToolIndex = -1;
	private int toolIndexCount = -1;
	private List<StickerTool> stickerTools = new List<StickerTool> ();
	private StickerTool currStickerTool;

	[Header("Display")]
	public Transform touchBall;
	public Renderer[] touchArrows;
	public Transform widthBar;
	private Vector2 currTouchpadAxis2;
	private Vector2 pastTouchpadAxis2;

	private List<Material> touchArrowOriMats = new List<Material> ();
	private Renderer touchBallRenderer;
	private Vector2 currTouchBallAxis;

	private int clickRotateTweenId;
	private float rotTargetByClick;

	// for macbook touchpad simulating vive controller
	private bool touchStop = true;
	private float eachR;
	private bool inRotating = false;

	private bool m_enable = true;
	public bool ToolsetEnable
	{
		get { return m_enable; }
		set { m_enable = value; }
	}

	private float ori_StrokeWidth = 1f;
	private float minStrokeScale = 0.1f;
	private float maxStrokeScale = 10;
	private float touchpadRawStrokeWidth = 1f;
	public float TouchPadDrawWidth
	{
		get { 
			if (widthBar)
				return ori_StrokeWidth * touchpadRawStrokeWidth;
			else
				return 1f;
		}
	}

	//============================================================
	void Start()
	{
		eachR = 360f / transform.childCount;
		for(int i=0; i<transform.childCount; i++)
		{
			var _t = transform.GetChild (i).gameObject;
			toolObjects.Add (_t);
			toolRotateZones.Add (eachR*i);

			var s_t = _t.GetComponent<StickerTool> ();
			s_t.ToolIndex = i;
			s_t.IdealAngle = eachR * i;
			stickerTools.Add (s_t);
		}

		toolLayer = 1 << 10;

		for(int i=0; i<touchArrows.Length; i++)
		{
			touchArrowOriMats.Add (touchArrows [i].material);
		}
		touchBallRenderer = touchBall.gameObject.GetComponent<Renderer> ();
		touchBallRenderer.enabled = false;

		CheckRaycast();
	}

	void OnEnable()
	{
		if (controller != null)
		{
			controller.PadClicked += OnClick;

			controller.PadTouched += OnTouch;
			controller.PadUntouched += OnTouchOut;
			controller.PadTouching += OnTouching;
		}
	}

	void OnDisable()
	{
		if (controller != null)
		{
			controller.PadClicked -= OnClick;

			controller.PadTouched -= OnTouch;
			controller.PadUntouched -= OnTouchOut;
			controller.PadTouching -= OnTouching;
		}
	}

	void Update()
	{
		if(controller==null)
		{
			float moveHorizontal = Input.GetAxis ("Mouse X");
			ToolSwiping (moveHorizontal);

			if (moveHorizontal == 0 && !touchStop) {
				touchStop = true;
				OnTouchStop ();
			} else if (moveHorizontal != 0 && touchStop) {
				touchStop = false;
			}
		}
	}

	//============================================================
	public void OnTouch (object sender, ClickedEventArgs e)
	{
		if (!ToolsetEnable)
			return;
		
		isTouching = true;
		currTouchpadAxis = pastTouchpadAxis = GetTouchpadAxis ();
		currTouchpadAxis2 = pastTouchpadAxis2 = GetTouchpadAxis ();
		DeviceVibrate ();

		currTouchBallAxis = GetTouchpadAxis ();
		touchBall.localPosition = new Vector3 (
			currTouchBallAxis.x * 0.018f,
			currTouchBallAxis.y * 0.018f,
			0
		);
		touchBallRenderer.enabled = true;
	}

	public void OnTouchOut (object sender, ClickedEventArgs e)
	{
		if (!ToolsetEnable)
			return;
		
		isTouching = false;

		// snap to the closest tool
		if (currStickerTool != null)
		{
			currStickerTool.TurnToIdealAngle(transform.localEulerAngles.z);
		}

		touchArrows [0].material.color = Color.white;
		touchArrows [1].material.color = Color.white;
		touchArrows [2].material.color = Color.white;
		touchArrows [3].material.color = Color.white;

		touchBallRenderer.enabled = false;
	}

	public void OnTouching (object sender, ClickedEventArgs e)
	{
		if (!ToolsetEnable)
			return;

		// TouchPad Disply
		// Touch Ball - x: -0.018 ~ 0.018, y: -0.018 ~ 0.018
		currTouchBallAxis = GetTouchpadAxis ();
		touchBall.localPosition = new Vector3 (currTouchBallAxis.x * 0.018f, 0, currTouchBallAxis.y * 0.018f);
		if (currTouchBallAxis.y > 0.5f && IfInBetween (currTouchBallAxis.x)) {
			touchArrows [0].material.color = Color.Lerp (touchArrows [0].material.color, Color.green, 0.1f);
			//Debug.Log ("pad up");

			touchArrows [1].material.color = Color.white;
			touchArrows [2].material.color = Color.white;
			touchArrows [3].material.color = Color.white;
		} else if (currTouchBallAxis.y < -0.5f && IfInBetween (currTouchBallAxis.x)) {
			touchArrows [1].material.color = Color.Lerp (touchArrows [1].material.color, Color.green, 0.1f);
			//Debug.Log ("pad down");

			touchArrows [0].material.color = Color.white;
			touchArrows [2].material.color = Color.white;
			touchArrows [3].material.color = Color.white;
		} else if (currTouchBallAxis.x < -0.5f && IfInBetween (currTouchBallAxis.y)) {
			touchArrows [2].material.color = Color.Lerp (touchArrows [2].material.color, Color.green, 0.1f);
			//Debug.Log ("pad left");

			touchArrows [1].material.color = Color.white;
			touchArrows [0].material.color = Color.white;
			touchArrows [3].material.color = Color.white;
		} else if (currTouchBallAxis.x > 0.5f && IfInBetween (currTouchBallAxis.y)) {
			touchArrows [3].material.color = Color.Lerp (touchArrows [3].material.color, Color.green, 0.1f);
			//Debug.Log ("pad right");

			touchArrows [1].material.color = Color.white;
			touchArrows [2].material.color = Color.white;
			touchArrows [0].material.color = Color.white;
		} else {
			touchArrows [0].material.color = Color.white;
			touchArrows [1].material.color = Color.white;
			touchArrows [2].material.color = Color.white;
			touchArrows [3].material.color = Color.white;
		}

		// Stroke Width
		if (widthBar)
		{
			float distY = currTouchpadAxis2.y - pastTouchpadAxis2.y;
			float absDistY = Mathf.Abs (distY);

			if(absDistY > 0.1f)
			{
				if (distY > 0) {
					// go up
					touchpadRawStrokeWidth *= 1.1f;
				} else {
					// go down
					touchpadRawStrokeWidth /= 1.1f;
				}
				touchpadRawStrokeWidth = Mathf.Clamp (touchpadRawStrokeWidth, minStrokeScale, maxStrokeScale);
				float normalizedWidth = touchpadRawStrokeWidth * (20f / 99f) - (101f / 99f);
				widthBar.localPosition = new Vector3 (
					0,
					0,
					normalizedWidth*0.018f
				);
				pastTouchpadAxis2 = currTouchpadAxis2 = GetTouchpadAxis ();
			}
			else
			{
				currTouchpadAxis2 = GetTouchpadAxis ();
			}
		}

		//
		if (LeanTween.isTweening(gameObject)) {
			return;
		}
		
		//steps on X-Axis: -1~1 break down into 10 steps, each step: 0.2f
		float dist = currTouchpadAxis.x - pastTouchpadAxis.x;
		float absDist = Mathf.Abs (dist);

		// Swiping => nah
		/*
		if (absDist > 0.4f)
		{
			if(dist > 0) {
				toolIndexCount--;
			} else {
				toolIndexCount++;
			}

			if (toolIndexCount < 0)
				toolIndexCount = stickerTools.Count - 1;
			else if (toolIndexCount >= stickerTools.Count)
				toolIndexCount = 0;
			
			SnapToTargetAngleAction(toolIndexCount, 0.3f);
			inRotating = true;

			pastTouchpadAxis = currTouchpadAxis = GetTouchpadAxis ();
			DeviceVibrate ();
		}*/

		// Wait until dist is accumulated to 0.1f
		if (absDist > 0.1f) {// && absDist < 0.2f) {
			if (dist > 0) {
				// swipe right
				transform.Rotate (-Vector3.forward * rotDegreePerStep);
			} else {
				// swipe left
				transform.Rotate (Vector3.forward * rotDegreePerStep);
			}
			pastTouchpadAxis = currTouchpadAxis = GetTouchpadAxis ();
			DeviceVibrate ();
//			Debug.Log ("rotate! : " + absDist);

			// Raycasting to detect which tool is showing up
			CheckRaycast();
		}
		// if not, then wait until it's accumulated to 0.2f
		else {
//			Debug.Log ("else dist : " + absDist);
			currTouchpadAxis = GetTouchpadAxis ();
		}
	}

	public void OnClick (object sender, ClickedEventArgs e)
	{
		if (LeanTween.isTweening(gameObject)) {
			Debug.Log ("in tweening");
			return;
		}

		if (!ToolsetEnable) {
			Debug.Log ("Toolset Enable");
			return;
		}
		
		Vector2 currTouchpadAxis = GetTouchpadAxis ();
		//Debug.Log ("x: " + currTouchpadAxis.x + ", y: " + currTouchpadAxis.y);

		if(IfInBetween(currTouchpadAxis.y) && IfInBetween(currTouchpadAxis.x))
		{
			if (OnTouchpadClickCenter != null)
				OnTouchpadClickCenter (true);
			//Debug.Log ("pad center");
		}
		else if(currTouchpadAxis.y > 0.5f && IfMightInBetween(currTouchpadAxis.x))
		{
			if (OnTouchpadClick != null)
				OnTouchpadClick (true);
			touchArrows [0].material.color = Color.red;
			//Debug.Log ("pad up");
		}
		else if(currTouchpadAxis.y < -0.5f && IfMightInBetween(currTouchpadAxis.x))
		{
			if (OnTouchpadClick != null)
				OnTouchpadClick (false);
			touchArrows [1].material.color = Color.red;
			//Debug.Log ("pad down");
		}
		else if(currTouchpadAxis.x > 0.5f && IfMightInBetween(currTouchpadAxis.y))
		{
			SnapToAngleAction(-eachR, 0.3f);
			touchArrows [3].material.color = Color.red;
			//Debug.Log ("pad right");
		}
		else if(currTouchpadAxis.x < -0.5f && IfMightInBetween(currTouchpadAxis.y))
		{
			SnapToAngleAction(eachR, 0.3f);
			touchArrows [2].material.color = Color.red;
			//Debug.Log ("pad left");
		}

		DeviceVibrate ();
	}

	public void OnClickEnd (object sender, ClickedEventArgs e)
	{
		//
	}

	//===========================================================================
	/// <summary>
	/// Touch Stop event of Touchpad on Macbook. For testing only.
	/// </summary>
	public void OnTouchStop ()
	{
		// snap to the closest tool
		if (currStickerTool != null)
		{
			currStickerTool.TurnToIdealAngle(transform.localEulerAngles.z);
		}

		// enable the function
	}

	/// <summary>
	/// Tool Swiping of Touchpad on Macbook. For testing only.
	/// </summary>
	/// <param name="currTouchValue">Curr touch value.</param>
	void ToolSwiping(float currTouchValue)
	{
		if( Mathf.Abs(currTouchValue) > 0.15f )
		{
			if(currTouchValue>0)
			{
				// swipe right
				transform.Rotate(transform.forward * rotDegreePerStep);
			}
			else
			{
				// swipe left
				transform.Rotate(-transform.forward * rotDegreePerStep);
			}
			pastTouchValue = currTouchValue;

			// Raycasting to detect which tool to show up
			RaycastHit hit;
			if (Physics.Raycast(transform.position, transform.parent.up, out hit, 5f, toolLayer))
			{
				var s_t = hit.transform.gameObject.GetComponent<StickerTool> ();
				if(!s_t.inUse)
				{					
					// disable
					for(int i=0; i<stickerTools.Count; i++)
					{
						if (i != s_t.ToolIndex && stickerTools[i].inUse)
						{
							stickerTools [i].DisableTool ();
						}
					}
					// enable
					s_t.EnableTool ();
					currStickerTool = s_t;
					currToolIndex = toolIndexCount = s_t.ToolIndex;
				}
			}
		}
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

	public void SnapToAngle(float angle, float time)
	{
		LeanTween.rotateAroundLocal ( gameObject, Vector3.forward, angle, time );
	}

	public void SnapToAngleAction(float angle, float time)
	{
		LeanTween.rotateAroundLocal ( gameObject, Vector3.forward, angle, time ).setOnComplete(CheckRaycast).setEaseInOutBack();
	}

	public void SnapToTargetAngleAction(int targetToolIndex, float time)
	{
		float c_angle = (360f + transform.localEulerAngles.z) % 360f;
		float t_angle = stickerTools [targetToolIndex].IdealAngle - c_angle;
		LeanTween.rotateAroundLocal ( gameObject, Vector3.forward, t_angle, time ).setOnComplete(CheckRaycast).setEaseInOutBack();
	}

	void CheckRaycastUpdate(float _val)
	{
		// Raycasting to detect which tool is showing up
		RaycastHit hit;
		if (Physics.Raycast(transform.position, transform.parent.up, out hit, 10f, toolLayer))
		{
			Debug.DrawRay (transform.position, transform.parent.up);
			StickerTool s_t = hit.collider.gameObject.GetComponent<StickerTool> ();
			if(!s_t.inUse)
			{
				// disable
				for(int i=0; i<stickerTools.Count; i++)
				{
					if (i != s_t.ToolIndex && stickerTools[i].inUse)
					{
						stickerTools [i].DisableTool ();
					}
				}

				// enable
				s_t.EnableTool ();
				currStickerTool = s_t;
				currToolIndex = toolIndexCount = s_t.ToolIndex;
			}
		}
	}

	void CheckRaycast()
	{
		// Raycasting to detect which tool is showing up
		RaycastHit hit;
		if (Physics.Raycast(transform.position, transform.parent.up, out hit, 10f, toolLayer))
		{
			StickerTool s_t = hit.collider.gameObject.GetComponent<StickerTool> ();
			if(!s_t.inUse)
			{
				// disable
				for(int i=0; i<stickerTools.Count; i++)
				{
					if (i != s_t.ToolIndex && stickerTools[i].inUse)
					{
						stickerTools [i].DisableTool ();
					}
				}

				// enable
				s_t.EnableTool ();
				currStickerTool = s_t;
				currToolIndex = toolIndexCount = s_t.ToolIndex;
			}
		}
	}

	public void DisableAllTools()
	{
		for(int i=0; i<stickerTools.Count; i++)
		{
			stickerTools [i].DisableTool ();
		}
		ToolsetEnable = false;
		currStickerTool = null;
		currToolIndex = toolIndexCount = -1;
	}

	public void EnableAllTools()
	{
		ToolsetEnable = true;
	}

	private bool IfMightInBetween(float input)
	{
		return (input < 0.5f) || (input > -0.5f);
	}

	private bool IfInBetween(float input)
	{
		return (input < 0.5f) && (input > -0.5f);
	}
}
