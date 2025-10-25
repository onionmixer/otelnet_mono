# Otelnet Development TODO

**Project**: Otelnet - Telnet Client for .NET 8.0
**Version**: 2.0.0-net8.0
**Status**: Migration Complete, Ongoing Improvements
**Last Updated**: 2025-10-25

---

## Project Overview

Cross-platform telnet client implemented in C# targeting .NET 8.0, featuring full RFC compliance (854/855/856/858/1073/1079/1091/1184/1572) with console mode, file transfer support, and session logging capabilities.

### Current State
- ✅ Successfully migrated from Mono to .NET 8.0
- ✅ All core telnet protocol features implemented
- ✅ Terminal control using native .NET APIs
- ✅ Cross-platform support (Linux, macOS, Windows)
- ✅ NativeAOT ready

---

## Completed Work

### Phase 1: Initial C to C# Migration (Completed)
- ✅ Project structure setup
- ✅ Telnet protocol implementation (RFC 854)
- ✅ Option negotiation (RFC 855)
- ✅ All standard telnet options (BINARY, SGA, ECHO, NAWS, TTYPE, LINEMODE, TSPEED, ENVIRON)
- ✅ Terminal control implementation
- ✅ Console mode with file operations
- ✅ File transfer support (XMODEM/YMODEM/ZMODEM/Kermit)
- ✅ Session logging with hex dump

### Phase 2: Mono to .NET 8.0 Migration (Completed)
- ✅ Converted to SDK-style .csproj targeting net8.0
- ✅ Replaced Mono.Posix with native .NET interop
  - ✅ LibraryImport for P/Invoke (termios, ioctl)
  - ✅ PosixSignalRegistration for signal handling
- ✅ Removed Mono.Unix dependencies
- ✅ File-scoped namespaces
- ✅ Nullable reference types enabled
- ✅ Build system updated (dotnet build)

---

## Current Tasks

### High Priority

#### Testing & Validation
- [ ] Run comprehensive protocol compliance tests
  - [ ] Test with multiple telnet servers (3+ different implementations)
  - [ ] Validate all RFC specifications (854, 855, 856, 858, 1073, 1079, 1091, 1184, 1572)
  - [ ] Verify option negotiation with various server types
  - [ ] Test console mode functionality
  - [ ] Validate file transfer operations

#### Bug Fixes & Stability
- [ ] Review and fix option negotiation edge cases
- [ ] Improve error handling and recovery
- [ ] Fix terminal restoration on abnormal exit
- [ ] Test signal handling (SIGINT, SIGTERM, SIGWINCH)

#### Performance Testing
- [ ] Benchmark throughput (target: >10 MB/s)
- [ ] Measure startup time with NativeAOT
- [ ] Profile memory allocations
- [ ] Compare performance vs Mono baseline

### Medium Priority

#### Code Quality Improvements
- [ ] Add XML documentation comments to all public APIs
- [ ] Implement comprehensive unit tests
- [ ] Add integration test suite
- [ ] Code analysis cleanup (zero warnings)

#### Advanced .NET 8.0 Features
- [ ] Convert to async/await pattern throughout
  - [ ] `TelnetConnection.ConnectAsync()`
  - [ ] Async network I/O with `NetworkStream.ReadAsync/WriteAsync`
  - [ ] `CancellationToken` support
- [ ] Use `Span<T>` and `Memory<T>` for better performance
  - [ ] Replace `byte[]` with `ReadOnlySpan<byte>` in protocol processing
  - [ ] Use `ArrayPool<byte>` for buffer management
  - [ ] Stackalloc for small temporary buffers
- [ ] Improve pattern matching with modern C# syntax
- [ ] Consider record types for data structures

#### NativeAOT Optimization
- [ ] Configure PublishAot settings
- [ ] Test NativeAOT compilation
- [ ] Optimize for size and startup time
- [ ] Handle reflection requirements (if any)
- [ ] Configure trimming properly

### Low Priority

#### Documentation
- [ ] Update README.md with .NET 8.0 requirements
- [ ] Create QUICK_START.md guide
- [ ] Write USER_MANUAL.md
- [ ] Add TROUBLESHOOTING.md
- [ ] Document architecture in docs/ARCHITECTURE.md
- [ ] Create migration notes (Mono → .NET 8.0)

