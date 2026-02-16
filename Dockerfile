# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["NetToDo.csproj", "./"]
RUN dotnet restore "NetToDo.csproj"

# Copy everything and build
COPY . .
RUN dotnet build "NetToDo.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "NetToDo.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create uploads directory
RUN mkdir -p wwwroot/uploads

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "NetToDo.dll"]
