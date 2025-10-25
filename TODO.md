# .NET 8.0 Migration and Modernization Plan

**Project**: Otelnet Mono → Otelnet .NET 8.0
**Created**: 2025-10-25
**Status**: Planning Phase

## Executive Summary

Migrate the current Mono-based Telnet client (targeting .NET Framework 4.5) to .NET 8.0, leveraging modern C# features, improved performance, and cross-platform capabilities while maintaining full backward compatibility with existing functionality.

## Current State Analysis

- **Target Framework**: .NET Framework 4.5 (Mono)
- **Language Version**: C# 5.0
- **Platform Dependencies**: Mono.Posix, Mono.Unix.Native
- **Build System**: Mono mcs compiler + MSBuild
- **Code Size**: ~3,210 lines of C# code
- **Features**: Complete telnet client with RFC 854/855 compliance
- **Status**: 100% feature complete, all tests passing

## Migration Goals

1. **Modernize to .NET 8.0** - Latest LTS runtime with best performance
2. **Remove Mono dependency** - Use native .NET APIs for POSIX operations
3. **Improve performance** - Leverage Span<T>, Memory<T>, async/await
4. **Enhance code quality** - Nullable references, modern C# patterns
5. **Optimize deployment** - NativeAOT, trimming, single-file publish
6. **Maintain compatibility** - All existing features must work identically

---

## Migration Phases

### Phase 1: Project Infrastructure (Priority: Critical)

#### Task 1.1: Create New .NET 8.0 Project Structure
- [ ] Create new SDK-style `.csproj` file
  - Target framework: `net8.0`
  - Language version: `latest` (C# 12)
  - Enable nullable reference types
  - Configure output settings

**Current** (.NET Framework 4.5 style):
```xml
<Project DefaultTargets="Build" ToolsVersion="4.0">
  <PropertyGroup>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
</Project>
```

**Target** (.NET 8.0 SDK style):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <OutputType>Exe</OutputType>
    <RootNamespace>Otelnet</RootNamespace>
    <AssemblyName>otelnet</AssemblyName>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>false</InvariantGlobalization>
  </PropertyGroup>
</Project>
```

#### Task 1.2: Update Build System
- [ ] Update Makefile to use `dotnet build` instead of `mcs`
- [ ] Add `dotnet publish` configurations
- [ ] Configure Release/Debug profiles
- [ ] Update install.sh for .NET runtime detection

#### Task 1.3: Verify .NET 8.0 SDK Installation
- [ ] Check `dotnet --version` (should be 8.0.x)
- [ ] Verify SDK availability
- [ ] Test basic compilation

**Estimated Time**: 2-3 hours
**Risk Level**: Low

---

### Phase 2: Platform API Migration (Priority: Critical)

#### Task 2.1: Replace Mono.Posix Dependencies

**Current Dependencies**:
- `Mono.Unix` - Unix signal handling
- `Mono.Unix.Native` - POSIX termios, ioctl

**Migration Strategy**:
1. Use `System.Runtime.InteropServices` for P/Invoke
2. Replace `Mono.Unix.Native.Termios` with custom struct
3. Use `LibraryImport` instead of `DllImport` (source-generated)
4. Implement cross-platform abstractions

#### Task 2.2: Terminal Control Migration (`src/Terminal/TerminalControl.cs`)

**Functions to migrate**:
```csharp
// Current (Mono.Unix.Native)
tcgetattr(int fd, ref Termios termios)
tcsetattr(int fd, int actions, ref Termios termios)
ioctl(int fd, int request, ref Winsize ws)

// Target (.NET 8.0 LibraryImport)
[LibraryImport("libc", SetLastError = true)]
private static partial int tcgetattr(int fd, ref Termios termios);

[LibraryImport("libc", SetLastError = true)]
private static partial int tcsetattr(int fd, int actions, ref Termios termios);

[LibraryImport("libc", SetLastError = true)]
private static partial int ioctl(int fd, int request, ref Winsize ws);
```

**Detailed Changes**:

1. **Define Native Structures** (replace Mono.Unix.Native):
```csharp
[StructLayout(LayoutKind.Sequential)]
public struct Termios
{
    public uint c_iflag;   // Input modes
    public uint c_oflag;   // Output modes
    public uint c_cflag;   // Control modes
    public uint c_lflag;   // Local modes
    public byte c_line;    // Line discipline

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
    public byte[] c_cc;    // Control characters

    public uint c_ispeed;  // Input speed
    public uint c_ospeed;  // Output speed
}

[StructLayout(LayoutKind.Sequential)]
public struct Winsize
{
    public ushort ws_row;
    public ushort ws_col;
    public ushort ws_xpixel;
    public ushort ws_ypixel;
}
```

2. **Signal Handling Migration**:
```csharp
// Current (Mono.Unix)
UnixSignal[] signals = new UnixSignal[] {
    new UnixSignal(Signum.SIGINT),
    new UnixSignal(Signum.SIGTERM),
    new UnixSignal(Signum.SIGWINCH)
};

// Target (.NET 8.0)
PosixSignalRegistration.Create(PosixSignal.SIGINT, OnSignalReceived);
PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnSignalReceived);
PosixSignalRegistration.Create(PosixSignal.SIGWINCH, OnWindowSizeChanged);
```

#### Task 2.3: File I/O and Descriptors
- [ ] Replace Unix-specific file operations with .NET APIs
- [ ] Use `FileStream` with appropriate options
- [ ] Ensure stdin/stdout handling works correctly

**Estimated Time**: 8-10 hours
**Risk Level**: Medium (requires careful testing)

---

### Phase 3: Performance Optimization (Priority: High)

#### Task 3.1: Network I/O Optimization

**Current Implementation** (synchronous):
```csharp
byte[] buffer = new byte[4096];
int bytesRead = stream.Read(buffer, 0, buffer.Length);
```

**Target Implementation** (async + Span):
```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 4096), cancellationToken);
    ProcessData(buffer.AsSpan(0, bytesRead));
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

