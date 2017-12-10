using UnityEngine;
using System.Collections;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Messages;
using System.Threading;
using System.Collections.Generic;

public class mesh_surface_rendering : MonoBehaviour {

  Publisher<Messages.std_msgs.String> pub;
  Subscriber<Messages.std_msgs.Float32MultiArray> sub;
  NodeHandle nh;
  Messages.std_msgs.Float32MultiArray msg;
  private bool closing;
  private Thread pubthread;

  public static Material lineMaterial;
  public static Material mat;
  public int home_x=0,home_y=0,home_z=0,count=0,adj=0,whichBuffer=0;
  public System.Collections.ArrayList points=new ArrayList();
  public bool doIt = false, didIt=false;
  public GameObject cloudGameObject;
  public Mesh cloudMesh;
  List<Vector3[]> vertexBuffers = new List<Vector3[]>();
  int[] triangleBuffers;
  List<Color[]> colorBuffers = new List<Color[]>();
  public float triangleSize = .005F;
  public int HD_CLOUD_SIZE = 414720;


  static float ToFloat(byte a, byte b, byte c, byte d)
  {
    return System.BitConverter.ToSingle(new[] { a, b, c, d }, 0);
  }
  static int ToInt(byte a, byte b, byte c, byte d)
  {
    return System.BitConverter.ToInt32(new[] { d, c, b, a }, 0);
  }
  void Update()
  {
    //print("maybe doing it?");
    if (doIt)
    {
      print("diding it");
      int otherBuffer = (whichBuffer+1)%2;
      doIt = false;
      ((MeshRenderer)cloudGameObject.GetComponent(typeof(MeshRenderer))).enabled = true;
      //cloudMesh.Clear();
      cloudMesh.vertices = vertexBuffers[otherBuffer];
      if (!didIt)
      {
        cloudMesh.triangles = triangleBuffers;
      }
      cloudMesh.colors = colorBuffers[otherBuffer];
      didIt = true;
    }
  }
  public void subCallback(Messages.std_msgs.Float32MultiArray recentMsg)
  {
    msg = recentMsg;
    print("doing it");
    int numTriangles = msg.data.Length / (9) + 1;
    if (!didIt)
    {
      triangleBuffers = new int[(numTriangles) * 3];
      for (int i = 0; i < (numTriangles) * 3; i++)
      {
        triangleBuffers[i] = i;
      }
    }
    vertexBuffers[whichBuffer] = new Vector3[numTriangles * 8];
    colorBuffers[whichBuffer]  = new Color[numTriangles * 8];
    int counter = 0;
    for (int i = 0; i < msg.data.Length; i += 9)
    {
      vertexBuffers[whichBuffer][(counter * 3)] = new Vector3(msg.data[i + 0], msg.data[i + 1], msg.data[i + 2]);
      vertexBuffers[whichBuffer][(counter * 3) + 1] = new Vector3(msg.data[i + 3], msg.data[i + 4], msg.data[i + 5] );
      vertexBuffers[whichBuffer][(counter * 3) + 2] = new Vector3(msg.data[i + 6], msg.data[i + 7], msg.data[i + 8]);
      for (int i2 = 0; i2 < 3; i2++)
      {
        colorBuffers[whichBuffer][counter*3+i2] = Color.Lerp(Color.green, Color.red, msg.data[i + 4]/(1.0F)+.3F);
      }
      counter++;
    }
    print(counter);

    whichBuffer = (whichBuffer + 1) %2;
    doIt = true;
    //for (int i = 0; i < newVertices.Length; i++)
    //  colors[i] = Color.Lerp(Color.red, Color.green, newVertices[i].y);
  }
  // Use this for initialization
  void Start () {
    ROS.Init(new string[0], "mesh_surface_renderer");
    nh = new NodeHandle();
    sub = nh.subscribe<Messages.std_msgs.Float32MultiArray>("/numl/output/multisense/constructed_triangles", 1, subCallback);
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
    cloudGameObject = new GameObject("Block");
    cloudGameObject.AddComponent(typeof(MeshFilter));
    cloudGameObject.AddComponent(typeof(MeshRenderer));
    ((MeshFilter)cloudGameObject.GetComponent(typeof(MeshFilter))).transform.Translate(0, 1.3F, -.5F);
    ((MeshFilter)cloudGameObject.GetComponent(typeof(MeshFilter))).transform.Rotate(0, 0, 180, Space.Self);
    ((MeshFilter)cloudGameObject.GetComponent(typeof(MeshFilter))).mesh = cloudMesh;
    ((MeshRenderer)cloudGameObject.GetComponent(typeof(MeshRenderer))).enabled = false;
    ((Renderer)cloudGameObject.GetComponent(typeof(Renderer))).material = mat;



    //some test code

    /*float width = 3;
    float height = 2;
    Vector3[] newVertices = new Vector3[4];
    Vector2[] newUV = new Vector2[4];
    int[] newTriangles = new int[6];
    Mesh mesh = new Mesh();

    newVertices[0] = new Vector3(0, 0, 5);
    newVertices[1] = new Vector3(width, 0, 5);
    newVertices[2] = new Vector3(0, height, 5);
    newVertices[3] = new Vector3(width, height, 5);
    mesh.vertices = newVertices;

    newTriangles[0] = 0;
    newTriangles[1] = 2;
    newTriangles[2] = 1;

    newTriangles[3] = 2;
    newTriangles[4] = 3;
    newTriangles[5] = 1;
    mesh.triangles = newTriangles;

    newUV[0] = new Vector2(0, 0);
    newUV[1] = new Vector2(1, 0);
    newUV[2] = new Vector2(0, 1);
    newUV[3] = new Vector2(1, 1);
    mesh.uv = newUV;

    Color[] colors = new Color[newVertices.Length];

    for (int i = 0; i < newVertices.Length; i++)
      colors[i] = Color.Lerp(Color.red, Color.green, newVertices[i].y);
    mesh.colors = colors;

    GameObject renderedObject = new GameObject("Block");
    renderedObject.AddComponent(typeof(MeshFilter));
    renderedObject.AddComponent(typeof(MeshRenderer));
    ((MeshFilter)renderedObject.GetComponent(typeof(MeshFilter))).mesh = mesh;
    ((MeshRenderer)renderedObject.GetComponent(typeof(MeshRenderer))).enabled = true;
    ((Renderer)renderedObject.GetComponent(typeof(Renderer))).material = mat;*/


    /*pub = nh.advertise<Messages.std_msgs.String>("/chatter", 1, false);

    pubthread = new Thread(() =>
    {
      int i = 0;
      Messages.std_msgs.String msg;
      while (ROS.ok)
      {
        msg = new Messages.std_msgs.String("foo " + (i++));
        pub.publish(msg);
        Debug.Log("Sending: " + msg.data);
        Thread.Sleep(1000);
      }
    });
    pubthread.Start();*/
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
