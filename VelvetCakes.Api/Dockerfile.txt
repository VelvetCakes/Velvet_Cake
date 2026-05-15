FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VelvetCakes.Api/*.csproj ./VelvetCakes.Api/
WORKDIR /src/VelvetCakes.Api
RUN dotnet restore

COPY VelvetCakes.Api/. .

RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y \
    icu-devtools \
    tzdata \
    libgdiplus \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

RUN mkdir -p /app/wwwroot/uploads

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:8080
ENV TZ=Europe/Moscow

EXPOSE 8080

ENTRYPOINT ["dotnet", "VelvetCakes.Api.dll"]