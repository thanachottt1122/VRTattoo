using UnityEngine;

// Placeholder customer: a single capsule with a random colour that stands, sits or lies down.
public class CapsuleCustomerVisual : CustomerVisual
{
    [Tooltip("Child with the Unity capsule mesh (2 units tall, 1 unit wide)")]
    public Transform body;

    public float height = 1.7f;
    public float radius = 0.2f;

    [Tooltip("Capsule height while sitting (torso and head above the seat)")]
    public float sitHeight = 0.9f;

    [Tooltip("How far the sitting capsule leans back from the seat point (m)")]
    public float sitBackOffset = 0.05f;

    public bool randomColor = true;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        if (randomColor)
            ApplyRandomColor();
        SetPose(CustomerPose.Idle);
    }

    void ApplyRandomColor()
    {
        var bodyRenderer = body != null ? body.GetComponent<Renderer>() : null;
        if (bodyRenderer == null)
            return;

        var color = Random.ColorHSV(0f, 1f, 0.5f, 0.9f, 0.6f, 1f);
        var block = new MaterialPropertyBlock();
        bodyRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        bodyRenderer.SetPropertyBlock(block);
    }

    public override void SetPose(CustomerPose pose)
    {
        if (body == null)
            return;

        float diameter = radius * 2f;
        switch (pose)
        {
            case CustomerPose.Sit:
                body.localPosition = new Vector3(0f, sitHeight * 0.5f, -sitBackOffset);
                body.localRotation = Quaternion.identity;
                body.localScale = new Vector3(diameter, sitHeight * 0.5f, diameter);
                break;

            case CustomerPose.Lie:
                // Lying on the back along forward, head end towards +Z.
                body.localPosition = new Vector3(0f, radius, 0f);
                body.localRotation = Quaternion.Euler(90f, 0f, 0f);
                body.localScale = new Vector3(diameter, height * 0.5f, diameter);
                break;

            default:
                body.localPosition = new Vector3(0f, height * 0.5f, 0f);
                body.localRotation = Quaternion.identity;
                body.localScale = new Vector3(diameter, height * 0.5f, diameter);
                break;
        }
    }
}
