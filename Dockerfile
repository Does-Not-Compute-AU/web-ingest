FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
EXPOSE 80

# Copy csproj and restore as distinct layers
# COPY *.csproj ./
# RUN dotnet restore
WORKDIR /src
COPY web-ingest.sln ./
COPY WebIngest.Common/*.csproj ./WebIngest.Common/
COPY WebIngest.Core/*.csproj ./WebIngest.Core/
COPY WebIngest.WebAPI/*.csproj ./WebIngest.WebAPI/
COPY WebIngest.WebUI/*.csproj ./WebIngest.WebUI/
COPY WebIngest.Tests/*.csproj ./WebIngest.Tests/

RUN dotnet restore
COPY . .

WORKDIR /src/WebIngest.Common
RUN dotnet build -c Release -o /app

WORKDIR /src/WebIngest.Core
RUN dotnet build -c Release -o /app

WORKDIR /src/WebIngest.WebAPI
RUN dotnet build -c Release -o /app

WORKDIR /src/WebIngest.WebUI
RUN dotnet build -c Release -o /app

# publish
FROM build-env AS publish
RUN dotnet publish -c Release -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "WebIngest.WebAPI.dll"]