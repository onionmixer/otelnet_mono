# Makefile for Otelnet .NET 8.0
# Migrated from Mono to .NET 8.0 Core
# Output: otelnet_mono (native executable) in project root

# Project configuration
PROJECT = Otelnet.csproj
OUTPUT = otelnet_mono

# .NET CLI
DOTNET = dotnet

# Build configurations
CONFIG_DEBUG = Debug
CONFIG_RELEASE = Release

# Targets
.PHONY: all build build-release clean run test help

all: build

# Build for development (Debug)
build:
	@echo "Building Otelnet (.NET 8.0 Debug)..."
	$(DOTNET) build $(PROJECT) -c $(CONFIG_DEBUG)
	@echo "Build complete: $(OUTPUT)"
	@ls -lh $(OUTPUT) 2>/dev/null || echo "Binary created"

# Build for release (Release, optimized)
build-release:
	@echo "Building Otelnet (.NET 8.0 Release)..."
	$(DOTNET) build $(PROJECT) -c $(CONFIG_RELEASE)
	@echo "Build complete: $(OUTPUT)"
	@ls -lh $(OUTPUT)

# Clean build artifacts
clean:
	@echo "Cleaning build artifacts..."
	rm -rf obj
	rm -f $(OUTPUT) $(OUTPUT).dll $(OUTPUT).deps.json $(OUTPUT).runtimeconfig.json *.pdb *.mdb
	@echo "Clean complete"

# Run the application (Debug build)
run: build
	@echo "Running Otelnet..."
	$(DOTNET) run --project $(PROJECT)

# Test build
test: build
	@echo "Testing build..."
	./$(OUTPUT) --version
	./$(OUTPUT) --help

# Help
help:
	@echo "Otelnet .NET 8.0 Build System"
	@echo ""
	@echo "Migration Status: ✓ Mono REMOVED, using .NET 8.0 Core exclusively"
	@echo "Output: otelnet_mono (native executable) in project root directory"
	@echo ""
	@echo "Prerequisites:"
	@echo "  - .NET 8.0 SDK (installed: $$(dotnet --version 2>/dev/null || echo 'NOT FOUND'))"
	@echo ""
	@echo "Targets:"
	@echo "  build           - Build Debug configuration (default)"
	@echo "  build-release   - Build Release configuration"
	@echo "  clean           - Remove build artifacts"
	@echo "  run             - Build and run (Debug)"
	@echo "  test            - Test build"
	@echo "  help            - Show this help message"
	@echo ""
	@echo "Quick Start:"
	@echo "  make build              # Build otelnet_mono"
	@echo "  ./otelnet_mono --version"
	@echo "  ./otelnet_mono localhost 23"
	@echo ""
	@echo "Examples:"
	@echo "  make build && ./otelnet_mono --version"
	@echo "  make test"
