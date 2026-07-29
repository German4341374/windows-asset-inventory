DOTNET ?= dotnet
SOLUTION := WindowsAssetInventory.sln
PROJECT := src/WindowsAssetInventory/WindowsAssetInventory.csproj

.PHONY: setup format format-check build test run up down clean

setup:
	$(DOTNET) tool restore
	$(DOTNET) restore $(SOLUTION)

format:
	$(DOTNET) format $(SOLUTION)

format-check:
	$(DOTNET) format $(SOLUTION) --verify-no-changes --no-restore

build:
	$(DOTNET) build $(SOLUTION) --configuration Release --no-restore

test:
	$(DOTNET) test $(SOLUTION) --configuration Release --no-build

run:
	$(DOTNET) run --project $(PROJECT)

up:
	docker compose up --build --detach

down:
	docker compose down

clean:
	$(DOTNET) clean $(SOLUTION)
	docker compose down --volumes --remove-orphans
