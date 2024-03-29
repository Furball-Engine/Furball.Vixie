﻿using System;

namespace Furball.Vixie.Backends.Shared.TextureEffects;

public abstract class TextureEffect : IDisposable {
    public abstract void UpdateTexture();

    public abstract void SetSourceTexture(VixieTexture tex);

    public abstract VixieTexture Texture { get; }

    public abstract void Dispose();
}