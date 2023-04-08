using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using NaughtyAttributes;

namespace Enter
{
  [RequireComponent(typeof(CompositeCollider2D))]
  public class RCAreaScript : MonoBehaviour
  {
    [SerializeField] private CompositeCollider2D _co;

    List<Vector2> _validPoints = new List<Vector2>();

    #region ================== Methods

    void Start()
    {
        updateValidPoints();
    }
    
    void OnEnable()
    {
		SceneTransitioner.Instance.OnTransitionAfter.AddListener(UpdateValidPoints);
		SceneTransitioner.Instance.OnReloadAfter.AddListener(UpdateValidPoints);
    }

	void OnDisable() {
		SceneTransitioner.Instance.OnTransitionAfter.RemoveListener(UpdateValidPoints);
		SceneTransitioner.Instance.OnReloadAfter.AddListener(UpdateValidPoints);
	}

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
      updateValidPoints(); // the scenes passed into here are useless
      _validPoints.ForEach((v) => Gizmos.DrawSphere(new Vector3(v.x, v.y, 0), 0.5f));
    }
#endif

    public Vector2 FindClosestValidPoint(Vector2 target)
    {
      // This should only ever be called when there are RC areas and one got clicked
      Assert.IsTrue(_validPoints.Count != 0);

      Vector2 bestPoint  = Vector2.zero;
      float bestDistance = Mathf.Infinity;

      for (int i = 0; i < _validPoints.Count; ++i)
      {
        float distance = Vector2.Distance(_validPoints[i], target);
        if (distance < bestDistance)
        {
          bestPoint    = _validPoints[i];
          bestDistance = distance;
        }
      }

      return bestPoint;
    }

    #endregion

    #region ================== Helpers

    public void UpdateValidPoints(Scene _, Scene __)
    {
		updateValidPoints();
    }

