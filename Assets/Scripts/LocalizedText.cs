using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private int stringId;

    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }
    void Start()
    {
        StringTableLoader.Instance.onLoaded.AddListener(UpdateText);
    }
    void OnEnable()
    {
        UpdateText();
    }
    public void UpdateText()
    {
        if(StringTableLoader.Instance != null && StringTableLoader.Instance.isLoaded) text.text = StringTableLoader.Instance.GetText(stringId);
    }
}