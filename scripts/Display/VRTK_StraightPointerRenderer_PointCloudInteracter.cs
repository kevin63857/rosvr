//Mostly copied directly from the StraightPointerRenderer.cs file
//I could not extend that file because there was no way to get around it's UpdateRenderer() method directly to the base class'.  C# does not allow base.base calls.
namespace VRTK
{
  using UnityEngine;
  using Valkyrie_VR;
  using System.Collections.Generic;
  using System.Collections;
  /// <summary>
  /// The Straight Pointer Renderer emits a coloured beam from the end of the object it is attached to and simulates a laser beam.
  /// </summary>
  /// <remarks>
  /// It can be useful for pointing to objects within a scene and it can also determine the object it is pointing at and the distance the object is from the controller the beam is being emitted from.
  /// </remarks>
  /// <example>
  /// `VRTK/Examples/003_Controller_SimplePointer` shows the simple pointer in action and code examples of how the events are utilised and listened to can be viewed in the script `VRTK/Examples/Resources/Scripts/VRTK_ControllerPointerEvents_ListenerExample.cs`
  /// </example>
  public class VRTK_StraightPointerRenderer_PointCloudInteracter : VRTK_BasePointerRenderer
  {
    [Header("PointCloudInteracter Specific Settings")]
    public float MaxDistanceOnRaycast=.1f;

    [Header("Straight Pointer Appearance Settings")]

    [Tooltip("The maximum length the pointer tracer can reach.")]
    public float maximumLength = 100f;
    [Tooltip("The scale factor to scale the pointer tracer object by.")]
    public float scaleFactor = 0.002f;
    [Tooltip("The scale multiplier to scale the pointer cursor object by in relation to the `Scale Factor`.")]
    public float cursorScaleMultiplier = 25f;
    [Tooltip("The cursor will be rotated to match the angle of the target surface if this is true, if it is false then the pointer cursor will always be horizontal.")]
    public bool cursorMatchTargetRotation = false;
    [Tooltip("Rescale the cursor proportionally to the distance from the tracer origin.")]
    public bool cursorDistanceRescale = false;
    [Tooltip("The maximum scale the cursor is allowed to reach. This is only used when rescaling the cursor proportionally to the distance from the tracer origin.")]
    public Vector3 maximumCursorScale = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

    [Header("Straight Pointer Custom Appearance Settings")]

    [Tooltip("A custom game object to use as the appearance for the pointer tracer. If this is empty then a Box primitive will be created and used.")]
    public GameObject customTracer;
    [Tooltip("A custom game object to use as the appearance for the pointer cursor. If this is empty then a Sphere primitive will be created and used.")]
    public GameObject customCursor;

    protected GameObject actualContainer;
    protected GameObject actualTracer;
    protected GameObject actualCursor;
    protected List<mesh_pcloud> registered_pclouds = new List<mesh_pcloud>();
    protected Vector3 cursorOriginalScale = Vector3.one;

    public void RegisterPcloud(mesh_pcloud new_recruit)
    {
      registered_pclouds.Add(new_recruit);
      print("Registered a new pointcloud to the raycaster");
      print(new_recruit.ConfirmRegistration());
      print(registered_pclouds[registered_pclouds.Count - 1].ConfirmRegistration());
    }
    /// <summary>
    /// The UpdateRenderer method is used to run an Update routine on the pointer.
    /// </summary>
    public override void UpdateRenderer()
    {
      if ((controllingPointer != null && controllingPointer.IsPointerActive()) || IsVisible())
      {
        float tracerLength = CastRayForward();
        SetPointerAppearance(tracerLength);
        MakeRenderersVisible();
      }
      base.UpdateRenderer();
    }

    /// <summary>
    /// The GetPointerObjects returns an array of the auto generated GameObjects associated with the pointer.
    /// </summary>
    /// <returns>An array of pointer auto generated GameObjects.</returns>
    public override GameObject[] GetPointerObjects()
    {
      return new GameObject[] { actualContainer, actualCursor, actualTracer };
    }

    protected override void ToggleRenderer(bool pointerState, bool actualState)
    {
      ToggleElement(actualTracer, pointerState, actualState, tracerVisibility, ref tracerVisible);
      ToggleElement(actualCursor, pointerState, actualState, cursorVisibility, ref cursorVisible);
    }

    protected override void CreatePointerObjects()
    {
      actualContainer = new GameObject(VRTK_SharedMethods.GenerateVRTKObjectName(true, gameObject.name, "StraightPointerRenderer_Container"));
      actualContainer.transform.SetParent(pointerOriginTransformFollowGameObject.transform);
      actualContainer.transform.localPosition = Vector3.zero;
      actualContainer.transform.localRotation = Quaternion.identity;
      actualContainer.transform.localScale = Vector3.one;
      VRTK_PlayerObject.SetPlayerObject(actualContainer, VRTK_PlayerObject.ObjectTypes.Pointer);

      CreateTracer();
      CreateCursor();
      Toggle(false, false);
      if (controllingPointer != null)
      {
        controllingPointer.ResetActivationTimer(true);
        controllingPointer.ResetSelectionTimer(true);
      }
    }

    protected override void DestroyPointerObjects()
    {
      if (actualContainer != null)
      {
        Destroy(actualContainer);
      }
    }

