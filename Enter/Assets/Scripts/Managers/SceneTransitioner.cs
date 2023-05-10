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
  public enum STState
  {
    Idle = 0,
    Transitioning,
    Reloading
  }

  [DisallowMultipleComponent]
  public class SceneTransitioner : MonoBehaviour
  {
    public static SceneTransitioner Instance;

    private const float _eps = 0.001f;

    public STState      STState           { get; private set; } = STState.Idle;

    public Scene        PrevScene         { get; private set; }
    public Scene        CurrScene         { get; private set; }
    public SpawnPoint   CurrSpawnPoint    { get; private set; }
    public EntryPassage CurrEntryPassage  { get; private set; }
    public Transform    CurrRootTransform { get; private set; }

    [SerializeField]
    private ScreenWipe _screenWiper;

    private bool        _firstLoadComplete;
    private ExitPassage _tempExitPassage;
    private Vector2     _tempPreviousRootPosition;

    #region ================== Accessors

    public Vector3 SpawnPosition => CurrSpawnPoint.transform.position;

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
      CurrSpawnPoint = findInAnyScene<SpawnPoint>();

      Assert.IsNotNull(_screenWiper, "SceneTransitioner must have a reference to screen wiper.");
    }

    void Start()
    {
      setSceneFields(SceneManager.GetActiveScene());
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
        // Set PrevScene
        PrevScene = SceneManager.GetActiveScene();
        Assert.AreEqual(PrevScene, CurrScene, "At this moment, both scenes should be equal.");

        // Do pre-transition actions
        OnTransitionBefore?.Invoke(PrevScene);

        // Load next scene (additively!).
        // This causes SceneManager to call onSceneLoadHelper(), which will set CurrScene
        // and CurrSpawnPoint; this should be done in exactly one frame.
        _tempExitPassage = exitPassage;
        _tempExitPassage.NextSceneReference.LoadScene(LoadSceneMode.Additive);
        yield return null;
        Assert.AreNotEqual(PrevScene, CurrScene, "At this moment, both scenes should be different.");

        // Wait for camera to move smoothly
        yield return cameraTransition();

        // Do post-transition actions
        OnTransitionAfter?.Invoke(PrevScene, CurrScene);

        // Unload PrevScene
        yield return SceneManager.UnloadSceneAsync(PrevScene);
      }

      // Unfreeze time
      Time.timeScale = temp;

      STState = STState.Idle;
    }

    private IEnumerator reloadHelper(Action onLoaded = null)
    {
      STState = STState.Reloading;

      _tempPreviousRootPosition = CurrRootTransform.position;

      {
        // Set PrevScene
        PrevScene = SceneManager.GetActiveScene();
        Assert.AreEqual(PrevScene, CurrScene, "At this moment, both scenes should be equal.");

        // Do pre-reload actions
        PauseManager.Instance.Unpause();
        yield return _screenWiper.Block();
        OnReloadBefore?.Invoke(PrevScene);

        // Load current scene (non-additively, i.e. replaces this scene).
        // This causes SceneManager to call onSceneLoadHelper(), which will set CurrScene
        // and CurrSpawnPoint; this should be done in exactly one frame.
        SceneManager.LoadScene(CurrScene.name);
        yield return null;
        onLoaded?.Invoke();
        Assert.AreNotEqual(PrevScene, CurrScene, "At this moment, both scenes should be different.");

        // Do post-reload actions
        OnReloadAfter?.Invoke(PrevScene, CurrScene);

        PlayerManager.PlayerScript.SetFieldsAlive();
        yield return _screenWiper.Unblock();

      }

      STState = STState.Idle;
    }

    private IEnumerator cameraTransition()
    {
      // Disable PrevScene's camera, so that CurrScene's camera becomes the one in use
      foreach (Camera x in FindObjectsOfType<Camera>())
        if (x.gameObject.scene == PrevScene) x.gameObject.SetActive(false);

      // Get CurrScene's camera (the only remaining one)
      Camera camera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

      // Max priority of PrevScene's virtual camera, so that the camera snaps there
      // Important: you can't just set the camera's position, or the virtual camera will slap you
      CinemachineVirtualCamera _prevSceneVC = findHighestPriorityVC(PrevScene);
      Assert.IsNotNull(_prevSceneVC, "Previous scene's virtual camera not found");
      _prevSceneVC.Priority = int.MaxValue;

      // Must wait one frame for the cinemachine camera to adjust its internal position
      // it's important that the cinemachine brain's update method is set to LateUpdate. FixedUpdate does not 
      // work, due to us freezing timescale
      yield return null;

      // Min priority of PrevScene's virtual camera
      _prevSceneVC.Priority = 0;

      // Wait until camera has moved to the highest-priority virtual camera in CurrScene
      CinemachineVirtualCamera _currSceneVC = findHighestPriorityVC(CurrScene);
      int temp = _currSceneVC.Priority;
      _currSceneVC.Priority = int.MaxValue;

      yield return null; // Must wait one frame for the cinemachine camera to adjust its internal position

      while (Vector2.Distance(camera.transform.position, _currSceneVC.State.CorrectedPosition) > _eps)
        yield return null;
      _currSceneVC.Priority = temp;
    }

    private T findInAnyScene<T>() where T : MonoBehaviour
    {
      return FindObjectOfType<T>();
    }

    private T findInScene<T>(Scene scene) where T : MonoBehaviour
    {
      // Assumes only one spawn point per scene
      foreach (T x in FindObjectsOfType<T>())
        if (x.gameObject.scene == scene)
          return x;

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

      // Check for weird state
      if (STState == STState.Idle) 
      {
        if (_firstLoadComplete)
        {
          throw new Exception("Scene loaded, but SceneTransitioner is idle, and this is not the first load.");
        }
        Debug.Log("Scene loaded, but SceneTransitioner is idle. This must be the first load.");
        _firstLoadComplete = true;
      }

      // Set fields obtained from new scene
      setSceneFields(newScene);
        
      // Align next scene
      if (STState == STState.Transitioning)
      {
        CurrRootTransform.position += _tempExitPassage.transform.position - CurrEntryPassage.transform.position;
      }
      else if (STState == STState.Reloading)
      {
        CurrRootTransform.position = _tempPreviousRootPosition;
      }
      
      // Fixme: is this the right place to put this?
      OnSceneLoad?.Invoke(newScene);
    }

    private void setSceneFields(Scene scene)
    {
      CurrScene         = scene;
      CurrSpawnPoint    = findInScene<SpawnPoint>(scene);
      CurrEntryPassage  = findInScene<EntryPassage>(scene);
      CurrRootTransform = CurrEntryPassage.transform.root.transform;
      Assert.IsNotNull(CurrSpawnPoint,    "Scene's spawnPoint not found.");
      Assert.IsNotNull(CurrEntryPassage,  "Scene's entryPassage not found.");
      Assert.IsNotNull(CurrRootTransform, "Scene's root transform not found.");
    }

    #endregion
  }
}
