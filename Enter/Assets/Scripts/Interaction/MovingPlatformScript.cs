using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatformScript : MonoBehaviour
{
    // points is a list of game objects that have transforms that indicate the points at which the platform should move through
    public Transform[] points;
    public float speed;
    public int start;
    public int currentPoint;

    void Start()
    {
        transform.position = points[start].position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Vector2.Distance(transform.position, points[currentPoint].position) < 0.002f){
            currentPoint ++;
            if (currentPoint >= points.Length){
                currentPoint = 0;
            }
        }
        transform.position = Vector2.MoveTowards(transform.position, points[currentPoint].position, speed * Time.deltaTime);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i=0; i < points.Length; i++){
            Vector2 start = points[i].position;
            Vector2 end;
            if (i==points.Length-1){
                end = points[0].position;
            } else
            {
                end = points[i+1].position;
            }
            Gizmos.DrawLine(start, end);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player" && collision.transform.position.y > transform.position.y){
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player") {
            collision.transform.SetParent(null);
            DontDestroyOnLoad(collision.gameObject);            
        }
    }

}
