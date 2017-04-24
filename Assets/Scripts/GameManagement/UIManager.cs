﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using System;

public class UIManager : MonoBehaviour {

	public static UIManager instance = null;
	public float 			transitionTime = 0.5f;
	public int				storyToTell = 0;

	private GameObject[]	overlayObjects;
	private Animator 		screenFaderAnimator;
	private Slider			musicSlider;
	private Slider			sfxSlider;
	private bool			isFading = false;
	private bool			inTransition = false;

	void Awake() {
		if (instance == null)
			instance = this;
		else if (instance != this)
			Destroy(gameObject);	
		
		DontDestroyOnLoad(gameObject);
	}

	void Start() {
		screenFaderAnimator = GetComponent<Animator> ();
		screenFaderAnimator.speed = 1.0f / transitionTime;
		instance.musicSlider = GameObject.Find ("MusicSlider").GetComponent<Slider> ();
		instance.sfxSlider = GameObject.Find ("SFxSlider").GetComponent<Slider> ();
		InitScene ();
	}

    void Update() {
		if (Input.GetButtonDown ("Cancel") && !inTransition) { 		// set to escape key in project settings, other simultaneous keys can be added (eg: Pause/Break key)
			if (isSceneMainMenu)
				ToggleOverlay ();
			else
				TogglePause ();
		}
    }

	public bool isSceneMainMenu {
		get { 
			return SceneManager.GetActiveScene ().buildIndex == 0;
		}
	}

	public void QuitGame() {
		Application.Quit ();
	}
		
    public void ResetLevel() {
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

	public IEnumerator FadeToBlack() {
		inTransition = true;
		isFading = true;
		instance.screenFaderAnimator.SetTrigger ("FadeToBlack");

		while (isFading)
			yield return null;
	}

	public IEnumerator FadeToClear() {
		isFading = true;
		instance.screenFaderAnimator.SetTrigger ("FadeToClear");

		while (isFading)
			yield return null;
	}

	public void FadeComplete() {
		instance.isFading = false;
	}


	// scene 0 in the build must be set to the MainMenu scene
	public void LoadRandomLevel() {
		int buildIndex = Random.Range (1, SceneManager.sceneCountInBuildSettings);
		FadeToLevel (buildIndex);
	}

	public void FadeToLevel(int buildIndex) {
		instance.StartCoroutine (instance.FadeToLevelCoroutine (buildIndex));
	}
		
	public IEnumerator FadeToLevelCoroutine (int buildIndex) {
		GameManager.instance.enabled = false;
		yield return instance.StartCoroutine (instance.FadeToBlack ());

		SceneManager.LoadScene (buildIndex);

		yield return instance.StartCoroutine (instance.FadeToClear ());
	}

	public void FadeToGameOver() {
		storyToTell = 4;
		FadeToStory ();
	}

	public void FadeToStory() {
		instance.StartCoroutine (instance.FadeToStoryCoroutine ());
	}

	public IEnumerator FadeToStoryCoroutine () {
		GameManager.instance.enabled = false;

		yield return instance.StartCoroutine (instance.FadeToBlack ());

		DisableCurrentScene ();
		Cursor.visible = true;
		RobotGrabber.instance.gameObject.SetActive (false);
		HUDManager.instance.gameObject.SetActive (false);
		TransitionManager.instance.gameObject.SetActive (true);
		TransitionManager.instance.StartIntermission (storyToTell++);
		SoundManager.instance.PlayIntermissionMusic ();

		yield return instance.StartCoroutine (instance.FadeToClear ());
	}

	// HACK: prevents scene from continuing to process while intermission plays overtop before the next scene loads
	public void DisableCurrentScene() {
		GameObject[] allSceneObjects = SceneManager.GetActiveScene ().GetRootGameObjects ();
		foreach (GameObject item in allSceneObjects) {
			if (item.tag == "MainCamera")
				continue;

			item.SetActive (false);
		}
	}

	//this is called only once, and the paramter tell it to be called only after the scene was loaded
	//(otherwise, our Scene Load callback would be called the very first load, and we don't want that)
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static public void CallbackInitialization() {
		//register the callback to be called everytime the scene is loaded
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	static private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1) {
		instance.InitScene();
	}

	void InitScene() {
		if (Time.timeScale == 0.0f)
			TogglePause();
		
		// TODO: show and play intermission and gameover stuff

		if (!isSceneMainMenu) {
			GameManager.instance.enabled = true;
			HUDManager.instance.gameObject.SetActive (true);
			RobotGrabber.instance.gameObject.SetActive (true);
			SoundManager.instance.PlayGameMusic ();
			GameManager.instance.InitLevel ();
			TransitionManager.instance.StartInGameDialogue();
			Cursor.visible = false;
		} else {
			GameManager.instance.enabled = false;
			HUDManager.instance.gameObject.SetActive (false);
			RobotGrabber.instance.gameObject.SetActive (false);
			TransitionManager.instance.DisableTransitionManager ();
			SoundManager.instance.PlayMenuMusic ();
			Cursor.visible = true;
		}
		instance.inTransition = false;
		instance.UpdateSoundConfiguration ();
		PauseManager.instance.gameObject.SetActive (false);
		instance.overlayObjects = GameObject.FindGameObjectsWithTag("Overlay");			// Overlay == CreditsCard
		instance.ToggleOverlay ();
	}
		
	public void TogglePause() {
		Time.timeScale = Time.timeScale == 1.0f ? 0.0f : 1.0f;
		RobotGrabber.instance.enabled = Time.timeScale == 1.0f;	
		Cursor.visible = Time.timeScale == 0.0f; 
		PauseManager.instance.gameObject.SetActive (Time.timeScale == 0.0f);
		HUDManager.instance.TogglePauseButtonImage ();
	}

	public void ToggleOverlay() {
		foreach (GameObject overlayItem in instance.overlayObjects)
			overlayItem.SetActive (!overlayItem.activeSelf);
	}

	private void UpdateSoundConfiguration() {
		if (!isSceneMainMenu) {
			musicSlider.value = SoundManager.instance.musicVolume;
			sfxSlider.value = SoundManager.instance.sfxVolume;
		}
	}
}
