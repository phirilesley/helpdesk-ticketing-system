import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Paper,
  IconButton,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  CircularProgress,
  Alert,
  Chip,
  LinearProgress,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip
} from '@mui/material';
import {
  Download,
  Refresh,
  People,
  SupportAgent,
  Speed,
  Assessment,
  SentimentSatisfied,
  PriorityHigh
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  BarChart,
  Bar
} from 'recharts';
import { format } from 'date-fns';
import axios from 'axios';

interface RealTimeMetrics {
  totalActiveTickets: number;
  newTicketsLast24Hours: number;
  resolvedTicketsLast24Hours: number;
  criticalTickets: number;
  overdueTickets: number;
  averageFirstResponseTime: number;
  activeAgents: number;
  systemLoadPercentage: number;
}

interface TicketTrend {
  date: string;
  createdTickets: number;
  resolvedTickets: number;
  openTickets: number;
}

interface CategoryPerformance {
  categoryId: number;
  categoryName: string;
  totalTickets: number;
  resolvedTickets: number;
  averageResolutionTimeHours: number;
  slaCompliancePercentage: number;
}

interface CustomerSatisfaction {
  totalClosedTickets: number;
  ticketsWithFeedback: number;
  feedbackResponseRate: number;
  averageSatisfactionScore: number;
}

interface AgentMetric {
  agentName: string;
  totalTickets: number;
  resolvedTickets: number;
  averageResolutionTime: number;
  satisfactionScore: number;
}

