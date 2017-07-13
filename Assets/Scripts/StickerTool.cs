using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StickerTool : MonoBehaviour {

	private int _toolIndex;
	public int ToolIndex
	{
		get { return _toolIndex; }
		set { _toolIndex = value; }
	}
	public bool inUse = false;
	public float sizeChangeSpeed = 0.1f;
	public event Action<bool, int> OnChangeToolStatus;

	public float _angle;
	public float IdealAngle
	{
		get { return _angle; }
		set { _angle = value; }
	}
	private ToolHub toolHub;
	private Vector3 oriSize = new Vector3(0.12f,0.12f,0.12f);
	private Vector3 smallSize = new Vector3(0.01f,0.01f,0.01f);

	void Start()
	{
		toolHub = GetComponentInParent<ToolHub> ();
	}

	public void EnableTool()
	{
		LeanTween.scale (transform.gameObject, oriSize, sizeChangeSpeed);//.setEaseInOutBack();
		inUse = true;

		if (OnChangeToolStatus != null)
			OnChangeToolStatus (inUse, ToolIndex);
	}

	public void DisableTool()
	{
		LeanTween.scale (gameObject, smallSize, sizeChangeSpeed);//.setEaseInOutBack();
		inUse = false;

		if (OnChangeToolStatus != null)
			OnChangeToolStatus (inUse, ToolIndex);
	}

	public void TurnToIdealAngle(float currAngle)
	{
		if (!inUse)
			return;
		
		float angleToTurn = IdealAngle - currAngle;
		if(angleToTurn>200f || angleToTurn<-200f)
			angleToTurn = 360f - currAngle;
		//Debug.Log ("currAngle: " + currAngle + ", IdealAngle: " + IdealAngle + ", angleToTurn: " + angleToTurn);

		toolHub.SnapToAngle(angleToTurn, sizeChangeSpeed*2);
	}
}
