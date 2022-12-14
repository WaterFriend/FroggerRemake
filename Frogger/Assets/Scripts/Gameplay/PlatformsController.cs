using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class PlatformsController : MonoBehaviour
{
    // Start is called before the first frame update
    [Serializable]
    public class Path   // used to make the object move along the trajectory
    {
        public Transform point;
        public float moveTime;
        public float waitTime;
        public Vector3 speed;  // move speed to next point, automatically calculated in start function
    }

    public Path[] path;
    public enum MovementMethod 
    {
        repeating, bouncing, stopping
    };
    public MovementMethod movementMethod;
    public int position = 0;  // the number of point the object currently at
    public float sinkTime = 0;  // how long will platform sink
    public float sinkInterval = -1;  // -1 means never sink
    public bool sink = false;   // platform is sinking or not
    public float initialWaitTime = 0;  // wait time for starting only
    public float direct = 0;
    public Color linesColor = Color.blue;  // line's color that links points in the editor
    public Animator animator;
    private float remainingWaitTime = 0, remainingMoveTime=0;
    private int direction = 1;
    private bool stopped=false, turned=false;  // turned: record if object turned in last frame when it's in blancing method
    private float remainingSinkTime=0, remainingSinkInterval=0;

    void Start()
    {
        for(int i = 0; i < path.Length - 1; i++)
            path[i].speed = (path[i + 1].point.position - path[i].point.transform.position) / path[i].moveTime;
        path[^1].speed = (path[0].point.position - path[^1].point.transform.position) / path[^1].moveTime;
        // dont forget to set the last point's speed
        remainingMoveTime = path[0].moveTime;
        remainingWaitTime = path[0].waitTime;
        if (sink)
        {
            remainingSinkTime = sinkTime;
            remainingSinkInterval = 0;
        }
        else
        {
            remainingSinkInterval = sinkInterval;
            remainingSinkTime = 0;
        }
        transform.position = path[position].point.position;

        if (animator != null)
            animator.SetBool("IsObstacle", tag == "Obstacle" ? true : false);
    }

    // Update is called once per frame
    void Update()
    {
        if (animator != null)
            animator.SetFloat("Direction", direct);

        if (stopped)
            return;
        if (initialWaitTime>0)
        {
            initialWaitTime -= Time.deltaTime;
            return;
        }
        if (remainingWaitTime > 0) // still waiting
        {
            remainingWaitTime -= Time.deltaTime;
            remainingWaitTime = Mathf.Max(remainingWaitTime, 0);
        }
        if (remainingWaitTime == 0 && remainingMoveTime >0)  // still moving
        {
            remainingMoveTime -= Time.deltaTime;
            remainingMoveTime = Mathf.Max(remainingMoveTime, 0);
            transform.position += direction * Time.deltaTime * path[position].speed;
        }
        if (remainingWaitTime==0&& remainingMoveTime == 0)  // time to head off next point
        {
            position += direction;
            if (movementMethod == MovementMethod.bouncing &&
                ((position == path.Length && direction == 1) || (position == -1 && direction == -1)))
            {
                direction = -direction;  // change direction
                position += direction;
                turned = true;
            }
            else if (movementMethod == MovementMethod.repeating && position == path.Length)
            {
                position = 0;
                transform.position = path[0].point.position;  // make it back to first point
            }
            else if (movementMethod == MovementMethod.stopping && position == path.Length)
            {
                stopped = true;
                return;
            }

            // prevent funny stuffs from happening if dropping frames
            if (turned)
            {
                transform.position = path[0].point.position;
                turned = false;
            }
            else if (movementMethod == MovementMethod.bouncing && direction<0)
                transform.position = path[position-direction].point.position;   // when object is blancing back
            else
                transform.position = path[position].point.position;
            remainingMoveTime = path[position].moveTime;
            remainingWaitTime = path[position].waitTime;
        }

        
        if (sinkInterval>0)
        {
            if (remainingSinkInterval>0)    
                remainingSinkInterval -= Time.deltaTime;
            if (remainingSinkTime > 0)
                remainingSinkTime -= Time.deltaTime;
            if (!sink && remainingSinkInterval <= 0)  // Time to sink!
            {
                sink = true;
                remainingSinkInterval = 0;
                remainingSinkTime = sinkTime;
                Color color = GetComponent<Renderer>().material.color;   // sink code
                color.a = 0;
                GetComponent<Renderer>().material.color = color;
            }
            else if (sink && remainingSinkTime <= 0)  // Time to float!
            {
                sink = false;
                remainingSinkInterval = sinkInterval;
                remainingSinkTime = sinkTime;
                Color color = GetComponent<Renderer>().material.color;
                color.a = 1;
                GetComponent<Renderer>().material.color = color;
            }
        }

    }
    void OnDrawGizmos()  // draw lines on the editer
    {
        Gizmos.color = linesColor;
        for (int i = 0; i < path.Length - 1; i++)
        {
            if (path[i].point && path[i + 1].point)
            {
                Gizmos.DrawLine(path[i].point.position, path[i + 1].point.position);
            }
        }
    }
}
