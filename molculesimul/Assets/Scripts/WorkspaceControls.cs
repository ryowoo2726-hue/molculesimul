using UnityEngine;

public sealed class WorkspaceControls : MonoBehaviour
{
    [SerializeField] private MoleculeWorkspace workspace;
    [SerializeField] private MoleculeBuilderController builderController;
    [SerializeField] private MoleculeStatusView statusView;

    public void ClearAll()
    {
        if (workspace == null)
            return;

        workspace.ClearAll();
        if (builderController != null)
            builderController.RebuildSlots();

        if (statusView != null)
            statusView.Show(null, 0, 0);
    }
}
