import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Button,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  Avatar,
  LinearProgress,
  Menu,
  MenuList,
  MenuItem as MenuItemComponent,
  ListItemIcon,
  ListItemText,
  Divider,
  Alert,
  Snackbar,
} from '@mui/material';
import {
  Add,
  Edit,
  Delete,
  Visibility,
  MoreVert,
  Assignment,
  Person,
  Schedule,
  Flag,
  CheckCircle,
  RadioButtonUnchecked,
  PlayArrow,
  Pause,
  Stop,
  ZoomIn,
  ZoomOut,
  ViewWeek,
  ViewDay,
  ViewAgenda,
} from '@mui/icons-material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import axios from 'axios';
import { format, addDays, differenceInDays, startOfDay, endOfDay, isWithinInterval } from 'date-fns';

interface GanttTask {
  id: string;
  title: string;
  description?: string;
  startDate: Date;
  endDate: Date;
  progress: number;
  status: 'not_started' | 'in_progress' | 'completed' | 'blocked' | 'cancelled';
  priority: 'low' | 'medium' | 'high' | 'critical';
  assignee?: {
    id: string;
    name: string;
    avatar?: string;
  };
  dependencies: string[];
  parentTaskId?: string;
  subtasks: GanttTask[];
  tags: string[];
  estimatedHours?: number;
  actualHours?: number;
  milestone: boolean;
  criticalPath: boolean;
  color?: string;
}

interface GanttChartProps {
  projectId?: string;
  tasks?: GanttTask[];
  onTaskUpdate?: (task: GanttTask) => void;
  onTaskCreate?: (task: Omit<GanttTask, 'id'>) => void;
  onTaskDelete?: (taskId: string) => void;
  viewMode?: 'day' | 'week' | 'month';
  showDependencies?: boolean;
  showCriticalPath?: boolean;
}

interface GanttBarProps {
  task: GanttTask;
  viewStartDate: Date;
  viewEndDate: Date;
  totalDays: number;
  onTaskClick: (task: GanttTask) => void;
  onTaskEdit: (task: GanttTask) => void;
  onTaskDelete: (taskId: string) => void;
  level: number;
}

