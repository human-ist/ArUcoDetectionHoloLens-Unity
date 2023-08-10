using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class DebugLineRender : MonoBehaviour
{
    public GameObject markerGo;
    public LineRenderer arcRenderer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        DrawArcAroundMarker();
    }

    private void DrawArcAroundMarker(float angle = 180f, float radius = 1f)
    {
        int nbPos = (int)angle / 5;

        Vector3[] positions;
        positions = new Vector3[nbPos];
        Vector3 axisX = markerGo.transform.right;
        Vector3 axisY = markerGo.transform.up;

        for (int i = 0; i < nbPos; i++)
        {
            positions[i] = new Vector3(radius * Mathf.Cos((angle * i / (nbPos - 1))* Mathf.Deg2Rad), radius * Mathf.Sin((angle * i / (nbPos - 1)) * Mathf.Deg2Rad), 0f);
        }

        arcRenderer.positionCount = nbPos;
        arcRenderer.SetPositions(positions);
        arcRenderer.startWidth = 0.01f;
        arcRenderer.endWidth = 0.01f;
    }
}