**Changes Required**:
1. Convert `TelnetConnection` to async/await pattern
2. Use `NetworkStream.ReadAsync()` / `WriteAsync()`
3. Implement `CancellationToken` support
4. Use `ArrayPool<byte>` for buffer management

#### Task 3.2: Protocol Processing Optimization

**Current**:
```csharp
public void ProcessInput(byte[] data, int length)
{
    for (int i = 0; i < length; i++)
    {
        byte b = data[i];
        // State machine processing
    }
}
```

**Target** (Span-based):
```csharp
public void ProcessInput(ReadOnlySpan<byte> data)
{
    foreach (byte b in data)
    {
        // State machine processing
    }
}
```

**Benefits**:
- Zero-copy processing
- Reduced allocations
- Better memory locality
- Improved throughput

#### Task 3.3: String Operations
- [ ] Use `Span<char>` for string building
- [ ] Replace `StringBuilder` with `StringWriter` where appropriate
- [ ] Use `stackalloc` for small temporary buffers
- [ ] Leverage string interpolation improvements

**Example**:
```csharp
// Current
byte[] temp = new byte[256];
string result = Encoding.ASCII.GetString(temp);

// Target
Span<byte> temp = stackalloc byte[256];
string result = Encoding.ASCII.GetString(temp);
```

**Estimated Time**: 12-15 hours
**Risk Level**: Medium

---

### Phase 4: Code Modernization (Priority: Medium)

#### Task 4.1: Nullable Reference Types

Enable nullable annotations throughout:
```csharp
#nullable enable

public class TelnetConnection
{
    private NetworkStream? stream;
    private string? terminalType;

    public bool Connect(string host, int port)
    {
        if (string.IsNullOrEmpty(host))
            throw new ArgumentNullException(nameof(host));
        // ...
    }
}
```

