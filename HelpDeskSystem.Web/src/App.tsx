import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';
import Box from '@mui/material/Box';
import CircularProgress from '@mui/material/CircularProgress';
import { QueryClient, QueryClientProvider } from 'react-query';
import Login from './components/Login';
import Landing from './components/Landing';
import Dashboard from './components/Dashboard';
import AdminDashboard from './components/AdminDashboard';
import EnterpriseAdmin from './components/EnterpriseAdmin';
import TicketList from './components/TicketList';
import TicketDetail from './components/TicketDetail';
import CreateTicket from './components/CreateTicket';
import Profile from './components/Profile';
import Settings from './components/Settings';
import KnowledgeBase from './components/KnowledgeBase';
import CustomerPortal from './components/CustomerPortal';
import Layout from './components/Layout';
import { AuthProvider, useAuth } from './hooks/useAuth';

const theme = createTheme({
  palette: {
    primary: {
      main: '#0f766e',
    },
    secondary: {
      main: '#0f172a',
    },
    background: {
      default: '#f4f7fb',
      paper: '#ffffff',
    },
  },
  shape: {
    borderRadius: 14,
  },
  typography: {
    fontFamily: '"IBM Plex Sans", "Segoe UI", sans-serif',
    h1: { fontFamily: '"Space Grotesk", sans-serif', fontWeight: 700 },
    h2: { fontFamily: '"Space Grotesk", sans-serif', fontWeight: 700 },
    h3: { fontFamily: '"Space Grotesk", sans-serif', fontWeight: 700 },
    h4: { fontFamily: '"Space Grotesk", sans-serif', fontWeight: 700 },
    h5: { fontFamily: '"Space Grotesk", sans-serif', fontWeight: 700 },
    h6: { fontFamily: '"Space Grotesk", sans-serif', fontWeight: 700 },
  }
});

const queryClient = new QueryClient();

const ProtectedLayout: React.FC = () => {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return (
      <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return <Layout />;
};

const LoginRoute: React.FC = () => {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return (
      <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <CircularProgress />
      </Box>
    );
  }

  return user ? <Navigate to="/app/dashboard" replace /> : <Login />;
};

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
          <Router>
        <AuthProvider>
            <Routes>
              <Route path="/" element={<Landing />} />
              <Route path="/login" element={<LoginRoute />} />
              <Route path="/app" element={<ProtectedLayout />}>
                <Route index element={<Navigate to="/app/dashboard" replace />} />
                <Route path="dashboard" element={<Dashboard />} />
                <Route path="customer-portal" element={<CustomerPortal />} />
                <Route path="admin" element={<AdminDashboard />} />
                <Route path="enterprise" element={<EnterpriseAdmin />} />
                <Route path="tickets" element={<TicketList />} />
                <Route path="tickets/:id" element={<TicketDetail />} />
                <Route path="tickets/create" element={<CreateTicket />} />
                <Route path="profile" element={<Profile />} />
                <Route path="settings" element={<Settings />} />
                <Route path="knowledge-base" element={<KnowledgeBase />} />
              </Route>
              <Route path="/dashboard" element={<Navigate to="/app/dashboard" replace />} />
              <Route path="/customer-portal" element={<Navigate to="/app/customer-portal" replace />} />
              <Route path="/admin" element={<Navigate to="/app/admin" replace />} />
              <Route path="/enterprise" element={<Navigate to="/app/enterprise" replace />} />
              <Route path="/tickets" element={<Navigate to="/app/tickets" replace />} />
              <Route path="/tickets/create" element={<Navigate to="/app/tickets/create" replace />} />
              <Route path="/knowledge-base" element={<Navigate to="/app/knowledge-base" replace />} />
              <Route path="/profile" element={<Navigate to="/app/profile" replace />} />
              <Route path="/settings" element={<Navigate to="/app/settings" replace />} />
            </Routes>
        </AuthProvider>
          </Router>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
