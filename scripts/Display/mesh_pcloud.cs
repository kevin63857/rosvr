using UnityEngine;
using System.Collections;
using Ros_CSharp;
using tf.net;
using XmlRpc_Wrapper;
using Messages;
using System;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace Valkyrie_VR
{
  public class mesh_pcloud : MonoBehaviour
  {
    public bool isStarted=false;
    public static Material mat;
    public int whichBuffer = 0;
    public bool updateVertexData = false, updateTriangleData = false, updateColorData = false;
    bool spread_updates = false;
    public float position_x = 0F, position_y = 0F, position_z = 0F;
    public Quaternion rotation;
    public List<Color[]> colorBuffers = new List<Color[]>();
    public List<Vector3[]> vertexBuffers = new List<Vector3[]>();
    int[] triangleBuffers;
    public int numPoints_in_triangle_buffer = 0;
    Mesh cloudMesh;
    public mesh_pcloud()
    {
    }
    public void UpdateMesh()
    {
      //((MeshRenderer)gameObject.GetComponent(typeof(MeshRenderer))).enabled = true;
      //cloudMesh.Clear();
      int otherBuffer = (whichBuffer + 1) % 2;
      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();
      bool have_updated = false;
      if (updateVertexData)
      {
        //print("Updating Vertex Data");
        cloudMesh.vertices = vertexBuffers[otherBuffer];
        updateVertexData = false;
        stopWatch.Stop();
        print("Vertex Load Time " + stopWatch.ElapsedMilliseconds);
        have_updated = true;
      }
      if (updateTriangleData && (!have_updated || !spread_updates))
      {
        //print("Updating Triangle Data");
        cloudMesh.triangles = triangleBuffers;
        updateTriangleData = false;
        stopWatch.Stop();
        print("Triangle Load Time " + stopWatch.ElapsedMilliseconds);
        have_updated = true;
      }
      if (updateColorData && (!have_updated || !spread_updates))
      {
        //print("Updating Color Data");
        cloudMesh.colors = colorBuffers[otherBuffer];
        updateColorData = false;
        stopWatch.Stop();
        print("Color Load Time " + stopWatch.ElapsedMilliseconds);
        have_updated = true;
      }
      if (!have_updated)
      {
        ((MeshFilter)gameObject.GetComponent(typeof(MeshFilter))).transform.SetPositionAndRotation(new Vector3(position_x, position_y, position_z), rotation);
        //stopWatch.Stop();
        //print("Transform Load Time " + stopWatch.ElapsedMilliseconds);
      }
      ((MeshRenderer)gameObject.GetComponent(typeof(MeshRenderer))).enabled = true;
    }
    public void recalculateTriangles(int numPoints, int[] shapeIndices, int vertexCount)
    {
      print("Recalculating Triangles");
      triangleBuffers = new int[(numPoints) * shapeIndices.Length];
      int pointVertexCounter = 0;
      int pointTriangleDataCounter = 0;
      for (int i = 0; i < numPoints; i++)
      {
        for (int i2 = 0; i2 < shapeIndices.Length; i2++)
        {
          triangleBuffers[pointTriangleDataCounter + i2] = (pointVertexCounter) + shapeIndices[i2]; //The *8 comes from 8 vertices per cube
        }
        pointTriangleDataCounter += shapeIndices.Length;//i*shapeIndices.Length
        pointVertexCounter += vertexCount;//i*vertexCount[pointShape]
      }
      numPoints_in_triangle_buffer = numPoints;
      //print("Recaclulated triangle buffer data");
    }
    void Start()
    {
      print("Starting theCloud");
      if (!mat)
      {
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        mat = new Material(shader);
        mat.hideFlags = HideFlags.HideAndDontSave;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        mat.SetInt("_ZWrite", 1);
      }
      for (int i = 0; i < 2; i++)
      {
        vertexBuffers.Add(new Vector3[0]);
        colorBuffers.Add(new Color[0]);
      }
      cloudMesh = new Mesh();
      cloudMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      gameObject.AddComponent(typeof(MeshFilter));
      gameObject.AddComponent(typeof(MeshRenderer));
      ((MeshFilter)gameObject.GetComponent(typeof(MeshFilter))).transform.Translate(0, 0, -.5F);
      ((MeshFilter)gameObject.GetComponent(typeof(MeshFilter))).mesh = cloudMesh;
      ((MeshRenderer)gameObject.GetComponent(typeof(MeshRenderer))).enabled = true;
      ((Renderer)gameObject.GetComponent(typeof(Renderer))).material = mat;
      isStarted = true;
    }
  }
}