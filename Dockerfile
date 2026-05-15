# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
# Note: Paths are now relative to the Backend repository root
COPY ["MyStudents.WebApi/MyStudents.WebApi.csproj", "MyStudents.WebApi/"]
COPY ["MyStudents.Application/MyStudents.Application.csproj", "MyStudents.Application/"]
COPY ["MyStudents.Domain/MyStudents.Domain.csproj", "MyStudents.Domain/"]
COPY ["MyStudents.Infrastructure/MyStudents.Infrastructure.csproj", "MyStudents.Infrastructure/"]

RUN dotnet restore "MyStudents.WebApi/MyStudents.WebApi.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/MyStudents.WebApi"
RUN dotnet build "MyStudents.WebApi.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "MyStudents.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Railway uses PORT environment variable
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyStudents.WebApi.dll"]
