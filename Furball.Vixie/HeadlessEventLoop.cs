using System.Diagnostics;
using System.Threading;
using Furball.Vixie.Helpers;

namespace Furball.Vixie; 

public class HeadlessEventLoop : EventLoop {
    private Stopwatch _stopwatch;
    
    public override void Run() {
        Guard.Assert(!this.Running);
        
        this.Running = true;
        
        this.CallStart();
        
        this._stopwatch = Stopwatch.StartNew();

        //Begin the event loop
        this.Loop();
    }

    private void Loop() {
        double lastTime = this._stopwatch.Elapsed.TotalMilliseconds;
        while (this.Running) {
            double delta = this._stopwatch.Elapsed.TotalMilliseconds - lastTime;
            this.CallUpdate(delta / 1000d);
            
            //In case the user closes the window during update, dont draw :^)
            if (!this.Running)
                break;
            
            this.CallDraw(delta / 1000d);
            lastTime = this._stopwatch.Elapsed.TotalSeconds;
            
            Thread.Sleep(100);
        }
    }
    
    public override void Close() {
        Guard.Assert(this.Running);
        
        this.Running = false;
        
        this._stopwatch.Stop();
        this._stopwatch = null;
        
        this.CallClosing();
    }
    public override void DoDraw() {
        this.CallDraw(1 / 60d);
    }
    public override void DoUpdate() {
        this.CallUpdate(1 / 60d);
    }
}