const AdminDashboard: React.FC = () => {
  const [realTimeMetrics, setRealTimeMetrics] = useState<RealTimeMetrics | null>(null);
  const [ticketTrends, setTicketTrends] = useState<TicketTrend[]>([]);
  const [categoryPerformance, setCategoryPerformance] = useState<CategoryPerformance[]>([]);
  const [customerSatisfaction, setCustomerSatisfaction] = useState<CustomerSatisfaction | null>(null);
  const [topAgents, setTopAgents] = useState<AgentMetric[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [timeRange, setTimeRange] = useState(30);

  const fetchDashboardData = useCallback(async () => {
    try {
      setLoading(true);
      setError('');

      const [metrics, trends, categories, satisfaction, performanceReport] = await Promise.all([
        axios.get('/api/advancedanalytics/real-time-metrics'),
        axios.get(`/api/advancedanalytics/trends?days=${timeRange}`),
        axios.get('/api/advancedanalytics/category-performance'),
        axios.get('/api/advancedanalytics/customer-satisfaction'),
        axios.get('/api/advancedanalytics/performance-report')
      ]);

      setRealTimeMetrics(metrics.data);
      setTicketTrends(trends.data.map((t: any) => ({
        ...t,
        date: format(new Date(t.date), 'MMM dd')
      })));
      setCategoryPerformance(categories.data);
      setCustomerSatisfaction(satisfaction.data);
      setTopAgents(performanceReport.data.topPerformingAgents);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to fetch dashboard data');
    } finally {
      setLoading(false);
    }
  }, [timeRange]);

  useEffect(() => {
    fetchDashboardData();
    const interval = setInterval(fetchDashboardData, 30000);
    return () => clearInterval(interval);
  }, [fetchDashboardData]);

  const handleExportReport = async () => {
    try {
      const response = await axios.get('/api/advancedanalytics/export-report', {
        responseType: 'blob'
      });
      
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', `performance-report-${format(new Date(), 'yyyy-MM-dd')}.csv`);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err: any) {
      setError('Failed to export report');
    }
  };

  if (loading && !realTimeMetrics) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4" component="h1">
          Analytics Dashboard
        </Typography>
        <Box display="flex" gap={2}>
          <FormControl size="small">
            <InputLabel>Time Range</InputLabel>
            <Select
              value={timeRange}
              label="Time Range"
              onChange={(e) => setTimeRange(Number(e.target.value))}
              sx={{ minWidth: 120 }}
            >
              <MenuItem value={7}>7 Days</MenuItem>
              <MenuItem value={30}>30 Days</MenuItem>
              <MenuItem value={90}>90 Days</MenuItem>
            </Select>
          </FormControl>
          <Button
            variant="outlined"
            startIcon={<Download />}
            onClick={handleExportReport}
          >
            Export Report
          </Button>
          <IconButton onClick={fetchDashboardData}>
            <Refresh />
          </IconButton>
        </Box>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      {/* Real-time Metrics */}
      <Grid container spacing={3} mb={3}>
        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" mb={1}>
                <SupportAgent color="primary" />
                <Typography variant="h6" ml={1}>
                  Active Tickets
                </Typography>
              </Box>
              <Typography variant="h4" color="primary">
                {realTimeMetrics?.totalActiveTickets || 0}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {realTimeMetrics?.newTicketsLast24Hours || 0} new in 24h
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" mb={1}>
                <PriorityHigh color="error" />
                <Typography variant="h6" ml={1}>
                  Critical Tickets
                </Typography>
              </Box>
              <Typography variant="h4" color="error">
                {realTimeMetrics?.criticalTickets || 0}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {realTimeMetrics?.overdueTickets || 0} overdue
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" mb={1}>
                <People color="success" />
                <Typography variant="h6" ml={1}>
                  Active Agents
                </Typography>
              </Box>
              <Typography variant="h4" color="success">
                {realTimeMetrics?.activeAgents || 0}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {realTimeMetrics?.resolvedTicketsLast24Hours || 0} resolved in 24h
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" mb={1}>
                <Speed color="warning" />
                <Typography variant="h6" ml={1}>
                  System Load
                </Typography>
              </Box>
              <Typography variant="h4" color="warning">
                {realTimeMetrics?.systemLoadPercentage.toFixed(1) || 0}%
              </Typography>
              <LinearProgress
                variant="determinate"
                value={realTimeMetrics?.systemLoadPercentage || 0}
                sx={{ mt: 1 }}
              />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Charts */}
      <Grid container spacing={3} mb={3}>
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Ticket Trends
            </Typography>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={ticketTrends}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis />
                <RechartsTooltip />
                <Line type="monotone" dataKey="createdTickets" stroke="#8884d8" name="Created" />
                <Line type="monotone" dataKey="resolvedTickets" stroke="#82ca9d" name="Resolved" />
                <Line type="monotone" dataKey="openTickets" stroke="#ffc658" name="Open" />
              </LineChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>

        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Customer Satisfaction
            </Typography>
            <Box textAlign="center" py={2}>
              <Typography variant="h3" color="primary">
                {customerSatisfaction?.averageSatisfactionScore.toFixed(1) || 0}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                Average Score (1-5)
              </Typography>
              <Box display="flex" justifyContent="center" mt={2}>
                <SentimentSatisfied color="primary" fontSize="large" />
              </Box>
              <Typography variant="body2" sx={{ mt: 1 }}>
                Response Rate: {customerSatisfaction?.feedbackResponseRate.toFixed(1) || 0}%
              </Typography>
            </Box>
          </Paper>
        </Grid>
      </Grid>

      {/* Category Performance */}
      <Grid container spacing={3} mb={3}>
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Category Performance
            </Typography>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={categoryPerformance}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="categoryName" />
                <YAxis />
                <RechartsTooltip />
                <Bar dataKey="totalTickets" fill="#8884d8" name="Total Tickets" />
              </BarChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>

        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              SLA Compliance by Category
            </Typography>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={categoryPerformance}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="categoryName" />
                <YAxis />
                <RechartsTooltip />
                <Bar dataKey="slaCompliancePercentage" fill="#82ca9d" name="SLA %" />
              </BarChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>
      </Grid>

      {/* Top Performing Agents */}
      <Grid item xs={12}>
        <Paper sx={{ p: 2 }}>
          <Typography variant="h6" gutterBottom>
            Top Performing Agents
          </Typography>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Agent</TableCell>
                  <TableCell align="center">Total Tickets</TableCell>
                  <TableCell align="center">Resolved</TableCell>
                  <TableCell align="center">Avg Resolution Time</TableCell>
                  <TableCell align="center">Satisfaction Score</TableCell>
                  <TableCell align="center">Performance</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {topAgents.map((agent, index) => (
                  <TableRow key={agent.agentName}>
                    <TableCell>{agent.agentName}</TableCell>
                    <TableCell align="center">{agent.totalTickets}</TableCell>
                    <TableCell align="center">{agent.resolvedTickets}</TableCell>
                    <TableCell align="center">
                      {agent.averageResolutionTime.toFixed(1)}h
                    </TableCell>
                    <TableCell align="center">
                      <Chip
                        label={agent.satisfactionScore.toFixed(1)}
                        color={agent.satisfactionScore >= 4 ? 'success' : agent.satisfactionScore >= 3 ? 'warning' : 'error'}
                        size="small"
                      />
                    </TableCell>
                    <TableCell align="center">
                      <Tooltip title={`Rank #${index + 1}`}>
                        <Assessment color={index < 3 ? 'primary' : 'disabled'} />
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      </Grid>
    </Box>
  );
};

export default AdminDashboard;
