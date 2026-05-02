# Help Desk System

A comprehensive enterprise help desk system built with ASP.NET Core and React.

## Features

- ✅ Ticket Management (CRUD)
- ✅ Kanban Board
- 🔄 Real-time Chat (SignalR - ready for implementation)
- 🔄 Workflow Management
- 🔄 SLA Tracking
- 🔄 Reporting

## Architecture

- **HelpDeskSystem.API**: Web API with controllers
- **HelpDeskSystem.Application**: Business logic and services
- **HelpDeskSystem.Domain**: Core entities and business rules
- **HelpDeskSystem.Persistence**: EF Core database layer
- **HelpDeskSystem.Shared**: Common utilities
- **HelpDeskSystem.Web**: React frontend (to be implemented)

## Getting Started

1. Clone the repository
2. Run `dotnet build` in the root
3. Run `dotnet run --project HelpDeskSystem.API`
4. API available at http://localhost:5229/swagger

## Database

Uses SQL Server LocalDB. Migrations are included.

## MVP Scope - ✅ COMPLETED

- ✅ Ticket creation, list, details
- ✅ Assignment and status changes
- ✅ Kanban board
- ✅ Basic messaging
- ✅ Audit trail (status history)

## API Endpoints

- `POST /api/tickets` - Create ticket
- `GET /api/tickets` - List tickets
- `GET /api/tickets/{id}` - Get ticket details
- `PUT /api/tickets/{id}` - Update ticket
- `DELETE /api/tickets/{id}` - Delete ticket
- `POST /api/tickets/{id}/assign` - Assign ticket
- `POST /api/tickets/{id}/status` - Change status
- `GET /api/tickets/kanban` - Kanban view
- `POST /api/tickets/{id}/messages` - Send message
- `GET /api/tickets/{id}/messages` - Get messages