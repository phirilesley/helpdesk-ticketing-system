import React from 'react';
import {
  Grid,
  Card,
  CardContent,
  Typography,
  Box,
  Paper,
  List,
  ListItem,
  ListItemText,
  Chip,
  Button,
} from '@mui/material';
import { useQuery } from 'react-query';
import { format } from 'date-fns';
import { Link } from 'react-router-dom';
import { getDashboardStats, getRecentTickets } from '../services/api';

interface DashboardStats {
  totalTickets: number;
  openTickets: number;
  resolvedTickets: number;
  averageResolutionTime: number;
}

interface Ticket {
  id: number;
  ticketNumber: string;
  title: string;
  status: string;
  priority: string;
  createdAt: string;
}

const Dashboard: React.FC = () => {
  const { data: stats, isLoading: statsLoading } = useQuery<DashboardStats>(
    'dashboard-stats',
    getDashboardStats
  );

  const { data: tickets, isLoading: ticketsLoading } = useQuery<Ticket[]>(
    'recent-tickets',
    getRecentTickets
  );

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'New': return 'info';
      case 'In Progress': return 'warning';
      case 'Resolved': return 'success';
      case 'Closed': return 'default';
      default: return 'default';
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'High': return 'error';
      case 'Medium': return 'warning';
      case 'Low': return 'success';
      default: return 'default';
    }
  };

  return (
    <Box sx={{ flexGrow: 1, p: 3 }}>
      <Typography variant="h4" gutterBottom>
        Customer Dashboard
      </Typography>

      <Grid container spacing={3}>
        {/* Stats Cards */}
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Total Tickets
              </Typography>
              <Typography variant="h4">
                {statsLoading ? '...' : stats?.totalTickets || 0}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Open Tickets
              </Typography>
              <Typography variant="h4" color="warning.main">
                {statsLoading ? '...' : stats?.openTickets || 0}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Resolved Tickets
              </Typography>
              <Typography variant="h4" color="success.main">
                {statsLoading ? '...' : stats?.resolvedTickets || 0}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Typography color="textSecondary" gutterBottom>
                Avg Resolution Time
              </Typography>
              <Typography variant="h4">
                {statsLoading ? '...' : `${Math.round(stats?.averageResolutionTime || 0)}h`}
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        {/* Recent Tickets */}
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 2 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
              <Typography variant="h6">Recent Tickets</Typography>
              <Button component={Link} to="/tickets/create" variant="contained">
                Create New Ticket
              </Button>
            </Box>
            
            <List>
              {ticketsLoading ? (
                <ListItem>Loading...</ListItem>
              ) : tickets?.length === 0 ? (
                <ListItem>No tickets found</ListItem>
              ) : (
                tickets?.map((ticket) => (
                  <ListItem
                    key={ticket.id}
                    component={Link}
                    to={`/tickets/${ticket.id}`}
                    sx={{ textDecoration: 'none', color: 'inherit' }}
                  >
                    <ListItemText
                      primary={
                        <Box display="flex" alignItems="center" gap={1}>
                          <Typography variant="subtitle1">
                            {ticket.ticketNumber} - {ticket.title}
                          </Typography>
                          <Chip
                            label={ticket.status}
                            color={getStatusColor(ticket.status) as any}
                            size="small"
                          />
                          <Chip
                            label={ticket.priority}
                            color={getPriorityColor(ticket.priority) as any}
                            size="small"
                          />
                        </Box>
                      }
                      secondary={format(new Date(ticket.createdAt), 'MMM dd, yyyy')}
                    />
                  </ListItem>
                ))
              )}
            </List>
          </Paper>
        </Grid>

        {/* Quick Actions */}
        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Quick Actions
            </Typography>
            <Box display="flex" flexDirection="column" gap={2}>
              <Button
                component={Link}
                to="/tickets/create"
                variant="contained"
                fullWidth
              >
                Create New Ticket
              </Button>
              <Button
                component={Link}
                to="/tickets"
                variant="outlined"
                fullWidth
              >
                View All Tickets
              </Button>
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default Dashboard;
