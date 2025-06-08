# Fig.Integration.VersionCompatibilityTest

This project tests compatibility between different versions of the Fig.Client NuGet package and the Fig.Api REST API.

## Test Scenarios

1. **Old Client vs Latest API**
   - Uses Fig.Client v1.2.0 (from NuGet) against the latest Fig.Api backend.
   - Ensures backward compatibility for settings registration and update scenarios.

2. **Latest Client vs Old API**
   - Uses the latest Fig.Client project against a containerized Fig.Api v1.2.0 (using Testcontainers).
   - Ensures forward compatibility for the same scenarios.

## Running the Tests

- The tests use NUnit and can be run with your preferred test runner.
- For the old API scenario, ensure a Docker image for Fig.Api v1.2.0 is available (see Dockerfile or image build instructions).
- The test project uses conditional references to switch between the released NuGet package and the latest client code.

## Best Practices

- Tests follow SOLID, YAGNI, and DRY principles.
- Test logic is shared where possible and setup/teardown is handled via NUnit attributes.

## Structure

- `VersionCompatibilityTests.cs`: Contains the main test classes for both scenarios.
- `GlobalUsings.cs`: Global setup/teardown for the test suite.

---

For more details, see the existing integration test projects and reuse their patterns for backend setup and assertions.
