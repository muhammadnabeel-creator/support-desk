# NFR Requirements — U1 Questions

Most targets are already fixed (NFR1–NFR10 in requirements, team practices, contracts). Two tech-stack choices remain open for U1.

### Q1. How should the API log?
A. (Suggested) Built-in ASP.NET Core logging (`ILogger`) to the console: Information level for requests and errors, never logging request bodies, secrets or ticket text sent to the AI
B. Serilog with a rolling log file in addition to the console
X. Other (please specify)

[Answer]: A

### Q2. Which Swagger/OpenAPI package?
A. (Suggested) Swashbuckle (`Swashbuckle.AspNetCore`), which the .NET 8 Web API template already uses, with enums shown as names and XML comments on endpoints
B. NSwag
X. Other (please specify)

[Answer]: A
