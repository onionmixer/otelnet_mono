#!/bin/bash
# Otelnet Mono - Installation Script
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
INSTALL_DIR="/usr/local/bin"
LIB_DIR="/usr/local/lib/otelnet"
DOC_DIR="/usr/local/share/doc/otelnet"
MAN_DIR="/usr/local/share/man/man1"

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
    print_msg "$BLUE" "  Otelnet Mono Installer v${VERSION}"
    print_msg "$BLUE" "=============================================="
    echo ""
}

# Check if running as root
check_root() {
    if [ "$EUID" -ne 0 ]; then
        print_msg "$YELLOW" "Note: This script will need sudo for system-wide installation"
        print_msg "$YELLOW" "You will be prompted for your password"
        echo ""
    fi
}

# Check prerequisites
check_prerequisites() {
    print_msg "$BLUE" "Checking prerequisites..."

    # Check for mono
    if ! command -v mono &> /dev/null; then
        print_msg "$RED" "Error: Mono runtime not found"
        echo ""
        print_msg "$YELLOW" "Please install Mono first:"
        echo "  Ubuntu/Debian: sudo apt-get install mono-complete"
        echo "  Fedora:        sudo dnf install mono-complete"
        echo "  Arch Linux:    sudo pacman -S mono"
        echo ""
        exit 1
    fi

    MONO_VERSION=$(mono --version | head -n 1)
    print_msg "$GREEN" "✓ Found: $MONO_VERSION"

    # Check for mcs
    if ! command -v mcs &> /dev/null; then
        print_msg "$RED" "Error: Mono C# compiler (mcs) not found"
        echo ""
        print_msg "$YELLOW" "Please install mono-complete package"
        exit 1
    fi
    print_msg "$GREEN" "✓ Found: Mono C# compiler"

    # Check for make (optional)
    if command -v make &> /dev/null; then
        print_msg "$GREEN" "✓ Found: make"
    else
        print_msg "$YELLOW" "⚠ make not found (optional - will use direct mcs)"
    fi

    echo ""
}

# Build the project
build_project() {
    print_msg "$BLUE" "Building otelnet..."

    if [ ! -f "Makefile" ]; then
        print_msg "$RED" "Error: Makefile not found"
        print_msg "$RED" "Please run this script from the otelnet_mono directory"
        exit 1
    fi

    # Clean previous builds
    rm -f otelnet.exe otelnet.exe.mdb

    # Build directly with mcs
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

    echo ""
}

# Verify build
verify_build() {
    print_msg "$BLUE" "Verifying build..."

    if [ ! -f "otelnet.exe" ]; then
        print_msg "$RED" "Error: otelnet.exe not found"
        exit 1
    fi

    # Test version
    if mono otelnet.exe --version > /dev/null 2>&1; then
        VERSION_OUTPUT=$(mono otelnet.exe --version)
        print_msg "$GREEN" "✓ $VERSION_OUTPUT"
    else
        print_msg "$RED" "✗ Executable test failed"
        exit 1
    fi

    echo ""
}