**Files to Update**:
- [ ] `src/Program.cs`
- [ ] `src/Telnet/TelnetConnection.cs`
- [ ] `src/Telnet/TelnetProtocol.cs`
- [ ] `src/Terminal/TerminalControl.cs`
- [ ] `src/Logging/*.cs`
- [ ] `src/Interactive/*.cs`

#### Task 4.2: File-Scoped Namespaces

**Current**:
```csharp
namespace OtelnetMono.Telnet
{
    public class TelnetConnection
    {
        // ...
    }
}
```

**Target**:
```csharp
namespace Otelnet.Telnet;

public class TelnetConnection
{
    // ...
}
```

#### Task 4.3: Record Types for Data Structures

Replace simple classes with records:
```csharp
// Current
public class WindowSizeEventArgs : EventArgs
{
    public int Rows { get; set; }
    public int Columns { get; set; }
}

// Target
public sealed record WindowSizeEventArgs(int Rows, int Columns);
```

#### Task 4.4: Pattern Matching Improvements

**Current**:
```csharp
if (state == TelnetState.Data)
{
    // ...
}
else if (state == TelnetState.IAC)
{
    // ...
}
```

**Target**:
```csharp
switch (state)
{
    case TelnetState.Data:
        // ...
        break;
    case TelnetState.IAC:
        // ...
        break;
    default:
        throw new InvalidOperationException($"Unknown state: {state}");
}
```

#### Task 4.5: Init-Only Properties

```csharp
public class SessionLogger
{
    public string LogPath { get; init; }
    public bool EnableHexDump { get; init; } = true;
}
```

**Estimated Time**: 6-8 hours
**Risk Level**: Low

---

### Phase 5: Advanced Features (Priority: Medium)

#### Task 5.1: Async/Await Throughout

**Main Loop Conversion**:
```csharp
// Current (blocking)
while (running)
{
    // Poll stdin and socket
    if (Poll.poll(fds, timeout) > 0)
    {
        // Process data
    }
}

// Target (async)
await Task.WhenAny(
    ReadStdinAsync(cancellationToken),
    ReadNetworkAsync(cancellationToken)
);
```

**Benefits**:
- Better resource utilization
- Easier cancellation handling
- Improved responsiveness

#### Task 5.2: Modern Command Line Parsing

Consider integrating `System.CommandLine`:
```csharp
var rootCommand = new RootCommand("Telnet client for .NET 8.0");

var hostArg = new Argument<string>("host", "Remote host to connect to");
var portArg = new Argument<int>("port", "Port number");

rootCommand.SetHandler(async (host, port) =>
{
    await RunApplicationAsync(host, port);
}, hostArg, portArg);

return await rootCommand.InvokeAsync(args);
```

**Optional**: Can defer to maintain compatibility

#### Task 5.3: Logging Framework

Replace custom logging with `Microsoft.Extensions.Logging`:
```csharp
private readonly ILogger<TelnetConnection> logger;

logger.LogInformation("Connected to {Host}:{Port}", host, port);
logger.LogWarning("Unexpected byte: {Byte:X2}", b);
logger.LogError(ex, "Connection failed");
```

**Optional**: Can keep custom logging for now

**Estimated Time**: 10-12 hours
**Risk Level**: Medium

---

### Phase 6: Build and Deployment (Priority: High)

#### Task 6.1: NativeAOT Configuration

Enable ahead-of-time compilation:
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
  <IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>
</PropertyGroup>
```

**Benefits**:
- Single native executable (no runtime needed)
- Fast startup time
- Smaller memory footprint
- Better performance

**Challenges**:
- P/Invoke must be NativeAOT-compatible
- No reflection usage
- Requires testing

#### Task 6.2: Trimming Configuration

```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>link</TrimMode>
</PropertyGroup>

<ItemGroup>
  <!-- Preserve necessary types -->
  <TrimmerRootAssembly Include="otelnet" />
</ItemGroup>
```

#### Task 6.3: Multi-Platform Publishing

Create publish profiles:

**Linux x64**:
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishAot=true
```

**Linux ARM64**:
```bash
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishAot=true
```

