using System;
using Silk.NET.Vulkan;

namespace Furball.Vixie.Backends.Vulkan; 

public class QueueInfo : IDisposable {
    public int                   QueueFamilyIndex { get; }
    public int                   QueueIndex       { get; }
    public Queue                 Handle           { get; }
    public QueueFamilyProperties FamilyProperties { get; }

    public QueueInfo(
        int                   queueFamilyIndex,
        int                   queueIndex,
        Queue                 handle,
        QueueFamilyProperties familyProperties
    ) {
        this.QueueFamilyIndex = queueFamilyIndex;
        this.QueueIndex       = queueIndex;
        this.Handle           = handle;
        this.FamilyProperties = familyProperties;
    }

    public virtual void Dispose() {}
}