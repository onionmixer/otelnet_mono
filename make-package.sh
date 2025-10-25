#!/bin/bash
# Otelnet Mono - Package Creation Script
# Version: 1.0.0-mono
# Date: 2025-10-25

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
VERSION="1.0.0-mono"
PACKAGE_NAME="otelnet-mono"
PACKAGE_VERSION="1.0.0"
BUILD_DIR="build"
DIST_DIR="dist"

# Print colored message
print_msg() {
    local color=$1
    shift
    echo -e "${color}$@${NC}"
}

# Print header
print_header() {
    echo ""
    print_msg "$BLUE" "=============================================="
    print_msg "$BLUE" "  Otelnet Mono Package Creator v${VERSION}"
    print_msg "$BLUE" "=============================================="
    echo ""
}

# Clean build directories
clean_build() {
    print_msg "$BLUE" "Cleaning build directories..."

    if [ -d "$BUILD_DIR" ]; then
        rm -rf "$BUILD_DIR"
        print_msg "$GREEN" "✓ Removed $BUILD_DIR"
    fi

    if [ -d "$DIST_DIR" ]; then
        rm -rf "$DIST_DIR"
        print_msg "$GREEN" "✓ Removed $DIST_DIR"
    fi

    mkdir -p "$BUILD_DIR"
    mkdir -p "$DIST_DIR"
    print_msg "$GREEN" "✓ Created fresh directories"
    echo ""
}

