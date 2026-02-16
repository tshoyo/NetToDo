# NetToDo Backend

An ASP.NET Core Web API that serves as the backbone for the NetToDo application. It handles user authentication, task management with nesting, and file attachments.

## Tech Stack

- **Framework**: .NET 10.0 ASP.NET Core Web API
- **Database**: SQLite with Entity Framework Core
- **Authentication**: JWT (JSON Web Tokens) with BCrypt password hashing
- **File Management**: Local file storage for attachments
- **API Documentation**: Microsoft.AspNetCore.OpenApi (Swagger/OpenAPI)

## Features

- **JWT Authentication**: Secure user registration and login.
- **Hierarchical Tasks**: Support for nesting using parent-child relationships.
- **Position Tracking**: Position-based sorting for accurate task reordering.
- **Soft Deletion**: Soft delete logic for both tasks and lists with recursive cleanup for subtasks.
- **File Attachments**: Ability to upload and retrieve file attachments for any task.
- **Automatic Seeding**: Seeds initial test data on startup (Database is automatically created).

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Running Locally

1. **Clone the repository** (if you haven't already).
2. **Navigate to the backend directory**:
   ```bash
   cd NetToDo
   ```
3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
4. **Run the application**:
   ```bash
   dotnet run
   ```
   The API will be available at `http://localhost:5226`.

### Running Tests

This project uses xUnit for unit testing and Moq for mocking.

1. **Navigate to the test directory**:
   ```bash
   cd NetToDo.Tests
   ```
2. **Run tests**:
   ```bash
   dotnet test
   ```
3. **Run tests with coverage**:
   ```bash
   dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
   ```

### Docker Deployment

1. **Build the Docker image**:
   ```bash
   docker build -t nettodo-app .
   ```
2. **Run the container**:
   ```bash
   docker run -d -p 8080:8080 --name nettodo nettodo-app
   ```
   The API will be accessible at `http://localhost:8080`.

### Docker Compose (Full Stack)

To run both the frontend and backend together, create a `docker-compose.yml` at the root of the project:

```yaml
version: "3.8"
services:
  backend:
    build: ./NetToDo
    ports:
      - "5226:5226"
    environment:
      - ASPNETCORE_URLS=http://+:5226

  frontend:
    build: ./NetToDo-App
    ports:
      - "3000:3000"
    environment:
      - VITE_API_BASE_URL=http://backend:5226/api
    depends_on:
      - backend
```

Then run:

```bash
docker-compose up --build

### API Documentation

Once running, you can explore the API schema via the OpenAPI endpoint:

- `http://localhost:5226/openapi/v1.json` (Local)
- `http://localhost:8080/openapi/v1.json` (Docker)
```
