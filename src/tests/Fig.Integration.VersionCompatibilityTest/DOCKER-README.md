# Additional Docker/Testcontainers requirements for Version Compatibility Tests

## 1. Docker Image for Fig.API 1.2.0

- A `Dockerfile.figapi-1.2.0` is provided in this directory. It builds a Docker image for the Fig.API backend (example: `Fig.Examples.AspNetApi`) as it existed at version 1.2.0.
- Use the provided `build-figapi-1.2.0.sh` script to build the image:

```sh
./build-figapi-1.2.0.sh
```

This will create a local Docker image tagged `figapi:1.2.0`.

## 2. Testcontainers Usage

- The integration tests will automatically start a container from the `figapi:1.2.0` image on port 5000 for the compatibility tests.
- Ensure Docker is running and the image is built before running the tests.

## 3. Environment Variables (Optional)

- If you need to override the API URL or port, you can add environment variable support in the test setup.

## 4. Troubleshooting

- If the container fails to start, check the Docker build output and ensure the published app exposes the correct port (5000).
- The Dockerfile assumes the solution and project structure as in this repository. Adjust paths if your structure changes.

---

You are now ready to run the version compatibility tests with Docker and Testcontainers.
