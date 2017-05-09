﻿using UnityEngine;
using UnityEngine.UI;


public class RobotGrabber : MonoBehaviour {

	public static RobotGrabber 	instance = null;

	public LayerMask			grabbleMask;
	public LayerMask			grabbedRobotMask;
	public float				grabRadius = 10.0f;
	public float				mouseJointDistance = 0.1f;
	public float				touchJointDistance = 1.0f;
	public float				forceMultiplier = 2.0f;

	private Robot 				grabbedRobot;
	private Collider2D			grabbedRobotCollider;
    private DistanceJoint2D 	joint;
	private SpriteRenderer		spriteRenderer;
	private ParticleSystem 		glowParticles;
	private ParticleSystem		beamParticles;
	private Quaternion 			originalBeamRotation;
	private Vector3				dropForce;
	private bool				secondClickOnRobot = false;

	public Robot currentGrabbedRobot {
		get { 
			return grabbedRobot;
		}
	}

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	

		DontDestroyOnLoad(gameObject);
	}

	void Start () {
		spriteRenderer = GetComponent<SpriteRenderer> ();
		ParticleSystem[] particleChildren = GetComponentsInChildren<ParticleSystem> ();
		bool firstGlow = particleChildren [0].name == "GrabberGlow";
		glowParticles =  firstGlow ? particleChildren [0] : particleChildren [1];
		beamParticles = firstGlow ? particleChildren [1] : particleChildren [0];
		originalBeamRotation = beamParticles.transform.rotation;
	}

    void Update() {
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
		worldPosition.z = 0.0f;

		if (grabbedRobot != null && !beamParticles.isPlaying)
			beamParticles.Play ();

		// align the beam to the joint the robot is dangling from
		// FIXME: a cone emmiter has a different start rotation from the prior-used edge emmiter, and therefore needs a different rotation applied
		// to rotate about the global z (not its local z)
		if (joint != null) {
			dropForce = new Vector3(joint.connectedAnchor.x, joint.connectedAnchor.y) - grabbedRobot.transform.TransformPoint(new Vector3(joint.anchor.x, joint.anchor.y));
			Quaternion beamRotation = Quaternion.LookRotation(Vector3.forward, dropForce);
			beamParticles.transform.rotation = originalBeamRotation * Quaternion.Inverse(beamRotation);
		}
	
		#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER

		spriteRenderer.enabled = worldPosition.y < 7.0f || grabbedRobot != null;

		#elif UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE

		spriteRenderer.enabled = grabbedRobot != null;
		if (grabbedRobot != null)
			glowParticles.Play ();
		else
			glowParticles.Stop ();

		#endif

		bool updateGrabberPosition = (grabbedRobot == null || secondClickOnRobot);
		if (!Cursor.visible) {
			if (updateGrabberPosition)
				transform.position = worldPosition;		
			else
				transform.position = grabbedRobot.transform.position + Vector3.up * joint.distance;
		}

		// FIXME: magic number specific to the current y-position of the HUD interface
		Cursor.visible = worldPosition.y > 7.0f || !updateGrabberPosition;

		// first click LOCKS the robot on the ground (ie NOT HOVER via Robot.grabbed bool)
		// SET RobotGrabber SPRITE position DIRECTLY over grabbedRobot, and enable the cursor to manipulate the SLIDER, or draw a PATH

		// click and release, followed by second click and release logic:
		// if the user RELEASES the mouse the robot WILL UNLOCK and  GRAB to hover in place (remains the activly grabbedRobot)
		// if the user CLICKS AGAIN, start MOVING the RobotGrabber SPRITE again, and throw the robot via its joint as normal

		// click and drag, followed by single release logic:
		// if the user instead HOLDS AND DRAGS the mouse, then the robot STAYS LOCKED and starts getting a NEW PATH 
		// if the user RELEASES the mouse, UNLOCK the robot to follow the new path, and start MOVING the RobotGrabber SPRITE again as normal

		// input press
		if (Input.GetMouseButtonDown (0) && worldPosition.y < 7.0f) {		// BUGFIX: for clicking sprinkler button above a door and accidentally grabbing a robot
			if (joint == null) {

				// find the closest robot within the given radius of the click, if any
				grabbedRobot = null;
				int closestHitIndex = -1;
				Collider2D[] hits = Physics2D.OverlapCircleAll (worldPosition, grabRadius, grabbleMask);
				float closestRobotRangeSqr = float.MaxValue;

				for (int i = 0; i < hits.Length; i++) {
					if (hits [i].tag == "Robot") {
						float rangeSqr = (hits [i].transform.position - worldPosition).sqrMagnitude;
						if (rangeSqr < closestRobotRangeSqr) {
							closestRobotRangeSqr = rangeSqr;
							closestHitIndex = i;
						}
					}
				}

				if (closestHitIndex == -1)
					return;

				grabbedRobotCollider = hits [closestHitIndex];
				grabbedRobot = grabbedRobotCollider.GetComponent<Robot> ();
				grabbedRobot.lockedByPlayer = true;
				grabbedRobot.ClearDrawnPath ();
				grabbedRobot.SetTargeter (null);
				grabbedRobot.PlaySingleSoundFx (grabbedRobot.playerGrabbedSound);

				// create a joint on the robot sprite
				joint = grabbedRobotCollider.gameObject.AddComponent<DistanceJoint2D> ();
				joint.autoConfigureConnectedAnchor = false;
				joint.autoConfigureDistance = false;
				joint.enableCollision = true;

				#if UNITY_IOS || UNITY_ANDROID || UNITY_WP8 || UNITY_IPHONE
					joint.distance = touchJointDistance;
				#else
					joint.distance = mouseJointDistance;
				#endif

				// put the joint in robot local space slightly above its head
				// and align both parts of the joint initially
				joint.anchor = Vector2.up * grabbedRobotCollider.bounds.extents.y;												
				joint.connectedAnchor = new Vector2 (grabbedRobot.transform.position.x, grabbedRobot.transform.position.y) + (Vector2.up * joint.distance) + joint.anchor;


			} else {
				Collider2D hit = Physics2D.OverlapCircle (worldPosition, grabRadius, grabbedRobotMask);
				secondClickOnRobot = hit == grabbedRobotCollider;
			}
		}

		// input drag
		if (joint != null && (Input.GetAxis ("Mouse X") != 0.0f || Input.GetAxis ("Mouse Y") != 0.0f)) {
			if (secondClickOnRobot) {
				// allow the robot to swing on the hinge and spin in flight
				grabbedRobotCollider.attachedRigidbody.constraints = RigidbodyConstraints2D.None;
				joint.connectedAnchor = worldPosition;
			} else if (grabbedRobot.lockedByPlayer) {
				grabbedRobot.TryAddPathPoint (worldPosition);
			}
		}
			
		// input release
		if (Input.GetMouseButtonUp (0)) {
			if (grabbedRobot == null)
				return;

			if (grabbedRobot.lockedByPlayer) {
				grabbedRobot.lockedByPlayer = false;
				if (!grabbedRobot.FinishDrawingPath ()) {
					grabbedRobot.grabbedByPlayer = true;
				} else {
					ReleaseRobot ();
				}
			}				

			if (secondClickOnRobot) {
				grabbedRobot.gameObject.layer = (int)Mathf.Log (grabbleMask.value, 2.0f);
//				Vector3 dropForce = new Vector3(joint.connectedAnchor.x, joint.connectedAnchor.y) - grabbedRobot.transform.TransformPoint(new Vector3(joint.anchor.x, joint.anchor.y));
				if (dropForce.sqrMagnitude <= 2.0f * (joint.distance * joint.distance))
					dropForce = Vector3.zero;
					
				grabbedRobot.dropForce = forceMultiplier * dropForce;
				ReleaseRobot ();
			}
		}
    }

	private void ReleaseRobot () {
		beamParticles.Stop ();
		beamParticles.Clear ();
		Destroy (joint);
		joint = null;
		grabbedRobot.grabbedByPlayer = false;
		grabbedRobot = null;
		secondClickOnRobot = false;
		grabbedRobotCollider = null;
	}
}
