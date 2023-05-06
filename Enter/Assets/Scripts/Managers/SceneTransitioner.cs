using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections.Generic;

using Enter.Utils;

namespace Enter
{
  public enum STState {
    Idle = 0,
    Transitioning,
    Reloading
  }

  [DisallowMultipleComponent]
  public class SceneTransitioner : MonoBehaviour
  {
    public static SceneTransitioner Instance;

    private const float _eps = 0.001f;

    private Scene       _currScene;
    private ExitPassage _exitPassage;
    
    public STState STState { get; private set; } = STState.Idle;

    private GameObject _currSpawnPoint;

    [SerializeField]
    private ScreenWipe _screenWiper;

    #region ================== Accessors

    public Vector3 SpawnPosition => _currSpawnPoint.transform.position;
    
    [field:SerializeField] public UnityEvent<Scene>        OnSceneLoad        { get; private set; }

    [field:SerializeField] public UnityEvent<Scene>        OnReloadBefore     { get; private set; }
    [field:SerializeField] public UnityEvent<Scene, Scene> OnReloadAfter      { get; private set; }
    [field:SerializeField] public UnityEvent<Scene>        OnTransitionBefore { get; private set; }
    [field:SerializeField] public UnityEvent<Scene, Scene> OnTransitionAfter  { get; private set; }

    #endregion

    #region ================== Methods

    void Awake()
    {
      Instance = this;
      _currScene = SceneManager.GetActiveScene();
      _currSpawnPoint = findSpawnPointAny();

      Assert.IsNotNull(_screenWiper, "SceneTransitioner must have a reference to screen wiper.");
    }

    void Start()
    {
      _screenWiper.Unblock();
    }

    void OnEnable()
    {
      // Add to functions that should be called by the true Scene Manager
      SceneManager.sceneLoaded += onSceneLoadHelper;
    }

    void OnDisable()
    {
      // Remove from functions that should be called by the true Scene Manager
      SceneManager.sceneLoaded -= onSceneLoadHelper;
    }

    public void Transition(ExitPassage exitPassage)
    {
      Assert.IsNotNull(exitPassage, "Current scene's exitPassage must be provided.");
      Assert.IsNotNull(exitPassage.NextSceneReference, "ExitPassage must have a NextSceneReference.");

      // This shouldn't happen; only one scene transition/reload should be happening at a time
      if (STState != STState.Idle)
      {
        Debug.LogError("Attempting to transition scenes while another transition/reload is happening.");
        return;
      }

      StartCoroutine(transitionHelper(exitPassage));
    }

    public void Reload()
    {
      // This shouldn't happen; only one scene transition/reload should be happening at a time
      if (STState != STState.Idle) 
      {
        Debug.LogError("Attempting to reload scene while another transition/reload is happening.");
        return;
      }

      StartCoroutine(reloadHelper());
    }

    #endregion

    #region ================== Helpers

    private IEnumerator transitionHelper(ExitPassage exitPassage)
    {
      STState = STState.Transitioning;
      
      // Freeze time
      float temp = Time.timeScale;
      Time.timeScale = 0;

      {
        // Set _prevScene
        Scene _prevScene = SceneManager.GetActiveScene();
        Assert.AreEqual(_prevScene, _currScene, "At this moment, both scenes should be equal.");

        // Do pre-transition actions
        OnTransitionBefore?.Invoke(_prevScene);

        // Load next scene (additively!).
        // This causes SceneManager to call onSceneLoadHelper(), which will set _currScene
        // and _currSpawnPoint; this should be done in exactly one frame.
        _exitPassage = exitPassage;
        exitPassage.NextSceneReference.LoadScene(LoadSceneMode.Additive);
        yield return null;
        Assert.AreNotEqual(_prevScene, _currScene, "At this moment, both scenes should be different.");

        // Wait for camera to move smoothly
        yield return cameraTransition(_prevScene, _currScene);

        // Do post-transition actions
        OnTransitionAfter?.Invoke(_prevScene, _currScene);

        // Unload _prevScene
        yield return SceneManager.UnloadSceneAsync(_prevScene);
      }

      // Unfreeze time
      Time.timeScale = temp;

      STState = STState.Idle;
    }

