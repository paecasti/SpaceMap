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
            "Hay un escaneo activo. Si cierras ahora, se cancelara el trabajo en curso y se mantendran solo los resultados ya confirmados.",
            "Cerrar con escaneo activo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        return result == MessageBoxResult.Yes;
    }
}
