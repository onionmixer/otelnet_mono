# Otelnet Mono - Version Management

**Version**: 1.0.0-mono
**Last Updated**: 2025-10-25

---

## Table of Contents

1. [Version Numbering Scheme](#version-numbering-scheme)
2. [Current Version](#current-version)
3. [Version Locations](#version-locations)
4. [Updating Version Numbers](#updating-version-numbers)
5. [Release Process](#release-process)
6. [Version History](#version-history)

---

## Version Numbering Scheme

Otelnet Mono uses **Semantic Versioning** with a Mono suffix.

### Format

```
MAJOR.MINOR.PATCH-mono
```

### Components

- **MAJOR**: Incremented for incompatible API changes
- **MINOR**: Incremented for new functionality (backwards-compatible)
- **PATCH**: Incremented for backwards-compatible bug fixes
- **-mono**: Suffix to distinguish from original C version

### Examples

- `1.0.0-mono` - First stable release
- `1.1.0-mono` - Added file transfer support
- `1.1.1-mono` - Bug fix release
- `2.0.0-mono` - Major rewrite with breaking changes

---

## Current Version

**Current Release**: **1.0.0-mono**

**Release Date**: 2025-10-25
**Status**: Stable
**Codename**: Initial Release

---

## Version Locations

The version number appears in multiple locations throughout the project. When updating the version, **all** of these must be changed:

### 1. Source Code

**File**: `src/Program.cs`
**Line**: ~15
**Format**: `private const string VERSION = "1.0.0-mono";`

```csharp
public class Program
{
    private const string VERSION = "1.0.0-mono";  // ← UPDATE HERE
```

### 2. Project File

**File**: `OtelnetMono.csproj`
**Location**: `<PropertyGroup>` section
**Format**: XML elements

```xml
<PropertyGroup>
    <AssemblyName>otelnet</AssemblyName>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>        <!-- UPDATE -->
    <FileVersion>1.0.0.0</FileVersion>                <!-- UPDATE -->
    <InformationalVersion>1.0.0-mono</InformationalVersion>  <!-- UPDATE -->
</PropertyGroup>
```

### 3. Installation Scripts

**Files**:
- `install.sh` (line ~10)
- `uninstall.sh` (line ~10)
- `make-package.sh` (line ~10)

**Format**: `VERSION="1.0.0-mono"`

```bash
# Configuration
VERSION="1.0.0-mono"  # ← UPDATE HERE
```

### 4. Documentation

**Files** to update:
- `README.md` (bottom section)
- `RELEASE_NOTES.md` (version headings)
- `docs/QUICK_START.md` (footer)
- `docs/USER_MANUAL.md` (header and footer)
- `docs/TROUBLESHOOTING.md` (footer)
- `docs/USAGE_EXAMPLES.md` (footer)
- `docs/PROJECT_STATUS.md` (header)
- `docs/PROJECT_COMPLETE.md` (header)

**Format**: Various (see individual files)

### 5. Man Page

**File**: Created by `install.sh`
**Location**: Generated during installation
**Format**: `.TH OTELNET 1 "2025-10-25" "1.0.0-mono" "User Commands"`

---

## Updating Version Numbers

### Automated Update (Recommended)

Create a script to update all version locations:

**File**: `update-version.sh`

```bash
#!/bin/bash
# Update version number in all files

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <new-version>"
    echo "Example: $0 1.1.0-mono"
    exit 1
fi

NEW_VERSION="$1"
NEW_VERSION_NUMERIC="${NEW_VERSION%-mono}"

echo "Updating version to $NEW_VERSION..."

# Update Program.cs
sed -i "s/VERSION = \".*\"/VERSION = \"$NEW_VERSION\"/" src/Program.cs

# Update scripts
sed -i "s/VERSION=\".*\"/VERSION=\"$NEW_VERSION\"/" install.sh
sed -i "s/VERSION=\".*\"/VERSION=\"$NEW_VERSION\"/" uninstall.sh
sed -i "s/VERSION=\".*\"/VERSION=\"$NEW_VERSION\"/" make-package.sh

# Update documentation
find docs -name "*.md" -exec sed -i "s/\*\*Version\*\*:.*/**Version**: $NEW_VERSION/" {} \;

echo "Version updated to $NEW_VERSION"
echo "Please manually update:"
echo "  - OtelnetMono.csproj"
echo "  - RELEASE_NOTES.md (add new section)"
echo "  - README.md (verify)"
```

### Manual Update

For each file listed in [Version Locations](#version-locations):

1. Open the file
2. Find the version string
3. Replace with new version
4. Save the file

### Verification

After updating, verify all locations:

```bash
# Check Program.cs
grep "VERSION = " src/Program.cs

# Check scripts
grep "VERSION=" *.sh

# Check documentation
grep -r "Version.*1.0.0-mono" docs/

# Build and test
make clean
make build
mono bin/otelnet.exe --version
```

Expected output:
```
otelnet version 1.0.0-mono
```

---

## Release Process

### Pre-Release Checklist

Before releasing a new version:

1. **Update Version Numbers**
   - [ ] Update all version locations (see above)
   - [ ] Verify version with `mono bin/otelnet.exe --version`

2. **Update Documentation**
   - [ ] Add entry to RELEASE_NOTES.md
   - [ ] Update README.md if needed
   - [ ] Update changelog section

3. **Testing**
   - [ ] Run automated tests: `bash scripts/run_integration_tests.sh`
   - [ ] Verify all 24 tests pass
   - [ ] Manual testing with test servers
   - [ ] Test installation script

4. **Build Verification**
   - [ ] Clean build: `make clean && make build`
   - [ ] No compilation errors
   - [ ] No new warnings

5. **Documentation Review**
   - [ ] All docs updated
   - [ ] Version numbers consistent
   - [ ] Examples still valid

### Release Steps

1. **Tag the Release**

```bash
git tag -a v1.0.0-mono -m "Release version 1.0.0-mono"
git push origin v1.0.0-mono
```

2. **Create Packages**

```bash
./make-package.sh
```

This creates:
- Source package: `dist/otelnet-mono-1.0.0.tar.gz`
- Binary package: `dist/otelnet-mono-1.0.0-bin.tar.gz`
- Checksums: `dist/SHA256SUMS`, `dist/MD5SUMS`
- Manifest: `dist/MANIFEST.txt`

3. **Test Packages**

```bash
# Extract source package
cd /tmp
tar xzf /path/to/dist/otelnet-mono-1.0.0.tar.gz
cd otelnet-mono-1.0.0
./install.sh

# Test installation
otelnet --version
otelnet localhost 23  # (if server available)

# Uninstall
sudo ./uninstall.sh
```

4. **Publish Release**

- Upload packages to release location
- Update website/repository
- Announce release

### Post-Release

1. **Verify Release**
   - [ ] Packages accessible
   - [ ] Installation works
   - [ ] Documentation correct

2. **Update Development Branch**
   - [ ] Increment version for next development cycle
   - [ ] Example: 1.0.0-mono → 1.1.0-dev-mono

3. **Announce**
   - [ ] Release notes published
   - [ ] Users notified
   - [ ] Documentation updated

---

## Version History

### 1.0.0-mono (2025-10-25)

**Status**: Current Stable Release

**Features**:
- Complete telnet protocol (RFC 854, 855, 856, 858)
- Advanced options (NAWS, TTYPE, TSPEED, ENVIRON, LINEMODE)
- Interactive console mode
- Session logging and statistics
- Comprehensive documentation
- Automated installation

**Testing**:
- 24/24 automated tests passed
- Manual testing with multiple servers

**Known Issues**:
- None

**Breaking Changes**:
- Initial release - no compatibility concerns

---

## Planned Versions

### 1.1.0-mono (Planned)

**Target**: Q2 2025

**Features**:
- File transfer integration (ZMODEM, Kermit)
- Configuration file support
- Additional LINEMODE features (SLC, FORWARDMASK)

**Backwards Compatibility**: YES

### 1.2.0-mono (Planned)

**Target**: Q3 2025

**Features**:
- Scripting support
- Connection profiles
- Session recording/playback

**Backwards Compatibility**: YES

### 2.0.0-mono (Future)

**Target**: TBD

**Features**:
- Complete rewrite if needed
- Major architecture changes
- Plugin system

**Backwards Compatibility**: MAY BREAK

---

## Version Branching Strategy

### Main Branches

- **main**: Stable releases only
- **develop**: Development branch
- **release/X.Y.Z**: Release preparation branches

### Version Tags

All releases are tagged: `vX.Y.Z-mono`

Example: `v1.0.0-mono`

### Branch Workflow

1. **Development**: Work on `develop` branch
2. **Feature**: Create `feature/name` branches from `develop`
3. **Release**: Create `release/1.0.0` from `develop`
4. **Hotfix**: Create `hotfix/1.0.1` from `main`

---

## Compatibility Matrix

### Mono Runtime

| Otelnet Version | Minimum Mono | Recommended Mono |
|----------------|--------------|------------------|
| 1.0.0-mono | 6.8.0 | 6.12.0+ |
| 1.1.0-mono | 6.8.0 | 6.12.0+ |

### .NET Framework

| Otelnet Version | .NET Framework |
|----------------|----------------|
| 1.0.0-mono | 4.5+ |
| 1.1.0-mono | 4.5+ |

### Platform Support

| Platform | 1.0.0-mono | 1.1.0-mono |
|----------|------------|------------|
| Linux | ✅ Supported | ✅ Supported |
| macOS | ⚠️ Untested | ✅ Supported |
| Windows | ❌ Unsupported | ⚠️ Experimental |

---

## Support Policy

### Support Timeline

- **Current Release**: Full support (bug fixes, security updates)
- **Previous Release**: Security updates only (6 months)
- **Older Releases**: No support

### Support Status

| Version | Release Date | Support Status | End of Support |
|---------|--------------|----------------|----------------|
| 1.0.0-mono | 2025-10-25 | Current | - |

---

## Deprecation Policy

### Deprecation Process

1. **Announcement**: Feature marked deprecated in release notes
2. **Warning Period**: 2 minor versions (e.g., 1.1.0 → 1.3.0)
3. **Removal**: In next major version (e.g., 2.0.0)

### Currently Deprecated

None (initial release)

---

## References

- [Semantic Versioning](https://semver.org/)
- [Version Management Best Practices](https://semver.org/)

---

**Prepared by**: Development Team
**Last Updated**: 2025-10-25
**Next Review**: 2026-01-25
