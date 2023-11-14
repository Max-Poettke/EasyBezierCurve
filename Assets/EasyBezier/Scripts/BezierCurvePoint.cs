using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Unity.VisualScripting;
using UnityEditor.U2D.Path.GUIFramework;
using UnityEngine;

public class BezierCurvePoint : MonoBehaviour
{
    [Header("The curve")]
    public List<GameObject> controlPoints;
    List<List<Vector3>> allPositions = new List<List<Vector3>>();
    private int iterations;
    public int steps = 20;

    [Header ("Move Object along curve")]
    public GameObject objectToMove;
    public float speed = 1;
    //create different states for defining movement behavior (loop, pingpong, etc.)
    
    public MovementType movementType = MovementType.Once;
    
    [HideInInspector]public int currentStep = 0;
    [HideInInspector]public List<Vector3> finishedCurvePoints = new List<Vector3>();
    [HideInInspector]public enum MovementType {Loop, PingPong, Once}

    private void Update()
    {
        MoveAlong();
    }

    private void Start()
    {
        GenerateFinishedPoints(false);
    }

    void OnDrawGizmos()
    {
        GenerateFinishedPoints(true);
    }

    private void GenerateFinishedPoints(bool draw)
    {
        iterations = controlPoints.Count - 1;
        allPositions.Clear();
        Gizmos.color = Color.white;
        for (int i = 0; i < controlPoints.Count; i++)
        {
            if (i == 0)
            {
                if(draw) Gizmos.DrawLine(transform.position, controlPoints[i].transform.position);
                GetInterpolationPoints(transform.position, controlPoints[i].transform.position, steps, draw);
            }
            else
            {
                if(draw)Gizmos.DrawLine(controlPoints[i - 1].transform.position, controlPoints[i].transform.position);
                GetInterpolationPoints(controlPoints[i - 1].transform.position, controlPoints[i].transform.position, steps, draw);
            }
            if(draw)Gizmos.DrawSphere(controlPoints[i].transform.position, 0.1f);
        }
        if(draw)Gizmos.color = Color.red;
        Generate(draw);
    }
    
    void GetInterpolationPoints(Vector3 start, Vector3 end, int steps, bool draw)
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 direction = (end - start) / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector3 nPos = start + direction * i + direction / 2;
            positions.Add(nPos);
            if(draw) Gizmos.DrawSphere(nPos, 0.05f);
        }
        allPositions.Add(positions);
    }

    void Generate(bool draw)
    {
        List<Vector3> positions = new List<Vector3>();
        List<List<Vector3>> nAllPositions = new List<List<Vector3>>();
        if(iterations == 1 && draw) Gizmos.color = Color.green;
        for (int i = 1; i < allPositions.Count; i++)
        {
            for (int j = 0; j < allPositions[i].Count; j++)
            {
                //Gizmos.DrawLine(allPositions[i - 1][j], allPositions[i][j]);
                Vector3 newDirection = (allPositions[i][j] - allPositions[i - 1][j]).normalized;
                float stepDistance = Vector3.Distance(allPositions[i - 1][j], allPositions[i][j]) / steps;
                Vector3 newPoint = allPositions[i - 1][j] + newDirection * stepDistance * j;
                positions.Add(newPoint);
            }
            nAllPositions.Add(positions);
            positions = new List<Vector3>();
        }
        
        if (iterations == 1)
        {
            finishedCurvePoints.Clear();
            foreach (Vector3 newPoint in nAllPositions[0])
            {
                if(draw)Gizmos.DrawSphere(newPoint, 0.1f);
                finishedCurvePoints.Add(newPoint);
            }
        }
        
        allPositions.Clear();
        foreach (var pos in nAllPositions)
        {  
            allPositions.Add(pos);
        }
        iterations--;
        if (iterations <= 0) return;
        Generate(draw);
    }

    private void MoveAlong()
    { 
        //Change behavior based on movement type
        if (objectToMove.Equals(null)) return;
        if (currentStep >= finishedCurvePoints.Count) return;

        switch (movementType)
        {
            case MovementType.Loop:
                Loop();
                break;
            case MovementType.PingPong:
                PingPong();
                break;
            case MovementType.Once:
                Once();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void Once()
    {
        Vector3 currentTarget = finishedCurvePoints[currentStep];
        objectToMove.transform.position = Vector3.MoveTowards(objectToMove.transform.position, currentTarget, speed * Time.deltaTime);
        if (Vector3.Distance(objectToMove.transform.position, currentTarget) < 0.1)
        {
            currentStep++;
        }
    }
    
    private void Loop()
    {
        Vector3 currentTarget = finishedCurvePoints[currentStep];
        objectToMove.transform.position = Vector3.MoveTowards(objectToMove.transform.position, currentTarget, speed * Time.deltaTime);
        if (Vector3.Distance(objectToMove.transform.position, currentTarget) < 0.1)
        {
            currentStep++;
            if (currentStep >= finishedCurvePoints.Count)
            {
                currentStep = 0;
            }
        }
    }
    
    
    public bool reverse = false;
    private void PingPong()
    {
        Vector3 currentTarget = finishedCurvePoints[currentStep];
        objectToMove.transform.position = Vector3.MoveTowards(objectToMove.transform.position, currentTarget, speed * Time.deltaTime);
        if (Vector3.Distance(objectToMove.transform.position, currentTarget) < 0.1)
        {
            if (reverse)
            {
                currentStep--;
                if (currentStep <= 0)
                {
                    reverse = false;
                }
            }
            else
            {
                currentStep++;
                if(currentStep >= finishedCurvePoints.Count - 1)
                {
                    reverse = true;
                }
            }
        }
    }
}
