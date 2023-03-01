using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

using Enter.Utils;

namespace Enter
{
  [DisallowMultipleComponent]
  public class SceneTransitioner : MonoBehaviour
  {
    public static SceneTransitioner Instance;

    public static bool IsTransitioning = false;

    private const float _eps = 0.001f;

    #region ================== Methods

    void Awake()
    {
      Instance = this;
    }

    public bool TransitionTo(ExitPassage exitPassage, SceneReference nextSceneReference)
    {
      Debug.Log(nextSceneReference);
      StartCoroutine(_transitionTo(exitPassage, nextSceneReference));
      return true;
    }

    #endregion

    #region ================== Helpers

    private IEnumerator _transitionTo(ExitPassage exitPassage, SceneReference nextSceneReference)
    {
      Assert.IsNotNull(exitPassage, "Current scene's exitPassage must be provided.");

      Scene prevScene = SceneManager.GetActiveScene();

      // Load nextScene (must wait one frame for additive scene load)
      nextSceneReference.LoadScene(LoadSceneMode.Additive);
      yield return null;

      // Get nextScene's entry passage
      EntryPassage entryPassage = null;
      foreach (EntryPassage x in FindObjectsOfType<EntryPassage>())
      {
        if (x.gameObject.scene != prevScene) entryPassage = x;
      }
      Assert.IsNotNull(entryPassage, "New scene's entryPassage not found.");

      // Get nextScene's root transform
      Transform rootTransform = entryPassage.transform.root;
      Assert.IsNotNull(rootTransform, "New scene's root not found.");

      // Get nextScene
      Scene nextScene = entryPassage.gameObject.scene;

      // Align nextScene
      rootTransform.position += exitPassage.transform.position - entryPassage.transform.position;

      // Allow camera movement time
      yield return cameraTransition(prevScene, nextScene);

      // Unload prevScene
      SceneManager.UnloadSceneAsync(prevScene);
    }

    private IEnumerator cameraTransition(Scene prevScene, Scene nextScene)
    {
      // Disable prevScene's camera, so that nextScene's camera becomes the one in use
      foreach (Camera x in FindObjectsOfType<Camera>())
        if (x.gameObject.scene == prevScene) x.gameObject.SetActive(false);

      // Get nextScene's camera (the only remaining one)
      Camera camera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

      // Max priority of prevScene's virtual camera, so that the camera snaps there
      CinemachineVirtualCamera prevSceneVC = findHighestPriorityVC(prevScene);
      Assert.IsNotNull(prevSceneVC, "Previous scene's virtual camera not found");
      prevSceneVC.Priority = int.MaxValue;

      yield return null;

      // Min priority of prevScene's virtual camera
      prevSceneVC.Priority = 0;
      
      // Wait until camera has moved to the highest-priority virtual camera in nextScene
      CinemachineVirtualCamera nextSceneVC = findHighestPriorityVC(nextScene);
      while (Vector3.Distance(camera.transform.position, nextSceneVC.transform.position) > _eps)
        yield return null;
    }

    private CinemachineVirtualCamera findHighestPriorityVC(Scene scene)
    {
      CinemachineVirtualCamera highestPriorityVC = null;

      foreach (CinemachineVirtualCamera x in FindObjectsOfType<CinemachineVirtualCamera>())
        if (x.gameObject.scene == scene && (highestPriorityVC == null || highestPriorityVC.Priority < x.Priority))
          highestPriorityVC = x;

      return highestPriorityVC;
    }

    #endregion
  }
}