**macOS**:
```bash
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishAot=true
```

#### Task 6.4: Update Installation Scripts

Update `install.sh`:
- Detect .NET 8.0 runtime instead of Mono
- Copy native binary instead of .exe + mono wrapper
- Update man page references

**Estimated Time**: 6-8 hours
**Risk Level**: Medium

---

### Phase 7: Testing and Validation (Priority: Critical)

#### Task 7.1: Unit Test Migration
- [ ] Ensure existing tests run with .NET 8.0
- [ ] Add performance benchmarks
- [ ] Test async implementations

#### Task 7.2: Integration Testing
- [ ] Run all 24 automated tests
- [ ] Verify telnet protocol compliance
- [ ] Test with real telnet servers (3+ servers)
- [ ] Validate console mode functionality

#### Task 7.3: Performance Testing
- [ ] Benchmark throughput (bytes/sec)
- [ ] Measure latency improvements
- [ ] Memory allocation profiling
- [ ] Startup time comparison

**Baseline** (Mono):
- Startup: ~150ms
- Throughput: ~5 MB/s
- Memory: ~15 MB

**Target** (.NET 8.0):
- Startup: <50ms (with NativeAOT)
- Throughput: >10 MB/s
- Memory: <10 MB

#### Task 7.4: Cross-Platform Testing
- [ ] Test on Ubuntu 22.04 LTS
- [ ] Test on Fedora 39
- [ ] Test on macOS 14 Sonoma
- [ ] Test on ARM64 systems (Raspberry Pi)

**Estimated Time**: 8-10 hours
**Risk Level**: High

---

### Phase 8: Documentation Updates (Priority: Medium)

#### Task 8.1: Update README.md
- [ ] Change requirements from Mono to .NET 8.0
- [ ] Update installation instructions
- [ ] Add NativeAOT deployment info
- [ ] Update build instructions

#### Task 8.2: Update User Documentation
- [ ] `QUICK_START.md` - .NET 8.0 installation
- [ ] `USER_MANUAL.md` - Updated commands
- [ ] `TROUBLESHOOTING.md` - .NET-specific issues

#### Task 8.3: Create Migration Guide
- [ ] `MIGRATION.md` - Mono → .NET 8.0 guide
- [ ] Document breaking changes (if any)
- [ ] Performance improvements documentation

**Estimated Time**: 4-6 hours
**Risk Level**: Low

---

## Detailed Task Breakdown by File

### High Priority Files (Core Functionality)

#### 1. `OtelnetMono.csproj` → `Otelnet.csproj`
**Lines**: 51
**Complexity**: Simple
**Changes**:
- Convert to SDK-style
- Target net8.0
- Remove Mono.Posix reference
- Add NativeAOT properties

**Estimated Time**: 30 minutes

---

#### 2. `src/Terminal/TerminalControl.cs`
**Lines**: ~396
**Complexity**: High (P/Invoke, signals)
**Changes**:
- Remove `using Mono.Unix`
- Define native structs (Termios, Winsize)
- Replace DllImport with LibraryImport
- Use `PosixSignalRegistration` for signals
- Update ioctl/tcgetattr/tcsetattr calls

**Current Dependencies**:
```csharp
using Mono.Unix;
using Mono.Unix.Native;
```

**Target Dependencies**:
```csharp
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
```

**Critical Functions**:
1. `EnableRawMode()` - termios manipulation
2. `DisableRawMode()` - restore settings
3. `InstallSignalHandlers()` - SIGINT/SIGTERM/SIGWINCH
4. `GetWindowSize()` - TIOCGWINSZ ioctl
5. `UpdateWindowSize()` - NAWS updates

**Estimated Time**: 6-8 hours
**Testing Required**: Extensive

---

#### 3. `src/Telnet/TelnetConnection.cs`
**Lines**: ~800+
**Complexity**: High (protocol state machine)
**Changes**:
- Convert to async/await
- Use Span<byte> for protocol processing
- Use ArrayPool for buffers
- Add nullable annotations
- File-scoped namespace

