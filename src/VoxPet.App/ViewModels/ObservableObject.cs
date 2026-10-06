using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace VoxPet.App.ViewModels;

/// <summary>
/// 외부 MVVM 라이브러리 없이 WPF 바인딩에 변경을 알리는 기반 클래스. 호출은 UI 스레드에서 한다.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    // 실제 값이 달라질 때만 통지해 불필요한 렌더링과 이벤트를 줄인다. CallerMemberName으로 속성명을 얻는다.
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; Notify(name); return true;
    }
}

/// <summary>
/// 동기 UI 명령. Refresh를 통해 활성 조건이 바뀌었음을 버튼에 알린다.
/// </summary>
public sealed class RelayCommand(Action execute, Func<bool>? enabled = null) : ICommand
{
    public bool CanExecute(object? parameter) => enabled?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
    public event EventHandler? CanExecuteChanged;
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// 비동기 UI 명령. 실행 중 재진입을 막고 finally에서 버튼 활성 상태를 복구한다.
/// ICommand의 Execute는 void이므로 예상 오류 처리는 전달된 비동기 작업이 담당한다.
/// </summary>
public sealed class AsyncCommand(Func<Task> execute, Func<bool>? enabled = null) : ICommand
{
    private bool busy;
    public bool CanExecute(object? parameter) => !busy && (enabled?.Invoke() ?? true);
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        busy = true; Refresh();
        try { await execute(); }
        finally { busy = false; Refresh(); }
    }
    public event EventHandler? CanExecuteChanged;
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
