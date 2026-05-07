import React, { useEffect, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Chip,
  Button,
  Grid,
  TextField,
  Avatar,
  Divider,
  CircularProgress,
  Alert,
  List,
  ListItem,
  ListItemText,
  ListItemAvatar
} from '@mui/material';
import Autocomplete from '@mui/material/Autocomplete';
import { format } from 'date-fns';
import { useParams, useNavigate } from 'react-router-dom';
import { addComment, assignTicket, changeTicketStatus, getAssignableAgents, getTicket, getTicketComments, Ticket, TicketComment, AssignableUser } from '../services/api';
import { useAuth } from '../hooks/useAuth';

const TicketDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [ticket, setTicket] = useState<Ticket | null>(null);
  const [comments, setComments] = useState<TicketComment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [newComment, setNewComment] = useState('');
  const [submittingComment, setSubmittingComment] = useState(false);
  const [status, setStatus] = useState('InProgress');
  const [statusComment, setStatusComment] = useState('');
  const [assignUserId, setAssignUserId] = useState('');
  const [assignReason, setAssignReason] = useState('');
  const [updating, setUpdating] = useState(false);
  const [assignableUsers, setAssignableUsers] = useState<AssignableUser[]>([]);
  const { user } = useAuth();

  const roles = user?.roles ?? [];
  const normalizedRoles = roles.map((r) => String(r).trim().toLowerCase());
  const isAdmin = normalizedRoles.includes('admin') || normalizedRoles.includes('superadmin');
  const isAgent = normalizedRoles.includes('agent');
  const canOperate = isAdmin || isAgent;

  const formatDateSafe = (value?: string | null) => {
    if (!value) return 'N/A';
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return 'N/A';
    return format(parsed, 'MMM dd, yyyy HH:mm');
  };

  useEffect(() => {
    if (!id) {
      return;
    }

    const ticketId = parseInt(id, 10);
    if (Number.isNaN(ticketId)) {
      setError('Invalid ticket id');
      setLoading(false);
      return;
    }

    const fetchData = async () => {
      try {
        setLoading(true);
        const [ticketResult, commentsResult] = await Promise.all([
          getTicket(ticketId),
          getTicketComments(ticketId)
        ]);
        setTicket(ticketResult);
        setStatus(ticketResult.status);
        setComments(commentsResult);
      } catch {
        setError('Failed to fetch ticket details');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [id]);

  useEffect(() => {
    if (!canOperate) {
      return;
    }

    const loadAssignableUsers = async () => {
      try {
        const users = await getAssignableAgents();
        setAssignableUsers(users);
      } catch {
        setAssignableUsers([]);
      }
    };

    loadAssignableUsers();
  }, [canOperate]);

  const handleAddComment = async () => {
    if (!newComment.trim() || !ticket) {
      return;
    }

    try {
      setSubmittingComment(true);
      const created = await addComment(ticket.id, {
        content: newComment,
        isInternal: false
      });
      setComments((prev) => [...prev, created]);
      setNewComment('');
    } catch {
      setError('Failed to add comment');
    } finally {
      setSubmittingComment(false);
    }
  };

  const handleStatusUpdate = async () => {
    if (!ticket || !canOperate) {
      return;
    }

    if (status === 'Resolved' && !statusComment.trim()) {
      setError('Please add a resolution explanation.');
      return;
    }

    if (status === 'Closed' && !isAdmin) {
      setError('Only Admin/SuperAdmin can close tickets.');
      return;
    }

    try {
      setUpdating(true);
      setError('');
      await changeTicketStatus(ticket.id, status as any, statusComment);
      const latest = await getTicket(ticket.id);
      setTicket(latest);
    } catch (e: any) {
      setError(e?.response?.data || 'Failed to update status');
    } finally {
      setUpdating(false);
    }
  };

  const handleAssign = async () => {
    if (!ticket || !canOperate) {
      return;
    }

    const parsedUserId = parseInt(assignUserId, 10);
    if (Number.isNaN(parsedUserId) || parsedUserId <= 0) {
      setError('Enter a valid assignee user ID.');
      return;
    }

    try {
      setUpdating(true);
      setError('');
      await assignTicket(ticket.id, parsedUserId, assignReason || 'Re-assigned by operator');
      const latest = await getTicket(ticket.id);
      setTicket(latest);
      setAssignReason('');
    } catch (e: any) {
      setError(e?.response?.data || 'Failed to assign ticket');
    } finally {
      setUpdating(false);
    }
  };

  const getStatusColor = (status: string) => {
    switch ((status || '').toLowerCase()) {
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
    switch ((priority || '').toLowerCase()) {
      case 'critical': return 'error';
      case 'high': return 'warning';
      case 'medium': return 'info';
      case 'low': return 'success';
      default: return 'default';
    }
  };

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" p={4}>
        <CircularProgress />
      </Box>
    );
  }

  if (error || !ticket) {
    return (
      <Box p={4}>
        <Alert severity="error">{error || 'Ticket not found'}</Alert>
        <Button onClick={() => navigate('/app/tickets')} sx={{ mt: 2 }}>
          Back to Tickets
        </Button>
      </Box>
    );
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          {ticket.ticketNumber}
        </Typography>
        <Button variant="outlined" onClick={() => navigate('/app/tickets')}>
          Back to Tickets
        </Button>
      </Box>

      <Grid container spacing={3}>
        <Grid item xs={12} md={8}>
          <Card sx={{ mb: 3 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                {ticket.title}
              </Typography>

              <Box display="flex" gap={1} mb={2}>
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
                <Chip label={ticket.category} variant="outlined" size="small" />
              </Box>

              <Typography variant="body1" paragraph>
                {ticket.description}
              </Typography>

              <Divider sx={{ my: 2 }} />
              {canOperate && (
                <Box sx={{ mb: 3 }}>
                  <Typography variant="h6" gutterBottom>Agent/Admin Actions</Typography>
                  <Grid container spacing={2}>
                    <Grid item xs={12} md={4}>
                      <TextField
                        select
                        SelectProps={{ native: true }}
                        fullWidth
                        label="Status"
                        value={status}
                        onChange={(e) => setStatus(e.target.value)}
                      >
                        {['New', 'InProgress', 'Waiting', 'Resolved', 'Reopened', 'Escalated', ...(isAdmin ? ['Closed'] : [])].map((s) => (
                          <option key={s} value={s}>{s}</option>
                        ))}
                      </TextField>
                    </Grid>
                    <Grid item xs={12} md={8}>
                      <TextField
                        fullWidth
                        label="Status Note (required when resolving)"
                        value={statusComment}
                        onChange={(e) => setStatusComment(e.target.value)}
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <Button variant="contained" onClick={handleStatusUpdate} disabled={updating}>
                        Update Status
                      </Button>
                    </Grid>
                    <Grid item xs={12} md={4}>
                      <Autocomplete
                        options={assignableUsers}
                        value={assignableUsers.find((u) => String(u.id) === assignUserId) ?? null}
                        onChange={(_, value) => setAssignUserId(value ? String(value.id) : '')}
                        getOptionLabel={(option) => `${option.fullName || option.email} (${option.email})`}
                        isOptionEqualToValue={(option, value) => option.id === value.id}
                        renderInput={(params) => (
                          <TextField {...params} label="Assign To Agent" placeholder="Search agent by name/email" />
                        )}
                      />
                    </Grid>
                    <Grid item xs={12} md={8}>
                      <TextField
                        fullWidth
                        label="Assignment Reason"
                        value={assignReason}
                        onChange={(e) => setAssignReason(e.target.value)}
                      />
                    </Grid>
                    <Grid item xs={12}>
                      <Button variant="outlined" onClick={handleAssign} disabled={updating}>
                        Reassign Ticket
                      </Button>
                    </Grid>
                  </Grid>
                </Box>
              )}

              <Typography variant="h6" gutterBottom>
                Comments
              </Typography>

              <Box sx={{ mb: 3 }}>
                <TextField
                  fullWidth
                  multiline
                  rows={3}
                  placeholder="Add a comment..."
                  value={newComment}
                  onChange={(e) => setNewComment(e.target.value)}
                  disabled={submittingComment}
                />
                <Button
                  variant="contained"
                  onClick={handleAddComment}
                  disabled={!newComment.trim() || submittingComment}
                  sx={{ mt: 1 }}
                >
                  {submittingComment ? 'Adding...' : 'Add Comment'}
                </Button>
              </Box>

              <List>
                {comments.map((comment) => (
                  <ListItem key={comment.id} alignItems="flex-start">
                    <ListItemAvatar>
                      <Avatar>
                        U{comment.senderUserId}
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary={(
                        <Box display="flex" justifyContent="space-between" alignItems="center">
                          <Typography variant="subtitle2">
                            User {comment.senderUserId}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {formatDateSafe(comment.createdAt)}
                          </Typography>
                        </Box>
                      )}
                      secondary={(
                        <Typography variant="body2" sx={{ mt: 1 }}>
                          {comment.content}
                        </Typography>
                      )}
                    />
                  </ListItem>
                ))}
              </List>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Ticket Details
              </Typography>

              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Box>
                  <Typography variant="caption" color="text.secondary">
                    Created
                  </Typography>
                  <Typography variant="body2">
                    {formatDateSafe(ticket.createdAt)}
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="text.secondary">
                    Last Updated
                  </Typography>
                  <Typography variant="body2">
                    {formatDateSafe(ticket.updatedAt)}
                  </Typography>
                </Box>

                <Divider />

                <Box>
                  <Typography variant="caption" color="text.secondary">
                    Created By User ID
                  </Typography>
                  <Typography variant="body2">
                    {ticket.createdByUserId ?? 'Unknown'}
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="text.secondary">
                    Assigned To User ID
                  </Typography>
                  <Typography variant="body2">
                    {ticket.assignedToUserId ?? 'Unassigned'}
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default TicketDetail;