# Install files
install_files() {
    print_msg "$BLUE" "Installing files..."

    # Create directories
    sudo mkdir -p "$INSTALL_DIR"
    sudo mkdir -p "$LIB_DIR"
    sudo mkdir -p "$DOC_DIR"

    # Install executable
    print_msg "$YELLOW" "Installing otelnet.exe to $LIB_DIR..."
    sudo cp otelnet.exe "$LIB_DIR/"

    # Create wrapper script
    print_msg "$YELLOW" "Creating wrapper script..."
    sudo tee "$INSTALL_DIR/otelnet" > /dev/null << 'EOF'
#!/bin/bash
# Otelnet Mono wrapper script
exec mono /usr/local/lib/otelnet/otelnet.exe "$@"
EOF

    sudo chmod +x "$INSTALL_DIR/otelnet"
    print_msg "$GREEN" "✓ Installed executable"

    # Install documentation
    print_msg "$YELLOW" "Installing documentation..."
    if [ -d "docs" ]; then
        sudo cp -r docs/* "$DOC_DIR/" 2>/dev/null || true
        print_msg "$GREEN" "✓ Installed documentation to $DOC_DIR"
    fi

    # Install README
    if [ -f "README.md" ]; then
        sudo cp README.md "$DOC_DIR/"
    fi

    echo ""
}

# Create man page (optional)
create_man_page() {
    print_msg "$BLUE" "Creating man page..."

    sudo mkdir -p "$MAN_DIR"

    sudo tee "$MAN_DIR/otelnet.1" > /dev/null << 'EOF'
.TH OTELNET 1 "2025-10-25" "1.0.0-mono" "User Commands"
.SH NAME
otelnet \- Telnet client for Mono runtime
.SH SYNOPSIS
.B otelnet
[\fIOPTIONS\fR] \fIHOST\fR \fIPORT\fR
.SH DESCRIPTION
.B otelnet
is a full-featured telnet client implementation in C# for the Mono runtime.
It provides complete support for the telnet protocol (RFCs 854, 855) with
additional features like console mode, session logging, and statistics tracking.
.SH OPTIONS
.TP
.BR \-h ", " \-\-help
Display help message and exit
.TP
.BR \-v ", " \-\-version
Display version information and exit
.SH ARGUMENTS
.TP
.I HOST
Remote hostname or IP address to connect to
.TP
.I PORT
Remote port number (1-65535)
.SH CONSOLE MODE
Press
.B Ctrl+]
to enter console mode while connected.
.PP
Console commands:
.TP
.B help
Display console commands
.TP
.B stats
Show connection statistics
.TP
.B ls [dir]
List files in directory
.TP
.B pwd
Print working directory
.TP
.B cd <dir>
Change directory
.TP
.B quit
Disconnect and exit
.SH EXAMPLES
.TP
Connect to localhost on port 23:
.B otelnet localhost 23
.TP
Connect to a MUD server:
.B otelnet mud.example.org 4000
.SH FILES
.TP
.I /usr/local/lib/otelnet/otelnet.exe
Main executable
.TP
.I /usr/local/share/doc/otelnet/
Documentation directory
.SH SEE ALSO
.BR telnet (1),
.BR mono (1)
.SH BUGS
Report bugs at: https://github.com/yourusername/otelnet_mono/issues
.SH AUTHOR
Development Team
.SH COPYRIGHT
See LICENSE file for copyright information
EOF

    # Compress man page
    sudo gzip -f "$MAN_DIR/otelnet.1"
    print_msg "$GREEN" "✓ Created man page: man otelnet"
    echo ""
}

# Post-install verification
post_install_check() {
    print_msg "$BLUE" "Verifying installation..."

    # Check if otelnet is in PATH
    if command -v otelnet &> /dev/null; then
        print_msg "$GREEN" "✓ otelnet is in PATH"

        # Test version
        VERSION_CHECK=$(otelnet --version 2>&1)
        print_msg "$GREEN" "✓ $VERSION_CHECK"
    else
        print_msg "$RED" "✗ otelnet not found in PATH"
        print_msg "$YELLOW" "You may need to restart your shell or run: hash -r"
    fi

    echo ""
}

# Print completion message
print_completion() {
    print_msg "$GREEN" "=============================================="
    print_msg "$GREEN" "  Installation Complete!"
    print_msg "$GREEN" "=============================================="
    echo ""
    print_msg "$BLUE" "Quick Start:"
    echo "  1. Run: otelnet --help"
    echo "  2. Connect: otelnet <host> <port>"
    echo "  3. Read docs: cat $DOC_DIR/QUICK_START.md"
    echo "  4. Man page: man otelnet"
    echo ""
    print_msg "$BLUE" "Documentation:"
    echo "  Location: $DOC_DIR"
    echo "  Files:"
    echo "    - QUICK_START.md      (5-minute guide)"
    echo "    - USER_MANUAL.md      (complete reference)"
    echo "    - TROUBLESHOOTING.md  (problem solving)"
    echo "    - USAGE_EXAMPLES.md   (20+ examples)"
    echo ""
    print_msg "$BLUE" "Example:"
    echo "  otelnet localhost 23"
    echo ""
    print_msg "$YELLOW" "Press Ctrl+] for console mode when connected"
    echo ""
}

# Uninstall option
show_uninstall_info() {
    print_msg "$BLUE" "To uninstall later, run:"
    echo "  sudo ./uninstall.sh"
    echo "  or manually:"
    echo "    sudo rm $INSTALL_DIR/otelnet"
    echo "    sudo rm -rf $LIB_DIR"
    echo "    sudo rm -rf $DOC_DIR"
    echo "    sudo rm $MAN_DIR/otelnet.1.gz"
    echo ""
}

# Main installation flow
main() {
    print_header
    check_root
    check_prerequisites
    build_project
    verify_build

    print_msg "$YELLOW" "Ready to install to system directories"
    print_msg "$YELLOW" "This will require sudo privileges"
    echo ""
    read -p "Continue with installation? [Y/n] " -n 1 -r
    echo ""

    if [[ ! $REPLY =~ ^[Yy]$ ]] && [[ ! -z $REPLY ]]; then
        print_msg "$YELLOW" "Installation cancelled"
        exit 0
    fi

    install_files
    create_man_page
    post_install_check
    print_completion
    show_uninstall_info
}

# Run main function
main
