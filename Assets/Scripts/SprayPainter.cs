using UnityEngine;

public class SprayPainter : MonoBehaviour
{
    public Transform nozzle;
    public float range = 5f;
    public Color paintColor = Color.red;

    [Header("Brush")]
    public float brushSize = 0.12f;
    public float hardness = 0.4f;
    public float flow = 0.18f;

    [Header("Cone")]
    public int dropletsPerTick = 10;
    public float coneAngle = 4f;

    public LayerMask paintableMask;

    void Update()
    {
        if (Input.GetMouseButton(0)) Spray();
    }

    void Spray()
    {
        //Debug.Log("hit " + hit.collider.name);
        for (int i = 0; i < dropletsPerTick; i++)
        {
            // Circular spread, biased toward the center like a real nozzle.
            // sqrt of a random radius keeps it from clustering in the middle too hard;
            // remove the sqrt for a denser center.
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Pow(Random.value, 0.7f) * coneAngle;  // 0.7 = denser center
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Vector3 dir = Quaternion.Euler(offset.x, offset.y, 0f) * nozzle.forward;

            if (!Physics.Raycast(nozzle.position, dir, out var hit, range, paintableMask))
                continue;

            Debug.Log("Ray hit: " + hit.collider.name + " on layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));
            var p = hit.collider.GetComponent<Paintable>();
            if (p == null) continue;

            float t = hit.distance / range;
            float size = brushSize * Mathf.Lerp(1f, 2.2f, t);
            float fl   = flow      * Mathf.Lerp(1f, 0.4f, t);

            p.PaintAt(hit.point, dir, paintColor, size, hardness, fl);
        }
    }
}