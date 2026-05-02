import React, { useState, useEffect, useMemo } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Grid,
  Button,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  Avatar,
  LinearProgress,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Alert,
  Snackbar,
  Tabs,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
} from '@mui/material';
import {
  ZoomIn,
  ZoomOut,
  Speed,
  Assignment,
  People,
  Timeline,
  Refresh,
  Add,
  Edit,
  Delete,
  Visibility,
  Warning,
  CheckCircle,
  RadioButtonUnchecked,
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  Legend,
  ResponsiveContainer,
  BarChart,
  Bar,
  PieChart,
  Pie,
  Cell,
  AreaChart,
  Area,
} from 'recharts';
import axios from 'axios';
import { format, subDays, startOfDay, endOfDay, differenceInDays, addDays } from 'date-fns';

interface Sprint {
  id: string;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  status: 'planning' | 'active' | 'completed' | 'cancelled';
  projectId: string;
  teamId: string;
  capacity: number;
  velocity: number;
  focusFactor: number;
  burndownData: BurndownPoint[];
  tasks: SprintTask[];
  goals: SprintGoal[];
  retrospective?: SprintRetrospective;
}

interface BurndownPoint {
  date: string;
  ideal: number;
  actual: number;
  remaining: number;
  completed: number;
}

interface SprintTask {
  id: string;
  title: string;
  description?: string;
  status: 'todo' | 'in_progress' | 'done' | 'blocked';
  priority: 'low' | 'medium' | 'high' | 'critical';
  estimate: number;
  actualTime?: number;
  assignee?: {
    id: string;
    name: string;
    avatar?: string;
  };
  type: 'story' | 'bug' | 'task' | 'epic';
  tags: string[];
  createdAt: string;
  updatedAt: string;
}

interface SprintGoal {
  id: string;
  title: string;
  description: string;
  status: 'not_started' | 'in_progress' | 'achieved' | 'not_achieved';
  priority: 'low' | 'medium' | 'high';
}

interface SprintRetrospective {
  id: string;
  whatWentWell: string[];
  whatCouldBeImproved: string[];
  actionItems: string[];
  teamMorale: number;
  sprintRating: number;
  createdAt: string;
}

interface TeamMember {
  id: string;
  name: string;
  avatar?: string;
  role: string;
  capacity: number;
  velocity: number;
  tasksCompleted: number;
  tasksInProgress: number;
}

interface AgileDashboardProps {
  projectId: string;
  sprintId?: string;
}

