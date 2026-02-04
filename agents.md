# Agents Guide

## Solution overview
- Fig is a .NET 10 microservices configuration management solution.
- Projects live under src/ (API, web, client, common, hosting, integrations, tools, tests).
- Examples and documentation live under examples/ and doc/.

## JSON serialization guidance
- Prefer Newtonsoft.Json over System.Text.Json throughout the solution.
- Use shared settings from Fig.Common.NetStandard.Json (e.g., JsonSettings) when applicable to keep compatibility with existing contracts and the .NET Standard client.
- Avoid introducing System.Text.Json usage in new code.

## Notes for changes
- API uses Newtonsoft.Json settings (including TypeNameHandling in controlled scenarios) and should remain consistent with the client and shared contracts.
- For user-provided JSON, use safe JsonSerializerSettings (TypeNameHandling.None or whitelisted binders as needed).
