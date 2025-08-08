# Fig - Centralized Settings Management for .NET Microservices

Fig is a .NET 9.0 microservices configuration management solution consisting of an ASP.NET Core API, a Blazor WebAssembly web application, and a .NET Standard 2.0 client library distributed as a NuGet package.

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Prerequisites and Setup
- Install .NET 9.0 SDK: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 9.0.100 --install-dir $HOME/.dotnet`
- Add to PATH: `export PATH="$HOME/.dotnet:$PATH"`
- Verify installation: `dotnet --version` (should show 9.0.100 or later)

### Building and Testing
**CRITICAL TIMING - NEVER CANCEL these commands:**

1. **Package Restore** (first time: 2.5 minutes, subsequent: 4 seconds): 
   ```bash
   cd src
   dotnet restore Fig.sln
   ```
   - **NEVER CANCEL** - First time takes approximately 2.5 minutes, subsequent runs ~4 seconds due to caching. Set timeout to 10+ minutes.
   - Expect security warnings about vulnerable packages (System.Net.Http, System.Text.RegularExpressions) - these are transitive dependencies and expected.

2. **Build Solution** (50 seconds):
   ```bash
   dotnet build Fig.sln --configuration Release --no-restore
   ```
   - **NEVER CANCEL** - Takes approximately 50 seconds. Set timeout to 5+ minutes.

3. **Run Tests**:
   - **Unit Tests** (3 seconds): `dotnet test tests/Fig.Unit.Test/Fig.Unit.Test.csproj --configuration Release --no-build --verbosity minimal`
   - **Integration Tests** (7 minutes): `dotnet test tests/Fig.Integration.Test/Fig.Integration.Test.csproj --configuration Release --no-build --verbosity minimal`
     - **NEVER CANCEL** - Takes approximately 7 minutes. Set timeout to 15+ minutes.
   - **End-to-End Tests**: Currently contain no executable tests (Playwright setup exists but tests not implemented)

### Running Applications

#### API (Fig.Api)
```bash
cd src
dotnet run --project api/Fig.Api/Fig.Api.csproj --configuration Release --no-build
```
- Listens on HTTPS: `https://localhost:7281` and HTTP: `http://localhost:5260`
- Uses SQLite database for development (automatically created)
- Logs indicate successful startup when you see "Application started. Press Ctrl+C to shut down"

#### Web Application (Fig.Web)
```bash
cd src  
dotnet run --project web/Fig.Web/Fig.Web.csproj --configuration Release --no-build
```
- Listens on HTTPS: `https://localhost:7148` and HTTP: `http://localhost:5217`
- Blazor WebAssembly application
- Requires Fig.Api to be running for full functionality

#### Example Console Application
```bash
cd examples/Fig.Examples.ConsoleApp
dotnet run --configuration Release
```
- Demonstrates Fig.Client integration
- Shows how applications connect to Fig API for configuration

## Docker Support
- Docker Compose available: `docker compose up` (requires environment variables for database)
- Individual Dockerfiles in `src/api/Fig.Api/Dockerfile` and `src/web/Fig.Web/Dockerfile`
- Database setup script: `scripts/setup_fig_db.sh`

## Validation Scenarios

**ALWAYS test these scenarios after making changes:**

1. **Basic Build Validation**:
   - Restore packages successfully
   - Build completes without errors
   - Unit tests pass

2. **Application Startup**:
   - API starts and listens on expected ports
   - Web application starts and loads
   - No critical errors in logs

3. **Integration Testing** (for significant changes):
   - Run integration test suite (allow 15+ minutes)
   - Verify all 383 tests pass

4. **Client Integration** (when changing client library):
   - Run console app example: `cd examples/Fig.Examples.ConsoleApp && dotnet run --configuration Release`
   - With API running: `export FIG_API_URI="http://localhost:5260" && dotnet run --configuration Release`
   - Verify settings are loaded from API
   - Test configuration provider functionality

5. **End-to-End Integration Testing**:
   - Start API: `cd src && dotnet run --project api/Fig.Api/Fig.Api.csproj --configuration Release --no-build` (in background)
   - Wait for "Application started" message
   - Start Web: `cd src && dotnet run --project web/Fig.Web/Fig.Web.csproj --configuration Release --no-build` (in new terminal)
   - Test console app with API: `export FIG_API_URI="http://localhost:5260" && cd examples/Fig.Examples.ConsoleApp && dotnet run --configuration Release`
   - Verify no critical errors and proper API communication logs

## Development Workflow

- **Before committing**: Always run `dotnet build Fig.sln --configuration Release` to ensure no build errors
- **For client changes**: Test with example console application
- **For API changes**: Run integration tests to verify database interactions
- **For web changes**: Start both API and web applications to verify integration

## Key Projects Structure

```
src/
├── api/Fig.Api/                    # ASP.NET Core Web API (port 7281/5260)
├── web/Fig.Web/                    # Blazor WebAssembly UI (port 7148/5217)
├── client/Fig.Client/              # NuGet client library (.NET Standard 2.0)
├── common/                         # Shared libraries
├── tests/
│   ├── Fig.Unit.Test/             # Fast unit tests (3 seconds)
│   ├── Fig.Integration.Test/      # Database integration tests (7 minutes)
│   └── Fig.EndToEnd.Tests/        # Playwright tests (no tests implemented)
└── examples/                      # Usage examples

scripts/                           # Database and deployment scripts
docker-compose.yml                 # Container orchestration
```

## Common Commands Quick Reference

- **Full build from scratch**: `cd src && dotnet restore Fig.sln && dotnet build Fig.sln --configuration Release --no-restore` (Total: ~3 minutes first time, ~1 minute subsequent)
- **Test everything**: `cd src && dotnet test Fig.sln --configuration Release --no-build`
- **Start API**: `cd src && dotnet run --project api/Fig.Api/Fig.Api.csproj --configuration Release --no-build`
- **Start Web**: `cd src && dotnet run --project web/Fig.Web/Fig.Web.csproj --configuration Release --no-build`

## Troubleshooting

- **Build fails**: Ensure .NET 9.0 SDK is installed and in PATH
- **Database errors**: API uses SQLite by default (automatically created), SQL Server for production
- **Port conflicts**: Default ports are 7281 (API HTTPS), 5260 (API HTTP), 7148 (Web HTTPS), 5217 (Web HTTP)
- **Package restore timeout**: Allow full 10+ minutes, network-dependent operation
- **Integration test failures**: Often database-related, ensure clean state between runs

## Security Notes

- Development uses self-signed certificates (warnings expected)
- Client secret examples use hardcoded values (not for production)
- SQLite database permissions automatically handled in development