const AgileDashboard: React.FC<AgileDashboardProps> = ({ projectId, sprintId }) => {
  const [sprints, setSprints] = useState<Sprint[]>([]);
  const [currentSprint, setCurrentSprint] = useState<Sprint | null>(null);
  const [selectedSprint, setSelectedSprint] = useState<Sprint | null>(null);
  const [teamMembers, setTeamMembers] = useState<TeamMember[]>([]);
  const [tabValue, setTabValue] = useState(0);
  const [isSprintDialogOpen, setIsSprintDialogOpen] = useState(false);
  const [isTaskDialogOpen, setIsTaskDialogOpen] = useState(false);
  const [editingSprint, setEditingSprint] = useState<Sprint | null>(null);
  const [editingTask, setEditingTask] = useState<SprintTask | null>(null);
  const [notification, setNotification] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'warning' | 'info';
  }>({ open: false, message: '', severity: 'info' });

  useEffect(() => {
    loadData();
  }, [projectId, sprintId]);

  const loadData = async () => {
    try {
      const [sprintsResponse, teamResponse] = await Promise.all([
        axios.get(`/api/projects/${projectId}/sprints`),
        axios.get(`/api/projects/${projectId}/team`),
      ]);

      setSprints(sprintsResponse.data);
      setTeamMembers(teamResponse.data);

      // Set current sprint
      const activeSprint = sprintsResponse.data.find((s: Sprint) => s.status === 'active');
      setCurrentSprint(activeSprint || null);

      // Set selected sprint
      if (sprintId) {
        const sprint = sprintsResponse.data.find((s: Sprint) => s.id === sprintId);
        setSelectedSprint(sprint || null);
      } else {
        setSelectedSprint(activeSprint || null);
      }
    } catch (error) {
      showNotification('Failed to load data', 'error');
    }
  };

  const burndownData = useMemo(() => {
    if (!selectedSprint) return [];

    return selectedSprint.burndownData.map(point => ({
      date: format(new Date(point.date), 'MMM dd'),
      ideal: point.ideal,
      actual: point.actual,
      remaining: point.remaining,
      completed: point.completed,
    }));
  }, [selectedSprint]);

  const velocityData = useMemo(() => {
    return sprints
      .filter(s => s.status === 'completed')
      .slice(-6)
      .map(sprint => ({
        name: sprint.name,
        planned: sprint.capacity,
        completed: sprint.velocity,
        efficiency: sprint.focusFactor,
      }));
  }, [sprints]);

  const taskDistribution = useMemo(() => {
    if (!selectedSprint) return [];

    const distribution = selectedSprint.tasks.reduce((acc, task) => {
      const status = task.status;
      acc[status] = (acc[status] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    return Object.entries(distribution).map(([status, count]) => ({
      name: status.replace('_', ' ').toUpperCase(),
      value: count,
      color: getStatusColor(status),
    }));
  }, [selectedSprint]);

  const priorityDistribution = useMemo(() => {
    if (!selectedSprint) return [];

    const distribution = selectedSprint.tasks.reduce((acc, task) => {
      const priority = task.priority;
      acc[priority] = (acc[priority] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    return Object.entries(distribution).map(([priority, count]) => ({
      name: priority.toUpperCase(),
      value: count,
      color: getPriorityColor(priority),
    }));
  }, [selectedSprint]);

  const teamPerformance = useMemo(() => {
    return teamMembers.map(member => ({
      name: member.name,
      capacity: member.capacity,
      completed: member.tasksCompleted,
      inProgress: member.tasksInProgress,
      velocity: member.velocity,
      efficiency: member.capacity > 0 ? (member.velocity / member.capacity) * 100 : 0,
    }));
  }, [teamMembers]);

  const getStatusColor = (status: string) => {
    const colors = {
      'todo': '#9E9E9E',
      'in_progress': '#FF9800',
      'done': '#4CAF50',
      'blocked': '#F44336',
    };
    return colors[status as keyof typeof colors] || '#607D8B';
  };

  const getPriorityColor = (priority: string) => {
    const colors = {
      'low': '#4CAF50',
      'medium': '#FF9800',
      'high': '#FF5722',
      'critical': '#F44336',
    };
    return colors[priority as keyof typeof colors] || '#607D8B';
  };

  const handleCreateSprint = () => {
    setEditingSprint(null);
    setIsSprintDialogOpen(true);
  };

  const handleEditSprint = (sprint: Sprint) => {
    setEditingSprint(sprint);
    setIsSprintDialogOpen(true);
  };

  const handleCreateTask = () => {
    setEditingTask(null);
    setIsTaskDialogOpen(true);
  };

  const handleEditTask = (task: SprintTask) => {
    setEditingTask(task);
    setIsTaskDialogOpen(true);
  };

  const handleSaveSprint = async (sprintData: Partial<Sprint>) => {
    try {
      if (editingSprint) {
        const response = await axios.put(`/api/projects/${projectId}/sprints/${editingSprint.id}`, sprintData);
        setSprints(sprints.map(s => s.id === editingSprint.id ? response.data : s));
        showNotification('Sprint updated successfully', 'success');
      } else {
        const response = await axios.post(`/api/projects/${projectId}/sprints`, sprintData);
        setSprints([...sprints, response.data]);
        showNotification('Sprint created successfully', 'success');
      }
      setIsSprintDialogOpen(false);
    } catch (error) {
      showNotification('Failed to save sprint', 'error');
    }
  };

  const handleSaveTask = async (taskData: Partial<SprintTask>) => {
    try {
      if (!selectedSprint) return;

      if (editingTask) {
        const response = await axios.put(`/api/sprints/${selectedSprint.id}/tasks/${editingTask.id}`, taskData);
        setSelectedSprint({
          ...selectedSprint,
          tasks: selectedSprint.tasks.map(t => t.id === editingTask.id ? response.data : t),
        });
        showNotification('Task updated successfully', 'success');
      } else {
        const response = await axios.post(`/api/sprints/${selectedSprint.id}/tasks`, taskData);
        setSelectedSprint({
          ...selectedSprint,
          tasks: [...selectedSprint.tasks, response.data],
        });
        showNotification('Task created successfully', 'success');
      }
      setIsTaskDialogOpen(false);
    } catch (error) {
      showNotification('Failed to save task', 'error');
    }
  };

  const showNotification = (message: string, severity: 'success' | 'error' | 'warning' | 'info') => {
    setNotification({ open: true, message, severity });
  };

  const handleNotificationClose = () => {
    setNotification({ ...notification, open: false });
  };

  const calculateSprintHealth = () => {
    if (!selectedSprint) return 0;

    const totalTasks = selectedSprint.tasks.length;
    const completedTasks = selectedSprint.tasks.filter(t => t.status === 'done').length;
    const blockedTasks = selectedSprint.tasks.filter(t => t.status === 'blocked').length;
    
    const completionRate = totalTasks > 0 ? (completedTasks / totalTasks) * 100 : 0;
    const blockRate = totalTasks > 0 ? (blockedTasks / totalTasks) * 100 : 0;
    
    return Math.max(0, completionRate - blockRate);
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Agile Dashboard</Typography>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <FormControl size="small" sx={{ minWidth: 200 }}>
            <InputLabel>Sprint</InputLabel>
            <Select
              value={selectedSprint?.id || ''}
              onChange={(e) => {
                const sprint = sprints.find(s => s.id === e.target.value);
                setSelectedSprint(sprint || null);
              }}
            >
              {sprints.map(sprint => (
                <MenuItem key={sprint.id} value={sprint.id}>
                  {sprint.name} ({sprint.status})
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Button variant="contained" startIcon={<Add />} onClick={handleCreateSprint}>
            New Sprint
          </Button>
          <IconButton onClick={loadData}>
            <Refresh />
          </IconButton>
        </Box>
      </Box>

      {/* Sprint Overview */}
      {selectedSprint && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 2 }}>
              <Box>
                <Typography variant="h6">{selectedSprint.name}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {format(new Date(selectedSprint.startDate), 'MMM dd')} - {format(new Date(selectedSprint.endDate), 'MMM dd')}
                </Typography>
              </Box>
              <Box sx={{ display: 'flex', gap: 1 }}>
                <Chip label={selectedSprint.status} color="primary" size="small" />
                <IconButton size="small" onClick={() => handleEditSprint(selectedSprint)}>
                  <Edit />
                </IconButton>
              </Box>
            </Box>
            
            <Grid container spacing={2}>
              <Grid item xs={12} md={3}>
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">Capacity</Typography>
                  <Typography variant="h6">{selectedSprint.capacity} points</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} md={3}>
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">Velocity</Typography>
                  <Typography variant="h6">{selectedSprint.velocity} points</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} md={3}>
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">Focus Factor</Typography>
                  <Typography variant="h6">{selectedSprint.focusFactor}%</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} md={3}>
                <Box>
                  <Typography variant="subtitle2" color="text.secondary">Sprint Health</Typography>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography variant="h6">{calculateSprintHealth()}%</Typography>
                    <LinearProgress
                      variant="determinate"
                      value={calculateSprintHealth()}
                      sx={{ flex: 1 }}
                      color={calculateSprintHealth() > 70 ? 'success' : calculateSprintHealth() > 40 ? 'warning' : 'error'}
                    />
                  </Box>
                </Box>
              </Grid>
            </Grid>
          </CardContent>
        </Card>
      )}

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={(e, newValue) => setTabValue(newValue)}>
          <Tab label="Burndown" />
          <Tab label="Velocity" />
          <Tab label="Tasks" />
          <Tab label="Team" />
          <Tab label="Analytics" />
        </Tabs>
      </Box>

      {/* Tab Content */}
      {tabValue === 0 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Sprint Burndown</Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <LineChart data={burndownData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" />
                    <YAxis />
                    <RechartsTooltip />
                    <Legend />
                    <Line type="monotone" dataKey="ideal" stroke="#9E9E9E" strokeDasharray="5 5" name="Ideal" />
                    <Line type="monotone" dataKey="actual" stroke="#F44336" name="Actual" />
                    <Line type="monotone" dataKey="remaining" stroke="#2196F3" name="Remaining" />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Burndown Summary</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Total Points</Typography>
                    <Typography variant="h6">
                      {selectedSprint?.capacity || 0}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Completed</Typography>
                    <Typography variant="h6">
                      {selectedSprint?.burndownData[selectedSprint.burndownData.length - 1]?.completed || 0}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Remaining</Typography>
                    <Typography variant="h6">
                      {selectedSprint?.burndownData[selectedSprint.burndownData.length - 1]?.remaining || 0}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Days Remaining</Typography>
                    <Typography variant="h6">
                      {selectedSprint ? Math.max(0, differenceInDays(new Date(selectedSprint.endDate), new Date())) : 0}
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {tabValue === 1 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Team Velocity</Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <BarChart data={velocityData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis />
                    <RechartsTooltip />
                    <Legend />
                    <Bar dataKey="planned" fill="#9E9E9E" name="Planned" />
                    <Bar dataKey="completed" fill="#4CAF50" name="Completed" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Velocity Metrics</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Average Velocity</Typography>
                    <Typography variant="h6">
                      {velocityData.length > 0 ? Math.round(velocityData.reduce((sum, s) => sum + s.completed, 0) / velocityData.length) : 0}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Average Efficiency</Typography>
                    <Typography variant="h6">
                      {velocityData.length > 0 ? Math.round(velocityData.reduce((sum, s) => sum + s.efficiency, 0) / velocityData.length) : 0}%
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Predictability</Typography>
                    <Typography variant="h6">
                      {velocityData.length > 0 ? Math.round((velocityData.filter(s => s.completed >= s.planned * 0.8).length / velocityData.length) * 100) : 0}%
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {tabValue === 2 && selectedSprint && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                  <Typography variant="h6">Sprint Tasks</Typography>
                  <Button variant="contained" startIcon={<Add />} onClick={handleCreateTask}>
                    Add Task
                  </Button>
                </Box>
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Title</TableCell>
                        <TableCell>Status</TableCell>
                        <TableCell>Priority</TableCell>
                        <TableCell>Estimate</TableCell>
                        <TableCell>Assignee</TableCell>
                        <TableCell>Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {selectedSprint.tasks.map((task) => (
                        <TableRow key={task.id}>
                          <TableCell>
                            <Box>
                              <Typography variant="body2">{task.title}</Typography>
                              <Typography variant="caption" color="text.secondary">{task.type}</Typography>
                            </Box>
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={task.status.replace('_', ' ').toUpperCase()}
                              size="small"
                              sx={{ backgroundColor: getStatusColor(task.status), color: 'white' }}
                            />
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={task.priority.toUpperCase()}
                              size="small"
                              sx={{ backgroundColor: getPriorityColor(task.priority), color: 'white' }}
                            />
                          </TableCell>
                          <TableCell>{task.estimate}h</TableCell>
                          <TableCell>
                            {task.assignee ? (
                              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                <Avatar src={task.assignee.avatar} sx={{ width: 24, height: 24 }}>
                                  {task.assignee.name.charAt(0).toUpperCase()}
                                </Avatar>
                                <Typography variant="caption">{task.assignee.name}</Typography>
                              </Box>
                            ) : (
                              <Typography variant="caption" color="text.secondary">Unassigned</Typography>
                            )}
                          </TableCell>
                          <TableCell>
                            <IconButton size="small" onClick={() => handleEditTask(task)}>
                              <Edit fontSize="small" />
                            </IconButton>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>Task Distribution</Typography>
                    <ResponsiveContainer width="100%" height={200}>
                      <PieChart>
                        <Pie
                          data={taskDistribution}
                          cx="50%"
                          cy="50%"
                          outerRadius={80}
                          fill="#8884d8"
                          dataKey="value"
                          label
                        >
                          {taskDistribution.map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={entry.color} />
                          ))}
                        </Pie>
                        <RechartsTooltip />
                      </PieChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>
              </Grid>
              <Grid item xs={12}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>Priority Distribution</Typography>
                    <ResponsiveContainer width="100%" height={200}>
                      <PieChart>
                        <Pie
                          data={priorityDistribution}
                          cx="50%"
                          cy="50%"
                          outerRadius={80}
                          fill="#8884d8"
                          dataKey="value"
                          label
                        >
                          {priorityDistribution.map((entry, index) => (
                            <Cell key={`cell-${index}`} fill={entry.color} />
                          ))}
                        </Pie>
                        <RechartsTooltip />
                      </PieChart>
                    </ResponsiveContainer>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Grid>
        </Grid>
      )}

      {tabValue === 3 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Team Performance</Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <BarChart data={teamPerformance}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis />
                    <RechartsTooltip />
                    <Legend />
                    <Bar dataKey="capacity" fill="#9E9E9E" name="Capacity" />
                    <Bar dataKey="completed" fill="#4CAF50" name="Completed" />
                    <Bar dataKey="inProgress" fill="#FF9800" name="In Progress" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Team Members</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  {teamMembers.map((member) => (
                    <Box key={member.id} sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                      <Avatar src={member.avatar} sx={{ width: 40, height: 40 }}>
                        {member.name.charAt(0).toUpperCase()}
                      </Avatar>
                      <Box sx={{ flex: 1 }}>
                        <Typography variant="body2">{member.name}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {member.role} • {member.velocity}/{member.capacity} points
                        </Typography>
                      </Box>
                      <Box sx={{ textAlign: 'right' }}>
                        <Typography variant="caption" color="text.secondary">
                          {member.tasksCompleted} completed
                        </Typography>
                      </Box>
                    </Box>
                  ))}
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {tabValue === 4 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Cumulative Flow</Typography>
                <ResponsiveContainer width="100%" height={300}>
                  <AreaChart data={burndownData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="date" />
                    <YAxis />
                    <RechartsTooltip />
                    <Legend />
                    <Area type="monotone" dataKey="completed" stackId="1" stroke="#4CAF50" fill="#4CAF50" name="Completed" />
                    <Area type="monotone" dataKey="actual" stackId="1" stroke="#FF9800" fill="#FF9800" name="In Progress" />
                    <Area type="monotone" dataKey="remaining" stackId="1" stroke="#2196F3" fill="#2196F3" name="To Do" />
                  </AreaChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Efficiency Trend</Typography>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={velocityData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis />
                    <RechartsTooltip />
                    <Legend />
                    <Line type="monotone" dataKey="efficiency" stroke="#9C27B0" name="Efficiency %" />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Sprint Dialog */}
      <SprintDialog
        open={isSprintDialogOpen}
        sprint={editingSprint}
        onClose={() => setIsSprintDialogOpen(false)}
        onSave={handleSaveSprint}
      />

      {/* Task Dialog */}
      <TaskDialog
        open={isTaskDialogOpen}
        task={editingTask}
        teamMembers={teamMembers}
        onClose={() => setIsTaskDialogOpen(false)}
        onSave={handleSaveTask}
      />

      {/* Notification */}
      <Snackbar
        open={notification.open}
        autoHideDuration={6000}
        onClose={handleNotificationClose}
      >
        <Alert onClose={handleNotificationClose} severity={notification.severity}>
          {notification.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

// Sprint Dialog Component
const SprintDialog: React.FC<{
  open: boolean;
  sprint: Sprint | null;
  onClose: () => void;
  onSave: (sprint: Partial<Sprint>) => void;
}> = ({ open, sprint, onClose, onSave }) => {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    startDate: new Date(),
    endDate: addDays(new Date(), 14),
    capacity: 40,
    goals: [] as SprintGoal[],
  });

  useEffect(() => {
    if (sprint) {
      setFormData({
        name: sprint.name,
        description: sprint.description || '',
        startDate: new Date(sprint.startDate),
        endDate: new Date(sprint.endDate),
        capacity: sprint.capacity,
        goals: sprint.goals,
      });
    } else {
      setFormData({
        name: '',
        description: '',
        startDate: new Date(),
        endDate: addDays(new Date(), 14),
        capacity: 40,
        goals: [],
      });
    }
  }, [sprint]);

  const handleSave = () => {
    onSave({
      ...formData,
      startDate: formData.startDate.toISOString(),
      endDate: formData.endDate.toISOString(),
    });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{sprint ? 'Edit Sprint' : 'Create Sprint'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField
            label="Sprint Name"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            fullWidth
            required
          />
          
          <TextField
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            multiline
            rows={3}
            fullWidth
          />
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <DatePicker
              label="Start Date"
              value={formData.startDate}
              onChange={(date) => date && setFormData({ ...formData, startDate: date })}
              slotProps={{ textField: { fullWidth: true } }}
            />
            
            <DatePicker
              label="End Date"
              value={formData.endDate}
              onChange={(date) => date && setFormData({ ...formData, endDate: date })}
              slotProps={{ textField: { fullWidth: true } }}
            />
          </Box>
          
          <TextField
            label="Capacity (story points)"
            type="number"
            value={formData.capacity}
            onChange={(e) => setFormData({ ...formData, capacity: parseInt(e.target.value) || 0 })}
            inputProps={{ min: 0 }}
            fullWidth
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained" disabled={!formData.name.trim()}>
          {sprint ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

// Task Dialog Component
const TaskDialog: React.FC<{
  open: boolean;
  task: SprintTask | null;
  teamMembers: TeamMember[];
  onClose: () => void;
  onSave: (task: Partial<SprintTask>) => void;
}> = ({ open, task, teamMembers, onClose, onSave }) => {
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    status: 'todo' as const,
    priority: 'medium' as const,
    estimate: 0,
    assigneeId: '',
    type: 'story' as const,
    tags: [] as string[],
  });

  useEffect(() => {
    if (task) {
      setFormData({
        title: task.title,
        description: task.description || '',
        status: task.status,
        priority: task.priority,
        estimate: task.estimate,
        assigneeId: task.assignee?.id || '',
        type: task.type,
        tags: task.tags,
      });
    } else {
      setFormData({
        title: '',
        description: '',
        status: 'todo',
        priority: 'medium',
        estimate: 0,
        assigneeId: '',
        type: 'story',
        tags: [],
      });
    }
  }, [task]);

  const handleSave = () => {
    const assignee = teamMembers.find(m => m.id === formData.assigneeId);
    onSave({
      ...formData,
      assignee: assignee ? {
        id: assignee.id,
        name: assignee.name,
        avatar: assignee.avatar,
      } : undefined,
    });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{task ? 'Edit Task' : 'Create Task'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField
            label="Task Title"
            value={formData.title}
            onChange={(e) => setFormData({ ...formData, title: e.target.value })}
            fullWidth
            required
          />
          
          <TextField
            label="Description"
            value={formData.description}
            onChange={(e) => setFormData({ ...formData, description: e.target.value })}
            multiline
            rows={3}
            fullWidth
          />
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl fullWidth>
              <InputLabel>Status</InputLabel>
              <Select
                value={formData.status}
                onChange={(e) => setFormData({ ...formData, status: e.target.value as any })}
              >
                <MenuItem value="todo">To Do</MenuItem>
                <MenuItem value="in_progress">In Progress</MenuItem>
                <MenuItem value="done">Done</MenuItem>
                <MenuItem value="blocked">Blocked</MenuItem>
              </Select>
            </FormControl>
            
            <FormControl fullWidth>
              <InputLabel>Priority</InputLabel>
              <Select
                value={formData.priority}
                onChange={(e) => setFormData({ ...formData, priority: e.target.value as any })}
              >
                <MenuItem value="low">Low</MenuItem>
                <MenuItem value="medium">Medium</MenuItem>
                <MenuItem value="high">High</MenuItem>
                <MenuItem value="critical">Critical</MenuItem>
              </Select>
            </FormControl>
          </Box>
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl fullWidth>
              <InputLabel>Type</InputLabel>
              <Select
                value={formData.type}
                onChange={(e) => setFormData({ ...formData, type: e.target.value as any })}
              >
                <MenuItem value="story">Story</MenuItem>
                <MenuItem value="bug">Bug</MenuItem>
                <MenuItem value="task">Task</MenuItem>
                <MenuItem value="epic">Epic</MenuItem>
              </Select>
            </FormControl>
            
            <TextField
              label="Estimate (hours)"
              type="number"
              value={formData.estimate}
              onChange={(e) => setFormData({ ...formData, estimate: parseFloat(e.target.value) || 0 })}
              inputProps={{ min: 0 }}
              fullWidth
            />
          </Box>
          
          <FormControl fullWidth>
            <InputLabel>Assignee</InputLabel>
            <Select
              value={formData.assigneeId}
              onChange={(e) => setFormData({ ...formData, assigneeId: e.target.value })}
            >
              <MenuItem value="">Unassigned</MenuItem>
              {teamMembers.map(member => (
                <MenuItem key={member.id} value={member.id}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Avatar src={member.avatar} sx={{ width: 20, height: 20 }}>
                      {member.name.charAt(0).toUpperCase()}
                    </Avatar>
                    {member.name}
                  </Box>
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained" disabled={!formData.title.trim()}>
          {task ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default AgileDashboard;
