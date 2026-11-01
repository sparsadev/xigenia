# Xigenia API image. Build from the repository root so sibling
# project references (Application, Infrastructure, Domain) resolve:
#   docker build -f src/Xigenia.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Xigenia.Api/Xigenia.Api.csproj src/Xigenia.Api/
COPY src/Xigenia.Application/Xigenia.Application.csproj src/Xigenia.Application/
COPY src/Xigenia.Infrastructure/Xigenia.Infrastructure.csproj src/Xigenia.Infrastructure/
COPY src/Xigenia.Domain/Xigenia.Domain.csproj src/Xigenia.Domain/
COPY src/Xigenia.Shared/Xigenia.Shared.csproj src/Xigenia.Shared/
RUN dotnet restore src/Xigenia.Api/Xigenia.Api.csproj
COPY src/ src/
RUN dotnet publish src/Xigenia.Api/Xigenia.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Xigenia.Api.dll"]
