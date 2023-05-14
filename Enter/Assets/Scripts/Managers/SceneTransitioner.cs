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

    public bool CanCutPaste   { get; set; }
    public bool CanRCAnywhere { get; set; }
    public bool CanDelete     { get; set; }

    [SerializeField]
    private ScreenWipe _screenWiper;

    private bool        _firstLoadComplete;
    private Camera      _invincibleCamera;
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
      UpdateRCBoxPermissionsFromCurrSpawnPoint();
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

    public void UpdateCurrSpawnPoint(SpawnPoint x)
    {
      CurrSpawnPoint = x;
      UpdateRCBoxPermissionsFromCurrSpawnPoint();
    }

    public void UpdateRCBoxPermissionsFromCurrSpawnPoint()
    {
      CanCutPaste   = CurrSpawnPoint.CanCutPaste;
      CanRCAnywhere = CurrSpawnPoint.CanRCAnywhere;
      CanDelete     = CurrSpawnPoint.CanDelete;
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
        setInvincibleCamera(PrevScene);

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
        yield return cameraTransition(PrevScene, CurrScene);

        // Do post-transition actions
        UpdateRCBoxPermissionsFromCurrSpawnPoint();
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
        setInvincibleCamera(PrevScene);

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

        // Handle camera existence

        // Do post-reload actions
        UpdateRCBoxPermissionsFromCurrSpawnPoint();
        OnReloadAfter?.Invoke(PrevScene, CurrScene);

        PlayerManager.PlayerScript.SetFieldsAlive();
        yield return _screenWiper.Unblock();
      }

      STState = STState.Idle;
    }

    private IEnumerator cameraTransition(Scene prevScene, Scene currScene)
    {
      // Min priority of prevScene's virtual camera
      CinemachineVirtualCamera prevSceneVC = findHighestPriorityVC(prevScene);
      Assert.IsNotNull(prevSceneVC, "Previous scene's virtual camera not found");
      prevSceneVC.Priority = 0;

      // Wait until camera has moved to the highest-priority virtual camera in currScene
      CinemachineVirtualCamera currSceneVC = findHighestPriorityVC(currScene);
      int temp = currSceneVC.Priority;
      currSceneVC.Priority = int.MaxValue;
      yield return null; // Must wait one frame for the cinemachine camera to adjust its internal position
      while (Vector2.Distance(_invincibleCamera.transform.position, currSceneVC.State.CorrectedPosition) > _eps)
        yield return null;
      currSceneVC.Priority = temp;
    }

    private void setInvincibleCamera(Scene scene)
    {
      if (_invincibleCamera != null) return;

      // Make scene's camera invincible if there isn't already one
      Camera camera = findCameraInScene(scene);
      Assert.IsNotNull(camera, "Previous scene's camera not found");
      camera.transform.SetParent(null);
      DontDestroyOnLoad(camera.gameObject);
      _invincibleCamera = camera;
    }

    private T findInAnyScene<T>() where T : MonoBehaviour
    {
      return FindObjectOfType<T>();
    }

    private T findInScene<T>(Scene scene) where T : MonoBehaviour
    {
      // Assumes only one per scene
      foreach (T x in FindObjectsOfType<T>())
        if (x.gameObject.scene == scene)
          return x;

      return null;
    }

    private Camera findCameraInScene(Scene scene)
    {
      // Assumes the main camera is tagged
      foreach (Camera x in FindObjectsOfType<Camera>())
        if (x.gameObject.scene == scene && x.tag == "MainCamera")
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

      // Set fields obtained from new scene
      setSceneFields(newScene);

      // Disable newScene's camera if invincible one exists
      if (_invincibleCamera != null)
      {
        Camera cameraToDisable = findCameraInScene(newScene);
        Assert.IsNotNull(cameraToDisable, "New scene's camera not found");
        cameraToDisable.gameObject.SetActive(false);
      }
      else
      {
        Assert.IsFalse(_firstLoadComplete); // Must be first load if invincible camera isn't in
      }

      // Align new scene's root, while checking for weird state / first load
      switch (STState)
      {
        case STState.Idle:
        {
          if (_firstLoadComplete)
            throw new Exception("Scene loaded, but SceneTransitioner is idle, and this is not the first load.");

          Debug.Log("Scene loaded, but SceneTransitioner is idle. This must be the first load.");
          _firstLoadComplete = true;
          break;
        }
        case STState.Transitioning:
        {
          CurrRootTransform.position += _tempExitPassage.transform.position - CurrEntryPassage.transform.position;
          break;
        }
        case STState.Reloading:
        {
          CurrRootTransform.position = _tempPreviousRootPosition;
          break;
        }
      }

      // Invoke events after scene load
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
