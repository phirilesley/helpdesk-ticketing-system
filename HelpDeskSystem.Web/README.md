# Help Desk Customer Portal

A modern, responsive React-based customer portal for the Help Desk System. Built with Material-UI, React Router, and TypeScript.

## Features

### Customer Self-Service
- **Dashboard** - Overview of ticket statistics and recent activity
- **Ticket Management** - Create, view, and track support tickets
- **Real-time Updates** - Live status updates and notifications
- **File Attachments** - Upload and download ticket attachments
- **Comments & Communication** - Interactive comment threads

### User Experience
- **Modern UI** - Material Design components with responsive layout
- **Mobile Friendly** - Works seamlessly on desktop and mobile devices
- **Intuitive Navigation** - Easy-to-use sidebar navigation
- **Fast Performance** - Optimized with React Query for data fetching

### Technical Features
- **TypeScript** - Full type safety and better development experience
- **Authentication** - JWT-based authentication with secure token handling
- **API Integration** - RESTful API integration with error handling
- **Form Validation** - React Hook Form for robust form management

## Technology Stack

- **React 18** - Modern React with hooks and concurrent features
- **TypeScript** - Type-safe JavaScript
- **Material-UI (MUI)** - React component library
- **React Router** - Declarative routing
- **React Query** - Data fetching and caching
- **React Hook Form** - Form validation and management
- **Axios** - HTTP client for API requests
- **Date-fns** - Date manipulation utilities

## Getting Started

### Prerequisites
- Node.js 16+ 
- npm or yarn

### Installation

1. Install dependencies:
```bash
npm install
```

2. Start the development server:
```bash
npm start
```

3. Open [http://localhost:3000](http://localhost:3000) in your browser

### Build for Production

```bash
npm run build
```

## Project Structure

```
src/
├── components/          # React components
│   ├── Login.tsx        # Login page
│   ├── Layout.tsx       # Main layout with navigation
│   ├── Dashboard.tsx    # Customer dashboard
│   ├── TicketList.tsx   # List of customer tickets
│   ├── TicketDetail.tsx # Individual ticket view
│   └── CreateTicket.tsx # Create new ticket form
├── hooks/               # Custom React hooks
│   └── useAuth.ts       # Authentication hook
├── services/            # API services
│   └── api.ts           # API client and functions
├── App.tsx              # Main app component
├── index.tsx            # App entry point
└── index.css            # Global styles
```

## API Integration

The portal integrates with the Help Desk System API at `http://localhost:5229` (configured via proxy).

### Key Endpoints
- `POST /api/auth/login` - User authentication
- `GET /api/dashboard/stats` - Dashboard statistics
- `GET /api/tickets` - List tickets
- `POST /api/tickets` - Create new ticket
- `GET /api/tickets/:id` - Get ticket details
- `POST /api/tickets/:id/comments` - Add comment
- `POST /api/attachments/tickets/:id/upload` - Upload attachment

## Authentication

The portal uses JWT-based authentication:
- Login credentials are sent to `/api/auth/login`
- Received JWT token is stored in localStorage
- Token is automatically included in API requests
- Automatic logout on token expiration

## Responsive Design

The portal is fully responsive:
- **Desktop**: Full sidebar navigation with spacious layout
- **Tablet**: Collapsible sidebar with adaptive components
- **Mobile**: Hamburger menu with stacked layout

## Development Notes

### State Management
- React Query for server state
- React Context for authentication state
- Local component state for UI interactions

### Error Handling
- Global error boundaries
- API error handling with user-friendly messages
- Form validation with inline error messages

### Performance Optimizations
- React Query caching for API responses
- Lazy loading of components
- Optimized re-renders with React.memo

## Future Enhancements

- **Real-time Notifications** - WebSocket integration for live updates
- **Knowledge Base** - Self-service documentation and FAQs
- **Ticket Templates** - Pre-defined ticket types
- **Multi-language Support** - Internationalization (i18n)
- **Dark Mode** - Theme switching capability
- **Offline Support** - Service worker for offline functionality

## Contributing

1. Follow the existing code style and conventions
2. Use TypeScript for all new code
3. Add appropriate error handling
4. Test on different screen sizes
5. Document new features

## License

This project is part of the Help Desk System enterprise solution.
