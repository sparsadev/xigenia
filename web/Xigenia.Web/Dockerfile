# Xigenia Web (Blazor WebAssembly) image. Build from the repository root
# so the sibling Xigenia.Shared project reference resolves:
#   docker build -f web/Xigenia.Web/Dockerfile .
# The browser-to-API base URL is baked at build time via API_BASE_URL
# because static nginx hosting cannot read container runtime environment.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY web/Xigenia.Web/Xigenia.Web.csproj web/Xigenia.Web/
COPY src/Xigenia.Shared/Xigenia.Shared.csproj src/Xigenia.Shared/
RUN dotnet restore web/Xigenia.Web/Xigenia.Web.csproj
COPY web/ web/
COPY src/Xigenia.Shared/ src/Xigenia.Shared/
ARG API_BASE_URL=http://localhost:8080
RUN printf '{"ApiBaseUrl":"%s"}' "$API_BASE_URL" > web/Xigenia.Web/wwwroot/appsettings.json
RUN dotnet publish web/Xigenia.Web/Xigenia.Web.csproj -c Release -o /app/publish --no-restore

FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
