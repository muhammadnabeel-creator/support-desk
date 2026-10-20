# NFR Design — U1 Questions

The NFR review flagged that a plain relative SQLite path can resolve to different files depending on where `dotnet ef` or `dotnet run` is started. One decision is needed.

### Q1. Where should the SQLite database file live?
A. (Suggested) Inside the API project at `src/SupportDesk.Api/App_Data/supportdesk.db`, with the path built from the app's content root at startup (so `dotnet run` and `dotnet ef database update` always use the same file, wherever they are started from); `App_Data/*.db` is git-ignored
B. In your user profile's local app-data folder (e.g. `~/.local/share/SupportDesk/supportdesk.db`), outside the repository
X. Other (please specify)

[Answer]: A
