PUBLISH_DIR := src/BinaryLane.Cli/bin/Release/net10.0/linux-musl-x64/publish
INSTALL_DIR := $(HOME)/.local/bin

.PHONY: generate build publish install test clean

generate:
	dotnet run --project src/BinaryLane.Cli.Generator -- openapi.json src/BinaryLane.Cli/Generated

build:
	dotnet build

publish:
	dotnet publish src/BinaryLane.Cli -c Release

install: publish
	mkdir -p $(INSTALL_DIR)
	cp $(PUBLISH_DIR)/bl $(INSTALL_DIR)/blnet

test: install
	dotnet test

clean:
	dotnet clean
	rm -f $(INSTALL_DIR)/blnet
