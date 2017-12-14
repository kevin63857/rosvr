using UnityEngine;
using System.Collections;

public class InteractableItem : MonoBehaviour {
    public Rigidbody rigidBody;

    private bool currentlyInteracting;
    public bool doubleInteracting;

    private WandController attachedWand;
    private WandController doubleWand;

    private Transform interactionPoint;
    private Transform doubleinteractionPoint;

    private float velocityFactor = 20000f;
    private float rotationFactor = 400f;

    private Vector3 posDelta;
    private Quaternion rotDelta;

    private float angle;
    private Vector3 axis;

	// Use this for initialization
	void Start () {
        rigidBody = GetComponent<Rigidbody>();
        interactionPoint = new GameObject().transform;
        velocityFactor /= 10;// rigidBody.mass;
        rotationFactor /= 10;// rigidBody.mass;
	}
	
	// Update is called once per frame
	void Update () {
        if (attachedWand != null && currentlyInteracting)
        {
            if (doubleInteracting)
            {

            }
            else
            {
                posDelta = attachedWand.transform.position - interactionPoint.position;
                this.rigidBody.velocity = posDelta * velocityFactor * Time.fixedDeltaTime; //todo research Time.deltaTime vs others 
                rotDelta = attachedWand.transform.rotation * Quaternion.Inverse(interactionPoint.rotation);
                rotDelta.ToAngleAxis(out angle, out axis);

                if (angle > 180)
                    angle -= 360;
                else if (angle < -180)
                    angle += 360;

                Vector3 vec = (Time.fixedDeltaTime * angle * axis) * rotationFactor;
                if (!(float.IsNaN(vec.x) || float.IsNaN(vec.y) || float.IsNaN(vec.z)))
                    this.rigidBody.angularVelocity = vec;
            }
        }
        else
        {
            this.rigidBody.velocity = new Vector3(0.0f, 0.0f, 0.0f);
            this.rigidBody.angularVelocity = new Vector3(0.0f, 0.0f, 0.0f);
        }
    }

    public void BeginInteraction(WandController wand)
    {
        attachedWand = wand;
        interactionPoint.position = wand.transform.position;
        interactionPoint.rotation = wand.transform.rotation;
        interactionPoint.SetParent(transform, true);

        currentlyInteracting = true;
    }

    public void EndInteraction(WandController wand)
    {
        if(wand == attachedWand)
        {
            attachedWand = null;
            currentlyInteracting = false;
        }
    }

    public void DoubleInteraction(WandController wand)
    {
        doubleWand = wand;
        doubleinteractionPoint.position = wand.transform.position;
        doubleinteractionPoint.rotation = wand.transform.rotation;
        doubleinteractionPoint.SetParent(transform, true);
        doubleInteracting = true;
    }

    public bool IsInteracting()
    {
        return currentlyInteracting;
    }

    public bool IsDouble()
    {
        return doubleInteracting;
    }
}
