using System;

namespace Penguin690_sMusicPlayer.Models;

internal class Status(Action<string> action)
{
    private readonly Action<string> _action = action;

    public void Register(IStatusSender sender)
    {
        sender.StatusSendEvent += OnStatusUpdate;
    }

    public void OnStatusUpdate(object sender, StatusEventArgs e)
    {
        string message = $"[{DateTime.Now:hh:mm}][{sender.GetType().Name}] {e.message}";
        _action(message);
    }
}

internal interface IStatusSender
{
    public Status Status { get; }
    public event EventHandler<StatusEventArgs> StatusSendEvent;
    public void StatusRegister();
    public void StatusUpdate(string message);
}

internal class StatusEventArgs(string message) : EventArgs
{
    public string message { get; set; } = message;
}