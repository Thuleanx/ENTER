using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Enter {
    public class BubbleManager : MonoBehaviour {
        public static BubbleManager Instance;

        [SerializeField, Min(1)] private int _poolDefaultSize = 5;
        [SerializeField, Min(1)] private int _poolExpansionSize = 5;

        class Pool {
            // TODO: Edit this with a more appropriate datastructure 
            // like a queue or something 
            public List<Bubble> bubbles = new List<Bubble>();
        };

        // Dictionary that maps InstanceID to Pools of objects spawned from one
        // with this instance id.
        Dictionary<int, Pool> pools = new Dictionary<int, Pool>();

        void Awake() { Instance = this; }

        void Start() {
            // when reloading, we find all bubbles belonging to that scene, and
            // calls collect on them
            SceneTransitioner.Instance?.OnReload.AddListener((scene) => {
                foreach (Bubble bubble in FindObjectsOfType<Bubble>()) {
                    if (bubble.gameObject.scene == scene)
                        bubble.gameObject.SetActive(false);
                }
            });
            SceneTransitioner.Instance?.OnTransition.AddListener((current, next) => {
                foreach (Bubble bubble in FindObjectsOfType<Bubble>()) 
                    if (bubble.gameObject.scene == current)
                        bubble.gameObject.SetActive(false);
            });
        }

        // TODO: If a pool goes cold for too long, we start destroying its 
        // objects to free memory

        /** 
         * <summary> Collect a bubble and bring it back into its pool </summary>
         */
        void Collect(Bubble bubble) {
            int id = bubble.id;
            // if the parent isn't null, we give up on collecting this bubble
            if (bubble.transform.parent == null) {
                if (!pools.ContainsKey(bubble.id))
                    pools.Add(id, new Pool());
                bubble.gameObject.SetActive(false);
                pools[bubble.id].bubbles.Add(bubble);
                DontDestroyOnLoad(bubble.gameObject);
            }
        }

        /**
         * <summary> 
         * Borrow an instance of a prefab from its pool. 
         * We also tie it to a scene, so that it automatically returns to the pool
         * when the scene is unloaded
         * </summary>
         *
         * <param name="prefab"> the prefab you want an instance of </param>
         * <param name="positionNullable"> an optional position for the instance </param>
         * <param name="rotationNullable"> an optional rotation for the instance </param>
         */
        public GameObject Borrow(Scene scene, GameObject prefab, Vector3? positionNullable = null, Quaternion? rotationNullable = null) {
            int id = prefab.GetInstanceID();

            // we first see if the pool exists, if not we create it
            if (!pools.ContainsKey(id)) 
            {
                pools.Add(id, new Pool());
                ExpandPool(prefab, _poolDefaultSize);
            } 
            // if the pool doesn't have any element, we expand it
            else if (pools[id].bubbles.Count == 0) 
            {
                ExpandPool(prefab, _poolExpansionSize);
            }

            Pool currentPool = pools[id];

			Vector3 position = positionNullable == null ? Vector3.zero : (Vector3) positionNullable;
			Quaternion rotation = rotationNullable == null ? Quaternion.identity : (Quaternion) rotationNullable;

            int indexToRetrieve = currentPool.bubbles.Count - 1;
            // we get last bubble in its pool
			Bubble bubble = currentPool.bubbles[indexToRetrieve];
            currentPool.bubbles.RemoveAt(indexToRetrieve);
            // set its transform and rotation. 
			bubble.gameObject.transform.SetPositionAndRotation(position, rotation);
			bubble.gameObject.SetActive(true);
			SceneManager.MoveGameObjectToScene(bubble.gameObject, scene);
            // tells the bubble to run Collect on Disabled
			bubble.OnDisposal = Collect; 

            return bubble.gameObject;
        }

        /**
         * <summary> 
         * Shortcut for borrowing an instance and calling GetComponent on it. You will
         * get back a pair of values, the first being the component you ask for, the second the instance
         * you borrowed
         * </summary>
         *
         * <typeparam name="T"> the component to look for after spawning to object </typeparam>
         * <param name="prefab"> the prefab you want an instance of </param>
         * <param name="positionNullable"> an optional position for the instance </param>
         * <param name="rotationNullable"> an optional rotation for the instance </param>
         */
        public (T, GameObject) BorrowTyped<T>(Scene scene, GameObject prefab, Vector3? positionNullable = null, Quaternion? rotationNullable = null) {
            GameObject gameObject = Borrow(scene, prefab, positionNullable, rotationNullable);
            return (gameObject.GetComponent<T>(), gameObject);
        }

        /**
         * @summary Expand a particular pool by some amount by instantiating that amount
         * of objects.
         * 
         * @param prefab prefab the pool is modeled with
         * @param count number of objects to instantiate and add to the pool
         */
        void ExpandPool(GameObject prefab, int count) {
            int id = prefab.GetInstanceID();
            while (count --> 0) {

                // instantiate the prefab in an inactive state
                bool defaultActive = prefab.activeSelf;
                prefab.SetActive(false);
                GameObject obj = Instantiate(prefab);
                prefab.SetActive(defaultActive);

                // grant immortality
                DontDestroyOnLoad(obj);

                // add bubble as a monobehaviour to this object. This makes it 
                // automatically return to the pool after being disabled
                Bubble bubble = obj.GetComponent<Bubble>();
                if (!bubble) bubble = obj.AddComponent<Bubble>();

                // very important: assign id to the bubble. 
                // this is because the bubble itself (or the system)
                // has no idea which prefab is used to spawn the object
                bubble.id = id;

                // add object to pool
                pools[id].bubbles.Add(bubble);
            }
        }
    }

    public class Bubble : MonoBehaviour {
        public int id;
        Coroutine collectCoroutine;
        [HideInInspector] public UnityAction<Bubble> OnDisposal = null;

        void OnEnable() {
            // if enabled somehow, we should stop collecting it
            if (collectCoroutine != null) BubbleManager.Instance.StopCoroutine(collectCoroutine);
        }

        void OnDestroy() {
            // if destroyed somehow and we were trying to collect it, we need to stop the 
            // coroutine because references might be lost when the object is destroyed
            if (collectCoroutine != null) BubbleManager.Instance.StopCoroutine(collectCoroutine);
        }

        void OnDisable() {
            if (transform.parent == null) {
                // if parent is null, then we can set this to dontdestroyonload and preserve the object
                OnDisposal?.Invoke(this);
            } else {
                // we need to wait one frame to be able to change 
                // we start the coroutine from the bubble manager because 
                // coroutines on disabled object (like this one) won't run, so we need
                // it to run on an object that's always active
                collectCoroutine = BubbleManager.Instance.StartCoroutine(CollectsAfterOneFrame());
            }
        }

        public IEnumerator CollectsAfterOneFrame() {
            // we wait for one frame. After this wait, we can set the parent.
            yield return null;
            if (gameObject) {
                transform.parent = null;
                // we need to check if the gameObject is destroyed already. 
                // If it has, then we give up on putting this Bubble back into the pool
                OnDisposal?.Invoke(this);
            }
        }
    }
}
