using Furball.Vixie.Helpers;

namespace Furball.Vixie; 

public class ViewEventLoop : EventLoop {
    internal WindowManager WindowManager;

    public override void Run() {
        Guard.Assert(!this.Running);

        this.WindowManager.GameView.Load    += this.ViewOnLoad;
        this.WindowManager.GameView.Closing += this.ViewOnClosing;
        this.WindowManager.GameView.Update += this.ViewOnUpdate;
        this.WindowManager.GameView.Render += this.ViewOnRender;
        
        this.WindowManager.RunWindow();
    }

    private void ViewOnLoad() {
        this.Running = true;
        
        this.CallStart();
    }
    
    private void ViewOnUpdate(double delta) {
        this.CallUpdate(delta);
    }
    private void ViewOnRender(double delta) {
        this.CallDraw(delta);
    }
    
    private void ViewOnClosing() {
        this.Running = false;
        
        this.CallClosing();
    }
    
    public override void Close() {
        Guard.Assert(this.Running);
        
        this.WindowManager.Close();
    }
    public override void DoDraw() {
        this.WindowManager.GameView.DoRender();
    }
    public override void DoUpdate() {
        this.WindowManager.GameView.DoUpdate();
    }
}