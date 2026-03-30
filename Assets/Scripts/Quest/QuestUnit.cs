using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestUnit : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI condition;
    [SerializeField] TextMeshProUGUI gageText;
    [SerializeField] TextMeshProUGUI amount;
    [SerializeField] Slider gage;
    [SerializeField] Button rerollButton;
    [SerializeField] Button claimButton;

    public void SetCondition(QuestState state)
    {
        condition.text = state.data.DescriptionKr;
        gageText.text = state.progress.ToString() + "/" + state.data.ConditionValue.ToString();
        gage.value = (float)state.progress / state.data.ConditionValue;
        amount.text = state.data.RewardDiamond.ToString();

        rerollButton.gameObject.SetActive(!state.isRerolled && !state.isClear);
        claimButton.enabled = state.isClear && !state.isClaimed;
    }
}
