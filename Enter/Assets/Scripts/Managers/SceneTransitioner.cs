using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections.Generic;

using Enter.Utils;

namespace Enter
{
  [DisallowMultipleComponent]
  public class SceneTransitioner : MonoBehaviour
  {
    public static SceneTransitioner Instance;

    private const float _eps = 0.001f;

    private Scene _currScene;
    private ExitPassage _exitPassage;
    private bool _repositionOnSceneTransition;
    private bool _transitioning;

    private GameObject _currSpawnPoint;

    #region ================== Accessors

    public Vector3 SpawnPosition => _currSpawnPoint.transform.position;

    #endregion

    #region ================== Methods

    void Awake()
    {
      Instance = this;
      _currScene = SceneManager.GetActiveScene();
      _currSpawnPoint = findSpawnPointAny();
    }

    void OnEnable()
    {
      _repositionOnSceneTransition = false;

      // Add to functions that should be called on scene-load
      SceneManager.sceneLoaded += onSceneLoad;
    }

    void OnDisable()
    {
      // Remove from functions that should be called on scene-load
      SceneManager.sceneLoaded -= onSceneLoad;
    }

    public void Transition(ExitPassage exitPassage)
    {
      Assert.IsNotNull(exitPassage, "Current scene's exitPassage must be provided.");
      Assert.IsNotNull(exitPassage.NextSceneReference, "ExitPassage must have a NextSceneReference.");

      if (!_transitioning)
      {
        StartCoroutine(transitionTo(exitPassage));
      }
      else
      {
        Debug.LogError("Attempting to scene transition while another scene transition is happening.");
      }
    }

    #endregion

    #region ================== Helpers

    private IEnumerator transitionTo(ExitPassage exitPassage)
    {
      _transitioning = true;

      // Set _prevScene
      Scene _prevScene = SceneManager.GetActiveScene();

      Assert.AreEqual(_prevScene, _currScene, "At this moment, both scenes should be the same.");

      // Effectively freeze time for the player
      PlayerManager.PlayerScript.ToggleTimeSensitiveComponents(enabled: false, affectSelf: true);

      // Load and align next scene (will update _currScene and SpawnPosition)
      yield return loadAndAlignNextScene(exitPassage);

      Assert.AreNotEqual(_prevScene, _currScene, "At this moment, both scenes should be different.");

      // Allow camera movement time
      yield return cameraTransition(_prevScene, _currScene);

      // Unload _prevScene
      yield return SceneManager.UnloadSceneAsync(_prevScene);

      PlayerManager.PlayerScript.ToggleTimeSensitiveComponents(enabled: true, affectSelf: false);
      // TODO: Detect if player needs help landing on a ledge. If so, help them.
      PlayerManager.PlayerScript.enabled = true;

      _transitioning = false;
    }

    private IEnumerator loadAndAlignNextScene(ExitPassage exitPassage)
    {
      _exitPassage = exitPassage;

      // Load next scene (must wait one frame for additive scene load)
      exitPassage.NextSceneReference.LoadScene(LoadSceneMode.Additive);

      // Allow scene to be repositioned on load
      _repositionOnSceneTransition = true;

      // Alignment should happen in between here. This should only run once
      while (_repositionOnSceneTransition) yield return null;

      _repositionOnSceneTransition = false;
    }

    private IEnumerator cameraTransition(Scene _prevScene, Scene _currScene)
    {
      // Disable _prevScene's camera, so that _currScene's camera becomes the one in use
      foreach (Camera x in FindObjectsOfType<Camera>())
        if (x.gameObject.scene == _prevScene) x.gameObject.SetActive(false);

      // Get _currScene's camera (the only remaining one)
      Camera camera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

      // Max priority of _prevScene's virtual camera, so that the camera snaps there
      CinemachineVirtualCamera _prevSceneVC = findHighestPriorityVC(_prevScene);
      Assert.IsNotNull(_prevSceneVC, "Previous scene's virtual camera not found");
      _prevSceneVC.Priority = int.MaxValue;

      yield return null;

      // Min priority of _prevScene's virtual camera
      _prevSceneVC.Priority = 0;

      // Wait until camera has moved to the highest-priority virtual camera in _currScene
      CinemachineVirtualCamera _currSceneVC = findHighestPriorityVC(_currScene);
      while (Vector2.Distance(camera.transform.position, _currSceneVC.State.CorrectedPosition) > _eps)
        yield return null;
    }

    private GameObject findSpawnPointAny()
    {
      return FindObjectOfType<SpawnPoint>()?.gameObject;
    }

    private GameObject findSpawnPoint(Scene scene)
    {
      GameObject temp = null;

      foreach (SpawnPoint x in FindObjectsOfType<SpawnPoint>())
        if (x.gameObject.scene == scene)
          temp = x.gameObject;

      return temp;
    }

    private CinemachineVirtualCamera findHighestPriorityVC(Scene scene)
    {
      CinemachineVirtualCamera highestPriorityVC = null;

      foreach (CinemachineVirtualCamera x in FindObjectsOfType<CinemachineVirtualCamera>())
        if (x.gameObject.scene == scene && (highestPriorityVC == null || highestPriorityVC.Priority < x.Priority))
          highestPriorityVC = x;

      return highestPriorityVC;
    }

    private void onSceneLoad(Scene nextScene, LoadSceneMode mode)
    {
      Debug.Log("SCENE LOADED");

      // Hopefully no physics frame happen in between scene load and this function. Else Unity documentation lied.
      if (!_repositionOnSceneTransition) return;

      // we align the new scene.
      // Get next scene's entry passage
      EntryPassage entryPassage = null;
      foreach (EntryPassage x in FindObjectsOfType<EntryPassage>())
        if (x.gameObject.scene == nextScene) entryPassage = x;

      Assert.IsNotNull(entryPassage, "Next scene's entryPassage not found.");

      // Get next scene's root transform
      Transform rootTransform = entryPassage.transform.root;

      Assert.IsNotNull(rootTransform, "Next scene's root not found.");

      // Align next scene
      rootTransform.position += _exitPassage.transform.position - entryPassage.transform.position;

      // Set _currScene
      _currScene = entryPassage.gameObject.scene;

      // Set _currSpawnPoint
      _currSpawnPoint = findSpawnPoint(_currScene);
      Assert.IsNotNull(_currSpawnPoint, "Next scene's spawnPoint not found.");
      _repositionOnSceneTransition = false;


    }

    #endregion
  }
}
