using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using System;

namespace Valkyrie_VR
{
  public enum Footstep_Creation_Mode { DRAW_PATH, CLICK_TRIGGER };
  public class StepPlanner : MonoBehaviour
  {
    public VRTK_Pointer StepControlPointer;
    public Footstep_Creation_Mode FootstepCreationMode;
    public Color FutureLeft;
    public Color FutureRight;
    public Color PastLeft;
    public Color PastRight;
    public Color PathColor;
    public float DistanceBetweenSteps;
    public float FeetSpreadDistance;
    static Material mat;
    Mesh feetMesh;
    List<GameObject> feet = new List<GameObject>();
    bool feetHaveChanged = false;
    bool isDrawingPath = false;
    List<Vector3> drawnPath = new List<Vector3>();
    int globalPlannedStepCounter=0;
    static int[] indices_cube = {
          0,  1,  2,  1,  2,  3,  //front
          1,  5,  7,  1,  7,  3,  //right
          2,  3,  7,  2,  6,  7,  //back
          4,  5,  7,  4,  6,  7,  //left
          0,  4,  6,  0,  2,  6,  //upper
          0,  1,  5,  0,  4,  5}; //bottom
    static int[] indices_path = {
          0, 8, 11, 0, 9, 10,
          1, 8, 11, 1, 9, 10,
          2, 8, 11, 2, 9, 10,
          3, 8, 11, 3, 9, 10, //end top
          4, 12, 15, 4, 13, 14,
          5, 12, 15, 5, 13, 14,
          6, 12, 15, 6, 13, 14,
          7, 12, 15, 7, 13, 14, // end bot
          1, 9, 13, 1, 11, 15,
          3, 9, 13, 3, 11, 15,
          4, 9, 13, 5, 11, 15,
          6, 9, 13, 7, 11, 15, // end right
          0, 8, 12, 0, 10, 14,
          2, 8, 12, 2, 10, 14,
          4, 8, 12, 4, 10, 14,
          6, 8, 12, 6, 10, 14, //end left
          8, 0, 3, 8, 1, 2,
          9, 0, 3, 9, 1, 2,
          10, 0, 3, 10, 1, 2,
          11, 0, 3, 11, 1, 2, //end reverse top
          12, 4, 7, 12, 5, 6,
          13, 4, 7, 13, 5, 6,
          14, 4, 7, 14, 5, 6,
          15, 4, 7, 15, 5, 6, // end reverse bot
          9, 1, 5, 9, 3, 7,
          11, 1, 5, 11, 3, 7,
          13, 1, 5, 13, 3, 7,
          15, 1, 5, 15, 3, 7, // end reverse right
          8, 0, 4, 8, 2, 6,
          10, 0, 4, 10, 2, 6,
          12, 0, 4, 12, 2, 6,
          14, 0, 4, 14, 2, 6}; //end reverse left

