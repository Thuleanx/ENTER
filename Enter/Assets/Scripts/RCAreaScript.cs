using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
      updateValidPoints();
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

    private void updateValidPoints()
    {
      _validPoints.Clear();

      List<Vector2> points = new List<Vector2>();
      int numPoints = 0;

      // For each path in this composite collider
      for (int i = 0; i < _co.pathCount; ++i)
      {
        numPoints = _co.GetPath(i, points);

        Assert.IsTrue(numPoints == 4, "All RC areas must be rectangles");

        // Find min and max x and y (unrolled version; regular version at bottom of file)
        Vector2 point0 = transform.TransformPoint(points[0]);
        Vector2 point1 = transform.TransformPoint(points[1]);
        Vector2 point2 = transform.TransformPoint(points[2]);
        Vector2 point3 = transform.TransformPoint(points[3]);
        List<float> xs = new List<float>{point0.x, point1.x, point2.x, point3.x};
        List<float> ys = new List<float>{point0.y, point1.y, point2.y, point3.y};
        int minX = Mathf.RoundToInt(xs.Min());
        int maxX = Mathf.RoundToInt(xs.Max());
        int minY = Mathf.RoundToInt(ys.Min());
        int maxY = Mathf.RoundToInt(ys.Max());

        // Get inner integer-grid points
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        for (int x = minX + 1; x < maxX; ++x)
        {
          for (int y = minY + 1; y < maxY; ++y)
          {
            Vector2 v = new Vector2(x, y);
            if (_co.OverlapPoint(v)) _validPoints.Add(v);
          }
        }
      }
    }

    #endregion
  }
}

// // Find min and max x and y (regular version, backup copy)
// int maxX = int.MinValue;
// int maxY = int.MinValue;
// int minX = int.MaxValue;
// int minY = int.MaxValue;
// for (int j = 0; j < numPoints; ++j)
// {
//   // The grid is local, but the point should be global!
//   Vector2 point = transform.TransformPoint(points[j]);
//   int x = Mathf.RoundToInt(point.x);
//   int y = Mathf.RoundToInt(point.y);
//   if (x > maxX) maxX = x;
//   if (x < minX) minX = x;
//   if (y > maxY) maxY = y;
//   if (y < minY) minY = y;
// }
