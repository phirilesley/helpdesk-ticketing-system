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
  CircularProgress,
} from '@mui/material';
import { useForm, Controller } from 'react-hook-form';
import { useMutation, useQuery } from 'react-query';
import { useNavigate } from 'react-router-dom';
import { createTicket, getTicketCategories, getTicketPriorities } from '../services/api';

interface CreateTicketForm {
  title: string;
  description: string;
  categoryId: number;
  priorityId: number;
}

const CreateTicket: React.FC = () => {
  const navigate = useNavigate();
  const { control, handleSubmit, formState: { errors } } = useForm<CreateTicketForm>();

  const { data: categories = [], isLoading: categoriesLoading } = useQuery(
    'ticket-categories',
    getTicketCategories
  );
  const { data: priorities = [], isLoading: prioritiesLoading } = useQuery(
    'ticket-priorities',
    getTicketPriorities
  );

  const createTicketMutation = useMutation(createTicket, {
    onSuccess: () => {
      navigate('/tickets');
    },
  });

  const onSubmit = (data: CreateTicketForm) => {
    createTicketMutation.mutate(data);
  };

  const metadataLoading = categoriesLoading || prioritiesLoading;

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

        {metadataLoading ? (
          <Box display="flex" justifyContent="center" py={4}>
            <CircularProgress />
          </Box>
        ) : (
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
              name="categoryId"
              control={control}
              rules={{ required: 'Category is required' }}
              render={({ field }) => (
                <FormControl fullWidth margin="normal" error={!!errors.categoryId}>
                  <InputLabel>Category</InputLabel>
                  <Select
                    {...field}
                    label="Category"
                    value={field.value ?? ''}
                    onChange={(e) => field.onChange(Number(e.target.value))}
                  >
                    {categories.map((category) => (
                      <MenuItem key={category.id} value={category.id}>
                        {category.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}
            />

            <Controller
              name="priorityId"
              control={control}
              rules={{ required: 'Priority is required' }}
              render={({ field }) => (
                <FormControl fullWidth margin="normal" error={!!errors.priorityId}>
                  <InputLabel>Priority</InputLabel>
                  <Select
                    {...field}
                    label="Priority"
                    value={field.value ?? ''}
                    onChange={(e) => field.onChange(Number(e.target.value))}
                  >
                    {priorities.map((priority) => (
                      <MenuItem key={priority.id} value={priority.id}>
                        {priority.name}
                      </MenuItem>
                    ))}
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
        )}
      </Paper>
    </Box>
  );
};

export default CreateTicket;
