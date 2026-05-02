import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Chip,
  Button,
  Grid,
  TextField,
  Paper,
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
  assignedTo?: {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
  };
  createdBy: {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
  };
  attachments: Array<{
    id: number;
    fileName: string;
    fileSize: number;
    contentType: string;
    downloadUrl: string;
  }>;
  comments: Array<{
    id: number;
    content: string;
    createdAt: string;
    author: {
      firstName: string;
      lastName: string;
    };
    isInternal: boolean;
  }>;
}

const TicketDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [ticket, setTicket] = useState<Ticket | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [newComment, setNewComment] = useState('');
  const [submittingComment, setSubmittingComment] = useState(false);

  useEffect(() => {
    if (id) {
      fetchTicket(parseInt(id));
    }
  }, [id]);

  const fetchTicket = async (ticketId: number) => {
    try {
      setLoading(true);
      const response = await axios.get(`/api/tickets/${ticketId}`);
      setTicket(response.data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to fetch ticket details');
    } finally {
      setLoading(false);
    }
  };

  const handleAddComment = async () => {
    if (!newComment.trim() || !ticket) return;

    try {
      setSubmittingComment(true);
      const response = await axios.post(`/api/tickets/${ticket.id}/comments`, {
        content: newComment,
        isInternal: false
      });
      
      setTicket(prev => prev ? {
        ...prev,
        comments: [...prev.comments, response.data]
      } : null);
      
      setNewComment('');
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to add comment');
    } finally {
      setSubmittingComment(false);
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
                {ticket.comments.map((comment) => (
                  <ListItem key={comment.id} alignItems="flex-start">
                    <ListItemAvatar>
                      <Avatar>
                        {comment.author.firstName[0]}{comment.author.lastName[0]}
                      </Avatar>
                    </ListItemAvatar>
                    <ListItemText
                      primary={
                        <Box display="flex" justifyContent="space-between" alignItems="center">
                          <Typography variant="subtitle2">
                            {comment.author.firstName} {comment.author.lastName}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {format(new Date(comment.createdAt), 'MMM dd, yyyy HH:mm')}
                          </Typography>
                        </Box>
                      }
                      secondary={
                        <Typography variant="body2" sx={{ mt: 1 }}>
                          {comment.content}
                        </Typography>
                      }
                    />
                  </ListItem>
                ))}
              </List>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={4}>
          <Card sx={{ mb: 3 }}>
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
                    Created By
                  </Typography>
                  <Typography variant="body2">
                    {ticket.createdBy.firstName} {ticket.createdBy.lastName}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {ticket.createdBy.email}
                  </Typography>
                </Box>

                {ticket.assignedTo && (
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      Assigned To
                    </Typography>
                    <Typography variant="body2">
                      {ticket.assignedTo.firstName} {ticket.assignedTo.lastName}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {ticket.assignedTo.email}
                    </Typography>
                  </Box>
                )}
              </Box>
            </CardContent>
          </Card>

          {ticket.attachments.length > 0 && (
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Attachments
                </Typography>
                {ticket.attachments.map((attachment) => (
                  <Button
                    key={attachment.id}
                    variant="outlined"
                    size="small"
                    href={attachment.downloadUrl}
                    download={attachment.fileName}
                    sx={{ mr: 1, mb: 1 }}
                  >
                    {attachment.fileName}
                  </Button>
                ))}
              </CardContent>
            </Card>
          )}
        </Grid>
      </Grid>
    </Box>
  );
};

export default TicketDetail;
