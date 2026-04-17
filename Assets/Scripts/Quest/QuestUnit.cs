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
    [SerializeField] GameObject done;

    public void SetCondition(QuestState state)
    {
        condition.text = state.data.DescriptionKr;
        title.text = state.data.TitleKr;
        gageText.text = state.progress.ToString() + "/" + state.data.ConditionValue.ToString();
        gage.value = state.data.ConditionValue > 0
            ? (float)state.progress / state.data.ConditionValue
            : 0f;
        amount.text = state.data.RewardDiamond.ToString();

        claimDisableImage.enabled = !state.isClear;
        rerollButton.gameObject.SetActive(!state.isRerolled && !state.isClear);
        claimButton.enabled = state.isClear && !state.isClaimed;
        done.SetActive(state.isClaimed);
    }
}
