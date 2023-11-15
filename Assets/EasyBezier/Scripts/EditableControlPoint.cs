using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public class EditableControlPoint : MonoBehaviour
{
    private float length;
    private Vector3 tangentDirection;
    private Vector3 tangentUp;
    private List<Vector3> tangentPoints;
    
    void OnDrawGizmos()
    {
        if (tangentPoints == null) tangentPoints = new List<Vector3>(2);
        if(tangentPoints.Count != 0)tangentPoints.Clear();
        tangentPoints.Add(transform.position + tangentDirection * length);
        tangentPoints.Add(transform.position - tangentDirection * length);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        if (tangentDirection != Vector3.zero && length != 0 && Selection.Contains(gameObject))
        {
            Gizmos.DrawLine(transform.position, tangentPoints[0]);
            Gizmos.DrawLine(transform.position, tangentPoints[1]);
        }
    }
    
    public float GetLength(){return length;}
    public void SetLength(float length){this.length = length;}
    public Vector3 GetTangentDirection(){return tangentDirection;}
    public void SetTangentDirection(Vector3 tangentDirection){this.tangentDirection = tangentDirection;}
    public Vector3 GetTangentUp(){return tangentUp;}
    public void SetTangentUp(Vector3 tangentUp){this.tangentUp = tangentUp;}
    public List<Vector3> GetTangentPoints(){return tangentPoints;}
    public void SetTangentPoints(List<Vector3> tangentPoints){this.tangentPoints = tangentPoints;}
    
}