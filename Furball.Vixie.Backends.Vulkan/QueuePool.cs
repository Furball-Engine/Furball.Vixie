using System;
using System.Diagnostics;
using Kettu;
using Silk.NET.Vulkan;

namespace Furball.Vixie.Backends.Vulkan; 

public sealed class QueuePool : IDisposable {

    private sealed class QueueReference : QueueInfo {
#if DEBUG
        private bool _released = false;
#endif

        private readonly QueuePool _parent;

        public QueueReference(QueuePool parent, int queueFamilyIndex, int queueIndex, Queue handle, QueueFamilyProperties familyProperties) : base(queueFamilyIndex, queueIndex, handle, familyProperties) {
            this._parent = parent;
        }

        public override void Dispose() {
            base.Dispose();

            this._parent.Release(this);

#if DEBUG
            if (this._released) {
                Logger.Log($"Double Release of Queue {Handle} detected.", LoggerLevelVulkan.InstanceWarning);
            }
            this._released = true;
#endif
        }
    }

    private readonly ReadOnlyMemory<QueueInfo> _queueInfos;
    private readonly Memory<int>               _usageCount;

    public QueuePool(ReadOnlyMemory<QueueInfo> queueInfos) {
        this._queueInfos = queueInfos;
        this._usageCount = new int[queueInfos.Length];
    }

    private void Release(QueueReference reference) {
        ReadOnlySpan<QueueInfo> s = this._queueInfos.Span;

        int i;
        for (i = 0; i < s.Length; i++) {
            if (s[i].Handle.Handle == reference.Handle.Handle) {
                break;
            }
        }

        this._usageCount.Span[i]--;
    }

    public QueueInfo GetReferenceTo(QueueInfo info) {
        ReadOnlySpan<QueueInfo> s = this._queueInfos.Span;

        int i;
        for (i = 0; i < s.Length; i++) {
            if (s[i].Handle.Handle == info.Handle.Handle) {
                break;
            }
        }

        QueueReference v = new(this, s[i].QueueFamilyIndex, s[i].QueueIndex, s[i].Handle, s[i].FamilyProperties);

        this._usageCount.Span[i]++;

        return v;
    }

    private bool TryFindNext(QueueFlags requiredFlags, out QueueInfo? best) {
        ReadOnlySpan<QueueInfo> s1 = this._queueInfos.Span;
        Span<int>               s2 = this._usageCount.Span;

        Debug.Assert(s1.Length == s2.Length);

        int iMax = 0;
        int max  = int.MaxValue;
        for (int i = 0; i < s1.Length; i++) {
            if (s2[i] < max && (s1[i].FamilyProperties.QueueFlags & requiredFlags) == requiredFlags) {
                iMax = i;
                max  = s2[i];
            }
        }

        if (max < 0) {
            best = null;
            return false;
        }

        best = new QueueReference(this, s1[iMax].QueueFamilyIndex, s1[iMax].QueueIndex, s1[iMax].Handle, s1[iMax].FamilyProperties);

        s2[iMax]++;

        return true;
    }

    public QueueInfo? NextTransferQueue() {
        if (!TryFindNext(QueueFlags.TransferBit, out QueueInfo? queueInfo)) {
            Logger.Log("Could not find Transfer queue", LoggerLevelVulkan.InstanceFatal);
            return null;
        }

        return queueInfo;
    }

    public QueueInfo? NextComputeQueue() {
        if (!TryFindNext(QueueFlags.ComputeBit, out QueueInfo? queueInfo)) {
            Logger.Log("Could not find Compute queue", LoggerLevelVulkan.InstanceFatal);
            return null;
        }

        return queueInfo;
    }

    public QueueInfo? NextGraphicsQueue() {
        if (!TryFindNext(QueueFlags.GraphicsBit, out QueueInfo? queueInfo)) {
            Logger.Log("Could not find Graphics queue", LoggerLevelVulkan.InstanceFatal);
            return null;
        }

        return queueInfo;
    }

    public void Dispose() {
        ReadOnlySpan<QueueInfo> s  = this._queueInfos.Span;
        Span<int>               s2 = this._usageCount.Span;

        Debug.Assert(s.Length == s2.Length);

        for (int index = 0; index < s.Length; index++) {
            s[index].Dispose();
            if (s2[index] > 0) {
                Logger.Log($"Queue {s[index].Handle} still had open references. R={s2[index]}", LoggerLevelVulkan.InstanceWarning);
            }
        }
    }
}