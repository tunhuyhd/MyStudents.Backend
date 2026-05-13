# Base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Build image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["MyStudents.WebApi/MyStudents.WebApi.csproj", "MyStudents.WebApi/"]
COPY ["MyStudents.Application/MyStudents.Application.csproj", "MyStudents.Application/"]
COPY ["MyStudents.Domain/MyStudents.Domain.csproj", "MyStudents.Domain/"]
COPY ["MyStudents.Infrastructure/MyStudents.Infrastructure.csproj", "MyStudents.Infrastructure/"]

RUN dotnet restore "MyStudents.WebApi/MyStudents.WebApi.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/MyStudents.WebApi"
RUN dotnet build "MyStudents.WebApi.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "MyStudents.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyStudents.WebApi.dll"]