#### Build & Deployment
- [ ] Multi-platform publishing scripts
  - [ ] Linux x64/ARM64
  - [ ] macOS x64/ARM64
  - [ ] Windows x64/ARM64
- [ ] Update installation scripts
- [ ] Create release automation
- [ ] Package for distribution (deb, rpm, etc.)

#### Modern Enhancements (Optional)
- [ ] Integrate `System.CommandLine` for argument parsing
- [ ] Add `Microsoft.Extensions.Logging` support
- [ ] Consider configuration file support (JSON/YAML)
- [ ] Add structured logging option

---

## Future Enhancements

### Phase 3: Advanced Features (Post-v2.0)
- [ ] SSH protocol support
- [ ] WebSocket telnet gateway
- [ ] MQTT telnet bridge
- [ ] TLS/SSL support for secure telnet

### Phase 4: Performance Optimization
- [ ] SIMD acceleration for protocol processing
- [ ] Memory-mapped I/O for large transfers
- [ ] Custom buffer pooling strategies
- [ ] Zero-allocation hot paths

### Phase 5: UI Development
- [ ] Terminal.Gui integration for TUI
- [ ] Web-based terminal (Blazor)
- [ ] Cross-platform GUI (Avalonia/MAUI)

---

## RFC Compliance Checklist

### Fully Implemented ✅
- ✅ **RFC 854**: Telnet Protocol Specification
- ✅ **RFC 855**: Telnet Option Negotiation
- ✅ **RFC 856**: Telnet Binary Transmission
- ✅ **RFC 858**: Telnet Suppress Go Ahead
- ✅ **RFC 1073**: Telnet Window Size (NAWS)
- ✅ **RFC 1079**: Telnet Terminal Speed
- ✅ **RFC 1091**: Telnet Terminal Type (basic)

### Partial Implementation ⚠️
- ⚠️ **RFC 1091**: Terminal-Type (multi-type cycling needs testing)
- ⚠️ **RFC 1184**: Linemode (MODE complete, FORWARDMASK/SLC need validation)

### Needs Testing 🔍
- 🔍 **RFC 1572**: Telnet Environment Option (implementation needs verification)

---

## Known Issues

### High Priority
- [ ] Verify option negotiation doesn't create loops
- [ ] Test terminal raw mode restoration on crash
- [ ] Validate IAC escaping in all contexts

### Medium Priority
- [ ] LINEMODE FORWARDMASK edge cases
- [ ] SLC (Special Line Characters) implementation validation
- [ ] Multi-terminal-type cycling behavior

### Low Priority
- [ ] TCP Urgent/OOB data handling
- [ ] Performance regression testing vs Mono

---

## Development Guidelines

### Code Standards
- Use C# 12 language features
- Enable nullable reference types
- File-scoped namespaces
- XML documentation on public APIs
- Follow .NET naming conventions

### Testing Requirements
- Unit tests for protocol logic
- Integration tests with real servers
- Performance benchmarks
- Cross-platform validation

### Platform Support
- **Primary**: Linux (Ubuntu 22.04+, Fedora 39+)
- **Secondary**: macOS 14+, Windows 11
- **Architecture**: x64, ARM64

---

## Build Information

### Requirements
- .NET 8.0 SDK (8.0.121+)
- Linux/macOS/Windows OS
- Git

### Build Commands
```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Run
dotnet run -- <host> <port>

# Publish (NativeAOT)
dotnet publish -c Release -r linux-x64 -p:PublishAot=true
```

### Optional Dependencies
- sz/rz: ZMODEM file transfer
- sx/sy: XMODEM/YMODEM file transfer
- kermit: Kermit protocol

---

## Timeline

### Immediate (Week 1-2)
- Testing and validation
- Bug fixes
- Documentation updates

### Short-term (Month 1-2)
- Performance optimization
- Async/await migration
- NativeAOT deployment

### Long-term (Quarter 1-2)
- Advanced features
- UI development
- Protocol extensions

---

## References

- [.NET 8.0 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8)
- [LibraryImport](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation)
- [PosixSignalRegistration](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.posixsignalregistration)
- [NativeAOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
- [Span<T> and Memory<T>](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/)

---

**Status**: ✅ Migration Complete | 🚧 Ongoing Improvements | 📋 Planning Future Features
