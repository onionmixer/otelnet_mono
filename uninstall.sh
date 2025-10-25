#!/bin/bash
# Otelnet Mono - Uninstallation Script
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
    print_msg "$BLUE" "  Otelnet Mono Uninstaller v${VERSION}"
    print_msg "$BLUE" "=============================================="
    echo ""
}

# Check if running as root for system-wide uninstall
check_permissions() {
    if [ "$EUID" -ne 0 ]; then
        print_msg "$YELLOW" "Note: This script needs sudo to remove system files"
        print_msg "$YELLOW" "You will be prompted for your password"
        echo ""
    fi
}

# Check what's installed
check_installed() {
    print_msg "$BLUE" "Checking installed files..."
    echo ""

    local found_any=false

    # Check executable
    if [ -f "$INSTALL_DIR/otelnet" ]; then
        print_msg "$GREEN" "✓ Found: $INSTALL_DIR/otelnet"
        found_any=true
    else
        print_msg "$YELLOW" "✗ Not found: $INSTALL_DIR/otelnet"
    fi

    # Check library directory
    if [ -d "$LIB_DIR" ]; then
        print_msg "$GREEN" "✓ Found: $LIB_DIR"
        found_any=true
    else
        print_msg "$YELLOW" "✗ Not found: $LIB_DIR"
    fi

    # Check documentation
    if [ -d "$DOC_DIR" ]; then
        print_msg "$GREEN" "✓ Found: $DOC_DIR"
        found_any=true
    else
        print_msg "$YELLOW" "✗ Not found: $DOC_DIR"
    fi

    # Check man page
    if [ -f "$MAN_DIR/otelnet.1.gz" ]; then
        print_msg "$GREEN" "✓ Found: $MAN_DIR/otelnet.1.gz"
        found_any=true
    else
        print_msg "$YELLOW" "✗ Not found: $MAN_DIR/otelnet.1.gz"
    fi

    echo ""

    if [ "$found_any" = false ]; then
        print_msg "$YELLOW" "No otelnet installation found"
        print_msg "$YELLOW" "Nothing to uninstall"
        exit 0
    fi
}

# Remove files
remove_files() {
    print_msg "$BLUE" "Removing files..."
    echo ""

    local removed_count=0

    # Remove executable
    if [ -f "$INSTALL_DIR/otelnet" ]; then
        print_msg "$YELLOW" "Removing $INSTALL_DIR/otelnet..."
        sudo rm -f "$INSTALL_DIR/otelnet"
        print_msg "$GREEN" "✓ Removed"
        ((removed_count++))
    fi

    # Remove library directory
    if [ -d "$LIB_DIR" ]; then
        print_msg "$YELLOW" "Removing $LIB_DIR..."
        sudo rm -rf "$LIB_DIR"
        print_msg "$GREEN" "✓ Removed"
        ((removed_count++))
    fi

    # Remove documentation
    if [ -d "$DOC_DIR" ]; then
        print_msg "$YELLOW" "Removing $DOC_DIR..."
        sudo rm -rf "$DOC_DIR"
        print_msg "$GREEN" "✓ Removed"
        ((removed_count++))
    fi

    # Remove man page
    if [ -f "$MAN_DIR/otelnet.1.gz" ]; then
        print_msg "$YELLOW" "Removing $MAN_DIR/otelnet.1.gz..."
        sudo rm -f "$MAN_DIR/otelnet.1.gz"
        print_msg "$GREEN" "✓ Removed"
        ((removed_count++))
    fi

    # Update man database
    if command -v mandb &> /dev/null; then
        print_msg "$YELLOW" "Updating man database..."
        sudo mandb -q 2>/dev/null || true
    fi

    echo ""
    print_msg "$GREEN" "Removed $removed_count item(s)"
    echo ""
}

# Verify uninstallation
verify_uninstall() {
    print_msg "$BLUE" "Verifying uninstallation..."
    echo ""

    local all_clean=true

    # Check if otelnet still in PATH
    if command -v otelnet &> /dev/null; then
        print_msg "$RED" "✗ otelnet still found in PATH"
        print_msg "$YELLOW" "  Location: $(which otelnet)"
        all_clean=false
    else
        print_msg "$GREEN" "✓ otelnet not in PATH"
    fi

    # Check directories
    if [ -d "$LIB_DIR" ]; then
        print_msg "$RED" "✗ $LIB_DIR still exists"
        all_clean=false
    else
        print_msg "$GREEN" "✓ Library directory removed"
    fi

    if [ -d "$DOC_DIR" ]; then
        print_msg "$RED" "✗ $DOC_DIR still exists"
        all_clean=false
    else
        print_msg "$GREEN" "✓ Documentation directory removed"
    fi

    echo ""

    if [ "$all_clean" = true ]; then
        print_msg "$GREEN" "All files successfully removed"
    else
        print_msg "$YELLOW" "Some files may remain - check messages above"
    fi

    echo ""
}

# Print completion message
print_completion() {
    print_msg "$GREEN" "=============================================="
    print_msg "$GREEN" "  Uninstallation Complete!"
    print_msg "$GREEN" "=============================================="
    echo ""
    print_msg "$BLUE" "Otelnet has been removed from your system"
    echo ""
    print_msg "$YELLOW" "Note: If you want to reinstall later:"
    echo "  ./install.sh"
    echo ""
    print_msg "$BLUE" "Thank you for using Otelnet Mono!"
    echo ""
}

# Show backup option
show_backup_option() {
    if [ -d "$DOC_DIR" ]; then
        print_msg "$YELLOW" "Note: Documentation will be removed"
        print_msg "$YELLOW" "If you want to keep a copy, cancel and run:"
        echo "  cp -r $DOC_DIR ~/otelnet-docs-backup"
        echo ""
    fi
}

# Main uninstallation flow
main() {
    print_header
    check_permissions
    check_installed
    show_backup_option

    print_msg "$YELLOW" "This will remove otelnet from your system"
    echo ""
    read -p "Continue with uninstallation? [y/N] " -n 1 -r
    echo ""

    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        print_msg "$YELLOW" "Uninstallation cancelled"
        exit 0
    fi

    echo ""
    remove_files
    verify_uninstall
    print_completion
}

# Run main function
main
