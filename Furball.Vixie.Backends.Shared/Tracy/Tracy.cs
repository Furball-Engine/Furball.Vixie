using System.Runtime.InteropServices;
using Furball.Vixie.Backends.Shared.Tracy.Structs;

namespace Furball.Vixie.Backends.Shared.Tracy;

public static partial class Tracy {
    private const string LIB_NAME = "tracy";

    //TRACY_API uint64_t ___tracy_alloc_srcloc( uint32_t line, const char* source, size_t sourceSz, const char* function, size_t functionSz );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_alloc_srcloc")]
    public static extern unsafe ulong AllocSourceLocation(
        uint line, byte* source, nint sourceSz, byte* function, nint functionSz
    );

    //TRACY_API uint64_t ___tracy_alloc_srcloc_name( uint32_t line, const char* source, size_t sourceSz, const char* function, size_t functionSz, const char* name, size_t nameSz );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_alloc_srcloc_name")]
    public static extern unsafe ulong AllocSourceLocationName(
        uint line, byte* source, nint sourceSz, byte* function, nint functionSz, byte* name, nint nameSz
    );

    //TRACY_API TracyCZoneCtx ___tracy_emit_zone_begin( const struct ___tracy_source_location_data* srcloc, int active );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_zone_begin")]
    public static extern unsafe TracyCZoneContext EmitZoneBegin(SourceLocationData* srcloc, int active);