**Key Methods to Update**:
1. `Connect()` → `ConnectAsync()`
2. `ProcessInput()` → use ReadOnlySpan<byte>
3. `PrepareOutput()` → use Span<byte>
4. `SendData()` → `SendDataAsync()`

**Example Refactor**:
```csharp
// Current
public bool Connect(string host, int port)
{
    tcpClient = new TcpClient(host, port);
    stream = tcpClient.GetStream();
    return true;
}

// Target
public async Task<bool> ConnectAsync(string host, int port,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(host);

    tcpClient = new TcpClient();
    await tcpClient.ConnectAsync(host, port, cancellationToken);
    stream = tcpClient.GetStream();
    return true;
}
```

**Estimated Time**: 10-12 hours
**Testing Required**: Protocol compliance tests

---

#### 4. `src/Program.cs`
**Lines**: ~324
**Complexity**: Medium (main loop)
**Changes**:
- Async main method
- Update terminal control usage
- CancellationToken support
- Modern exception handling

**Current**:
```csharp
static void Main(string[] args)
{
    RunApplication(host, port);
}
```

**Target**:
```csharp
static async Task<int> Main(string[] args)
{
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (s, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    return await RunApplicationAsync(host, port, cts.Token);
}
```

**Estimated Time**: 4-6 hours

---

### Medium Priority Files

#### 5. `src/Telnet/TelnetProtocol.cs`
**Lines**: ~100
**Complexity**: Low (constants)
**Changes**:
- File-scoped namespace
- Possibly convert to `static class`
- Add XML documentation

**Estimated Time**: 1 hour

---

#### 6. `src/Telnet/TelnetState.cs`
**Lines**: ~30
**Complexity**: Low (enum)
**Changes**:
- File-scoped namespace
- No other changes needed

**Estimated Time**: 15 minutes

---

#### 7. `src/Logging/SessionLogger.cs`
**Lines**: ~203
**Complexity**: Medium
**Changes**:
- Async file I/O
- Use `FileStream` with async methods
- Nullable annotations
- Consider `IAsyncDisposable`

**Estimated Time**: 3-4 hours

---

#### 8. `src/Logging/HexDumper.cs`
**Lines**: ~135
**Complexity**: Low
**Changes**:
- Use Span<byte> for processing
- File-scoped namespace
- Minor optimizations

**Estimated Time**: 2 hours

---

#### 9. `src/Interactive/ConsoleMode.cs`
**Lines**: ~198
**Complexity**: Low
**Changes**:
- File-scoped namespace
- Nullable annotations
- Record types for state

**Estimated Time**: 2 hours

---

#### 10. `src/Interactive/CommandProcessor.cs`
**Lines**: ~370
**Complexity**: Medium
**Changes**:
- Async command execution
- Pattern matching improvements
- Nullable annotations

**Estimated Time**: 4-5 hours

---

## Risk Assessment

### High Risk Areas

1. **Terminal Control (TerminalControl.cs)**
   - **Risk**: P/Invoke signatures may differ between Mono and .NET
   - **Mitigation**: Extensive testing, reference Linux man pages
   - **Fallback**: Keep Mono version in separate branch

2. **Signal Handling**
   - **Risk**: PosixSignalRegistration behavior differences
   - **Mitigation**: Test SIGWINCH, SIGINT, SIGTERM thoroughly
   - **Fallback**: Alternative signal handling approach

3. **NativeAOT Compatibility**
   - **Risk**: Not all code may be AOT-compatible
   - **Mitigation**: Test early, use rd.xml for preservation
   - **Fallback**: Self-contained deployment without AOT

### Medium Risk Areas

1. **Async/Await Conversion**
   - **Risk**: State machine complexity, deadlocks
   - **Mitigation**: Careful review, use ConfigureAwait(false)

2. **Performance Regressions**
   - **Risk**: Async overhead may impact latency
   - **Mitigation**: Benchmark before/after, profile hot paths

### Low Risk Areas

