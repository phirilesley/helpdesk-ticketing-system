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
  Slider,
  Switch,
  FormControlLabel,
  Divider,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  People,
  Assignment,
  Schedule,
  Warning,
  CheckCircle,
  Edit,
  Delete,
  Add,
  Refresh,
  Visibility,
  Timeline,
  PieChart,
  BarChart,
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  Legend,
  ResponsiveContainer,
  BarChart as RechartsBarChart,
  Bar,
  PieChart as RechartsPieChart,
  Pie,
  Cell,
  AreaChart,
  Area,
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
} from 'recharts';
import axios from 'axios';
import { format, addDays, startOfWeek, endOfWeek, differenceInDays, isWithinInterval } from 'date-fns';

interface Resource {
  id: string;
  name: string;
  email: string;
  role: string;
  department: string;
  skills: string[];
  capacity: number;
  availability: number;
  hourlyRate: number;
  utilization: number;
  currentWorkload: number;
  maxWorkload: number;
  avatar?: string;
  isActive: boolean;
  location: string;
  timezone: string;
  preferences: ResourcePreferences;
}

interface ResourcePreferences {
  preferredHours: number[];
  preferredProjects: string[];
  skills: string[];
  maxWeeklyHours: number;
  minWeeklyHours: number;
  overtimeAllowed: boolean;
  remoteWorkAllowed: boolean;
}

interface Allocation {
  id: string;
  resourceId: string;
  projectId: string;
  taskId: string;
  allocationPercentage: number;
  startDate: string;
  endDate: string;
  estimatedHours: number;
  actualHours: number;
  status: 'planned' | 'active' | 'completed' | 'cancelled';
  priority: 'low' | 'medium' | 'high' | 'critical';
  notes?: string;
}

interface Project {
  id: string;
  name: string;
  description: string;
  status: 'planning' | 'active' | 'completed' | 'cancelled';
  startDate: string;
  endDate: string;
  budget: number;
  priority: 'low' | 'medium' | 'high' | 'critical';
  requiredSkills: string[];
  estimatedHours: number;
  actualHours: number;
  teamSize: number;
  manager: string;
}

interface ResourceAllocationProps {
  projectId?: string;
  departmentId?: string;
  timeframe?: 'week' | 'month' | 'quarter';
}

