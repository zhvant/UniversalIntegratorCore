#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["UniversalIntegratorCore.csproj", "."]
RUN dotnet restore "./UniversalIntegratorCore.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "UniversalIntegratorCore.csproj" -c Release -o /app/build #--self-contained --runtime linux-64

FROM build AS publish
RUN dotnet publish "UniversalIntegratorCore.csproj" -c Release -o /app/publish #--self-contained --runtime linux-64

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV TZ=Europe/Moscow
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
RUN mkdir downloads
ENTRYPOINT ["dotnet", "UniversalIntegrator.dll"]