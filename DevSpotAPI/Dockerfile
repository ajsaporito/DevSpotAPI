FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DevSpotAPI.csproj .
RUN dotnet restore DevSpotAPI.csproj

COPY . .
RUN dotnet publish DevSpotAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

