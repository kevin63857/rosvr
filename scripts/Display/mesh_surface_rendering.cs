using UnityEngine;
using System.Collections;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Messages;
using System.Threading;
using System.Collections.Generic;

public class mesh_surface_rendering : MonoBehaviour {
  public TextAsset Mesh_File;

  public static Material lineMaterial;
  public static Material mat;
  public GameObject cloudGameObject;
  public Mesh cloudMesh;
  Vector3[] vertexBuffers;
  int[] triangleBuffers;
  Color[] colorBuffers;


  static float ToFloat(byte a, byte b, byte c, byte d)
  {
    return System.BitConverter.ToSingle(new[] { a, b, c, d }, 0);
  }
  static int ToInt(byte a, byte b, byte c, byte d)
  {
    return System.BitConverter.ToInt32(new[] { d, c, b, a }, 0);
  }
  // Use this for initialization
  void Start () {
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
    cloudMesh = new Mesh();
    cloudMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    cloudGameObject = new GameObject("Block");
    cloudGameObject.AddComponent(typeof(MeshFilter));
    cloudGameObject.AddComponent(typeof(MeshRenderer));
    ((MeshFilter)cloudGameObject.GetComponent(typeof(MeshFilter))).transform.Translate(0, 1.3F, -.5F);
    ((MeshFilter)cloudGameObject.GetComponent(typeof(MeshFilter))).transform.Rotate(0, 0, 180, Space.Self);
    ((MeshFilter)cloudGameObject.GetComponent(typeof(MeshFilter))).mesh = cloudMesh;
    ((MeshRenderer)cloudGameObject.GetComponent(typeof(MeshRenderer))).enabled = false;
    ((Renderer)cloudGameObject.GetComponent(typeof(Renderer))).material = mat;
    print("Starting to load static surfaces from " + Mesh_File.name);
    string[] pointLocations = Mesh_File.text.Split(' ');
    vertexBuffers = new Vector3[pointLocations.Length/3];
    triangleBuffers = new  int[pointLocations.Length / 3];
    colorBuffers = new Color[pointLocations.Length / 3];
    int counter = 0;
    for (int i = 0; i < pointLocations.Length-3; i+=3)
    {
      colorBuffers[counter] = Color.Lerp(Color.green, Color.red, float.Parse(pointLocations[i + 2]));
      triangleBuffers[counter] = counter;
      vertexBuffers[counter++] = new Vector3(float.Parse(pointLocations[i]), float.Parse(pointLocations[i + 1]), float.Parse(pointLocations[i + 2]));
    }

    print("loading mesh data to game object...");
    ((MeshRenderer)cloudGameObject.GetComponent(typeof(MeshRenderer))).enabled = true;
    cloudMesh.vertices = vertexBuffers;
    cloudMesh.triangles = triangleBuffers;
    cloudMesh.colors = colorBuffers;
  }
  void OnApplicationQuit()
  {
    ROS.shutdown();
    ROS.waitForShutdown();
  }
	// Update is called once per frame
	void OnPostRender() {

	}
}
