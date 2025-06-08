#!/bin/bash
# Build and tag the Fig.API 1.2.0 Docker image for integration testing
set -e

# Set the version tag
FIG_API_VERSION=1.2.0

# Build the Docker image from the provided Dockerfile
DOCKER_BUILDKIT=1 docker build -f Dockerfile.figapi-1.2.0 -t figapi:$FIG_API_VERSION ../../..

echo "Built figapi:$FIG_API_VERSION Docker image."
