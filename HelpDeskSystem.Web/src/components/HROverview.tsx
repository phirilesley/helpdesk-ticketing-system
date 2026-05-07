import React from 'react';
import { useQuery } from 'react-query';
import axios from 'axios';
import { Alert, Box, Card, CardContent, CircularProgress, Grid, Typography } from '@mui/material';

const HROverview: React.FC = () => {
  const { data, isLoading, isError } = useQuery('hr-overview', async () => {
    const response = await axios.get('/api/enterprise/dashboard');
    return response.data;
  });

  if (isLoading) return <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}><CircularProgress /></Box>;
  if (isError) return <Alert severity="error">Failed to load HR dashboard.</Alert>;

  const hr = data?.hrMetrics ?? {};
  const cards = [
    { label: 'Employees', value: hr.totalEmployees ?? 0 },
    { label: 'On Leave', value: hr.onLeave ?? 0 },
    { label: 'Open Requests', value: hr.openRequests ?? 0 },
    { label: 'Onboarding', value: hr.onboarding ?? 0 }
  ];

  return (
    <Box>
      <Typography variant="h4" gutterBottom>HR Overview</Typography>
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

export default HROverview;