1. **Namespace and syntax updates** - Purely syntactic
2. **Documentation updates** - No functional impact
3. **Build system changes** - Can revert easily

---

## Success Criteria

### Functional Requirements
- [ ] All 24 automated tests pass
- [ ] Manual testing with 3+ telnet servers successful
- [ ] All RFC compliance maintained (854, 855, etc.)
- [ ] Console mode fully functional
- [ ] Signal handling works correctly
- [ ] Terminal raw mode operates properly

### Performance Requirements
- [ ] Throughput >= 10 MB/s (Mono: ~5 MB/s)
- [ ] Startup time <= 50ms with NativeAOT
- [ ] Memory usage <= 10 MB (Mono: ~15 MB)
- [ ] Zero regressions in latency

### Quality Requirements
- [ ] Zero compiler warnings
- [ ] All nullable reference checks pass
- [ ] Code analysis (dotnet analyze) clean
- [ ] Documentation updated and accurate

---

## Timeline Estimate

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Project Infrastructure | 2-3 hours | None |
| Phase 2: Platform API Migration | 8-10 hours | Phase 1 |
| Phase 3: Performance Optimization | 12-15 hours | Phase 2 |
| Phase 4: Code Modernization | 6-8 hours | Phase 2 |
| Phase 5: Advanced Features | 10-12 hours | Phase 3 |
| Phase 6: Build and Deployment | 6-8 hours | Phases 2-5 |
| Phase 7: Testing and Validation | 8-10 hours | All previous |
| Phase 8: Documentation Updates | 4-6 hours | Phase 7 |

**Total Estimated Time**: 56-72 hours (7-9 working days)

---

## Dependencies and Prerequisites

### Required
- [ ] .NET 8.0 SDK (8.0.121 installed ✓)
- [ ] Linux development environment (Ubuntu/Fedora/etc.)
- [ ] Git for version control
- [ ] Access to telnet test servers

### Optional
- [ ] BenchmarkDotNet for performance testing
- [ ] dotMemory for profiling
- [ ] Visual Studio Code with C# DevKit
- [ ] Docker for multi-platform testing

---

## Rollback Plan

1. **Git Strategy**:
   - Create `dotnet8-migration` branch
   - Keep `main` branch on Mono version
   - Tag stable points: `v1.0.0-mono`, `v2.0.0-net8.0`

2. **Phased Merge**:
   - Merge phases individually after testing
   - Can rollback specific changes if issues found

3. **Dual Support** (temporary):
   - Maintain both Mono and .NET 8.0 builds
   - Deprecate Mono version after 3 months

---

## Future Enhancements (Post-Migration)

### Phase 9: Additional Features
- [ ] SSH protocol support (SSH.NET library)
- [ ] WebSocket telnet gateway
- [ ] MQTT telnet bridge
- [ ] Configuration file support (JSON/YAML)

### Phase 10: Advanced Optimizations
- [ ] SIMD acceleration for protocol processing
- [ ] Memory-mapped I/O for large file transfers
- [ ] Custom buffer pooling strategies
- [ ] Zero-allocation hot paths

### Phase 11: GUI Development
- [ ] Terminal.Gui integration
- [ ] Web-based terminal (Blazor)
- [ ] Cross-platform GUI (Avalonia)

---

## References

- [.NET 8.0 Migration Guide](https://learn.microsoft.com/en-us/dotnet/core/porting/)
- [LibraryImport Documentation](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation)
- [PosixSignalRegistration](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.posixsignalregistration)
- [NativeAOT Deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Span<T> and Memory<T>](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/)

---

## Approval and Sign-off

**Prepared by**: Claude Code Assistant
**Review Required**: Project Owner
**Approval Status**: Pending

**Notes**: This is a comprehensive plan covering all aspects of migrating to .NET 8.0. Each phase can be executed independently with proper testing. The migration maintains 100% feature parity while significantly improving performance and modernizing the codebase.

---

**Last Updated**: 2025-10-25
**Version**: 1.0
**Status**: Ready for Review
