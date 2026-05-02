import React from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Alert,
} from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { useMutation } from 'react-query';
import { useNavigate } from 'react-router-dom';
import { createTicket } from '../services/api';

interface CreateTicketForm {
  title: string;
  description: string;
  category: string;
  priority: string;
}

const CreateTicket: React.FC = () => {
  const navigate = useNavigate();
  const { control, handleSubmit, formState: { errors } } = useForm<CreateTicketForm>();

  const createTicketMutation = useMutation(createTicket, {
    onSuccess: () => {
      navigate('/tickets');
    },
  });

  const onSubmit = (data: CreateTicketForm) => {
    createTicketMutation.mutate(data);
  };

  return (
    <Box sx={{ maxWidth: 600, mx: 'auto', p: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          Create New Ticket
        </Typography>

        {createTicketMutation.isSuccess && (
          <Alert severity="success" sx={{ mb: 2 }}>
            Ticket created successfully!
          </Alert>
        )}

        {createTicketMutation.isError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            Failed to create ticket. Please try again.
          </Alert>
        )}

        <form onSubmit={handleSubmit(onSubmit)}>
          <Controller
            name="title"
            control={control}
            rules={{ required: 'Title is required' }}
            render={({ field }) => (
              <TextField
                {...field}
                label="Title"
                fullWidth
                margin="normal"
                error={!!errors.title}
                helperText={errors.title?.message}
              />
            )}
          />

          <Controller
            name="description"
            control={control}
            rules={{ required: 'Description is required' }}
            render={({ field }) => (
              <TextField
                {...field}
                label="Description"
                fullWidth
                multiline
                rows={4}
                margin="normal"
                error={!!errors.description}
                helperText={errors.description?.message}
              />
            )}
          />

          <Controller
            name="category"
            control={control}
            rules={{ required: 'Category is required' }}
            render={({ field }) => (
              <FormControl fullWidth margin="normal" error={!!errors.category}>
                <InputLabel>Category</InputLabel>
                <Select {...field} label="Category">
                  <MenuItem value="Technical Support">Technical Support</MenuItem>
                  <MenuItem value="Billing">Billing</MenuItem>
                  <MenuItem value="Account">Account</MenuItem>
                  <MenuItem value="Feature Request">Feature Request</MenuItem>
                </Select>
              </FormControl>
            )}
          />

          <Controller
            name="priority"
            control={control}
            rules={{ required: 'Priority is required' }}
            render={({ field }) => (
              <FormControl fullWidth margin="normal" error={!!errors.priority}>
                <InputLabel>Priority</InputLabel>
                <Select {...field} label="Priority">
                  <MenuItem value="Low">Low</MenuItem>
                  <MenuItem value="Medium">Medium</MenuItem>
                  <MenuItem value="High">High</MenuItem>
                  <MenuItem value="Critical">Critical</MenuItem>
                </Select>
              </FormControl>
            )}
          />

          <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
            <Button
              type="submit"
              variant="contained"
              disabled={createTicketMutation.isLoading}
            >
              {createTicketMutation.isLoading ? 'Creating...' : 'Create Ticket'}
            </Button>
            <Button
              variant="outlined"
              onClick={() => navigate('/tickets')}
              disabled={createTicketMutation.isLoading}
            >
              Cancel
            </Button>
          </Box>
        </form>
      </Paper>
    </Box>
  );
};

export default CreateTicket;
