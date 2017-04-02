﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowController : MonoBehaviour {

	// NOTE: project settings -> physics, the Shadow layer only collides with the Map layer
	// This object's z-coordinate determines the drop shadow sprite y-offset relative to its user sprite

	private Rigidbody2D 	parentRB;
	private Rigidbody 		rb3D;
	private BoxCollider 	boxCollider;
	private float 			offsetSlope = 0.0f;
	private float 			startPosX;
	private float 			colliderHeight;

	void Awake () { 
		rb3D = GetComponent<Rigidbody> ();
		boxCollider = GetComponent<BoxCollider> ();
		rb3D.isKinematic = true;

		// move the box origin/center to slightly above the plane so it can properly collide with the plane
		colliderHeight = boxCollider.size.z * 0.5f;
		rb3D.transform.position += Vector3.forward * colliderHeight;
		parentRB = GetComponentInParent<Rigidbody2D> ();
	}

	// for thrown boxes use velocity == Vector3.forward * parentRB.velocity.y
	public void SetVelocity(Vector3 velocity) {	
		rb3D.velocity = velocity;	
	}

	public void SetHeight (float height) {
		rb3D.transform.position = Vector3.forward * height + (Vector3.forward * colliderHeight);	
	}

	public void SetKinematic(bool isKinematic) {
		rb3D.isKinematic = isKinematic;
		startPosX = rb3D.transform.position.x;
	}

	public bool IsKinematic() {
		return rb3D.isKinematic;
	}

	public bool IsFalling () {
		return rb3D.velocity.z < 0.0f;
	}

	public bool grounded {
		get { 
			return GetShadowOffset() < 0.0f;
		}
		set {
			rb3D.velocity = value ? Vector3.zero : rb3D.velocity;
			rb3D.transform.position = value ? new Vector3(rb3D.transform.position.x, rb3D.transform.position.y, 0.0f) : rb3D.transform.position;
			rb3D.isKinematic = value;
			offsetSlope = 0.0f;
			startPosX = rb3D.transform.position.x;
		}
	}

	// height off the ground that determines the dropShadow sprite y-offset from its user sprite
	public float GetShadowOffset() {
		float offsetModifier = offsetSlope * (rb3D.transform.position.x - startPosX);
		return (rb3D.transform.position.z - colliderHeight) - offsetModifier;
	}
		
	public void SetTrajectory(float airTime) {

		// predicted landing point
		float xFinal = rb3D.transform.position.x + (parentRB.velocity.x * airTime);
		float yFinal = rb3D.transform.position.y + (parentRB.velocity.y * airTime) + (0.5f * Physics2D.gravity.y * airTime * airTime);

		// ground movement of the drop shadow
		Vector3 offsetDir = new Vector3 (xFinal, yFinal) - rb3D.transform.position;
		if (offsetDir.x == 0.0f)
			offsetDir.x = float.Epsilon;
		else
			offsetSlope = offsetDir.y / offsetDir.x;
	}
}