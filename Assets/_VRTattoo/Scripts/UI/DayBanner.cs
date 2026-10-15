using System.Collections;
using TMPro;
using UnityEngine;

// Big neon "Day N" (Pacifico, the shop's neon sign material) that appears in the middle of the shop in
// front of the player at the start of each day: fades and grows in, holds, fades out.
// ShopDayManager waits for it before letting customers in.
public class DayBanner : MonoBehaviour
{
    public TMP_Text label;
    [Tooltip("Defaults to the main camera")]
    public Transform viewer;

    [Tooltip("Distance in front of the player (m), level with the eyes")]
    public float distance = 2.5f;
    public float heightOffset = 0.1f;

    public float fadeInSeconds = 0.35f;
    public float holdSeconds = 2f;
    public float fadeOutSeconds = 0.5f;
    public float startScale = 0.8f;

    void Awake() => label.gameObject.SetActive(false);

    public IEnumerator Show(string text)
    {
        var view = viewer != null ? viewer : Camera.main != null ? Camera.main.transform : null;
        if (view != null)
        {
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.ProjectOnPlane(view.up, Vector3.up);
            forward.Normalize();
            transform.position = view.position + forward * distance + Vector3.up * heightOffset;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);  // TMP faces -Z
        }

        label.text = text;
        label.gameObject.SetActive(true);
        Debug.Log($"[DayBanner] {text}");

        for (float t = 0f; t < 1f; t += Time.deltaTime / fadeInSeconds)
        {
            float e = 1f - (1f - t) * (1f - t);
            SetLook(e, Mathf.Lerp(startScale, 1f, e));
            yield return null;
        }
        SetLook(1f, 1f);
        yield return new WaitForSeconds(holdSeconds);
        for (float t = 0f; t < 1f; t += Time.deltaTime / fadeOutSeconds)
        {
            SetLook(1f - t, 1f + 0.05f * t);
            yield return null;
        }
        label.gameObject.SetActive(false);
    }

    void SetLook(float alpha, float scale)
    {
        label.alpha = alpha;
        label.transform.localScale = Vector3.one * scale;
    }
}