    [Button("Recompute")]
	private void updateValidPoints() {
        _validPoints.Clear();

        List<Vector2> points = new List<Vector2>();
        int numPoints = 0;

        List<KeyValuePair<Vector2, Vector2>> allEdges = new List<KeyValuePair<Vector2, Vector2>>();

		// Unity weirdly gives us back vertices with (x,y) values slightly off to where they actually should be. 
		// For integer points, this is really bad, because you can imagine if we just shift the points by a little, our answer is now different.
		// Therefore, as a preprocessing step we are rounding the x and y coordinates of each point if they are too close to an integer point
        float EPS = 0.01f;
        Func<Vector2, Vector2> round = (p) => {
            if (Mathf.Abs(p.x - Mathf.Round(p.x)) < EPS) p.x = Mathf.Round(p.x);
            if (Mathf.Abs(p.y - Mathf.Round(p.y)) < EPS) p.y = Mathf.Round(p.y);
            return  p;
        };

		// We go through all the paths and accumulate all the edges after applying transform
        for (int i = 0; i < _co.pathCount; ++i) {
            numPoints = _co.GetPath(i, points);
            for (int j = 0; j < numPoints; j++) {
                Vector2 a = transform.TransformPoint(points[j]),
                        b = transform.TransformPoint(points[(j + 1) % numPoints]);
                a = round(a); 
                b = round(b);
                allEdges.Add(new KeyValuePair<Vector2, Vector2>(a, b));
            }
        }

		// so our scan line method can't classify points on the right edge of the polygon as being outside
		// making it recognize this wouldn't be hard, but it'll be hairy and requires thinking
		// how we fix this is we run the same algorithm on the input points with the x values negated, then only accept
		// points that is accepted by both runs of the algorithm
		// this insideMap exists just for this purpose. It maps a point to the number of passes accepts that point as "inside"
		// we want the number to be 2 to consider the point as inside
        Dictionary<Vector2Int, int> insideMap = new Dictionary<Vector2Int, int>();

        for (int negatedPass = 0; negatedPass < 2; negatedPass++) {
            // why do we have allEdges, and this edges List
            // we are running 2 passes and don't want to call GetPath twice on the same path
            // in fear that Unity will return slighly different result that might affect correctness of this algorithm
            List<KeyValuePair<Vector2, Vector2>> edges = new List<KeyValuePair<Vector2, Vector2>>();

            int lox = int.MaxValue, hix = int.MinValue;

            // this constructs edges and ignore any vertical edge
            for (int i = 0; i < allEdges.Count; i++) {
                Vector2 a = allEdges[i].Key, b = allEdges[i].Value;
				// we negate x if 
                if (negatedPass == 1) {
					a.x *= -1;
					b.x *= -1;
                }
                if (a.x == b.x) continue; // we don't care about vertical edges. Nearly vertical edges are fine though
                if (a.x > b.x) {
                    // swap edges to ensure that a.x < b.x
                    Vector2 intermediate = a;
                    a = b;
                    b = intermediate;
                }
                lox = Mathf.Min(Mathf.CeilToInt(a.x), lox); 
                hix = Mathf.Max(Mathf.FloorToInt(b.x), hix); 
                edges.Add(new KeyValuePair<Vector2, Vector2>(a,b));
            }

            int n = edges.Count;
            // we are storing the endpoints as integer index to the edges. These 
            // are in the range [-n, n). negative numbers represent the right endpoints of an edge, whereas positive one represent the starting endpoint
            // -i, for instance, represents the right endpoint of the edge at n - i
            List<int> indices = new List<int>(edges.Count * 2);
            for (int i = 0; i < 2*n; i++) 
                indices.Add( i - n );

            Func<int, Vector2> getEndpointByIndex = (index) => index < 0 ? edges[index + n].Value : edges[index].Key;

            indices.Sort((i,j) => {
                Vector2 p = getEndpointByIndex(i), 
                q = getEndpointByIndex(j);
                if (p.x == q.x) return 0;
                return (int) Mathf.Sign(p.x - q.x);
            });
            if (edges.Count == 0) continue;

            // we are moving a pointer k through the list of sorted edges to keep track of the edge whose x value is 
            // right below the one we're currently on. Normally, you might use a priority queue, but We use this 
            // instead of a priority queue because it's faster and easier since C# does not have a priority queue
            int k = 0;
            HashSet<int> activeEdges = new HashSet<int>();

            // loop through the possible ranges for x
            for (int x = lox; x <= hix; x++) {
                List<int> toRemoveAfterX = new List<int>();

                // we need to update to see which edges are in our scan line
                // one way is to loop through all edge endpoints whose x value is below x
                // and see which ones has the left endpoint < x, but right endpoint >= x.
                
                // a nicer way to do this is to take the previous scan line, and update 
                // it by looking at the endpoints in between the previous scan line and this x value
                // k lets us keep track of where to look for this set of endpoints
                Vector2 pos;
                while (k < 2 * n && (pos = getEndpointByIndex(indices[k])).x < x) {
                    if (indices[k] < 0) activeEdges.Remove(indices[k] + n);
                    else                activeEdges.Add(indices[k]);
                    k++;
                }

                List<int> activeEdgesList = activeEdges.ToList();

                Func<int, int, float> interpolate = (indexOfEdge, xValue) => {
                    if (indexOfEdge < 0) indexOfEdge += n;
                    float t = (xValue - edges[indexOfEdge].Value.x) / (edges[indexOfEdge].Key.x - edges[indexOfEdge].Value.x);
                    float pos = Mathf.Lerp(edges[indexOfEdge].Key.y, edges[indexOfEdge].Key.y, t);
                    return pos;
                };

                // sort by y value of each edge at x
                activeEdgesList.Sort((i, j) => (int) Mathf.Sign(interpolate(i,x) - interpolate(j,x)));

                Assert.IsTrue(activeEdgesList.Count % 2 == 0, "expect number of edges crossing a certain point to be divisible by 2. Get: " + activeEdgesList.Count + " at " + x);

                // in between every other adjacent pairs of edges are the points we're looking for
                // so we iterate through all these pairs
                for (int i = 0; i + 1 < activeEdgesList.Count; i+=2) {

                    int lo = (int) (interpolate(activeEdgesList[i], x) + 1);
                    int hi = Mathf.CeilToInt(interpolate(activeEdgesList[i + 1], x) - 1);

                    // enumerate all y values in between our adjacent pair of edges
                    for (int y = lo; y <= hi; y++) {
                        Vector2Int point = negatedPass == 1 ? new Vector2Int(-x, y) : new Vector2Int(x, y);
                        if (!insideMap.ContainsKey(point)) 
                            insideMap.Add(point, 0);
                        insideMap[point]++;
                    }
                }
            }

        }

        // we accept points as valid if they appear in both passes
        foreach (KeyValuePair<Vector2Int, int> pointMapped in insideMap) {
            if (pointMapped.Value > 1)
                _validPoints.Add(pointMapped.Key);
        }
	}

    #endregion
  }
}

