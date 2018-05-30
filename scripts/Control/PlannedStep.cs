using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Valkyrie_VR
{
  public enum Foot_Type { FUTURE, PAST };
  public enum Foot_Side { LEFT, RIGHT };
  public class PlannedStep : MonoBehaviour
  {
    public float x, y, z;
    public float yaw;
    public Foot_Type type;
    public Foot_Side side;
    static Material mat;
    Mesh footMesh;
    StepPlanner parent;
    bool mustUpdate;
    static int[] indices_cube = {
          0,  1,  2,  1,  2,  3,  //front
          1,  5,  7,  1,  7,  3,  //right
          2,  3,  7,  2,  6,  7,  //back
          4,  5,  7,  4,  6,  7,  //left
          0,  4,  6,  0,  2,  6,  //upper
          0,  1,  5,  0,  4,  5}; //bottom
    public void initialize(float _x, float _y, float _z, float _yaw, Foot_Type _type, Foot_Side _side, StepPlanner _parent)
    {
      x = _x;
      y = _y;
      z = _z;
      yaw = _yaw;
      type = _type;
      side = _side;
      parent = _parent;
      mustUpdate = true;
    }

    void updateStepRender()
    {
      
      Color[] colorBuffers = new Color[8];
      Vector3[] vertexBuffers = new Vector3[8];
      int[] triangleBuffers = new int[36];
      Color color_to_use;
      if (side == Foot_Side.LEFT)
        color_to_use = (type == Foot_Type.FUTURE ? parent.FutureLeft : parent.PastLeft);
      else// if (feet[i].side == Foot_Side.RIGHT)
        color_to_use = (type == Foot_Type.FUTURE ? parent.FutureRight : parent.PastRight);
      vertexBuffers[0] = new Vector3(-.10F, .20F, .20F);
      vertexBuffers[1] = new Vector3(.10F, .20F, .20F);
      vertexBuffers[2] = new Vector3(-.10F, .20F, -.20F);
      vertexBuffers[3] = new Vector3(.10F, .20F, -.20F);
      vertexBuffers[4] = new Vector3(-.10F, 0, .20F);
      vertexBuffers[5] = new Vector3(.10F, 0, .20F);
      vertexBuffers[6] = new Vector3(-.10F, 0, -.20F);
      vertexBuffers[7] = new Vector3(.10F, 0, -.20F);
      for (int i2 = 0; i2 < 8; i2++)
      {
        colorBuffers[i2] = color_to_use;
      }
      for (int i2 = 0; i2 < 36; i2++)
      {
        triangleBuffers[i2] = indices_cube[i2];
      }
      footMesh.vertices = vertexBuffers;
      footMesh.colors = colorBuffers;
      footMesh.triangles = triangleBuffers;
      transform.position = new Vector3(x, y, z);
      transform.rotation = Quaternion.Euler(0, yaw, 0);
    }

    // Use this for initialization
    void Start()
    {
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
      footMesh = new Mesh();
      footMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      gameObject.AddComponent(typeof(MeshFilter));
      gameObject.AddComponent(typeof(MeshRenderer));
      ((MeshFilter)gameObject.GetComponent(typeof(MeshFilter))).mesh = footMesh;
      ((MeshRenderer)gameObject.GetComponent(typeof(MeshRenderer))).enabled = true;
      ((Renderer)gameObject.GetComponent(typeof(Renderer))).material = mat;
    }
    // Update is called once per frame
    void Update()
    {
      if (mustUpdate)
      {
        updateStepRender();
        mustUpdate = false;
      }
    }
  }
}

