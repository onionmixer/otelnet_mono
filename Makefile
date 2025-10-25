# Makefile for otelnet_mono

# Compiler
MCS = mcs

# Output
OUTPUT = bin/otelnet.exe

# Source files
SOURCES = src/Program.cs \
          src/Telnet/TelnetProtocol.cs \
          src/Telnet/TelnetState.cs \
          src/Telnet/TelnetConnection.cs \
          src/Terminal/TerminalControl.cs \
          src/Logging/HexDumper.cs \
          src/Logging/SessionLogger.cs \
          src/Console/ConsoleMode.cs \
          src/Console/CommandProcessor.cs

# References
REFERENCES = -r:System.dll -r:Mono.Posix.dll

# Flags
FLAGS = -debug

# Targets
.PHONY: all build clean run

all: build

build:
	@mkdir -p bin
	$(MCS) $(FLAGS) $(REFERENCES) -out:$(OUTPUT) $(SOURCES)
	@echo "Build complete: $(OUTPUT)"

clean:
	rm -f $(OUTPUT) $(OUTPUT).mdb
	rm -rf bin obj

run: build
	mono $(OUTPUT)

help:
	@echo "Otelnet Mono Build System"
	@echo ""
	@echo "Targets:"
	@echo "  build  - Build the project (default)"
	@echo "  clean  - Remove build artifacts"
	@echo "  run    - Build and run otelnet"
	@echo "  help   - Show this help message"
