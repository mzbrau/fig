# Agents Guide

## Solution overview
- Fig is a .NET 10 microservices configuration management solution.
- Projects live under src/ (API, web, client, common, hosting, integrations, tools, tests).
- Examples and documentation live under examples/ and doc/.

## JSON serialization guidance
- Prefer Newtonsoft.Json over System.Text.Json throughout the solution.
- Use shared settings from Fig.Common.NetStandard.Json (e.g., JsonSettings) when applicable to keep compatibility with existing contracts and the .NET Standard client.
- Avoid introducing System.Text.Json usage in new code.
- **TypeNameHandling.Objects is required** for API controllers, `JsonSettings.FigDefault` / `FigHttp`, and Fig.Web HTTP deserialize. Do **not** use `TypeNameHandling.Auto` on shared or HTTP settings — Auto emits `$type` for LINQ iterators assigned to `IEnumerable` properties (e.g. deferred changes) and breaks clients.
- Keep `FigSerializationBinder` allow-listing for TypeNameHandling deserialize paths.

## Notes for changes
- API Newtonsoft settings must stay consistent with the client and shared contracts (`TypeNameHandling.Objects` + binder).
- For user-provided JSON, use safe JsonSerializerSettings (`TypeNameHandling.None` or whitelisted binders as needed).
