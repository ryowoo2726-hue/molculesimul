using UnityEngine;
using UnityEngine.UI;

public sealed class MoleculeStatusView : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text detailText;

    public void Show(MoleculeMatch match, int atomCount, int bondCount)
    {
        if (match == null)
        {
            SetText("불안정한 구조", $"원자 {atomCount}개, 결합 {bondCount}개");
            return;
        }

        SetText(match.Definition.nameKo, $"{match.Definition.formula} / 원자 {atomCount}개, 결합 {bondCount}개");
    }

    private void SetText(string title, string detail)
    {
        if (titleText != null)
            titleText.text = title;

        if (detailText != null)
            detailText.text = detail;
    }
}