    private IEnumerator reloadHelper(Action onLoaded = null)
    {
      STState = STState.Reloading;

      {
        // Set _prevScene
        Scene _prevScene = SceneManager.GetActiveScene();
        Assert.AreEqual(_prevScene, _currScene, "At this moment, both scenes should be equal.");

        // Do pre-reload actions
        PauseManager.Instance.Unpause();
        yield return _screenWiper.Block();
        OnReloadBefore?.Invoke(_prevScene);

        // Load current scene (non-additively, i.e. replaces this scene).
        // This causes SceneManager to call onSceneLoadHelper(), which will set _currScene
        // and _currSpawnPoint; this should be done in exactly one frame.
        SceneManager.LoadScene(_currScene.name);
        yield return null;
        onLoaded?.Invoke();
        Assert.AreNotEqual(_prevScene, _currScene, "At this moment, both scenes should be different.");

        // Do post-reload actions
        OnReloadAfter?.Invoke(_prevScene, _currScene);

        PlayerManager.PlayerScript.SetFieldsAlive();
        yield return _screenWiper.Unblock();

      }

      STState = STState.Idle;
    }

    private IEnumerator cameraTransition(Scene _prevScene, Scene _currScene)
    {
      // Disable _prevScene's camera, so that _currScene's camera becomes the one in use
      foreach (Camera x in FindObjectsOfType<Camera>())
        if (x.gameObject.scene == _prevScene) x.gameObject.SetActive(false);

      // Get _currScene's camera (the only remaining one)
      Camera camera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

      // Max priority of _prevScene's virtual camera, so that the camera snaps there
      // Important: you can't just set the camera's position, or the virtual camera will slap you
      CinemachineVirtualCamera _prevSceneVC = findHighestPriorityVC(_prevScene);
      Assert.IsNotNull(_prevSceneVC, "Previous scene's virtual camera not found");
      _prevSceneVC.Priority = int.MaxValue;

      yield return null; // Must wait one frame for the cinemachine camera to adjust its internal position
      // it's important that the cinemachine brain's update method is set to LateUpdate. FixedUpdate does not 
      // work, due to us freezing timescale

      // Min priority of _prevScene's virtual camera
      _prevSceneVC.Priority = 0;

      // Wait until camera has moved to the highest-priority virtual camera in _currScene
      CinemachineVirtualCamera _currSceneVC = findHighestPriorityVC(_currScene);
      int temp = _currSceneVC.Priority;
      _currSceneVC.Priority = int.MaxValue;

      yield return null; // Must wait one frame for the cinemachine camera to adjust its internal position

      while (Vector2.Distance(camera.transform.position, _currSceneVC.State.CorrectedPosition) > _eps)
        yield return null;
      _currSceneVC.Priority = temp;
    }

    private GameObject findSpawnPointAny()
    {
      return FindObjectOfType<SpawnPoint>()?.gameObject;
    }

    private GameObject findSpawnPoint(Scene scene)
    {
      // Assumes only one spawn point per scene
      foreach (SpawnPoint x in FindObjectsOfType<SpawnPoint>())
        if (x.gameObject.scene == scene)
          return x.gameObject;

      return null;
    }

    private CinemachineVirtualCamera findHighestPriorityVC(Scene scene)
    {
      CinemachineVirtualCamera highestPriorityVC = null;

      foreach (CinemachineVirtualCamera x in FindObjectsOfType<CinemachineVirtualCamera>())
        if (x.gameObject.scene == scene && (highestPriorityVC == null || highestPriorityVC.Priority < x.Priority))
          highestPriorityVC = x;

      return highestPriorityVC;
    }

    private void onSceneLoadHelper(Scene newScene, LoadSceneMode mode)
    {
      // Hopefully no physics frame happens between scene load and this function. Else Unity documentation lied.

      if (STState == STState.Transitioning)
      {
        // Get next scene's entry passage
        EntryPassage entryPassage = null;
        foreach (EntryPassage x in FindObjectsOfType<EntryPassage>())
          if (x.gameObject.scene == newScene) entryPassage = x;
        Assert.IsNotNull(entryPassage, "New scene's entryPassage not found.");

        // Get next scene's root transform
        Transform rootTransform = entryPassage.transform.root;
        Assert.IsNotNull(rootTransform, "New scene's root not found.");

        // Align next scene
        rootTransform.position += _exitPassage.transform.position - entryPassage.transform.position;
        Debug.Log("root moved");
      }

      // Set _currScene and _currSpawnPoint
      _currScene = newScene;
      _currSpawnPoint = findSpawnPoint(newScene);
      Assert.IsNotNull(_currSpawnPoint, "New scene's spawnPoint not found.");

      // Fixme: is this the right place to put this?
      OnSceneLoad?.Invoke(newScene);
    }

    #endregion
  }
}
