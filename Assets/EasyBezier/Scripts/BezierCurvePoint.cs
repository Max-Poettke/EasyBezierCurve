using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.U2D.Path.GUIFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class BezierCurvePoint : MonoBehaviour
{
    [Header("Customization")] 
    [SerializeField] public Color uIColor;
    [SerializeField] public Color curvePointColor;
    
    [Header("The curve")]
    public List<GameObject> hardPoints;

    private List<Vector3> pointPositions = new List<Vector3>();
    List<List<Vector3>> allPositions = new List<List<Vector3>>();
    private int iterations;
    public int steps = 20;
    [HideInInspector]public enum CurveGeneration {Generated, Direct}
    [HideInInspector]public enum CurveEnd {Open, Looping}

    public CurveGeneration generationType = CurveGeneration.Direct;
    public CurveEnd curveEnd = CurveEnd.Open;

    [Header ("Move Object along curve")]
    public GameObject objectToMove;
    public float speed = 1;
    //create different states for defining movement behavior (loop, pingpong, etc.)

    public MovementType movementType = MovementType.Once;
    
    [HideInInspector]public int currentStep = 0;
    [HideInInspector]public List<Vector3> finishedCurvePoints = new List<Vector3>();
    [HideInInspector]public enum MovementType {Loop, PingPong, Once}

    //Start of code ----------------------------------------------------------------------------------------------------
    private void Update()
    {
        MoveAlong();
    }

    //Gets called when a variable in the editor is changed
    private void OnValidate()
    {
        for (int i = 0; i < hardPoints.Count; i ++)
        {   
            if (hardPoints[i].GetComponent<EditableControlPoint>() == null)
            {
                hardPoints[i].AddComponent<EditableControlPoint>();
                ResetTangents();
            }

            hardPoints[i].GetComponent<EditableControlPoint>().color = uIColor;
        }
        GetComponent<EditableControlPoint>().color = uIColor;

        if (curveEnd == CurveEnd.Looping)
        {
            if (hardPoints[hardPoints.Count - 1] != gameObject)
            {
                hardPoints.Add(gameObject);
            }
        }
        else
        {
            if (hardPoints[hardPoints.Count - 1] == gameObject)
            {
                hardPoints.RemoveAt(hardPoints.Count - 1);
            }
        }
    }
    
    void ResetTangents()
    {
        for (int i = 0; i < hardPoints.Count; i++)
        {
            EditableControlPoint eP = hardPoints[i].GetComponent<EditableControlPoint>();
            if (i == hardPoints.Count - 1 && i != 0)
            {
                Vector3 dir = hardPoints[i].transform.position - hardPoints[i - 1].transform.position;
                eP.SetLength(dir.magnitude / 4);
                eP.SetTangentDirection(dir.normalized);
            }
            else if (i == 0)
            {
                Vector3 dir = hardPoints[i].transform.position - transform.position;
                eP.SetLength(dir.magnitude / 4);
                eP.SetTangentDirection(dir.normalized);               
            } else 
            {
                Vector3 dir = hardPoints[i + 1].transform.position - hardPoints[i].transform.position;
                eP.SetLength(dir.magnitude / 4);
                eP.SetTangentDirection(dir.normalized);
            }    
        }
    }
    
    private bool frameBuffer = false;
    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (frameBuffer == false)
        {
            frameBuffer = true;
            return;
        }
        Gizmos.color = uIColor;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        for (int i = 0; i < hardPoints.Count; i++)
        {
            if (hardPoints[i] == null) return;
        }
        if(pointPositions.Count != 0) pointPositions.Clear();
        allPositions.Clear();
        finishedCurvePoints.Clear();
        if (generationType == CurveGeneration.Direct)
        {
            pointPositions.Add(transform.position);
            foreach (var p in hardPoints)
            {
                pointPositions.Add(p.transform.position);
            }
            GenerateThroughControlPoints(true, pointPositions);
        }
        if (generationType == CurveGeneration.Generated) GenerateThroughSplines(true);
    }
    #endif
    
    //Everything to do with generating the Curves ----------------------------------------------------------------------
    
    private void GenerateThroughSplines(bool draw)
    {
        //now hardPoints represents points already on the curve. So we need to generate the controlPoints through them
        //need to generate the curve for all pairs
        if (hardPoints[0].GetComponent<EditableControlPoint>().GetTangentPoints() == null) return;
        for (int i = 0; i < hardPoints.Count; i++)
        {
            pointPositions.Clear();
            GameObject p;
            if (i == hardPoints.Count - 1 && curveEnd == CurveEnd.Looping)
            {
                p = gameObject;
            }
            else
            {
                p = hardPoints[i];    
            }
            var cp = p.GetComponent<EditableControlPoint>();
            var thisEditablePoint = GetComponent<EditableControlPoint>();

            if (i == 0)
            {
                pointPositions.Add(transform.position);
                pointPositions.Add(thisEditablePoint.GetTangentPoints()[0]);
                pointPositions.Add(cp.GetTangentPoints()[1]);
                pointPositions.Add(p.transform.position);
            } 
            else
            {
                var prevPoint = hardPoints[i - 1];
                var prevP = prevPoint.GetComponent<EditableControlPoint>();
                pointPositions.Add(prevPoint.transform.position);
                pointPositions.Add(prevP.GetTangentPoints()[0]);
                pointPositions.Add(cp.GetTangentPoints()[1]);
                pointPositions.Add(p.transform.position);
            }
            GenerateThroughControlPoints(draw, pointPositions);
        }
    }

    private void GenerateThroughControlPoints(bool draw, List<Vector3> controlPoints)
    {
        allPositions.Clear();
        iterations = controlPoints.Count - 2;
        for (int i = 1; i < controlPoints.Count; i++)
        { 
            GetInterpolationPoints(controlPoints[i - 1]
                , controlPoints[i], steps, draw);
        }
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
        }
        allPositions.Add(positions);
    }

    void Generate(bool draw)
    {
        List<Vector3> positions = new List<Vector3>();
        List<List<Vector3>> nAllPositions = new List<List<Vector3>>();
        if(iterations == 1 && draw) Gizmos.color = curvePointColor;
        for (int i = 1; i < allPositions.Count; i++)
        {
            for (int j = 0; j < allPositions[i].Count; j++)
            {
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

    //Here come all the movement functions -----------------------------------------------------------------------------
    
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


[CustomEditor(typeof(EditableControlPoint)), CanEditMultipleObjects]
public class CurveEditor : Editor
{
    private void OnSceneGUI()
    {
        EditableControlPoint p = (EditableControlPoint) target;
        EditorGUI.BeginChangeCheck();
        if (p.GetTangentPoints() == null) return;
        Vector3 newPosition = Handles.FreeMoveHandle(p.GetTangentPoints()[0], Quaternion.identity
            , 0.3f, Vector3.one * 0.01f, Handles.SphereHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            var difference = newPosition - p.transform.position;
            p.SetTangentDirection((difference).normalized);
            p.SetLength(difference.magnitude);
        }
    }
}
