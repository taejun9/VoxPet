using Forms = System.Windows.Forms;
using System.Drawing;

namespace VoxPet.App.Services;

/// <summary>WPF의 메시지 루프에서 로컬 트레이 아이콘만 관리한다. 입력 캡처와 분리한다.</summary>
internal sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon icon;
    private readonly Forms.ContextMenuStrip menu;
    private readonly Icon image;
    private bool disposed;
    internal bool Visible => !disposed && icon.Visible;
    internal TrayService(Action restore, Action broadcast, Action exit)
    {
        image = (Icon)SystemIcons.Application.Clone();
        menu = new Forms.ContextMenuStrip { ForeColor = Color.Black, BackColor = Color.White };
        menu.Items.Add("설정창 열기", null, (_, _) => restore());
        menu.Items.Add("OBS용 방송창 열기", null, (_, _) => broadcast());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => exit());
        icon = new Forms.NotifyIcon { Icon = image, Text = "VoxPet · 설정창 열기", ContextMenuStrip = menu };
        icon.DoubleClick += (_, _) => restore();
    }
    internal void Show() => icon.Visible = true;
    internal void Hide() => icon.Visible = false;
    internal void ActivateMenu(int index) => ((Forms.ToolStripMenuItem)menu.Items[index]).PerformClick();
    public void Dispose() { if (disposed) return; disposed = true; icon.Visible = false; icon.Dispose(); menu.Dispose(); image.Dispose(); }
}