const ResourceAllocation: React.FC<ResourceAllocationProps> = ({
  projectId,
  departmentId,
  timeframe = 'month',
}) => {
  const [resources, setResources] = useState<Resource[]>([]);
  const [allocations, setAllocations] = useState<Allocation[]>([]);
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedResource, setSelectedResource] = useState<Resource | null>(null);
  const [selectedProject, setSelectedProject] = useState<Project | null>(null);
  const [tabValue, setTabValue] = useState(0);
  const [isAllocationDialogOpen, setIsAllocationDialogOpen] = useState(false);
  const [isResourceDialogOpen, setIsResourceDialogOpen] = useState(false);
  const [editingAllocation, setEditingAllocation] = useState<Allocation | null>(null);
  const [editingResource, setEditingResource] = useState<Resource | null>(null);
  const [notification, setNotification] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'warning' | 'info';
  }>({ open: false, message: '', severity: 'info' });

  useEffect(() => {
    loadData();
  }, [projectId, departmentId, timeframe]);

  const loadData = async () => {
    try {
      const [resourcesResponse, allocationsResponse, projectsResponse] = await Promise.all([
        axios.get('/api/resources'),
        axios.get('/api/allocations'),
        axios.get('/api/projects'),
      ]);

      setResources(resourcesResponse.data);
      setAllocations(allocationsResponse.data);
      setProjects(projectsResponse.data);

      if (projectId) {
        const project = projectsResponse.data.find((p: Project) => p.id === projectId);
        setSelectedProject(project || null);
      }
    } catch (error) {
      showNotification('Failed to load data', 'error');
    }
  };

  const utilizationData = useMemo(() => {
    return resources.map(resource => ({
      name: resource.name,
      utilization: resource.utilization,
      capacity: resource.capacity,
      workload: resource.currentWorkload,
      availability: resource.availability,
    }));
  }, [resources]);

  const allocationData = useMemo(() => {
    const allocationByResource = resources.map(resource => {
      const resourceAllocations = allocations.filter(a => a.resourceId === resource.id);
      const totalAllocated = resourceAllocations.reduce((sum, a) => sum + a.allocationPercentage, 0);
      
      return {
        name: resource.name,
        allocated: totalAllocated,
        available: 100 - totalAllocated,
        projects: resourceAllocations.length,
      };
    });

    return allocationByResource;
  }, [resources, allocations]);

  const skillDistribution = useMemo(() => {
    const skillCount: Record<string, number> = {};
    
    resources.forEach(resource => {
      resource.skills.forEach(skill => {
        skillCount[skill] = (skillCount[skill] || 0) + 1;
      });
    });

    return Object.entries(skillCount)
      .map(([skill, count]) => ({
        name: skill,
        value: count,
        color: getSkillColor(skill),
      }))
      .sort((a, b) => b.value - a.value)
      .slice(0, 10);
  }, [resources]);

  const projectAllocationData = useMemo(() => {
    return projects.map(project => {
      const projectAllocations = allocations.filter(a => a.projectId === project.id);
      const totalAllocated = projectAllocations.reduce((sum, a) => sum + a.allocationPercentage, 0);
      
      return {
        name: project.name,
        allocated: totalAllocated,
        budget: project.budget,
        utilization: (project.actualHours / project.estimatedHours) * 100,
        status: project.status,
      };
    });
  }, [projects, allocations]);

  const capacityTrendData = useMemo(() => {
    // Generate mock trend data for the last 12 weeks
    const weeks = [];
    const now = new Date();
    
    for (let i = 11; i >= 0; i--) {
      const weekStart = startOfWeek(addDays(now, -i * 7));
      const weekEnd = endOfWeek(weekStart);
      
      weeks.push({
        week: format(weekStart, 'MMM dd'),
        capacity: Math.floor(Math.random() * 20) + 80,
        allocated: Math.floor(Math.random() * 15) + 70,
        utilization: Math.floor(Math.random() * 10) + 85,
      });
    }
    
    return weeks;
  }, []);

  const getSkillColor = (skill: string) => {
    const colors = [
      '#2196F3', '#4CAF50', '#FF9800', '#9C27B0', '#F44336',
      '#00BCD4', '#8BC34A', '#FF5722', '#795548', '#607D8B',
    ];
    return colors[skill.length % colors.length];
  };

  const handleCreateAllocation = () => {
    setEditingAllocation(null);
    setIsAllocationDialogOpen(true);
  };

  const handleEditAllocation = (allocation: Allocation) => {
    setEditingAllocation(allocation);
    setIsAllocationDialogOpen(true);
  };

  const handleCreateResource = () => {
    setEditingResource(null);
    setIsResourceDialogOpen(true);
  };

  const handleEditResource = (resource: Resource) => {
    setEditingResource(resource);
    setIsResourceDialogOpen(true);
  };

  const handleSaveAllocation = async (allocationData: Partial<Allocation>) => {
    try {
      if (editingAllocation) {
        const response = await axios.put(`/api/allocations/${editingAllocation.id}`, allocationData);
        setAllocations(allocations.map(a => a.id === editingAllocation.id ? response.data : a));
        showNotification('Allocation updated successfully', 'success');
      } else {
        const response = await axios.post('/api/allocations', allocationData);
        setAllocations([...allocations, response.data]);
        showNotification('Allocation created successfully', 'success');
      }
      setIsAllocationDialogOpen(false);
    } catch (error) {
      showNotification('Failed to save allocation', 'error');
    }
  };

  const handleSaveResource = async (resourceData: Partial<Resource>) => {
    try {
      if (editingResource) {
        const response = await axios.put(`/api/resources/${editingResource.id}`, resourceData);
        setResources(resources.map(r => r.id === editingResource.id ? response.data : r));
        showNotification('Resource updated successfully', 'success');
      } else {
        const response = await axios.post('/api/resources', resourceData);
        setResources([...resources, response.data]);
        showNotification('Resource created successfully', 'success');
      }
      setIsResourceDialogOpen(false);
    } catch (error) {
      showNotification('Failed to save resource', 'error');
    }
  };

  const showNotification = (message: string, severity: 'success' | 'error' | 'warning' | 'info') => {
    setNotification({ open: true, message, severity });
  };

  const handleNotificationClose = () => {
    setNotification({ ...notification, open: false });
  };

  const calculateResourceHealth = (resource: Resource) => {
    const utilizationScore = resource.utilization;
    const availabilityScore = resource.availability;
    const workloadScore = (resource.currentWorkload / resource.maxWorkload) * 100;
    
    return Math.min(100, (utilizationScore + availabilityScore + (100 - workloadScore)) / 3);
  };

  const getOverAllocatedResources = () => {
    return resources.filter(r => r.utilization > 100);
  };

  const getUnderUtilizedResources = () => {
    return resources.filter(r => r.utilization < 50 && r.availability > 50);
  };

  const getAllocationConflicts = () => {
    const conflicts: Array<{
      resource: Resource;
      conflictingAllocations: Allocation[];
    }> = [];

    resources.forEach(resource => {
      const resourceAllocations = allocations.filter(a => a.resourceId === resource.id);
      const conflicts = resourceAllocations.filter(a => a.allocationPercentage > 100);
      
      if (conflicts.length > 0) {
        conflicts.push({
          resource,
          conflictingAllocations: conflicts,
        });
      }
    });

    return conflicts;
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Resource Allocation</Typography>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <FormControl size="small" sx={{ minWidth: 150 }}>
            <InputLabel>Timeframe</InputLabel>
            <Select
              value={timeframe}
              onChange={(e) => setTabValue(e.target.value as number)}
            >
              <MenuItem value="week">Week</MenuItem>
              <MenuItem value="month">Month</MenuItem>
              <MenuItem value="quarter">Quarter</MenuItem>
            </Select>
          </FormControl>
          <Button variant="contained" startIcon={<Add />} onClick={handleCreateAllocation}>
            New Allocation
          </Button>
          <Button variant="outlined" startIcon={<Add />} onClick={handleCreateResource}>
            Add Resource
          </Button>
          <IconButton onClick={loadData}>
            <Refresh />
          </IconButton>
        </Box>
      </Box>

      {/* Alerts */}
      {getOverAllocatedResources().length > 0 && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          <Warning sx={{ mr: 1 }} />
          {getOverAllocatedResources().length} resources are over-allocated
        </Alert>
      )}

      {getUnderUtilizedResources().length > 0 && (
        <Alert severity="info" sx={{ mb: 2 }}>
          <Info sx={{ mr: 1 }} />
          {getUnderUtilizedResources().length} resources are under-utilized
        </Alert>
      )}

      {getAllocationConflicts().length > 0 && (
        <Alert severity="error" sx={{ mb: 2 }}>
          <Error sx={{ mr: 1 }} />
          {getAllocationConflicts().length} allocation conflicts detected
        </Alert>
      )}

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={tabValue} onChange={(e, newValue) => setTabValue(newValue)}>
          <Tab label="Overview" />
          <Tab label="Resources" />
          <Tab label="Allocations" />
          <Tab label="Projects" />
          <Tab label="Analytics" />
        </Tabs>
      </Box>

      {/* Tab Content */}
      {tabValue === 0 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Resource Utilization</Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <BarChart data={utilizationData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis />
                    <RechartsTooltip />
                    <Legend />
                    <Bar dataKey="utilization" fill="#2196F3" name="Utilization %" />
                    <Bar dataKey="availability" fill="#4CAF50" name="Availability %" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Resource Health</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Total Resources</Typography>
                    <Typography variant="h6">{resources.length}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Active Resources</Typography>
                    <Typography variant="h6">
                      {resources.filter(r => r.isActive).length}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Average Utilization</Typography>
                    <Typography variant="h6">
                      {resources.length > 0 ? Math.round(resources.reduce((sum, r) => sum + r.utilization, 0) / resources.length) : 0}%
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Over-allocated</Typography>
                    <Typography variant="h6" color="error">
                      {getOverAllocatedResources().length}
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
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Resource List</Typography>
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Resource</TableCell>
                        <TableCell>Role</TableCell>
                        <TableCell>Department</TableCell>
                        <TableCell>Skills</TableCell>
                        <TableCell>Utilization</TableCell>
                        <TableCell>Availability</TableCell>
                        <TableCell>Actions</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {resources.map((resource) => (
                        <TableRow key={resource.id}>
                          <TableCell>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                              <Avatar src={resource.avatar} sx={{ width: 32, height: 32 }}>
                                {resource.name.charAt(0).toUpperCase()}
                              </Avatar>
                              <Box>
                                <Typography variant="body2">{resource.name}</Typography>
                                <Typography variant="caption" color="text.secondary">{resource.email}</Typography>
                              </Box>
                            </Box>
                          </TableCell>
                          <TableCell>{resource.role}</TableCell>
                          <TableCell>{resource.department}</TableCell>
                          <TableCell>
                            <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                              {resource.skills.slice(0, 3).map((skill, index) => (
                                <Chip key={index} label={skill} size="small" variant="outlined" />
                              ))}
                              {resource.skills.length > 3 && (
                                <Chip label={`+${resource.skills.length - 3}`} size="small" variant="outlined" />
                              )}
                            </Box>
                          </TableCell>
                          <TableCell>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                              <LinearProgress
                                variant="determinate"
                                value={Math.min(100, resource.utilization)}
                                sx={{ flex: 1 }}
                                color={resource.utilization > 100 ? 'error' : resource.utilization > 80 ? 'warning' : 'success'}
                              />
                              <Typography variant="caption">{resource.utilization}%</Typography>
                            </Box>
                          </TableCell>
                          <TableCell>
                            <Chip
                              label={`${resource.availability}%`}
                              size="small"
                              color={resource.availability > 50 ? 'success' : 'warning'}
                            />
                          </TableCell>
                          <TableCell>
                            <IconButton size="small" onClick={() => handleEditResource(resource)}>
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
        </Grid>
      )}

      {tabValue === 2 && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={8}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Allocation Overview</Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <PieChart>
                    <Pie
                      data={allocationData}
                      cx="50%"
                      cy="50%"
                      outerRadius={120}
                      fill="#8884d8"
                      dataKey="allocated"
                      label
                    >
                      {allocationData.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={getSkillColor(entry.name)} />
                      ))}
                    </Pie>
                    <RechartsTooltip />
                  </PieChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={4}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Allocation Summary</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Total Allocations</Typography>
                    <Typography variant="h6">{allocations.length}</Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Active Allocations</Typography>
                    <Typography variant="h6">
                      {allocations.filter(a => a.status === 'active').length}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="subtitle2" color="text.secondary">Average Allocation</Typography>
                    <Typography variant="h6">
                      {allocations.length > 0 ? Math.round(allocations.reduce((sum, a) => sum + a.allocationPercentage, 0) / allocations.length) : 0}%
                    </Typography>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {tabValue === 3 && (
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Project Allocation</Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <BarChart data={projectAllocationData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="name" />
                    <YAxis />
                    <RechartsTooltip />
                    <Legend />
                    <Bar dataKey="allocated" fill="#2196F3" name="Allocated %" />
                    <Bar dataKey="utilization" fill="#FF9800" name="Utilization %" />
                  </BarChart>
                </ResponsiveContainer>
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
                <Typography variant="h6" gutterBottom>Skill Distribution</Typography>
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={skillDistribution}
                      cx="50%"
                      cy="50%"
                      outerRadius={80}
                      fill="#8884d8"
                      dataKey="value"
                      label
                    >
                      {skillDistribution.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                    </Pie>
                    <RechartsTooltip />
                  </PieChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>Capacity Trend</Typography>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={capacityTrendData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="week" />
                    <YAxis />
                    <RechartsTooltip />
                    <Legend />
                    <Line type="monotone" dataKey="capacity" stroke="#2196F3" name="Capacity" />
                    <Line type="monotone" dataKey="allocated" stroke="#FF9800" name="Allocated" />
                    <Line type="monotone" dataKey="utilization" stroke="#4CAF50" name="Utilization" />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Allocation Dialog */}
      <AllocationDialog
        open={isAllocationDialogOpen}
        allocation={editingAllocation}
        resources={resources}
        projects={projects}
        onClose={() => setIsAllocationDialogOpen(false)}
        onSave={handleSaveAllocation}
      />

      {/* Resource Dialog */}
      <ResourceDialog
        open={isResourceDialogOpen}
        resource={editingResource}
        onClose={() => setIsResourceDialogOpen(false)}
        onSave={handleSaveResource}
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

// Allocation Dialog Component
const AllocationDialog: React.FC<{
  open: boolean;
  allocation: Allocation | null;
  resources: Resource[];
  projects: Project[];
  onClose: () => void;
  onSave: (allocation: Partial<Allocation>) => void;
}> = ({ open, allocation, resources, projects, onClose, onSave }) => {
  const [formData, setFormData] = useState({
    resourceId: '',
    projectId: '',
    taskId: '',
    allocationPercentage: 0,
    startDate: new Date(),
    endDate: addDays(new Date(), 7),
    estimatedHours: 0,
    priority: 'medium' as const,
    notes: '',
  });

  useEffect(() => {
    if (allocation) {
      setFormData({
        resourceId: allocation.resourceId,
        projectId: allocation.projectId,
        taskId: allocation.taskId,
        allocationPercentage: allocation.allocationPercentage,
        startDate: new Date(allocation.startDate),
        endDate: new Date(allocation.endDate),
        estimatedHours: allocation.estimatedHours,
        priority: allocation.priority,
        notes: allocation.notes || '',
      });
    } else {
      setFormData({
        resourceId: '',
        projectId: '',
        taskId: '',
        allocationPercentage: 0,
        startDate: new Date(),
        endDate: addDays(new Date(), 7),
        estimatedHours: 0,
        priority: 'medium',
        notes: '',
      });
    }
  }, [allocation]);

  const handleSave = () => {
    onSave({
      ...formData,
      startDate: formData.startDate.toISOString(),
      endDate: formData.endDate.toISOString(),
    });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{allocation ? 'Edit Allocation' : 'Create Allocation'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <FormControl fullWidth>
            <InputLabel>Resource</InputLabel>
            <Select
              value={formData.resourceId}
              onChange={(e) => setFormData({ ...formData, resourceId: e.target.value })}
            >
              {resources.map(resource => (
                <MenuItem key={resource.id} value={resource.id}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Avatar src={resource.avatar} sx={{ width: 20, height: 20 }}>
                      {resource.name.charAt(0).toUpperCase()}
                    </Avatar>
                    {resource.name}
                  </Box>
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl fullWidth>
            <InputLabel>Project</InputLabel>
            <Select
              value={formData.projectId}
              onChange={(e) => setFormData({ ...formData, projectId: e.target.value })}
            >
              {projects.map(project => (
                <MenuItem key={project.id} value={project.id}>
                  {project.name}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <TextField
            label="Task ID"
            value={formData.taskId}
            onChange={(e) => setFormData({ ...formData, taskId: e.target.value })}
            fullWidth
          />

          <Box>
            <Typography variant="subtitle2" gutterBottom>Allocation Percentage</Typography>
            <Slider
              value={formData.allocationPercentage}
              onChange={(e, value) => setFormData({ ...formData, allocationPercentage: value as number })}
              valueLabelDisplay="auto"
              min={0}
              max={100}
              marks={[
                { value: 0, label: '0%' },
                { value: 25, label: '25%' },
                { value: 50, label: '50%' },
                { value: 75, label: '75%' },
                { value: 100, label: '100%' },
              ]}
            />
          </Box>

          <Box sx={{ display: 'flex', gap: 2 }}>
            <TextField
              label="Start Date"
              type="date"
              value={format(formData.startDate, 'yyyy-MM-dd')}
              onChange={(e) => setFormData({ ...formData, startDate: new Date(e.target.value) })}
              fullWidth
            />
            
            <TextField
              label="End Date"
              type="date"
              value={format(formData.endDate, 'yyyy-MM-dd')}
              onChange={(e) => setFormData({ ...formData, endDate: new Date(e.target.value) })}
              fullWidth
            />
          </Box>

          <TextField
            label="Estimated Hours"
            type="number"
            value={formData.estimatedHours}
            onChange={(e) => setFormData({ ...formData, estimatedHours: parseFloat(e.target.value) || 0 })}
            inputProps={{ min: 0 }}
            fullWidth
          />

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

          <TextField
            label="Notes"
            value={formData.notes}
            onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
            multiline
            rows={3}
            fullWidth
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained" disabled={!formData.resourceId || !formData.projectId}>
          {allocation ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

// Resource Dialog Component
const ResourceDialog: React.FC<{
  open: boolean;
  resource: Resource | null;
  onClose: () => void;
  onSave: (resource: Partial<Resource>) => void;
}> = ({ open, resource, onClose, onSave }) => {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    role: '',
    department: '',
    skills: [] as string[],
    capacity: 40,
    hourlyRate: 0,
    maxWorkload: 40,
    location: '',
    timezone: '',
    isActive: true,
    preferences: {
      preferredHours: [9, 10, 11, 12, 13, 14, 15, 16, 17],
      preferredProjects: [],
      skills: [],
      maxWeeklyHours: 40,
      minWeeklyHours: 20,
      overtimeAllowed: false,
      remoteWorkAllowed: true,
    },
  });

  useEffect(() => {
    if (resource) {
      setFormData({
        name: resource.name,
        email: resource.email,
        role: resource.role,
        department: resource.department,
        skills: resource.skills,
        capacity: resource.capacity,
        hourlyRate: resource.hourlyRate,
        maxWorkload: resource.maxWorkload,
        location: resource.location,
        timezone: resource.timezone,
        isActive: resource.isActive,
        preferences: resource.preferences,
      });
    } else {
      setFormData({
        name: '',
        email: '',
        role: '',
        department: '',
        skills: [],
        capacity: 40,
        hourlyRate: 0,
        maxWorkload: 40,
        location: '',
        timezone: '',
        isActive: true,
        preferences: {
          preferredHours: [9, 10, 11, 12, 13, 14, 15, 16, 17],
          preferredProjects: [],
          skills: [],
          maxWeeklyHours: 40,
          minWeeklyHours: 20,
          overtimeAllowed: false,
          remoteWorkAllowed: true,
        },
      });
    }
  }, [resource]);

  const handleSave = () => {
    onSave(formData);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{resource ? 'Edit Resource' : 'Create Resource'}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <TextField
            label="Name"
            value={formData.name}
            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
            fullWidth
            required
          />
          
          <TextField
            label="Email"
            value={formData.email}
            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
            type="email"
            fullWidth
            required
          />
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl fullWidth>
              <InputLabel>Role</InputLabel>
              <Select
                value={formData.role}
                onChange={(e) => setFormData({ ...formData, role: e.target.value })}
              >
                <MenuItem value="developer">Developer</MenuItem>
                <MenuItem value="designer">Designer</MenuItem>
                <MenuItem value="manager">Manager</MenuItem>
                <MenuItem value="analyst">Analyst</MenuItem>
                <MenuItem value="tester">Tester</MenuItem>
              </Select>
            </FormControl>
            
            <FormControl fullWidth>
              <InputLabel>Department</InputLabel>
              <Select
                value={formData.department}
                onChange={(e) => setFormData({ ...formData, department: e.target.value })}
              >
                <MenuItem value="engineering">Engineering</MenuItem>
                <MenuItem value="design">Design</MenuItem>
                <MenuItem value="marketing">Marketing</MenuItem>
                <MenuItem value="sales">Sales</MenuItem>
                <MenuItem value="support">Support</MenuItem>
              </Select>
            </FormControl>
          </Box>
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <TextField
              label="Capacity (hours/week)"
              type="number"
              value={formData.capacity}
              onChange={(e) => setFormData({ ...formData, capacity: parseInt(e.target.value) || 0 })}
              inputProps={{ min: 0 }}
              fullWidth
            />
            
            <TextField
              label="Hourly Rate"
              type="number"
              value={formData.hourlyRate}
              onChange={(e) => setFormData({ ...formData, hourlyRate: parseFloat(e.target.value) || 0 })}
              inputProps={{ min: 0 }}
              fullWidth
            />
          </Box>
          
          <TextField
            label="Skills (comma-separated)"
            value={formData.skills.join(', ')}
            onChange={(e) => setFormData({ ...formData, skills: e.target.value.split(',').map(s => s.trim()).filter(s => s) })}
            fullWidth
          />
          
          <TextField
            label="Location"
            value={formData.location}
            onChange={(e) => setFormData({ ...formData, location: e.target.value })}
            fullWidth
          />
          
          <TextField
            label="Timezone"
            value={formData.timezone}
            onChange={(e) => setFormData({ ...formData, timezone: e.target.value })}
            fullWidth
          />
          
          <FormControlLabel
            control={
              <Switch
                checked={formData.isActive}
                onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
              />
            }
            label="Active"
          />
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSave} variant="contained" disabled={!formData.name.trim() || !formData.email.trim()}>
          {resource ? 'Update' : 'Create'}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default ResourceAllocation;
