using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestUnit : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] TextMeshProUGUI condition;
    [SerializeField] TextMeshProUGUI gageText;
    [SerializeField] TextMeshProUGUI amount;
    [SerializeField] Slider gage;
    [SerializeField] Button rerollButton;
    [SerializeField] Button claimButton;
    [SerializeField] Image claimDisableImage;
    [SerializeField] Image claimAlarm;
    [SerializeField] GameObject done;
    public void SetCondition(QuestState state)
    {
        condition.text = LocalizationLoader.Instance.GetText(state.data.DescriptionId);
        title.text = LocalizationLoader.Instance.GetText(state.data.titleId);
        gageText.text = state.progress.ToString() + "/" + state.data.ConditionValue.ToString();
        gage.value = state.data.ConditionValue > 0
            ? (float)state.progress / state.data.ConditionValue
            : 0f;
        amount.text = state.data.RewardDiamond.ToString();

        claimDisableImage.enabled = !state.isClear;
        claimAlarm.enabled = state.isClear && !state.isClaimed;
        rerollButton.gameObject.SetActive(!state.isRerolled && !state.isClear);
        claimButton.enabled = state.isClear && !state.isClaimed;
        done.SetActive(state.isClaimed);
    }
}
