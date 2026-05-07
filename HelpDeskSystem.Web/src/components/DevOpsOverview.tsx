import React from 'react';
import { useQuery } from 'react-query';
import axios from 'axios';
import { Alert, Box, Card, CardContent, CircularProgress, Grid, Typography } from '@mui/material';

const DevOpsOverview: React.FC = () => {
  const { data, isLoading, isError } = useQuery('devops-overview', async () => {
    const response = await axios.get('/api/devops/dashboard');
    return response.data;
  });

  if (isLoading) return <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}><CircularProgress /></Box>;
  if (isError) return <Alert severity="error">Failed to load DevOps dashboard.</Alert>;

  const cards = [
    { label: 'Builds', value: data?.builds ?? 0 },
    { label: 'Deployments', value: data?.deployments ?? 0 },
    { label: 'Incidents', value: data?.incidents ?? 0 },
    { label: 'MTTR', value: data?.mttr ?? 0 }
  ];

  return (
    <Box>
      <Typography variant="h4" gutterBottom>DevOps Overview</Typography>
      <Grid container spacing={2}>
        {cards.map((item) => (
          <Grid item xs={12} sm={6} md={3} key={item.label}>
            <Card><CardContent>
              <Typography variant="body2" color="text.secondary">{item.label}</Typography>
              <Typography variant="h5" sx={{ mt: 1 }}>{item.value}</Typography>
            </CardContent></Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default DevOpsOverview;