# Build the project
build_project() {
    print_msg "$BLUE" "Building project..."

    # Clean previous builds
    rm -f otelnet.exe otelnet.exe.mdb

    # Build with mcs
    if mcs -debug -r:System.dll -r:Mono.Posix.dll -out:otelnet.exe \
        src/Program.cs \
        src/Telnet/*.cs \
        src/Terminal/*.cs \
        src/Logging/*.cs \
        src/Interactive/*.cs; then
        print_msg "$GREEN" "✓ Build successful"
    else
        print_msg "$RED" "✗ Build failed"
        exit 1
    fi

    # Verify build
    if [ ! -f "otelnet.exe" ]; then
        print_msg "$RED" "✗ Build output not found: otelnet.exe"
        exit 1
    fi

    echo ""
}

# Create source package structure
create_package_structure() {
    print_msg "$BLUE" "Creating package structure..."

    local pkg_dir="$BUILD_DIR/${PACKAGE_NAME}-${PACKAGE_VERSION}"

    mkdir -p "$pkg_dir"

    # Copy files
    print_msg "$YELLOW" "Copying project files..."

    # Binary
    cp otelnet.exe "$pkg_dir/"

    # Source code
    mkdir -p "$pkg_dir/src"
    cp -r src/* "$pkg_dir/src/"

    # Documentation
    mkdir -p "$pkg_dir/docs"
    if [ -d "docs" ]; then
        cp -r docs/* "$pkg_dir/docs/"
    fi
    [ -f "README.md" ] && cp README.md "$pkg_dir/"
    [ -f "LICENSE" ] && cp LICENSE "$pkg_dir/" || touch "$pkg_dir/LICENSE"
    [ -f "RELEASE_NOTES.md" ] && cp RELEASE_NOTES.md "$pkg_dir/"

    # Scripts
    mkdir -p "$pkg_dir/scripts"
    if [ -d "scripts" ]; then
        cp -r scripts/* "$pkg_dir/scripts/"
    fi

    # Build system
    [ -f "OtelnetMono.csproj" ] && cp OtelnetMono.csproj "$pkg_dir/"

    # Installation scripts
    cp install.sh "$pkg_dir/"
    cp uninstall.sh "$pkg_dir/"
    cp make-package.sh "$pkg_dir/"
    chmod +x "$pkg_dir"/*.sh

    print_msg "$GREEN" "✓ Package structure created"
    echo ""

    echo "$pkg_dir"
}

# Create tar.gz package
create_tarball() {
    local pkg_dir=$1
    local pkg_name=$(basename "$pkg_dir")

    print_msg "$BLUE" "Creating tar.gz package..."

    cd "$BUILD_DIR"
    if tar czf "../${DIST_DIR}/${pkg_name}.tar.gz" "$pkg_name"; then
        print_msg "$GREEN" "✓ Created ${pkg_name}.tar.gz"
    else
        print_msg "$RED" "✗ Failed to create tarball"
        cd ..
        exit 1
    fi
    cd ..

    # Get size
    local size=$(du -h "${DIST_DIR}/${pkg_name}.tar.gz" | cut -f1)
    print_msg "$GREEN" "  Size: $size"
    echo ""
}

# Create binary-only package
create_binary_package() {
    print_msg "$BLUE" "Creating binary package..."

    local bin_dir="$BUILD_DIR/${PACKAGE_NAME}-${PACKAGE_VERSION}-bin"

    mkdir -p "$bin_dir"

    # Binary and wrapper
    cp otelnet.exe "$bin_dir/"

    # Create wrapper script
    cat > "$bin_dir/otelnet" << 'EOF'
#!/bin/bash
# Otelnet Mono wrapper script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec mono "$SCRIPT_DIR/otelnet.exe" "$@"
EOF
    chmod +x "$bin_dir/otelnet"

    # Documentation
    mkdir -p "$bin_dir/docs"
    if [ -d "docs" ]; then
        cp docs/QUICK_START.md "$bin_dir/docs/" 2>/dev/null || true
        cp docs/USER_MANUAL.md "$bin_dir/docs/" 2>/dev/null || true
        cp docs/TROUBLESHOOTING.md "$bin_dir/docs/" 2>/dev/null || true
    fi

    [ -f "README.md" ] && cp README.md "$bin_dir/"
    [ -f "LICENSE" ] && cp LICENSE "$bin_dir/" || touch "$bin_dir/LICENSE"
    [ -f "RELEASE_NOTES.md" ] && cp RELEASE_NOTES.md "$bin_dir/"

    # Installation scripts
    cp install.sh "$bin_dir/"
    cp uninstall.sh "$bin_dir/"
    chmod +x "$bin_dir"/*.sh

    # Create README for binary package
    cat > "$bin_dir/README-BINARY.txt" << EOF
Otelnet Mono v${VERSION} - Binary Package

This is a pre-built binary package. To use:

1. Requirements:
   - Mono runtime 6.8.0 or later

2. Quick Start:
   ./bin/otelnet <host> <port>

3. System-Wide Install:
   ./install.sh

4. Documentation:
   See docs/ directory

For full source code, download the source package.
EOF

    # Create tarball
    cd "$BUILD_DIR"
    if tar czf "../${DIST_DIR}/${PACKAGE_NAME}-${PACKAGE_VERSION}-bin.tar.gz" "$(basename "$bin_dir")"; then
        print_msg "$GREEN" "✓ Created binary package"
    else
        print_msg "$RED" "✗ Failed to create binary package"
        cd ..
        exit 1
    fi
    cd ..

    local size=$(du -h "${DIST_DIR}/${PACKAGE_NAME}-${PACKAGE_VERSION}-bin.tar.gz" | cut -f1)
    print_msg "$GREEN" "  Size: $size"
    echo ""
}

# Create checksums
create_checksums() {
    print_msg "$BLUE" "Creating checksums..."

    cd "$DIST_DIR"

    # SHA256
    if command -v sha256sum &> /dev/null; then
        sha256sum *.tar.gz > SHA256SUMS
        print_msg "$GREEN" "✓ Created SHA256SUMS"
    fi

    # MD5
    if command -v md5sum &> /dev/null; then
        md5sum *.tar.gz > MD5SUMS
        print_msg "$GREEN" "✓ Created MD5SUMS"
    fi

    cd ..
    echo ""
}

# Generate package manifest
create_manifest() {
    print_msg "$BLUE" "Creating package manifest..."

    cat > "${DIST_DIR}/MANIFEST.txt" << EOF
Otelnet Mono v${VERSION} - Package Manifest
Generated: $(date)

Packages:
EOF

    cd "$DIST_DIR"
    for file in *.tar.gz; do
        if [ -f "$file" ]; then
            size=$(du -h "$file" | cut -f1)
            echo "  - $file ($size)" >> MANIFEST.txt
        fi
    done

    if [ -f "SHA256SUMS" ]; then
        echo "" >> MANIFEST.txt
        echo "SHA256 Checksums:" >> MANIFEST.txt
        cat SHA256SUMS >> MANIFEST.txt
    fi

    cd ..

    print_msg "$GREEN" "✓ Created MANIFEST.txt"
    echo ""
}

# List created packages
list_packages() {
    print_msg "$GREEN" "=============================================="
    print_msg "$GREEN" "  Packages Created Successfully!"
    print_msg "$GREEN" "=============================================="
    echo ""
    print_msg "$BLUE" "Output directory: $DIST_DIR/"
    echo ""

    cd "$DIST_DIR"
    for file in *; do
        if [ -f "$file" ]; then
            size=$(du -h "$file" | cut -f1)
            print_msg "$YELLOW" "  $file" "$GREEN" "($size)"
        fi
    done
    cd ..

    echo ""
    print_msg "$BLUE" "Package contents:"
    echo ""
    print_msg "$YELLOW" "Source package includes:"
    echo "  - Complete source code"
    echo "  - Build system (Makefile, .csproj)"
    echo "  - Installation scripts"
    echo "  - Documentation"
    echo "  - Test scripts"
    echo ""
    print_msg "$YELLOW" "Binary package includes:"
    echo "  - Pre-built executable"
    echo "  - Wrapper script"
    echo "  - Installation scripts"
    echo "  - User documentation"
    echo ""
}

# Show usage instructions
show_usage() {
    print_msg "$BLUE" "Usage Instructions:"
    echo ""
    print_msg "$YELLOW" "To extract and use source package:"
    echo "  tar xzf ${DIST_DIR}/${PACKAGE_NAME}-${PACKAGE_VERSION}.tar.gz"
    echo "  cd ${PACKAGE_NAME}-${PACKAGE_VERSION}"
    echo "  ./install.sh"
    echo ""
    print_msg "$YELLOW" "To extract and use binary package:"
    echo "  tar xzf ${DIST_DIR}/${PACKAGE_NAME}-${PACKAGE_VERSION}-bin.tar.gz"
    echo "  cd ${PACKAGE_NAME}-${PACKAGE_VERSION}-bin"
    echo "  ./install.sh"
    echo ""
    print_msg "$YELLOW" "To verify checksums:"
    echo "  cd ${DIST_DIR}"
    echo "  sha256sum -c SHA256SUMS"
    echo ""
}

# Main packaging flow
main() {
    print_header

    # Check if we're in the right directory
    if [ ! -f "README.md" ] || [ ! -d "src" ]; then
        print_msg "$RED" "Error: Run this script from the otelnet_mono directory"
        exit 1
    fi

    # Create packages
    clean_build
    build_project

    pkg_dir=$(create_package_structure)
    create_tarball "$pkg_dir"
    create_binary_package
    create_checksums
    create_manifest

    list_packages
    show_usage

    print_msg "$GREEN" "Package creation complete!"
    echo ""
}

# Run main function
main
