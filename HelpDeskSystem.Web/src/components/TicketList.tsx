import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Chip,
  Button,
  Grid,
  TextField,
  MenuItem,
  Pagination,
  CircularProgress,
  Alert
} from '@mui/material';
import { format } from 'date-fns';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';

interface Ticket {
  id: number;
  ticketNumber: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  category: string;
  createdAt: string;
  updatedAt: string;
}

const TicketList: React.FC = () => {
  const [tickets, setTickets] = useState<Ticket[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const navigate = useNavigate();

  React.useEffect(() => {
    fetchTickets();
  }, [page, statusFilter, searchTerm]);

  const fetchTickets = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: page.toString(),
        ...(statusFilter && { status: statusFilter }),
        ...(searchTerm && { search: searchTerm })
      });
      
      const response = await axios.get(`/api/tickets?${params}`);
      setTickets(response.data.data || response.data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to fetch tickets');
    } finally {
      setLoading(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'open': return 'error';
      case 'in_progress': return 'warning';
      case 'resolved': return 'success';
      case 'closed': return 'default';
      default: return 'default';
    }
  };

  const getPriorityColor = (priority: string) => {
    switch (priority.toLowerCase()) {
      case 'critical': return 'error';
      case 'high': return 'warning';
      case 'medium': return 'info';
      case 'low': return 'success';
      default: return 'default';
    }
  };

  if (loading && tickets.length === 0) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          My Tickets
        </Typography>
        <Button
          variant="contained"
          onClick={() => navigate('/tickets/create')}
        >
          Create New Ticket
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Grid container spacing={2} mb={3}>
        <Grid item xs={12} md={6}>
          <TextField
            fullWidth
            label="Search tickets..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </Grid>
        <Grid item xs={12} md={3}>
          <TextField
            fullWidth
            select
            label="Status"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <MenuItem value="">All Statuses</MenuItem>
            <MenuItem value="Open">Open</MenuItem>
            <MenuItem value="In Progress">In Progress</MenuItem>
            <MenuItem value="Resolved">Resolved</MenuItem>
            <MenuItem value="Closed">Closed</MenuItem>
          </TextField>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        {tickets.map((ticket) => (
          <Grid item xs={12} md={6} lg={4} key={ticket.id}>
            <Card
              sx={{
                cursor: 'pointer',
                '&:hover': {
                  elevation: 4,
                  transform: 'translateY(-2px)',
                  transition: 'all 0.2s ease-in-out'
                }
              }}
              onClick={() => navigate(`/tickets/${ticket.id}`)}
            >
              <CardContent>
                <Box display="flex" justifyContent="space-between" alignItems="flex-start" mb={2}>
                  <Typography variant="h6" component="h2" noWrap>
                    {ticket.ticketNumber}
                  </Typography>
                  <Box>
                    <Chip
                      label={ticket.status}
                      color={getStatusColor(ticket.status) as any}
                      size="small"
                      sx={{ mr: 1 }}
                    />
                    <Chip
                      label={ticket.priority}
                      color={getPriorityColor(ticket.priority) as any}
                      size="small"
                    />
                  </Box>
                </Box>
                
                <Typography variant="body1" gutterBottom>
                  {ticket.title}
                </Typography>
                
                <Typography variant="body2" color="text.secondary" noWrap>
                  {ticket.description}
                </Typography>
                
                <Box display="flex" justifyContent="space-between" alignItems="center" mt={2}>
                  <Typography variant="caption" color="text.secondary">
                    Category: {ticket.category}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {format(new Date(ticket.createdAt), 'MMM dd, yyyy')}
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      {tickets.length === 0 && !loading && (
        <Box textAlign="center" py={4}>
          <Typography variant="h6" color="text.secondary">
            No tickets found
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
            Create your first ticket to get started
          </Typography>
        </Box>
      )}

      <Box display="flex" justifyContent="center" mt={4}>
        <Pagination
          count={10}
          page={page}
          onChange={(e, value) => setPage(value)}
          color="primary"
        />
      </Box>
    </Box>
  );
};

export default TicketList;
