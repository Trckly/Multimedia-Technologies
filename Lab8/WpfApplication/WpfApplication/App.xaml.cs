using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Animated3DBalls
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Prevent driver issues with transparent maximized windows
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            base.OnStartup(e);
        }
    }
}