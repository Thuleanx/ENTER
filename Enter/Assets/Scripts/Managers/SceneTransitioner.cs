using Cinemachine;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

using Enter.Utils;

namespace Enter
{
  [DisallowMultipleComponent]
  public class SceneTransitioner : MonoBehaviour
  {
    public static SceneTransitioner Instance;

    public static bool IsTransitioning = false;

    // ================== Methods

    void Awake()
    {
      Instance = this;
    }

    public bool TransitionTo(SceneReference nextScene)
    {
      Debug.Log(nextScene);
      StartCoroutine(_transitionTo(nextScene));
      return true;
    }

    // ================== Helpers

    private IEnumerator _transitionTo(SceneReference nextScene)
    {
      Scene previousScene = SceneManager.GetActiveScene();

      PassageAnchor exitAnchor = null;
      foreach (PassageAnchor anchor in FindObjectsOfType<PassageAnchor>()) 
        if (anchor.IsExit)
          exitAnchor = anchor;

      if (!exitAnchor) Debug.Log("Exit anchor not found");

      nextScene.LoadScene(LoadSceneMode.Additive);

      yield return null;

      PassageAnchor entranceAnchor = null;
      foreach (PassageAnchor anchor in FindObjectsOfType<PassageAnchor>()) 
        if (!anchor.IsExit && anchor.gameObject.scene != previousScene)
          entranceAnchor = anchor;
      if (!entranceAnchor) Debug.Log("Entrance anchor not found");

      Transform rootObjectOfCurrentScene = null;
      foreach (GameObject obj in GameObject.FindGameObjectsWithTag("SceneRoot")) 
        if (obj.scene != previousScene)
          rootObjectOfCurrentScene = obj?.GetComponent<Transform>();
      if (rootObjectOfCurrentScene == null) Debug.Log("Root object not found");

      rootObjectOfCurrentScene.transform.position += exitAnchor.transform.position - entranceAnchor.transform.position;

      foreach (Camera cam in FindObjectsOfType<Camera>())
        if (cam.gameObject.scene == previousScene) 
          cam.gameObject.SetActive(false);

      yield return null;

      foreach (CinemachineVirtualCamera cam in FindObjectsOfType<CinemachineVirtualCamera>()) 
        if (cam.gameObject.scene == previousScene) 
          cam.Priority = 0;

      Camera camera = GameObject.FindWithTag("MainCamera")?.GetComponent<Camera>();
      CinemachineVirtualCamera virtualCamera = null;
      foreach (CinemachineVirtualCamera candidate in FindObjectsOfType<CinemachineVirtualCamera>())
        if (candidate.gameObject.scene != previousScene)
          if (virtualCamera == null || virtualCamera.Priority < candidate.Priority)
            virtualCamera = candidate;
          
      float eps = 0.001f;
      while (Vector3.Distance(camera.transform.position, virtualCamera.transform.position) > eps) 
        yield return null;

      SceneManager.UnloadSceneAsync(previousScene);
    }
  }
}
