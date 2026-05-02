import React, { useMemo, useState } from 'react';
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
import { useQuery } from 'react-query';
import { getTickets, Ticket } from '../services/api';

const pageSize = 9;

const TicketList: React.FC = () => {
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const navigate = useNavigate();

  const { data, isLoading, error } = useQuery(['tickets'], () => getTickets({ page: 1 }));
  const tickets = useMemo(() => data?.data ?? [], [data]);

  const filteredTickets = useMemo(() => {
    return tickets.filter((ticket) => {
      const statusMatch = !statusFilter || ticket.status === statusFilter;
      const search = searchTerm.trim().toLowerCase();
      const searchMatch = !search
        || ticket.ticketNumber.toLowerCase().includes(search)
        || ticket.title.toLowerCase().includes(search)
        || ticket.description.toLowerCase().includes(search)
        || ticket.category.toLowerCase().includes(search);

      return statusMatch && searchMatch;
    });
  }, [tickets, statusFilter, searchTerm]);

  const pagedTickets = useMemo(() => {
    const startIndex = (page - 1) * pageSize;
    return filteredTickets.slice(startIndex, startIndex + pageSize);
  }, [filteredTickets, page]);

  const pageCount = Math.max(1, Math.ceil(filteredTickets.length / pageSize));

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'new': return 'info';
      case 'inprogress': return 'warning';
      case 'waiting': return 'secondary';
      case 'resolved': return 'success';
      case 'closed': return 'default';
      case 'escalated': return 'error';
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

  if (isLoading && tickets.length === 0) {
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

      {error && <Alert severity="error" sx={{ mb: 2 }}>Failed to fetch tickets</Alert>}

      <Grid container spacing={2} mb={3}>
        <Grid item xs={12} md={6}>
          <TextField
            fullWidth
            label="Search tickets..."
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value);
              setPage(1);
            }}
          />
        </Grid>
        <Grid item xs={12} md={3}>
          <TextField
            fullWidth
            select
            label="Status"
            value={statusFilter}
            onChange={(e) => {
              setStatusFilter(e.target.value);
              setPage(1);
            }}
          >
            <MenuItem value="">All Statuses</MenuItem>
            <MenuItem value="New">New</MenuItem>
            <MenuItem value="InProgress">In Progress</MenuItem>
            <MenuItem value="Waiting">Waiting</MenuItem>
            <MenuItem value="Resolved">Resolved</MenuItem>
            <MenuItem value="Closed">Closed</MenuItem>
            <MenuItem value="Escalated">Escalated</MenuItem>
          </TextField>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        {pagedTickets.map((ticket: Ticket) => (
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

      {filteredTickets.length === 0 && !isLoading && (
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
          count={pageCount}
          page={Math.min(page, pageCount)}
          onChange={(_, value) => setPage(value)}
          color="primary"
        />
      </Box>
    </Box>
  );
};

export default TicketList;
