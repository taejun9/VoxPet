using System.Windows.Controls;
using System.Windows.Threading;

namespace VoxPet.App.Views;

/// <summary>Invoke는 Input 작업을 게시한다. 실제 입력·바인딩·레이아웃 이후에 판정한다.</summary>
internal static class UiAutomationQa
{
    internal static async Task InvokeAsync(Button button)
    {
        var peer = new System.Windows.Automation.Peers.ButtonAutomationPeer(button);
        ((System.Windows.Automation.Provider.IInvokeProvider)peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Invoke)).Invoke();
        await button.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
    }
}
