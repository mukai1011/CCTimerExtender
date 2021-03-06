using System.Diagnostics;
using System.Runtime.InteropServices;

public class CCTimerExtender : IDisposable
{
    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    const string CLASSNAME_APP = "WindowsForms10.Window.8.app.0.34f5582_r7_ad1";
    const string CLASSNAME_SYSTABCONTROL32 = "WindowsForms10.SysTabControl32.app.0.34f5582_r7_ad1";
    const string CLASSNAME_STATIC = "WindowsForms10.STATIC.app.0.34f5582_r7_ad1";
    const string CLASSNAME_BUTTON = "WindowsForms10.BUTTON.app.0.34f5582_r7_ad1";

    /// <summary>
    /// CCTimer.exe
    /// </summary>
    Process? _process;
    /// <summary>
    /// CCTimer GUI root
    /// </summary>
    IntPtr _application;
    /// <summary>
    /// CCTimer > 0:00
    /// </summary>
    IntPtr _txtTimer;
    /// <summary>
    /// CCTimer > CountUpTimer > Start/Cancel
    /// </summary>
    IntPtr _btnCountUpTimerStartCancel;

    CancellationTokenSource _cts;
    public event EventHandler OnCountUpTimerStart;
    public event EventHandler OnCountUpTimerStop;

    /// <summary>
    /// CCTimer.exe を起動し、タイマーの開始時/終了時に発火するイベントを提供するクラス<br/>
    /// デフォルトで "{AppContext.BaseDirectory}\CCTimer\CCTimer.exe" を参照します
    /// </summary>
    public CCTimerExtender() : this(Path.Join(AppContext.BaseDirectory, "CCTimer", "CCTimer.exe")) {}
    /// <summary>
    /// CCTimer.exe を起動し、タイマーの開始時/終了時に発火するイベントを提供するクラス
    /// </summary>
    /// <param name="path">CCTimer.exe</param>
    public CCTimerExtender(string path)
    {
        // CCTimer.exeを起動する
        _process = Process.Start(new ProcessStartInfo(path));
        if (_process == null) throw new Exception("CCTimer.exe dose not start.");
        
        // 要素のハンドルを掴めるようになるまで待機する
        // _process.MainWindowHandle と実際のウィンドウハンドルが対応しないタイプのアプリケーションらしい
        // 各要素のハンドルを掴めるようになるまでラグがある
        while (true)
        {
            try
            {
                GetDesktopWindow().GetChildHandle(CLASSNAME_APP, "CCTimer").GetChildHandle(CLASSNAME_APP, "").GetChildHandle(CLASSNAME_STATIC, "0:00");
                GetDesktopWindow().GetChildHandle(CLASSNAME_APP, "CCTimer").GetChildHandle(CLASSNAME_SYSTABCONTROL32, "").GetChildHandle(CLASSNAME_APP, "CountUpTimer").GetChildHandle(CLASSNAME_BUTTON, "Start");
                break;
            }
            catch
            {
                // Console.WriteLine("Waiting for controls to become operational.: {0}", DateTime.Now.Ticks);
            }
        }
        
        // 各要素のハンドルを掴む
        try
        {
            _application = GetDesktopWindow().GetChildHandle(CLASSNAME_APP, "CCTimer");
            _txtTimer = _application
                .GetChildHandle(CLASSNAME_APP, "")
                .GetChildHandle(CLASSNAME_STATIC, "0:00");
            _btnCountUpTimerStartCancel = _application
                .GetChildHandle(CLASSNAME_SYSTABCONTROL32, "")
                .GetChildHandle(CLASSNAME_APP, "CountUpTimer")
                .GetChildHandle(CLASSNAME_BUTTON, "Start");
        }
        catch
        {
            _process.Kill(true);
            _process.Dispose();
            throw;
        }

        OnCountUpTimerStart += (object? sender, EventArgs e) => {};
        OnCountUpTimerStop += (object? sender, EventArgs e) => {};

        _cts = new CancellationTokenSource();
        Task.Run(() => EventLoop(_cts.Token));
    }

    private void EventLoop(CancellationToken ct)
    {
        bool busy = false;

        string txt_txtTimer = "";
        string txt_btnCountUpTimerStartCancel = "";
        
        Task.Run(() => { while (!ct.IsCancellationRequested) txt_txtTimer = _txtTimer.GetText();});
        Task.Run(() => { while (!ct.IsCancellationRequested) txt_btnCountUpTimerStartCancel = _btnCountUpTimerStartCancel.GetText();});

        while (!ct.IsCancellationRequested)
        {
            if (!busy && txt_txtTimer != "0:00" && txt_btnCountUpTimerStartCancel == "Cancel")
            {
                busy = true;
                OnCountUpTimerStart(this, EventArgs.Empty);
            }
            else if (busy && txt_txtTimer == "0:00" && txt_btnCountUpTimerStartCancel == "Start")
            {
                busy = false;
                OnCountUpTimerStop(this, EventArgs.Empty);
            }
        }
    }

    public void WaitForExit()
    {
        _process?.WaitForExit();
    }
    public bool HasExited
    {
        get { return _process == null ? true : _process.HasExited; }
    }

    // IDisposable
    private bool _disposed = false;
    public void Dispose()
    {
        Dispose(true);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cts.Cancel();
                if (_process != null)
                {
                    _process.Kill(true);
                    _process.Dispose();
                    _process = null;
                }
            }
            _disposed = true;
        }
    }
}