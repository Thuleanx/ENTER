using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Enter
{
  [DisallowMultipleComponent]
  public class App : MonoBehaviour
  {
    public static App Instance;

    public void Awake()
    {
      Instance = this;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Bootstrap()
    {
      GameObject app = FindObjectOfType<App>()?.gameObject;
      if (app == null)
      {
        app = UnityEngine.Object.Instantiate(Resources.Load("App")) as GameObject;
        if (app == null) throw new ApplicationException();
      }
      UnityEngine.Object.DontDestroyOnLoad(app);
    }
  }
}