    //TRACY_API TracyCZoneCtx ___tracy_emit_zone_begin_callstack( const struct ___tracy_source_location_data* srcloc, int depth, int active );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_zone_begin_callstack"
    )]
    public static extern unsafe TracyCZoneContext EmitZoneBeginCallstack(
        SourceLocationData* srcloc, int depth, int active
    );

    //TRACY_API TracyCZoneCtx ___tracy_emit_zone_begin_alloc( uint64_t srcloc, int active );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_zone_begin_alloc")]
    public static extern unsafe TracyCZoneContext EmitZoneBeginAlloc(ulong srcloc, int active);

    //TRACY_API TracyCZoneCtx ___tracy_emit_zone_begin_alloc_callstack( uint64_t srcloc, int depth, int active );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_zone_begin_alloc_callstack"
    )]
    public static extern unsafe TracyCZoneContext EmitZoneBeginAllocCallstack(ulong srcloc, int depth, int active);

    //TRACY_API void ___tracy_emit_zone_end( TracyCZoneCtx ctx );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_zone_end")]
    private static extern unsafe void EmitZoneEnd(TracyCZoneContext ctx);

    //TRACY_API void ___tracy_emit_zone_text( TracyCZoneCtx ctx, const char* txt, size_t size );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_zone_text")]
    public static extern unsafe void EmitZoneText(
        TracyCZoneContext ctx, [MarshalAs(UnmanagedType.LPStr)] string txt, nint size
    );

    //TRACY_API void ___tracy_emit_zone_name( TracyCZoneCtx ctx, const char* txt, size_t size );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_zone_name")]
    public static extern unsafe void EmitZoneName(
        TracyCZoneContext ctx, [MarshalAs(UnmanagedType.LPStr)] string txt, nint size
    );

    //TRACY_API void ___tracy_emit_zone_color( TracyCZoneCtx ctx, uint32_t color );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_zone_color")]
    public static extern unsafe void EmitZoneColor(TracyCZoneContext ctx, uint color);

    //TRACY_API void ___tracy_emit_zone_value( TracyCZoneCtx ctx, uint64_t value );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_zone_value")]
    public static extern unsafe void EmitZoneValue(TracyCZoneContext ctx, ulong value);

    //TRACY_API void ___tracy_emit_gpu_zone_begin( const struct ___tracy_gpu_zone_begin_data );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_gpu_zone_begin")]
    public static extern unsafe void EmitGpuZoneBegin(GpuZoneBeginData data);

    //TRACY_API void ___tracy_emit_gpu_zone_begin_callstack( const struct ___tracy_gpu_zone_begin_callstack_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_zone_begin_callstack"
    )]
    public static extern unsafe void EmitGpuZoneBeginCallstack(GpuZoneBeginCallstackData data);

    //TRACY_API void ___tracy_emit_gpu_zone_begin_alloc( const struct ___tracy_gpu_zone_begin_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_zone_begin_alloc"
    )]
    public static extern unsafe void EmitGpuZoneBeginAlloc(GpuZoneBeginData data);

    //TRACY_API void ___tracy_emit_gpu_zone_begin_alloc_callstack( const struct ___tracy_gpu_zone_begin_callstack_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_zone_begin_alloc_callstack"
    )]
    public static extern unsafe void EmitGpuZoneBeginAllocCallstack(GpuZoneBeginCallstackData data);

    //TRACY_API void ___tracy_emit_gpu_zone_end( const struct ___tracy_gpu_zone_end_data data );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_gpu_zone_end")]
    public static extern unsafe void EmitGpuZoneEnd(GpuZoneEndData data);

    //TRACY_API void ___tracy_emit_gpu_time( const struct ___tracy_gpu_time_data );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_gpu_time")]
    public static extern unsafe void EmitGpuTime(GpuTimeData data);

    //TRACY_API void ___tracy_emit_gpu_new_context( const struct ___tracy_gpu_new_context_data );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_gpu_new_context")]
    public static extern unsafe void EmitGpuNewContext(GpuNewContextData data);

    //TRACY_API void ___tracy_emit_gpu_context_name( const struct ___tracy_gpu_context_name_data );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_gpu_context_name")]
    public static extern unsafe void EmitGpuContextName(GpuContextNameData data);

    //TRACY_API void ___tracy_emit_gpu_calibration( const struct ___tracy_gpu_calibration_data );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_gpu_calibration")]
    public static extern unsafe void EmitGpuCalibration(GpuCalibrationData data);

    //TRACY_API void ___tracy_emit_gpu_zone_begin_serial( const struct ___tracy_gpu_zone_begin_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_zone_begin_serial"
    )]
    public static extern unsafe void EmitGpuZoneBeginSerial(GpuZoneBeginData data);

    //TRACY_API void ___tracy_emit_gpu_zone_begin_callstack_serial( const struct ___tracy_gpu_zone_begin_callstack_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_zone_begin_callstack_serial"
    )]
    public static extern unsafe void EmitGpuZoneBeginCallstackSerial(GpuZoneBeginCallstackData data);

    //TRACY_API void ___tracy_emit_gpu_zone_begin_alloc_serial( const struct ___tracy_gpu_zone_begin_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_zone_begin_alloc_serial"
    )]
    public static extern unsafe void EmitGpuZoneBeginAllocSerial(GpuZoneBeginData data);

    //TRACY_API void ___tracy_emit_gpu_zone_begin_alloc_callstack_serial( const struct ___tracy_gpu_zone_begin_callstack_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_zone_begin_alloc_callstack_serial"
    )]
    public static extern unsafe void EmitGpuZoneBeginAllocCallstackSerial(GpuZoneBeginCallstackData data);

    //TRACY_API void ___tracy_emit_gpu_zone_end_serial( const struct ___tracy_gpu_zone_end_data data );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_gpu_zone_end_serial")]
    public static extern unsafe void EmitGpuZoneEndSerial(GpuZoneEndData data);

    //TRACY_API void ___tracy_emit_gpu_time_serial( const struct ___tracy_gpu_time_data );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_gpu_time_serial")]
    public static extern unsafe void EmitGpuTimeSerial(GpuTimeData data);

    //TRACY_API void ___tracy_emit_gpu_new_context_serial( const struct ___tracy_gpu_new_context_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_new_context_serial"
    )]
    public static extern unsafe void EmitGpuNewContextSerial(GpuNewContextData data);

    //TRACY_API void ___tracy_emit_gpu_context_name_serial( const struct ___tracy_gpu_context_name_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_context_name_serial"
    )]
    public static extern unsafe void EmitGpuContextNameSerial(GpuContextNameData data);

    //TRACY_API void ___tracy_emit_gpu_calibration_serial( const struct ___tracy_gpu_calibration_data );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_gpu_calibration_serial"
    )]
    public static extern unsafe void EmitGpuCalibrationSerial(GpuCalibrationData data);

    //TRACY_API int ___tracy_connected(void);
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_connected")]
    public static extern int Connected();

    //TRACY_API void ___tracy_emit_memory_alloc( const void* ptr, size_t size, int secure );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_memory_alloc")]
    public static extern unsafe void EmitMemoryAlloc(void* ptr, nint size, int secure);

    //TRACY_API void ___tracy_emit_memory_alloc_callstack( const void* ptr, size_t size, int depth, int secure );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_memory_alloc_callstack"
    )]
    public static extern unsafe void EmitMemoryAllocCallstack(void* ptr, nint size, int depth, int secure);

    //TRACY_API void ___tracy_emit_memory_free( const void* ptr, int secure );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_memory_free")]
    public static extern unsafe void EmitMemoryFree(void* ptr, int secure);

    //TRACY_API void ___tracy_emit_memory_free_callstack( const void* ptr, int depth, int secure );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_memory_free_callstack"
    )]
    public static extern unsafe void EmitMemoryFreeCallstack(void* ptr, int depth, int secure);

    //TRACY_API void ___tracy_emit_memory_alloc_named( const void* ptr, size_t size, int secure, const char* name );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_memory_alloc_named")]
    public static extern unsafe void EmitMemoryAllocNamed(
        void* ptr, nint size, int secure, [MarshalAs(UnmanagedType.LPStr)] string name
    );

    //TRACY_API void ___tracy_emit_memory_alloc_callstack_named( const void* ptr, size_t size, int depth, int secure, const char* name );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_memory_alloc_callstack_named"
    )]
    public static extern unsafe void EmitMemoryAllocCallstackNamed(
        void* ptr, nint size, int depth, int secure, [MarshalAs(UnmanagedType.LPStr)] string name
    );

    //TRACY_API void ___tracy_emit_memory_free_named( const void* ptr, int secure, const char* name );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_memory_free_named")]
    public static extern unsafe void EmitMemoryFreeNamed(
        void* ptr, int secure, [MarshalAs(UnmanagedType.LPStr)] string name
    );

    //TRACY_API void ___tracy_emit_memory_free_callstack_named( const void* ptr, int depth, int secure, const char* name );
    [DllImport(
    LIB_NAME,
    CallingConvention = CallingConvention.Cdecl,
    EntryPoint = "___tracy_emit_memory_free_callstack_named"
    )]
    public static extern unsafe void EmitMemoryFreeCallstackNamed(
        void* ptr, int depth, int secure, [MarshalAs(UnmanagedType.LPStr)] string name
    );

    //TRACY_API void ___tracy_emit_message( const char* txt, size_t size, int callstack );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_message")]
    public static extern unsafe void EmitMessage([MarshalAs(UnmanagedType.LPStr)] string txt, nint size, int callstack);
    //TRACY_API void ___tracy_emit_messageL( const char* txt, int callstack );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_messageL")]
    public static extern unsafe void EmitMessageL([MarshalAs(UnmanagedType.LPStr)] string txt, int callstack);
    //TRACY_API void ___tracy_emit_messageC( const char* txt, size_t size, uint32_t color, int callstack );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_messageC")]
    public static extern unsafe void EmitMessageC(
        [MarshalAs(UnmanagedType.LPStr)] string txt, nint size, uint color, int callstack
    );
    //TRACY_API void ___tracy_emit_messageLC( const char* txt, uint32_t color, int callstack );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_messageLC")]
    public static extern unsafe void EmitMessageLC(
        [MarshalAs(UnmanagedType.LPStr)] string txt, uint color, int callstack
    );

    //TRACY_API void ___tracy_emit_frame_mark( const char* name );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_frame_mark")]
    public static extern unsafe void EmitFrameMark([MarshalAs(UnmanagedType.LPStr)] string name);
    //TRACY_API void ___tracy_emit_frame_mark_start( const char* name );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_frame_mark_start")]
    public static extern unsafe void EmitFrameMarkStart([MarshalAs(UnmanagedType.LPStr)] string name);
    //TRACY_API void ___tracy_emit_frame_mark_end( const char* name );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_frame_mark_end")]
    public static extern unsafe void EmitFrameMarkEnd([MarshalAs(UnmanagedType.LPStr)] string name);
    //TRACY_API void ___tracy_emit_frame_image( const void* image, uint16_t w, uint16_t h, uint8_t offset, int flip );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_frame_image")]
    public static extern unsafe void EmitFrameImage(void* image, ushort w, ushort h, byte offset, int flip);

    //TRACY_API void ___tracy_emit_plot( const char* name, double val );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_plot")]
    public static extern unsafe void EmitPlot([MarshalAs(UnmanagedType.LPStr)] string name, double val);
    //TRACY_API void ___tracy_emit_plot_float( const char* name, float val );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_plot_float")]
    public static extern unsafe void EmitPlotFloat([MarshalAs(UnmanagedType.LPStr)] string name, float val);
    //TRACY_API void ___tracy_emit_plot_int( const char* name, int64_t val );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_plot_int")]
    public static extern unsafe void EmitPlotInt([MarshalAs(UnmanagedType.LPStr)] string name, long val);
    //TRACY_API void ___tracy_emit_message_appinfo( const char* txt, size_t size );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_emit_message_appinfo")]
    public static extern unsafe void EmitMessageAppInfo([MarshalAs(UnmanagedType.LPStr)] string txt, nint size);

    //TRACY_API void ___tracy_set_thread_name( const char* name );
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_set_thread_name")]
    public static extern unsafe void SetThreadName([MarshalAs(UnmanagedType.LPStr)] string name);

    //TRACY_API void ___tracy_startup_profiler(void);
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_startup_profiler")]
    public static extern unsafe void StartupProfiler();
    //TRACY_API void ___tracy_shutdown_profiler(void);
    [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "___tracy_shutdown_profiler")]
    public static extern unsafe void ShutdownProfiler();
}