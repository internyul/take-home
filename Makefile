# Makefile for take-home repo
# Usage: make <target> [CONFIG=Release] [IMAGE=wallet-web] [PORT=8080]

SOLUTION ?= Betsson.OnlineWallets.sln
DOCKERFILE ?= src/Betsson.OnlineWallets.Web/Dockerfile
IMAGE ?= wallet-web
CONFIG ?= Release
PORT ?= 8080

.PHONY: help restore build test coverage clean docker-test docker-build docker-run

restore: ## dotnet restore solution
	dotnet restore $(SOLUTION)

build: ## dotnet build solution (CONFIG=$(CONFIG))
	dotnet build $(SOLUTION) -c $(CONFIG)

test: ## dotnet test solution (CONFIG=$(CONFIG))
	dotnet test $(SOLUTION) -c $(CONFIG) --verbosity minimal

coverage: ## dotnet test with coverage (outputs to ./TestResults)
	mkdir -p TestResults
	dotnet test $(SOLUTION) -c $(CONFIG) \
		--collect:"XPlat Code Coverage" \
		--logger "trx;LogFileName=test_results.trx" \
		--results-directory ./TestResults \
		--verbosity minimal

clean: ## dotnet clean solution
	dotnet clean $(SOLUTION)

# Docker targets

docker-test: ## Build test stage in Docker (runs tests with coverage in build)
	docker build -f $(DOCKERFILE) --target test -t $(IMAGE)-tests .

docker-build: ## Build production image
	docker build -f $(DOCKERFILE) -t $(IMAGE) .

docker-run: ## Run production image (PORT=$(PORT) -> container:8080)
	docker run --rm -p $(PORT):8080 $(IMAGE)
