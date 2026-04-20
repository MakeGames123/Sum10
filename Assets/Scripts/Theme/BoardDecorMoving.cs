using UnityEngine;

public class BoardDecorMoving : MonoBehaviour
{
    [SerializeField] private float width = 200f;
    [SerializeField] private float height = 100f;
    [SerializeField] private float speed = 2f;

    private RectTransform rect;
    private Vector2 center;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        center = rect.anchoredPosition;
    }

    void Update()
    {
        float t = Time.time * speed;

        float x = Mathf.Sin(t) * width;
        float y = Mathf.Sin(t * 2f) * height * 0.5f;

        rect.anchoredPosition = center + new Vector2(x, y);
    }
}