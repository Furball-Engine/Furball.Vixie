using System;

namespace Furball.Vixie; 

public abstract class EventLoop {
    public bool Running { get; protected set; }

    public event EventHandler Start;
    protected void CallStart() {
        Start?.Invoke(this, EventArgs.Empty);
    }
    public event EventHandler Closing;
    protected void CallClosing() {
        Closing?.Invoke(this, EventArgs.Empty);
    }
    
    public event EventHandler<double> Update; 
    protected void CallUpdate(double d) {
        Update?.Invoke(this, d);
    }
    public event EventHandler<double> Draw; 
    protected void CallDraw(double d) {
        Draw?.Invoke(this, d);
    }
    public abstract void Run();
    public abstract void Close();

    public abstract void DoDraw();
    public abstract void DoUpdate();
}