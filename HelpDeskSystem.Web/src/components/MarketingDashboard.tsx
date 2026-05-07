import React from 'react';
import { useQuery } from 'react-query';
import axios from 'axios';
import { Alert, Box, Card, CardContent, CircularProgress, Grid, Typography } from '@mui/material';

const MarketingDashboard: React.FC = () => {
  const { data, isLoading, isError } = useQuery('marketing-dashboard', async () => {
    const response = await axios.get('/api/marketing/analytics/dashboard');
    return response.data;
  });

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (isError) {
    return <Alert severity="error">Failed to load marketing dashboard.</Alert>;
  }

  const stats = [
    { label: 'Active Campaigns', value: data?.activeCampaigns ?? data?.campaignsActive ?? 0 },
    { label: 'Leads', value: data?.totalLeads ?? data?.leads ?? 0 },
    { label: 'Conversions', value: data?.conversions ?? data?.conversionCount ?? 0 },
    { label: 'Revenue', value: data?.revenue ?? data?.totalRevenue ?? 0 }
  ];

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Marketing Dashboard
      </Typography>
      <Grid container spacing={2}>
        {stats.map((item) => (
          <Grid item xs={12} sm={6} md={3} key={item.label}>
            <Card>
              <CardContent>
                <Typography variant="body2" color="text.secondary">
                  {item.label}
                </Typography>
                <Typography variant="h5" sx={{ mt: 1 }}>
                  {item.value}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
};

export default MarketingDashboard;
