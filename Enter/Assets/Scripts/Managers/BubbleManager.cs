using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace Enter
{
  [DisallowMultipleComponent]
  public class BubbleManager : MonoBehaviour
  {
    public static BubbleManager Instance;

    // A pool of bubbles. Possible todo: change datastructure
    private class Pool
    {
      private List<Bubble> _bubbles = new List<Bubble>(); 

      public bool Empty => _bubbles.Count == 0;

      public void BubblePush(Bubble bubble)
      {
        _bubbles.Add(bubble);
      }

      public Bubble BubblePop()
      {
        int i = _bubbles.Count - 1;
        Bubble bubble = _bubbles[i];
        _bubbles.RemoveAt(i);

        return bubble;
      }
    };

    // A monobehaviour which must be attached to any game object (created
    // from a prefab) which is to be put into an object pool
    private class Bubble : MonoBehaviour
    {
      public int id; // the instance ID of the prefab used to create this object

      private Coroutine _currentCoroutine;
      
      [HideInInspector] public UnityAction<Bubble> OnDisposal = null;

      void OnEnable()
      {
        // If enabled mid-collection somehow, stop collecting it
        if (_currentCoroutine != null) BubbleManager.Instance.StopCoroutine(_currentCoroutine);
      }

      void OnDestroy()
      {
        // If destroyed mid-collection somehow, stop collecting it as
        // references might be lost when the object is destroyed
        if (_currentCoroutine != null) BubbleManager.Instance.StopCoroutine(_currentCoroutine);
      }

      void OnDisable()
      {
        if (transform.parent == null)
        {
          // If parent is null, we can set this to dontdestroyonload and preserve the object
          BubbleManager.Instance.Collect(this);
        }
        else
        {
          // we need to wait one frame to be able to change 
          // we start the coroutine from the bubble manager because 
          // coroutines on disabled object (like this one) won't run, so we need
          // it to run on an object that's always active
          _currentCoroutine = BubbleManager.Instance.StartCoroutine(CollectsAfterOneFrame());
        }
      }

      public IEnumerator CollectsAfterOneFrame()
      {
        // We wait for one frame. After this wait, we can set the parent.
        yield return null;
        if (gameObject)
        {
          transform.parent = null;
          // We need to check if the gameObject is destroyed already. 
          // If it has, then we give up on putting this Bubble back into the pool
          BubbleManager.Instance.Collect(this);
        }
      }
    }

    #region BubbleManager

    [SerializeField, Min(1)] private int _poolDefaultSize   = 5;
    [SerializeField, Min(1)] private int _poolExpansionSize = 5;

    Dictionary<int, Pool> pools = new Dictionary<int, Pool>();

    void Awake() { Instance = this; }

    void Start()
    {
      Debug.Log(SceneTransitioner.Instance.OnTransition);

      // Add listener to scene reload, to find all bubbles belonging to that
      // scene and Collect() them back by disabling them
      SceneTransitioner.Instance.OnReload.AddListener((scene) => {
        foreach (Bubble bubble in FindObjectsOfType<Bubble>())
          if (bubble.gameObject.scene == scene)
            bubble.gameObject.SetActive(false);
      });

      // Add listener to scene transition, to find all bubbles belonging to
      // the older scene and Collect() them back by disabling them
      SceneTransitioner.Instance.OnTransition.AddListener((current, next) => {
        Debug.Log(current);
        Debug.Log(next);
        foreach (Bubble bubble in FindObjectsOfType<Bubble>()) 
          if (bubble.gameObject.scene == current)
            bubble.gameObject.SetActive(false);
      });
    }

    // Collects a bubble by bringing it back into its pool
    void Collect(Bubble bubble)
    {
      int id = bubble.id;

      // If the parent isn't null, give up on collecting this bubble
      if (bubble.transform.parent != null) return;

      bubble.gameObject.SetActive(false);
      DontDestroyOnLoad(bubble.gameObject);

      pools[id].BubblePush(bubble);
    }

    // Borrow an instance of a prefab from its pool. 
    // We also tie it to a scene, so that it automatically returns to the pool
    // when the scene is unloaded
    public GameObject Borrow(Scene       scene,
                             GameObject  prefab,
                             Vector3?    positionNullable = null,
                             Quaternion? rotationNullable = null)
    {
      int id = prefab.GetInstanceID();

      Vector3    position = positionNullable ?? Vector3.zero;
      Quaternion rotation = rotationNullable ?? Quaternion.identity;

      // If the pool for that particular prefab does not yet exist, create it
      if (!pools.ContainsKey(id)) 
      {
        pools.Add(id, new Pool());
        ExpandPool(prefab, _poolDefaultSize);
      }
      else if (pools[id].Empty) 
      {
        ExpandPool(prefab, _poolExpansionSize);
      }


      // Get a bubble from the pool
      Bubble bubble = pools[id].BubblePop();

      // Set its transform and rotation, then activate it and put it in the scene
      bubble.gameObject.transform.SetPositionAndRotation(position, rotation);
      bubble.gameObject.SetActive(true);
      SceneManager.MoveGameObjectToScene(bubble.gameObject, scene);

      return bubble.gameObject;
    }

    // Expand a particular pool of prefab instances by some amount
    void ExpandPool(GameObject prefab, int amount)
    {
      int id = prefab.GetInstanceID();

      // Do this "amount" times
      while (amount-- > 0)
      {
        // Instantiate a prefab instance, obj, while the prefab is inactive
        // Important: this prevents any OnEnables from running
        bool temp = prefab.activeSelf;
        prefab.SetActive(false);
        GameObject obj = Instantiate(prefab);
        prefab.SetActive(temp);

        // Grant immortality
        DontDestroyOnLoad(obj);

        // Add a Bubble monobehaviour
        Bubble bubble = obj.GetComponent<Bubble>();
        if (!bubble) bubble = obj.AddComponent<Bubble>();

        // Assign ID to the bubble
        // Important: otherwise, there'd be no way to know which
        //            prefab was used to spawn the object
        bubble.id = id;

        // Add object to pool
        pools[id].BubblePush(bubble);
      }
    }

    // Possible todo: if a pool goes cold for too long, we should start 
    //                destroying its objects to free memory

    #endregion
  }
}
