using System.Windows;
using SpaceMap.Infrastructure.NativeShell;

namespace SpaceMap.App.Services;

public sealed class WindowLifecycleService(WindowCloseGuard closeGuard)
{
    public bool ConfirmClose(Window owner, bool hasActiveScan)
    {
        if (!closeGuard.ShouldPromptForClose(hasActiveScan))
        {
            return true;
        }

        var result = MessageBox.Show(
            owner,
            "A scan is currently running. Closing now will cancel the active work and keep only the results that were already confirmed.",
            "Close with active scan",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }
}
