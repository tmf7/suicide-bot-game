﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour {

	public static HUDManager	instance = null;
	public float				levelDuration = 30;

	[HideInInspector]
	public int 					robotsFiredThisLevel = 0;
	[HideInInspector]
	public int 					robotsBuiltThisLevel = 0;
	[HideInInspector]
	public int 					firesPutOutThisLevel = 0;
	[HideInInspector]
	public int 					boxesThisLevel = 0;
	[HideInInspector]
	public int					boxesRemaining = 0;
	[HideInInspector]
	public bool 				playSprinklerSystem = false;
	[HideInInspector]
	public bool 				resetSprinklerCooldown = false;

	// player stats
	private Text 				boxesText;
	private Text 				robotsText;
	private Text 				timeText;
	private float				levelEndTime;
	private float				lastTimeRemainingValue;
	private int 				boxesCollected = 0;
	private int 				robotsFired = 0;
	private int 				robotsBuilt = 0;
	private int 				firesPutOut = 0;
	private bool				emotionHandleHeld = false;

	private ImageSwapButton 	pauseButton;
	private Animator			haltButtonAnimator;
	private Slider 				globalEmotionSlider;
	private Button 				globalEmotionButton;
	private Image				globalEmotionImage;
	private Text[] 				globalEmotionText;

	public bool isLevelTimeUp {
		get { 
			return Time.time > levelEndTime;
		}
	}

	// FIXME: timestamp when the Robot.ToggleHaltAndCommand is pressed
	// and increment levelEndTime AND levelDuration by the difference between Time.time and the timestamp
	// ONCE PER FRAME, not once per query
	public int levelTimeRemaining {
		get { 
			return isLevelTimeUp ? 0 : Mathf.RoundToInt(levelDuration - Time.timeSinceLevelLoad);
		}
	}

	public bool allRobotsFired {
		get { 
			return robotsFired >= GameManager.instance.maxRobots;
		}
	}

	public int robotsRemaining {
		get {
			return GameManager.instance.maxRobots - robotsFired;			
		}																	
	}														
		

	public int totalBoxesCollected {
		get { 
			return boxesCollected;
		}
	}

	public int totalFiresPutOut {
		get { 
			return firesPutOut;
		}
	}

	public int totalRobotsBuilt {
		get { 
			return robotsBuilt;
		}
	}

	public bool isEmotionSliderHeld {
		get { 
			return emotionHandleHeld;
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
		boxesText = GameObject.Find ("BoxesText").GetComponent<Text>();
		robotsText = GameObject.Find ("RobotsText").GetComponent<Text>();
		timeText = GameObject.Find ("TimeText").GetComponent<Text>();
		pauseButton = GameObject.Find ("PauseButton").GetComponentInChildren<ImageSwapButton> ();
		haltButtonAnimator = GameObject.Find ("HaltButton").GetComponent<Animator> ();
		globalEmotionSlider = GameObject.Find ("EmotionSlider").GetComponent<Slider> ();
		globalEmotionButton = GameObject.Find ("EmotionButton").GetComponent<Button> ();
		globalEmotionText = globalEmotionButton.GetComponentsInChildren<Text> ();
		globalEmotionImage = GameObject.Find ("EmotionImage").GetComponent<Image> ();
		globalEmotionImage.enabled = false;
		UpdatetGlobalEmotionInterface ();
	}
		
	void Update() {
		if (Robot.isHalted) {
			levelEndTime += Robot.deltaTimeHalted;
			levelDuration += Robot.deltaTimeHalted;
		}

		if (!GameManager.instance.levelEnded)
			lastTimeRemainingValue = levelTimeRemaining;
		
		boxesText.text = "Boxes Left: " + boxesRemaining.ToString();
		robotsText.text = "Robots Left: " + GameManager.instance.robotCount.ToString();
		timeText.text = "Time: " + (GameManager.instance.levelEnded ? lastTimeRemainingValue : levelTimeRemaining).ToString();
		UpdatetGlobalEmotionInterface ();
	}

	public void UpdatetGlobalEmotionInterface() {
		Robot robot = RobotGrabber.instance.currentGrabbedRobot;
		globalEmotionSlider.gameObject.SetActive (robot != null);
		globalEmotionButton.gameObject.SetActive (robot != null);
		if (!globalEmotionSlider.gameObject.activeSelf)
			return;

		globalEmotionText [0].text = robot.name;
		globalEmotionText [1].text = robot.name;

		if (!emotionHandleHeld)
			globalEmotionSlider.value = robot.emotionalStability;
		else
			robot.emotionalStability = globalEmotionSlider.value;
		
		globalEmotionButton.interactable = robot.emotionalStability >= 1.0f;
		globalEmotionImage.enabled = globalEmotionButton.interactable;
		globalEmotionImage.sprite = robot.currentSpeech.sprite;
	}
		
	public void HoldEmotionHandle() {
		instance.emotionHandleHeld = true;
	}
		
	public void ReleaseEmotionHandle() {
		instance.emotionHandleHeld = false;
	}
		
	public void ToggleRobotEmotionalState() {
		Robot robot = RobotGrabber.instance.currentGrabbedRobot;
		if (robot == null)
			return;
		robot.ToggleCrazyEmotion ();
	}

	public void StartSprinklerSystem () {
		instance.playSprinklerSystem = true;
	}

	public void ResetSprinklerCooldown() {
		instance.resetSprinklerCooldown = true;
	}

	public void TogglePauseButtonImage() {
		pauseButton.ToggleImage ();
	}

	public void ToggleHaltButton () {
		Robot.ToggleHaltAndCommand ();
		if (Robot.isHalted)
			instance.haltButtonAnimator.SetTrigger ("StartDance");
		else
			instance.haltButtonAnimator.SetTrigger ("StopDance");
	}

	public void StartLevelTimer() {
		levelEndTime = Time.time + levelDuration;
		ResetLevelStats ();
	}

	public void SpendBoxes(int points) {
		boxesRemaining -= points;
	}

	public void CollectBox(int points) {
		boxesCollected += points;
		boxesThisLevel += points;
		boxesRemaining += points;
	}

	public void ExtinguishFire() {
		firesPutOut++;
		firesPutOutThisLevel++;
	}

	public void FireRobot() {
		robotsFired++;
		robotsFiredThisLevel++;
	}

	public void BuildRobot() {
		robotsBuilt++;
		robotsBuiltThisLevel++;
	}

	public void ResetGameStats() {
		firesPutOut = 0;
		boxesCollected = 0;
		boxesRemaining = 0;
		robotsBuilt = 0;
		robotsFired = 0;
	}

	public void ResetLevelStats() {
		firesPutOutThisLevel = 0;
		robotsFiredThisLevel = 0;
		robotsBuiltThisLevel = 0;
		boxesThisLevel = 0;
	}
}
