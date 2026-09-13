# Task Management

A small task management web application built to practice ASP.NET Core backend development.

## Technologies

- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Supabase Auth
- React
- TypeScript
- Vite
- xUnit

## Features

- User registration and login
- JWT authentication
- Create and view tasks
- Update task status
- Delete tasks
- User-specific task ownership
- Request logging with request IDs
- Backend integration tests

## Project Structure

- `TaskManagement.Server` — ASP.NET Core backend
- `TaskManagement.Server.Tests` — backend integration tests
- `taskmanagement.client` — React frontend

## Running the Project

### Backend

The backend requires Supabase and PostgreSQL configuration through .NET User Secrets.

Run the server with:

```bash
dotnet run --project TaskManagement.Server
```

### Frontend

Install dependencies:

```bash
npm install
```

Start the development server:

```bash
npm run dev
```

## Notes

After registration, the user is redirected to the login page and must log in manually.

The backend validates the user's JWT and uses the authenticated user's ID to ensure that tasks can only be accessed and modified by their owner.

## Project Duration

Developed as a short portfolio project over approximately 6 days.