    void HandleNewStep(object sender, ControllerInteractionEventArgs e)
    {
      print(e);
      print((VRTK_Pointer)sender);
      GameObject cursor_tip = GameObject.Find("[VRTK][AUTOGEN][RightController][StraightPointerRenderer_Cursor]");
      Vector3 world_coords = cursor_tip.transform.TransformPoint(cursor_tip.transform.localPosition);

      print(world_coords);
      //((VRTK_Pointer)sender).currentCollider.base.base.base.name == "Floor"
      //((VRTK_Pointer)sender).pointerRenderer.actualTracer.transform.position
      GameObject theStepObject = new GameObject("PlannedStep" + globalPlannedStepCounter);
      theStepObject.transform.parent = gameObject.transform;
      PlannedStep nextStep = theStepObject.AddComponent<PlannedStep>();
      nextStep.initialize(world_coords.x, world_coords.y, world_coords.z, 0, Foot_Type.FUTURE, Foot_Side.LEFT, this);
      feet.Add(theStepObject);
      feetHaveChanged = true;
    }
    void TogglePathStart(object sender, ControllerInteractionEventArgs e)
    {
      isDrawingPath = true;
      drawnPath = new List<Vector3>();
      for (int i = 0; i < feet.Count; i++)
      {
        Destroy(feet[i]);
      }
      feet.Clear();
    }
    void TogglePathStop(object sender, ControllerInteractionEventArgs e)
    {
      isDrawingPath = false;
      processPath();
    }
    void processPath()
    {
      Foot_Side currentSide = Foot_Side.LEFT;
      float running_distance = DistanceBetweenSteps;
      for (int i = 0; i < drawnPath.Count-1; i++)
      {
        float distance_between_points = Vector3.Distance(drawnPath[i], drawnPath[i + 1]);
        if (distance_between_points < running_distance)
        {
          running_distance -= distance_between_points;
          continue;
        }
        else
        {
          float offset = running_distance;
          float FeetOffCenterDistance = FeetSpreadDistance / 2;
          while (offset < distance_between_points)
          {
            double theta = Math.Atan((drawnPath[i + 1].z - drawnPath[i].z) / (drawnPath[i + 1].x - drawnPath[i].x));
            double eita = Math.Atan(FeetOffCenterDistance / offset);
            double multiplier = Math.Sqrt(FeetOffCenterDistance * FeetOffCenterDistance + offset * offset);
            float x = 0, z = 0;
            if (currentSide == Foot_Side.RIGHT)
            {
              x = (float)(Math.Cos(theta - eita) * multiplier + drawnPath[i].x);
              z = (float)(Math.Sin(theta - eita) * multiplier + drawnPath[i].z);
            }
            else
            {
              x = (float)(Math.Cos(theta + eita) * multiplier + drawnPath[i].x);
              z = (float)(Math.Sin(theta + eita) * multiplier + drawnPath[i].z);
            }
            GameObject theStepObject = new GameObject("PlannedStep" + globalPlannedStepCounter);
            theStepObject.transform.parent = gameObject.transform;
            PlannedStep nextStep = theStepObject.AddComponent<PlannedStep>();
            nextStep.initialize(x, 0, z, 90.0F - (float)theta * 57.2957F, Foot_Type.FUTURE, currentSide, this);
            feet.Add(theStepObject);
            currentSide = (currentSide == Foot_Side.LEFT ? Foot_Side.RIGHT : Foot_Side.LEFT);
            offset += DistanceBetweenSteps;
          }
          running_distance = DistanceBetweenSteps - distance_between_points + offset;
        }
      }
    }
    void updatePathRender()
    {
      int num_segments = drawnPath.Count - 1;
      Color[] colorBuffers = new Color[num_segments * 16];
      Vector3[] vertexBuffers = new Vector3[num_segments * 16];
      int[] triangleBuffers = new int[num_segments * 264];
      for (int i = 0; i < num_segments; i++)
      {
        float x = drawnPath[i].x;
        float y = 0F;//drawnPath[i].y;
        float z = drawnPath[i].z;
        vertexBuffers[i * 16 + 0] = new Vector3(x - .05F, y + .05F, z + .05F);
        vertexBuffers[i * 16 + 1] = new Vector3(x + .05F, y + .05F, z + .05F);
        vertexBuffers[i * 16 + 2] = new Vector3(x - .05F, y + .05F, z - .05F);
        vertexBuffers[i * 16 + 3] = new Vector3(x + .05F, y + .05F, z - .05F);
        vertexBuffers[i * 16 + 4] = new Vector3(x - .05F, y, z + .05F);
        vertexBuffers[i * 16 + 5] = new Vector3(x + .05F, y, z + .05F);
        vertexBuffers[i * 16 + 6] = new Vector3(x - .05F, y, z - .05F);
        vertexBuffers[i * 16 + 7] = new Vector3(x + .05F, y, z - .05F);
        x = drawnPath[i + 1].x;
        y = 0F;//drawnPath[i+1].y;
        z = drawnPath[i + 1].z;
        vertexBuffers[i * 16 + 8] = new Vector3(x - .05F, y + .05F, z + .05F);
        vertexBuffers[i * 16 + 9] = new Vector3(x + .05F, y + .05F, z + .05F);
        vertexBuffers[i * 16 + 10] = new Vector3(x - .05F, y + .05F, z - .05F);
        vertexBuffers[i * 16 + 11] = new Vector3(x + .05F, y + .05F, z - .05F);
        vertexBuffers[i * 16 + 12] = new Vector3(x - .05F, y, z + .05F);
        vertexBuffers[i * 16 + 13] = new Vector3(x + .05F, y, z + .05F);
        vertexBuffers[i * 16 + 14] = new Vector3(x - .05F, y, z - .05F);
        vertexBuffers[i * 16 + 15] = new Vector3(x + .05F, y, z - .05F);
        //print(start_y+" "+end_y);
        for (int i2 = 0; i2 < 16; i2++)
        {
          colorBuffers[i * 16 + i2] = PathColor;
        }
        for (int i2 = 0; i2 < 36; i2++)
        {
          triangleBuffers[i * 264 + i2] = i * 16 + indices_cube[i2];
        }
        for (int i2 = 36; i2 < 72; i2++)
        {
          triangleBuffers[i * 264 + i2] = i * 16 + 8 + indices_cube[i2 - 36];
        }
        for (int i2 = 72; i2 < 264; i2++)
        {
          triangleBuffers[i * 264 + i2] = i * 16 + indices_path[i2 - 72];
        }
      }
      feetMesh.vertices = vertexBuffers;
      feetMesh.colors = colorBuffers;
      feetMesh.triangles = triangleBuffers;
    }
    void Start()
    {
      if (FootstepCreationMode == Footstep_Creation_Mode.CLICK_TRIGGER)
      {
        StepControlPointer.SelectionButtonPressed += HandleNewStep;
      }
      if (FootstepCreationMode == Footstep_Creation_Mode.DRAW_PATH)
      {
        StepControlPointer.SelectionButtonPressed += TogglePathStart;
        StepControlPointer.SelectionButtonReleased += TogglePathStop;
      }
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
      feetMesh = new Mesh();
      feetMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      gameObject.AddComponent(typeof(MeshFilter));
      gameObject.AddComponent(typeof(MeshRenderer));
      ((MeshFilter)gameObject.GetComponent(typeof(MeshFilter))).mesh = feetMesh;
      ((MeshRenderer)gameObject.GetComponent(typeof(MeshRenderer))).enabled = true;
      ((Renderer)gameObject.GetComponent(typeof(Renderer))).material = mat;

      drawnPath.Add(new Vector3(0F, 0F, 0F));
      drawnPath.Add(new Vector3(2.5F, 0F, 5F));
      updatePathRender();
      processPath();
    }

    // Update is called once per frame
    void Update()
    {
      if (feetHaveChanged)
      {
        //updateStepRender();
        feetHaveChanged = false;
      }
      if (isDrawingPath)
      {
        GameObject cursor_tip = GameObject.Find("[VRTK][AUTOGEN][RightController][StraightPointerRenderer_Cursor]");
        Vector3 world_coords = cursor_tip.transform.TransformPoint(cursor_tip.transform.localPosition);
        drawnPath.Add(new Vector3(world_coords.x, world_coords.y, world_coords.z));
        updatePathRender();
      }
    }
  }
}