const GanttBar: React.FC<GanttBarProps> = ({
  task,
  viewStartDate,
  viewEndDate,
  totalDays,
  onTaskClick,
  onTaskEdit,
  onTaskDelete,
  level,
}) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [isHovered, setIsHovered] = useState(false);

  const taskStart = startOfDay(task.startDate);
  const taskEnd = endOfDay(task.endDate);
  
  const startOffset = Math.max(0, differenceInDays(taskStart, viewStartDate));
  const duration = Math.min(totalDays - startOffset, differenceInDays(taskEnd, taskStart) + 1);
  
  const barWidth = (duration / totalDays) * 100;
  const barLeft = (startOffset / totalDays) * 100;

  const getStatusColor = () => {
    switch (task.status) {
      case 'completed': return '#4CAF50';
      case 'in_progress': return '#2196F3';
      case 'blocked': return '#FF9800';
      case 'cancelled': return '#9E9E9E';
      default: return '#607D8B';
    }
  };

  const getPriorityColor = () => {
    switch (task.priority) {
      case 'critical': return '#F44336';
      case 'high': return '#FF5722';
      case 'medium': return '#FF9800';
      case 'low': return '#4CAF50';
      default: return '#9E9E9E';
    }
  };

  const handleMenuClick = (event: React.MouseEvent<HTMLElement>) => {
    event.stopPropagation();
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  return (
    <Box
      sx={{
        position: 'absolute',
        left: `${barLeft}%`,
        width: `${barWidth}%`,
        height: task.milestone ? 24 : 32,
        top: level * 40,
        zIndex: isHovered ? 10 : 1,
      }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <Box
        sx={{
          height: '100%',
          backgroundColor: task.color || getStatusColor(),
          borderRadius: task.milestone ? '50%' : 4,
          border: task.criticalPath ? '2px solid #F44336' : '1px solid rgba(0,0,0,0.1)',
          cursor: 'pointer',
          position: 'relative',
          display: 'flex',
          alignItems: 'center',
          justifyContent: task.milestone ? 'center' : 'flex-start',
          px: task.milestone ? 0 : 1,
          transition: 'all 0.2s ease',
          transform: isHovered ? 'scale(1.02)' : 'scale(1)',
          boxShadow: isHovered ? '0 4px 8px rgba(0,0,0,0.2)' : '0 2px 4px rgba(0,0,0,0.1)',
        }}
        onClick={() => onTaskClick(task)}
      >
        {task.milestone ? (
          <Flag sx={{ color: 'white', fontSize: 16 }} />
        ) : (
          <>
            <Typography
              variant="caption"
              sx={{
                color: 'white',
                fontWeight: 500,
                fontSize: '11px',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
                flex: 1,
              }}
            >
              {task.title}
            </Typography>
            {task.progress > 0 && (
              <LinearProgress
                variant="determinate"
                value={task.progress}
                sx={{
                  position: 'absolute',
                  bottom: 0,
                  left: 0,
                  right: 0,
                  height: 3,
                  backgroundColor: 'rgba(255,255,255,0.3)',
                  '& .MuiLinearProgress-bar': {
                    backgroundColor: 'rgba(255,255,255,0.8)',
                  },
                }}
              />
            )}
          </>
        )}
        
        {/* Priority indicator */}
        <Box
          sx={{
            position: 'absolute',
            top: -2,
            right: -2,
            width: 8,
            height: 8,
            borderRadius: '50%',
            backgroundColor: getPriorityColor(),
            border: '1px solid white',
          }}
        />
        
        {/* Assignee avatar */}
        {task.assignee && (
          <Avatar
            src={task.assignee.avatar}
            sx={{
              position: 'absolute',
              top: -8,
              right: -8,
              width: 20,
              height: 20,
              fontSize: 10,
              border: '2px solid white',
            }}
          >
            {task.assignee.name.charAt(0).toUpperCase()}
          </Avatar>
        )}
      </Box>

      {/* Context menu */}
      <Menu
        anchorEl={anchorEl}
        open={Boolean(anchorEl)}
        onClose={handleMenuClose}
      >
        <MenuItemComponent onClick={() => { onTaskEdit(task); handleMenuClose(); }}>
          <ListItemIcon><Edit fontSize="small" /></ListItemIcon>
          <ListItemText>Edit Task</ListItemText>
        </MenuItemComponent>
        <MenuItemComponent onClick={() => { onTaskClick(task); handleMenuClose(); }}>
          <ListItemIcon><Visibility fontSize="small" /></ListItemIcon>
          <ListItemText>View Details</ListItemText>
        </MenuItemComponent>
        <Divider />
        <MenuItemComponent onClick={() => { onTaskDelete(task.id); handleMenuClose(); }} sx={{ color: 'error.main' }}>
          <ListItemIcon><Delete fontSize="small" color="error" /></ListItemIcon>
          <ListItemText>Delete Task</ListItemText>
        </MenuItemComponent>
      </Menu>

      {/* Quick action buttons */}
      {isHovered && (
        <Box
          sx={{
            position: 'absolute',
            top: -30,
            right: 0,
            display: 'flex',
            gap: 0.5,
            backgroundColor: 'white',
            borderRadius: 1,
            p: 0.5,
            boxShadow: '0 2px 8px rgba(0,0,0,0.15)',
          }}
        >
          <IconButton size="small" onClick={handleMenuClick}>
            <MoreVert fontSize="small" />
          </IconButton>
        </Box>
      )}
    </Box>
  );
};

const GanttChart: React.FC<GanttChartProps> = ({
  projectId,
  tasks: initialTasks,
  onTaskUpdate,
  onTaskCreate,
  onTaskDelete,
  viewMode = 'week',
  showDependencies = true,
  showCriticalPath = true,
}) => {
  const [tasks, setTasks] = useState<GanttTask[]>(initialTasks || []);
  const [viewStartDate, setViewStartDate] = useState(startOfDay(new Date()));
  const [viewEndDate, setViewEndDate] = useState(endOfDay(addDays(new Date(), 30)));
  const [selectedTask, setSelectedTask] = useState<GanttTask | null>(null);
  const [isTaskDialogOpen, setIsTaskDialogOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<GanttTask | null>(null);
  const [zoomLevel, setZoomLevel] = useState(1);
  const [notification, setNotification] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'warning' | 'info';
  }>({ open: false, message: '', severity: 'info' });

  useEffect(() => {
    if (projectId) {
      loadTasks();
    }
  }, [projectId]);

  useEffect(() => {
    updateViewDates();
  }, [viewMode, viewStartDate]);

  const loadTasks = async () => {
    try {
      const response = await axios.get(`/api/projects/${projectId}/gantt-tasks`);
      setTasks(response.data);
    } catch (error) {
      showNotification('Failed to load tasks', 'error');
    }
  };

  const updateViewDates = () => {
    let endDate = addDays(viewStartDate, 30);
    
    switch (viewMode) {
      case 'day':
        endDate = addDays(viewStartDate, 7);
        break;
      case 'week':
        endDate = addDays(viewStartDate, 14);
        break;
      case 'month':
        endDate = addDays(viewStartDate, 90);
        break;
    }
    
    setViewEndDate(endOfDay(endDate));
  };

  const totalDays = differenceInDays(viewEndDate, viewStartDate) + 1;

  const dateHeaders = useMemo(() => {
    const headers = [];
    const current = new Date(viewStartDate);
    
    while (current <= viewEndDate) {
      headers.push(new Date(current));
      
      switch (viewMode) {
        case 'day':
          current.setDate(current.getDate() + 1);
          break;
        case 'week':
          current.setDate(current.getDate() + 7);
          break;
        case 'month':
          current.setMonth(current.getMonth() + 1);
          break;
      }
    }
    
    return headers;
  }, [viewStartDate, viewEndDate, viewMode]);

  const flattenedTasks = useMemo(() => {
    const flatten = (tasks: GanttTask[], level = 0): (GanttTask & { level: number })[] => {
      return tasks.reduce((acc, task) => {
        acc.push({ ...task, level });
        if (task.subtasks && task.subtasks.length > 0) {
          acc.push(...flatten(task.subtasks, level + 1));
        }
        return acc;
      }, [] as (GanttTask & { level: number })[]);
    };
    
    return flatten(tasks);
  }, [tasks]);

  const criticalPathTasks = useMemo(() => {
    // Simplified critical path calculation
    return tasks.filter(task => task.criticalPath);
  }, [tasks]);

  const handleTaskClick = (task: GanttTask) => {
    setSelectedTask(task);
    setIsTaskDialogOpen(true);
  };

  const handleTaskEdit = (task: GanttTask) => {
    setEditingTask(task);
    setIsTaskDialogOpen(true);
  };

  const handleTaskCreate = () => {
    setEditingTask(null);
    setIsTaskDialogOpen(true);
  };

  const handleTaskSave = async (taskData: Partial<GanttTask>) => {
    try {
      if (editingTask) {
        const updatedTask = { ...editingTask, ...taskData };
        const response = await axios.put(`/api/projects/${projectId}/gantt-tasks/${editingTask.id}`, updatedTask);
        
        setTasks(tasks.map(t => t.id === editingTask.id ? response.data : t));
        onTaskUpdate?.(response.data);
        showNotification('Task updated successfully', 'success');
      } else {
        const newTask = {
          ...taskData,
          id: `task-${Date.now()}`,
          startDate: taskData.startDate || new Date(),
          endDate: taskData.endDate || addDays(new Date(), 7),
          progress: 0,
          status: 'not_started' as const,
          priority: 'medium' as const,
          dependencies: [],
          subtasks: [],
          tags: [],
          milestone: false,
          criticalPath: false,
        };
        
        const response = await axios.post(`/api/projects/${projectId}/gantt-tasks`, newTask);
        setTasks([...tasks, response.data]);
        onTaskCreate?.(newTask);
        showNotification('Task created successfully', 'success');
      }
      
      setIsTaskDialogOpen(false);
      setEditingTask(null);
    } catch (error) {
      showNotification('Failed to save task', 'error');
    }
  };

  const handleTaskDelete = async (taskId: string) => {
    try {
      await axios.delete(`/api/projects/${projectId}/gantt-tasks/${taskId}`);
      setTasks(tasks.filter(t => t.id !== taskId));
      onTaskDelete?.(taskId);
      showNotification('Task deleted successfully', 'success');
    } catch (error) {
      showNotification('Failed to delete task', 'error');
    }
  };

  const handleZoomIn = () => {
    setZoomLevel(prev => Math.min(prev + 0.1, 2));
  };

  const handleZoomOut = () => {
    setZoomLevel(prev => Math.max(prev - 0.1, 0.5));
  };

  const showNotification = (message: string, severity: 'success' | 'error' | 'warning' | 'info') => {
    setNotification({ open: true, message, severity });
  };

  const handleNotificationClose = () => {
    setNotification({ ...notification, open: false });
  };

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Box sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
        {/* Header */}
        <Box sx={{ p: 2, borderBottom: 1, borderColor: 'divider' }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6">Project Gantt Chart</Typography>
            <Box sx={{ display: 'flex', gap: 1 }}>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={handleTaskCreate}
              >
                Add Task
              </Button>
              <IconButton onClick={handleZoomIn}>
                <ZoomIn />
              </IconButton>
              <IconButton onClick={handleZoomOut}>
                <ZoomOut />
              </IconButton>
            </Box>
          </Box>
          
          {/* View controls */}
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <FormControl size="small" sx={{ minWidth: 120 }}>
              <InputLabel>View</InputLabel>
              <Select
                value={viewMode}
                onChange={(e) => setViewStartDate(viewStartDate)} // Force re-render
              >
                <MenuItem value="day">Day</MenuItem>
                <MenuItem value="week">Week</MenuItem>
                <MenuItem value="month">Month</MenuItem>
              </Select>
            </FormControl>
            
            <DatePicker
              label="Start Date"
              value={viewStartDate}
              onChange={(date) => date && setViewStartDate(startOfDay(date))}
              slotProps={{ textField: { size: 'small', sx: { minWidth: 150 } } }}
            />
            
            <DatePicker
              label="End Date"
              value={viewEndDate}
              onChange={(date) => date && setViewEndDate(endOfDay(date))}
              slotProps={{ textField: { size: 'small', sx: { minWidth: 150 } } }}
            />
          </Box>
        </Box>

        {/* Gantt Chart */}
        <Box sx={{ flex: 1, overflow: 'auto', position: 'relative' }}>
          {/* Timeline headers */}
          <Box sx={{ display: 'flex', position: 'sticky', top: 0, zIndex: 20, backgroundColor: 'white', borderBottom: 1, borderColor: 'divider' }}>
            <Box sx={{ width: 250, p: 2, borderRight: 1, borderColor: 'divider' }}>
              <Typography variant="subtitle2" fontWeight="bold">Task</Typography>
            </Box>
            <Box sx={{ flex: 1, display: 'flex' }}>
              {dateHeaders.map((date, index) => (
                <Box
                  key={index}
                  sx={{
                    flex: 1,
                    minWidth: 60,
                    p: 1,
                    borderRight: 1,
                    borderColor: 'divider',
                    textAlign: 'center',
                  }}
                >
                  <Typography variant="caption" display="block">
                    {format(date, viewMode === 'day' ? 'MMM dd' : viewMode === 'week' ? 'MMM dd' : 'MMM')}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {format(date, viewMode === 'day' ? 'EEE' : viewMode === 'week' ? "'Week' w" : 'yyyy')}
                  </Typography>
                </Box>
              ))}
            </Box>
          </Box>

          {/* Task rows */}
          <Box sx={{ position: 'relative', minHeight: 400 }}>
            {/* Grid lines */}
            {dateHeaders.map((date, index) => (
              <Box
                key={`grid-${index}`}
                sx={{
                  position: 'absolute',
                  top: 0,
                  bottom: 0,
                  left: `${(index / dateHeaders.length) * 100}%`,
                  width: `${100 / dateHeaders.length}%`,
                  borderRight: 1,
                  borderColor: 'divider',
                  zIndex: 0,
                }}
              />
            ))}

            {/* Task list */}
            <Box sx={{ width: 250, position: 'absolute', left: 0, top: 0, bottom: 0, backgroundColor: 'white', borderRight: 1, borderColor: 'divider', zIndex: 10 }}>
              {flattenedTasks.map((task, index) => (
                <Box
                  key={task.id}
                  sx={{
                    height: 40,
                    p: 1,
                    borderBottom: 1,
                    borderColor: 'divider',
                    display: 'flex',
                    alignItems: 'center',
                    gap: 1,
                    pl: task.level * 2 + 1,
                    backgroundColor: task.level % 2 === 0 ? 'transparent' : 'grey.50',
                  }}
                >
                  {task.milestone ? (
                    <Flag sx={{ fontSize: 16, color: 'primary.main' }} />
                  ) : (
                    <RadioButtonUnchecked sx={{ fontSize: 16, color: 'text.secondary' }} />
                  )}
                  <Box sx={{ flex: 1, minWidth: 0 }}>
                    <Typography variant="body2" noWrap>
                      {task.title}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {format(task.startDate, 'MMM dd')} - {format(task.endDate, 'MMM dd')}
                    </Typography>
                  </Box>
                  {task.assignee && (
                    <Avatar sx={{ width: 24, height: 24, fontSize: 12 }}>
                      {task.assignee.name.charAt(0).toUpperCase()}
                    </Avatar>
                  )}
                </Box>
              ))}
            </Box>

            {/* Gantt bars */}
            <Box
              sx={{
                position: 'absolute',
                left: 250,
                right: 0,
                top: 0,
                bottom: 0,
                transform: `scaleX(${zoomLevel})`,
                transformOrigin: 'left center',
              }}
            >
              {flattenedTasks.map((task) => (
                <GanttBar
                  key={task.id}
                  task={task}
                  viewStartDate={viewStartDate}
                  viewEndDate={viewEndDate}
                  totalDays={totalDays}
                  onTaskClick={handleTaskClick}
                  onTaskEdit={handleTaskEdit}
                  onTaskDelete={handleTaskDelete}
                  level={task.level}
                />
              ))}
            </Box>
          </Box>
        </Box>

        {/* Task Dialog */}
        <TaskDialog
          open={isTaskDialogOpen}
          task={editingTask}
          onClose={() => setIsTaskDialogOpen(false)}
          onSave={handleTaskSave}
        />

        {/* Task Details Dialog */}
        <TaskDetailsDialog
          open={Boolean(selectedTask) && !editingTask}
          task={selectedTask}
          onClose={() => setSelectedTask(null)}
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
    </LocalizationProvider>
  );
};

// Task Dialog Component
const TaskDialog: React.FC<{
  open: boolean;
  task: GanttTask | null;
  onClose: () => void;
  onSave: (task: Partial<GanttTask>) => void;
}> = ({ open, task, onClose, onSave }) => {
  const [formData, setFormData] = useState({
    title: '',
    description: '',
    startDate: new Date(),
    endDate: addDays(new Date(), 7),
    progress: 0,
    status: 'not_started' as const,
    priority: 'medium' as const,
    milestone: false,
    estimatedHours: 0,
  });

  useEffect(() => {
    if (task) {
      setFormData({
        title: task.title,
        description: task.description || '',
        startDate: task.startDate,
        endDate: task.endDate,
        progress: task.progress,
        status: task.status,
        priority: task.priority,
        milestone: task.milestone,
        estimatedHours: task.estimatedHours || 0,
      });
    } else {
      setFormData({
        title: '',
        description: '',
        startDate: new Date(),
        endDate: addDays(new Date(), 7),
        progress: 0,
        status: 'not_started',
        priority: 'medium',
        milestone: false,
        estimatedHours: 0,
      });
    }
  }, [task]);

  const handleSave = () => {
    onSave(formData);
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
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <FormControl fullWidth>
              <InputLabel>Status</InputLabel>
              <Select
                value={formData.status}
                onChange={(e) => setFormData({ ...formData, status: e.target.value as any })}
              >
                <MenuItem value="not_started">Not Started</MenuItem>
                <MenuItem value="in_progress">In Progress</MenuItem>
                <MenuItem value="completed">Completed</MenuItem>
                <MenuItem value="blocked">Blocked</MenuItem>
                <MenuItem value="cancelled">Cancelled</MenuItem>
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
          
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
            <TextField
              label="Progress (%)"
              type="number"
              value={formData.progress}
              onChange={(e) => setFormData({ ...formData, progress: parseInt(e.target.value) || 0 })}
              inputProps={{ min: 0, max: 100 }}
              sx={{ width: 150 }}
            />
            
            <TextField
              label="Estimated Hours"
              type="number"
              value={formData.estimatedHours}
              onChange={(e) => setFormData({ ...formData, estimatedHours: parseFloat(e.target.value) || 0 })}
              inputProps={{ min: 0 }}
              sx={{ width: 150 }}
            />
          </Box>
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

// Task Details Dialog Component
const TaskDetailsDialog: React.FC<{
  open: boolean;
  task: GanttTask | null;
  onClose: () => void;
}> = ({ open, task, onClose }) => {
  if (!task) return null;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{task.title}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          {task.description && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>Description</Typography>
              <Typography variant="body2">{task.description}</Typography>
            </Box>
          )}
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Status</Typography>
              <Chip
                label={task.status.replace('_', ' ').toUpperCase()}
                color={task.status === 'completed' ? 'success' : task.status === 'blocked' ? 'warning' : 'default'}
                size="small"
              />
            </Box>
            
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Priority</Typography>
              <Chip
                label={task.priority.toUpperCase()}
                color={task.priority === 'critical' ? 'error' : task.priority === 'high' ? 'warning' : 'default'}
                size="small"
              />
            </Box>
          </Box>
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Start Date</Typography>
              <Typography variant="body2">{format(task.startDate, 'PPP')}</Typography>
            </Box>
            
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>End Date</Typography>
              <Typography variant="body2">{format(task.endDate, 'PPP')}</Typography>
            </Box>
          </Box>
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Progress</Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <LinearProgress
                  variant="determinate"
                  value={task.progress}
                  sx={{ flex: 1 }}
                />
                <Typography variant="body2">{task.progress}%</Typography>
              </Box>
            </Box>
            
            {task.estimatedHours && (
              <Box sx={{ flex: 1 }}>
                <Typography variant="subtitle2" gutterBottom>Hours</Typography>
                <Typography variant="body2">
                  {task.actualHours ? `${task.actualHours} / ${task.estimatedHours}` : `Est: ${task.estimatedHours}`}
                </Typography>
              </Box>
            )}
          </Box>
          
          {task.assignee && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>Assignee</Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Avatar src={task.assignee.avatar} sx={{ width: 24, height: 24 }}>
                  {task.assignee.name.charAt(0).toUpperCase()}
                </Avatar>
                <Typography variant="body2">{task.assignee.name}</Typography>
              </Box>
            </Box>
          )}
          
          {task.tags.length > 0 && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>Tags</Typography>
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                {task.tags.map((tag, index) => (
                  <Chip key={index} label={tag} size="small" variant="outlined" />
                ))}
              </Box>
            </Box>
          )}
          
          {task.milestone && (
            <Alert severity="info">
              <Flag sx={{ mr: 1 }} />
              This is a milestone task
            </Alert>
          )}
          
          {task.criticalPath && (
            <Alert severity="warning">
              <Flag sx={{ mr: 1 }} />
              This task is on the critical path
            </Alert>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
};

export default GanttChart;
