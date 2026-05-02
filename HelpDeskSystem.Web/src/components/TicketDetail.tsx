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
import { format } from 'date-fns';
import { useParams, useNavigate } from 'react-router-dom';
import { addComment, getTicket, getTicketComments, Ticket, TicketComment } from '../services/api';

const TicketDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [ticket, setTicket] = useState<Ticket | null>(null);
  const [comments, setComments] = useState<TicketComment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [newComment, setNewComment] = useState('');
  const [submittingComment, setSubmittingComment] = useState(false);

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
        setComments(commentsResult);
      } catch {
        setError('Failed to fetch ticket details');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [id]);

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
        <Button onClick={() => navigate('/tickets')} sx={{ mt: 2 }}>
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
        <Button variant="outlined" onClick={() => navigate('/tickets')}>
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
                            {format(new Date(comment.createdAt), 'MMM dd, yyyy HH:mm')}
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
                    {format(new Date(ticket.createdAt), 'MMM dd, yyyy HH:mm')}
                  </Typography>
                </Box>

                <Box>
                  <Typography variant="caption" color="text.secondary">
                    Last Updated
                  </Typography>
                  <Typography variant="body2">
                    {format(new Date(ticket.updatedAt), 'MMM dd, yyyy HH:mm')}
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
