#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base

RUN apt-get update \
&& apt-get install -y --allow-unauthenticated \
    libc6-dev \
    libgdiplus \
    libx11-dev \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 80	

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["BoldReportsCore/BoldReportsCore.csproj", "BoldReportsCore/"]
RUN dotnet restore "BoldReportsCore/BoldReportsCore.csproj"
COPY . .
WORKDIR "/src/BoldReportsCore"
RUN dotnet build "BoldReportsCore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BoldReportsCore.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BoldReportsCore.dll"]