    protected override void ChangeMaterial(Color givenColor)
    {
      base.ChangeMaterial(givenColor);
      ChangeMaterialColor(actualTracer, givenColor);
      ChangeMaterialColor(actualCursor, givenColor);
    }

    protected override void UpdateObjectInteractor()
    {
      base.UpdateObjectInteractor();
      //if the object interactor is too far from the pointer tip then set it to the pointer tip position to prevent glitching.
      if (objectInteractor != null && actualCursor != null && Vector3.Distance(objectInteractor.transform.position, actualCursor.transform.position) > 0f)
      {
        objectInteractor.transform.position = actualCursor.transform.position;
      }
    }

    protected virtual void CreateTracer()
    {
      if (customTracer != null)
      {
        actualTracer = Instantiate(customTracer);
      }
      else
      {
        actualTracer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        actualTracer.GetComponent<BoxCollider>().isTrigger = true;
        actualTracer.AddComponent<Rigidbody>().isKinematic = true;
        actualTracer.layer = LayerMask.NameToLayer("Ignore Raycast");

        SetupMaterialRenderer(actualTracer);
      }

      actualTracer.transform.name = VRTK_SharedMethods.GenerateVRTKObjectName(true, gameObject.name, "StraightPointerRenderer_Tracer");
      actualTracer.transform.SetParent(actualContainer.transform);

      VRTK_PlayerObject.SetPlayerObject(actualTracer, VRTK_PlayerObject.ObjectTypes.Pointer);
    }

    protected virtual void CreateCursor()
    {
      if (customCursor != null)
      {
        actualCursor = Instantiate(customCursor);
      }
      else
      {
        actualCursor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        actualCursor.transform.localScale = Vector3.one * (scaleFactor * cursorScaleMultiplier);
        actualCursor.GetComponent<Collider>().isTrigger = true;
        actualCursor.AddComponent<Rigidbody>().isKinematic = true;
        actualCursor.layer = LayerMask.NameToLayer("Ignore Raycast");

        SetupMaterialRenderer(actualCursor);
      }

      cursorOriginalScale = actualCursor.transform.localScale;
      actualCursor.transform.name = VRTK_SharedMethods.GenerateVRTKObjectName(true, gameObject.name, "StraightPointerRenderer_Cursor");
      actualCursor.transform.SetParent(actualContainer.transform);
      VRTK_PlayerObject.SetPlayerObject(actualCursor, VRTK_PlayerObject.ObjectTypes.Pointer);
    }

    protected float CastRayForward()
    {
      Transform origin = GetOrigin();
      Vector3 running_closest_point=new Vector3(0,0,0);
      float running_distance=-1F;
      Vector3 world_origin_position = transform.TransformPoint(origin.position);
      Vector3 world_origin_direction = transform.TransformDirection(origin.forward);
      for (int i = 0; i < registered_pclouds.Count;i++)
      {
        Vector3 closestPoint = registered_pclouds[i].ClosestPointToRay(world_origin_position, world_origin_direction);
        float distance=Vector3.Cross(world_origin_direction, closestPoint - world_origin_position).magnitude;
        if (running_distance == -1F || distance < running_distance)
        {
          running_distance = distance;
          running_closest_point = closestPoint;
        }
      }
      float actualLength = maximumLength;
      if (running_distance<MaxDistanceOnRaycast && Vector3.Distance(world_origin_position,running_closest_point) < maximumLength)
      {
        actualLength = Vector3.Distance(world_origin_position, running_closest_point);
      }

      return actualLength;
    }

    protected virtual void SetPointerAppearance(float tracerLength)
    {
      if (actualContainer != null)
      {
        //if the additional decimal isn't added then the beam position glitches
        float beamPosition = tracerLength / (2f + BEAM_ADJUST_OFFSET);

        actualTracer.transform.localScale = new Vector3(scaleFactor, scaleFactor, tracerLength);
        actualTracer.transform.localPosition = Vector3.forward * beamPosition;
        actualCursor.transform.localScale = Vector3.one * (scaleFactor * cursorScaleMultiplier);
        actualCursor.transform.localPosition = new Vector3(0f, 0f, tracerLength);

        Transform origin = GetOrigin();

        float objectInteractorScaleIncrease = 1.05f;
        ScaleObjectInteractor(actualCursor.transform.lossyScale * objectInteractorScaleIncrease);

        if (destinationHit.transform != null)
        {
          if (cursorMatchTargetRotation)
          {
            actualCursor.transform.forward = -destinationHit.normal;
          }
          if (cursorDistanceRescale)
          {
            float collisionDistance = Vector3.Distance(destinationHit.point, origin.position);
            actualCursor.transform.localScale = Vector3.Min(cursorOriginalScale * collisionDistance, maximumCursorScale);
          }
        }
        else
        {
          if (cursorMatchTargetRotation)
          {
            actualCursor.transform.forward = origin.forward;
          }
          if (cursorDistanceRescale)
          {
            actualCursor.transform.localScale = Vector3.Min(cursorOriginalScale * tracerLength, maximumCursorScale);
          }
        }

        ToggleRenderer(controllingPointer.IsPointerActive(), false);
        UpdateDependencies(actualCursor.transform.position);
      }
    }
  }
}
