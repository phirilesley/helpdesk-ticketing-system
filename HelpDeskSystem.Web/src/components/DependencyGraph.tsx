import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  Avatar,
  Badge,
  Menu,
  MenuList,
  MenuItem as MenuItemComponent,
  ListItemIcon,
  ListItemText,
  Divider,
  TextField,
  Alert,
  Snackbar,
  LinearProgress,
} from '@mui/material';
import {
  ZoomIn,
  ZoomOut,
  CenterFocusStrong,
  FilterList,
  MoreVert,
  Link,
  LinkOff,
  Add,
  Edit,
  Delete,
  Visibility,
  Refresh,
  Fullscreen,
  FullscreenExit,
} from '@mui/icons-material';
import { ForceGraph2D } from 'react-force-graph-2d';
import axios from 'axios';

interface TicketNode {
  id: string;
  title: string;
  description?: string;
  status: string;
  priority: string;
  category: string;
  assignee?: {
    id: string;
    name: string;
    avatar?: string;
  };
  createdAt: string;
  updatedAt: string;
  dueDate?: string;
  estimatedHours?: number;
  actualHours?: number;
  tags: string[];
  customFields: Record<string, any>;
}

interface TicketDependency {
  id: string;
  sourceTicketId: string;
  targetTicketId: string;
  type: 'blocks' | 'depends_on' | 'relates_to' | 'duplicates' | 'parent_child';
  description?: string;
  createdAt: string;
  createdBy: string;
}

interface DependencyGraphProps {
  projectId?: string;
  ticketId?: string;
  height?: number;
  showControls?: boolean;
  interactive?: boolean;
}

const DependencyGraph: React.FC<DependencyGraphProps> = ({
  projectId,
  ticketId,
  height = 600,
  showControls = true,
  interactive = true,
}) => {
  const graphRef = useRef<any>();
  const [tickets, setTickets] = useState<TicketNode[]>([]);
  const [dependencies, setDependencies] = useState<TicketDependency[]>([]);
  const [filteredTickets, setFilteredTickets] = useState<TicketNode[]>([]);
  const [filteredDependencies, setFilteredDependencies] = useState<TicketDependency[]>([]);
  const [selectedTicket, setSelectedTicket] = useState<TicketNode | null>(null);
  const [selectedDependency, setSelectedDependency] = useState<TicketDependency | null>(null);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [zoomLevel, setZoomLevel] = useState(1);
  const [filterMenu, setFilterMenu] = useState<null | HTMLElement>(null);
  const [contextMenu, setContextMenu] = useState<null | HTMLElement>(null);
  const [contextTicket, setContextTicket] = useState<TicketNode | null>(null);
  const [isDependencyDialogOpen, setIsDependencyDialogOpen] = useState(false);
  const [isTicketDialogOpen, setIsTicketDialogOpen] = useState(false);
  const [notification, setNotification] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'warning' | 'info';
  }>({ open: false, message: '', severity: 'info' });

  // Filter states
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [priorityFilter, setPriorityFilter] = useState<string>('all');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');
  const [assigneeFilter, setAssigneeFilter] = useState<string>('all');
  const [dependencyTypeFilter, setDependencyTypeFilter] = useState<string>('all');

  useEffect(() => {
    loadData();
  }, [projectId, ticketId]);

  useEffect(() => {
    applyFilters();
  }, [tickets, dependencies, statusFilter, priorityFilter, categoryFilter, assigneeFilter, dependencyTypeFilter]);

  const loadData = async () => {
    try {
      const [ticketsResponse, dependenciesResponse] = await Promise.all([
        projectId 
          ? axios.get(`/api/projects/${projectId}/tickets`)
          : ticketId
          ? axios.get(`/api/tickets/${ticketId}/related`)
          : axios.get('/api/tickets'),
        projectId
          ? axios.get(`/api/projects/${projectId}/dependencies`)
          : ticketId
          ? axios.get(`/api/tickets/${ticketId}/dependencies`)
          : axios.get('/api/dependencies'),
      ]);

      setTickets(ticketsResponse.data);
      setDependencies(dependenciesResponse.data);
    } catch (error) {
      showNotification('Failed to load data', 'error');
    }
  };

  const applyFilters = () => {
    let filteredTickets = [...tickets];
    let filteredDependencies = [...dependencies];

    // Apply ticket filters
    if (statusFilter !== 'all') {
      filteredTickets = filteredTickets.filter(t => t.status === statusFilter);
    }
    if (priorityFilter !== 'all') {
      filteredTickets = filteredTickets.filter(t => t.priority === priorityFilter);
    }
    if (categoryFilter !== 'all') {
      filteredTickets = filteredTickets.filter(t => t.category === categoryFilter);
    }
    if (assigneeFilter !== 'all') {
      filteredTickets = filteredTickets.filter(t => t.assignee?.id === assigneeFilter);
    }

    // Apply dependency filters
    if (dependencyTypeFilter !== 'all') {
      filteredDependencies = filteredDependencies.filter(d => d.type === dependencyTypeFilter);
    }

    // Only keep dependencies where both tickets are in the filtered set
    const ticketIds = new Set(filteredTickets.map(t => t.id));
    filteredDependencies = filteredDependencies.filter(d => 
      ticketIds.has(d.sourceTicketId) && ticketIds.has(d.targetTicketId)
    );

    setFilteredTickets(filteredTickets);
    setFilteredDependencies(filteredDependencies);
  };

  const graphData = useMemo(() => {
    const nodes = filteredTickets.map(ticket => ({
      id: ticket.id,
      name: ticket.title,
      color: getNodeColor(ticket),
      size: getNodeSize(ticket),
      ticket,
    }));

    const links = filteredDependencies.map(dep => ({
      source: dep.sourceTicketId,
      target: dep.targetTicketId,
      type: dep.type,
      color: getLinkColor(dep.type),
      width: getLinkWidth(dep.type),
      dependency: dep,
    }));

    return { nodes, links };
  }, [filteredTickets, filteredDependencies]);

  const getNodeColor = (ticket: TicketNode) => {
    const statusColors = {
      'open': '#2196F3',
      'in_progress': '#FF9800',
      'resolved': '#4CAF50',
      'closed': '#9E9E9E',
      'blocked': '#F44336',
      'escalated': '#9C27B0',
    };
    return statusColors[ticket.status as keyof typeof statusColors] || '#607D8B';
  };

  const getNodeSize = (ticket: TicketNode) => {
    const baseSize = 8;
    const priorityMultiplier = {
      'low': 0.8,
      'medium': 1,
      'high': 1.2,
      'critical': 1.5,
    };
    return baseSize * (priorityMultiplier[ticket.priority as keyof typeof priorityMultiplier] || 1);
  };

  const getLinkColor = (type: string) => {
    const colors = {
      'blocks': '#F44336',
      'depends_on': '#FF9800',
      'relates_to': '#2196F3',
      'duplicates': '#9C27B0',
      'parent_child': '#4CAF50',
    };
    return colors[type as keyof typeof colors] || '#607D8B';
  };

  const getLinkWidth = (type: string) => {
    const widths = {
      'blocks': 3,
      'depends_on': 2,
      'relates_to': 1,
      'duplicates': 2,
      'parent_child': 2,
    };
    return widths[type as keyof typeof widths] || 1;
  };

  const handleNodeClick = useCallback((node: any) => {
    setSelectedTicket(node.ticket);
    setIsTicketDialogOpen(true);
  }, []);

  const handleLinkClick = useCallback((link: any) => {
    setSelectedDependency(link.dependency);
    setIsDependencyDialogOpen(true);
  }, []);

  const handleNodeRightClick = useCallback((node: any, event: MouseEvent) => {
    event.preventDefault();
    setContextTicket(node.ticket);
    setContextMenu(event as any);
  }, []);

  const handleZoomIn = () => {
    if (graphRef.current) {
      const currentZoom = graphRef.current.zoom();
      const newZoom = Math.min(currentZoom * 1.2, 5);
      graphRef.current.zoom(newZoom, 400);
      setZoomLevel(newZoom);
    }
  };

  const handleZoomOut = () => {
    if (graphRef.current) {
      const currentZoom = graphRef.current.zoom();
      const newZoom = Math.max(currentZoom / 1.2, 0.1);
      graphRef.current.zoom(newZoom, 400);
      setZoomLevel(newZoom);
    }
  };

  const handleCenter = () => {
    if (graphRef.current) {
      graphRef.current.zoomToFit(400);
    }
  };

  const handleRefresh = () => {
    loadData();
  };

  const handleCreateDependency = () => {
    if (contextTicket) {
      // Open dependency creation dialog
      setIsDependencyDialogOpen(true);
    }
    setContextMenu(null);
  };

  const handleDeleteDependency = async (dependencyId: string) => {
    try {
      await axios.delete(`/api/dependencies/${dependencyId}`);
      setDependencies(dependencies.filter(d => d.id !== dependencyId));
      showNotification('Dependency deleted successfully', 'success');
    } catch (error) {
      showNotification('Failed to delete dependency', 'error');
    }
    setContextMenu(null);
  };

  const showNotification = (message: string, severity: 'success' | 'error' | 'warning' | 'info') => {
    setNotification({ open: true, message, severity });
  };

  const handleNotificationClose = () => {
    setNotification({ ...notification, open: false });
  };

  const uniqueAssignees = useMemo(() => {
    const assignees = new Map<string, { id: string; name: string; avatar?: string }>();
    tickets.forEach(ticket => {
      if (ticket.assignee) {
        assignees.set(ticket.assignee.id, ticket.assignee);
      }
    });
    return Array.from(assignees.values());
  }, [tickets]);

  const uniqueCategories = useMemo(() => {
    return [...new Set(tickets.map(t => t.category))];
  }, [tickets]);

  const uniqueStatuses = useMemo(() => {
    return [...new Set(tickets.map(t => t.status))];
  }, [tickets]);

  const uniquePriorities = useMemo(() => {
    return [...new Set(tickets.map(t => t.priority))];
  }, [tickets]);

  return (
    <Box sx={{ height: isFullscreen ? '100vh' : height, position: 'relative' }}>
      {/* Controls */}
      {showControls && (
        <Box sx={{ position: 'absolute', top: 16, left: 16, zIndex: 10, display: 'flex', gap: 1 }}>
          <Card sx={{ p: 1 }}>
            <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
              <Tooltip title="Zoom In">
                <IconButton size="small" onClick={handleZoomIn}>
                  <ZoomIn />
                </IconButton>
              </Tooltip>
              <Tooltip title="Zoom Out">
                <IconButton size="small" onClick={handleZoomOut}>
                  <ZoomOut />
                </IconButton>
              </Tooltip>
              <Tooltip title="Center">
                <IconButton size="small" onClick={handleCenter}>
                  <CenterFocusStrong />
                </IconButton>
              </Tooltip>
              <Tooltip title="Refresh">
                <IconButton size="small" onClick={handleRefresh}>
                  <Refresh />
                </IconButton>
              </Tooltip>
              <Tooltip title="Filters">
                <IconButton size="small" onClick={(e) => setFilterMenu(e.currentTarget)}>
                  <FilterList />
                </IconButton>
              </Tooltip>
              <Tooltip title={isFullscreen ? "Exit Fullscreen" : "Fullscreen"}>
                <IconButton size="small" onClick={() => setIsFullscreen(!isFullscreen)}>
                  {isFullscreen ? <FullscreenExit /> : <Fullscreen />}
                </IconButton>
              </Tooltip>
            </Box>
          </Card>

          {/* Legend */}
          <Card sx={{ p: 2, minWidth: 200 }}>
            <Typography variant="subtitle2" gutterBottom>Legend</Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ width: 12, height: 12, borderRadius: '50%', bgcolor: '#2196F3' }} />
                <Typography variant="caption">Open</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ width: 12, height: 12, borderRadius: '50%', bgcolor: '#FF9800' }} />
                <Typography variant="caption">In Progress</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ width: 12, height: 12, borderRadius: '50%', bgcolor: '#4CAF50' }} />
                <Typography variant="caption">Resolved</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ width: 12, height: 12, borderRadius: '50%', bgcolor: '#F44336' }} />
                <Typography variant="caption">Blocked</Typography>
              </Box>
              <Divider />
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ width: 20, height: 2, bgcolor: '#F44336' }} />
                <Typography variant="caption">Blocks</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ width: 20, height: 2, bgcolor: '#FF9800' }} />
                <Typography variant="caption">Depends On</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Box sx={{ width: 20, height: 2, bgcolor: '#2196F3' }} />
                <Typography variant="caption">Relates To</Typography>
              </Box>
            </Box>
          </Card>

          {/* Statistics */}
          <Card sx={{ p: 2, minWidth: 200 }}>
            <Typography variant="subtitle2" gutterBottom>Statistics</Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Typography variant="caption">Tickets: {filteredTickets.length}</Typography>
              <Typography variant="caption">Dependencies: {filteredDependencies.length}</Typography>
              <Typography variant="caption">Blocked: {filteredDependencies.filter(d => d.type === 'blocks').length}</Typography>
              <Typography variant="caption">Critical Path: {calculateCriticalPath().length}</Typography>
            </Box>
          </Card>
        </Box>
      )}

      {/* Force Graph */}
      <ForceGraph2D
        ref={graphRef}
        graphData={graphData}
        nodeLabel="name"
        nodeAutoColorBy="group"
        nodeCanvasObject={(node: any, ctx, globalScale) => {
          const label = node.name;
          const fontSize = 12 / globalScale;
          ctx.font = `${fontSize}px Sans-Serif`;
          ctx.textAlign = 'center';
          ctx.textBaseline = 'middle';
          
          // Draw node circle
          ctx.beginPath();
          ctx.arc(node.x, node.y, node.size, 0, 2 * Math.PI);
          ctx.fillStyle = node.color;
          ctx.fill();
          ctx.strokeStyle = '#fff';
          ctx.lineWidth = 1;
          ctx.stroke();
          
          // Draw label
          ctx.fillStyle = '#333';
          ctx.fillText(label, node.x, node.y + node.size + fontSize);
        }}
        linkWidth="width"
        linkColor="color"
        linkDirectionalParticles={2}
        linkDirectionalParticleSpeed={0.005}
        onNodeClick={interactive ? handleNodeClick : undefined}
        onLinkClick={interactive ? handleLinkClick : undefined}
        onNodeRightClick={interactive ? handleNodeRightClick : undefined}
        enableNodeDrag={!isFullscreen}
        enableZoomInteraction={!isFullscreen}
        cooldownTicks={100}
        d3AlphaDecay={0.02}
        d3VelocityDecay={0.3}
      />

      {/* Filter Menu */}
      <Menu
        anchorEl={filterMenu}
        open={Boolean(filterMenu)}
        onClose={() => setFilterMenu(null)}
        PaperProps={{ sx: { minWidth: 250 } }}
      >
        <MenuItemComponent disabled>
          <Typography variant="subtitle2">Filters</Typography>
        </MenuItemComponent>
        <Divider />
        
        <Box sx={{ p: 2 }}>
          <FormControl fullWidth size="small" sx={{ mb: 2 }}>
            <InputLabel>Status</InputLabel>
            <Select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
            >
              <MenuItem value="all">All Statuses</MenuItem>
              {uniqueStatuses.map(status => (
                <MenuItem key={status} value={status}>{status}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl fullWidth size="small" sx={{ mb: 2 }}>
            <InputLabel>Priority</InputLabel>
            <Select
              value={priorityFilter}
              onChange={(e) => setPriorityFilter(e.target.value)}
            >
              <MenuItem value="all">All Priorities</MenuItem>
              {uniquePriorities.map(priority => (
                <MenuItem key={priority} value={priority}>{priority}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl fullWidth size="small" sx={{ mb: 2 }}>
            <InputLabel>Category</InputLabel>
            <Select
              value={categoryFilter}
              onChange={(e) => setCategoryFilter(e.target.value)}
            >
              <MenuItem value="all">All Categories</MenuItem>
              {uniqueCategories.map(category => (
                <MenuItem key={category} value={category}>{category}</MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl fullWidth size="small" sx={{ mb: 2 }}>
            <InputLabel>Assignee</InputLabel>
            <Select
              value={assigneeFilter}
              onChange={(e) => setAssigneeFilter(e.target.value)}
            >
              <MenuItem value="all">All Assignees</MenuItem>
              {uniqueAssignees.map(assignee => (
                <MenuItem key={assignee.id} value={assignee.id}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Avatar src={assignee.avatar} sx={{ width: 20, height: 20 }}>
                      {assignee.name.charAt(0).toUpperCase()}
                    </Avatar>
                    {assignee.name}
                  </Box>
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl fullWidth size="small">
            <InputLabel>Dependency Type</InputLabel>
            <Select
              value={dependencyTypeFilter}
              onChange={(e) => setDependencyTypeFilter(e.target.value)}
            >
              <MenuItem value="all">All Types</MenuItem>
              <MenuItem value="blocks">Blocks</MenuItem>
              <MenuItem value="depends_on">Depends On</MenuItem>
              <MenuItem value="relates_to">Relates To</MenuItem>
              <MenuItem value="duplicates">Duplicates</MenuItem>
              <MenuItem value="parent_child">Parent/Child</MenuItem>
            </Select>
          </FormControl>
        </Box>
      </Menu>

      {/* Context Menu */}
      <Menu
        anchorEl={contextMenu}
        open={Boolean(contextMenu)}
        onClose={() => setContextMenu(null)}
      >
        {contextTicket && (
          <>
            <MenuItemComponent onClick={() => { setSelectedTicket(contextTicket); setIsTicketDialogOpen(true); setContextMenu(null); }}>
              <ListItemIcon><Visibility fontSize="small" /></ListItemIcon>
              <ListItemText>View Details</ListItemText>
            </MenuItemComponent>
            <MenuItemComponent onClick={handleCreateDependency}>
              <ListItemIcon><Link fontSize="small" /></ListItemIcon>
              <ListItemText>Create Dependency</ListItemText>
            </MenuItemComponent>
            <Divider />
            <MenuItemComponent onClick={() => setContextMenu(null)}>
              <ListItemIcon><Edit fontSize="small" /></ListItemIcon>
              <ListItemText>Edit Ticket</ListItemText>
            </MenuItemComponent>
          </>
        )}
      </Menu>

      {/* Ticket Details Dialog */}
      <TicketDetailsDialog
        open={isTicketDialogOpen}
        ticket={selectedTicket}
        onClose={() => setIsTicketDialogOpen(false)}
      />

      {/* Dependency Details Dialog */}
      <DependencyDetailsDialog
        open={isDependencyDialogOpen}
        dependency={selectedDependency}
        onClose={() => setIsDependencyDialogOpen(false)}
        onDelete={handleDeleteDependency}
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

  function calculateCriticalPath(): string[] {
    // Simplified critical path calculation
    // In a real implementation, this would use proper graph algorithms
    const blockedTickets = new Set<string>();
    filteredDependencies
      .filter(d => d.type === 'blocks')
      .forEach(d => blockedTickets.add(d.targetTicketId));
    
    return Array.from(blockedTickets);
  }
};

// Ticket Details Dialog Component
const TicketDetailsDialog: React.FC<{
  open: boolean;
  ticket: TicketNode | null;
  onClose: () => void;
}> = ({ open, ticket, onClose }) => {
  if (!ticket) return null;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{ticket.title}</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          {ticket.description && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>Description</Typography>
              <Typography variant="body2">{ticket.description}</Typography>
            </Box>
          )}
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Status</Typography>
              <Chip
                label={ticket.status.replace('_', ' ').toUpperCase()}
                color={ticket.status === 'resolved' ? 'success' : ticket.status === 'blocked' ? 'error' : 'default'}
                size="small"
              />
            </Box>
            
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Priority</Typography>
              <Chip
                label={ticket.priority.toUpperCase()}
                color={ticket.priority === 'critical' ? 'error' : ticket.priority === 'high' ? 'warning' : 'default'}
                size="small"
              />
            </Box>
          </Box>
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Category</Typography>
              <Typography variant="body2">{ticket.category}</Typography>
            </Box>
            
            {ticket.assignee && (
              <Box sx={{ flex: 1 }}>
                <Typography variant="subtitle2" gutterBottom>Assignee</Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Avatar src={ticket.assignee.avatar} sx={{ width: 24, height: 24 }}>
                    {ticket.assignee.name.charAt(0).toUpperCase()}
                  </Avatar>
                  <Typography variant="body2">{ticket.assignee.name}</Typography>
                </Box>
              </Box>
            )}
          </Box>
          
          <Box sx={{ display: 'flex', gap: 2 }}>
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Created</Typography>
              <Typography variant="body2">{new Date(ticket.createdAt).toLocaleDateString()}</Typography>
            </Box>
            
            <Box sx={{ flex: 1 }}>
              <Typography variant="subtitle2" gutterBottom>Updated</Typography>
              <Typography variant="body2">{new Date(ticket.updatedAt).toLocaleDateString()}</Typography>
            </Box>
          </Box>
          
          {ticket.dueDate && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>Due Date</Typography>
              <Typography variant="body2">{new Date(ticket.dueDate).toLocaleDateString()}</Typography>
            </Box>
          )}
          
          {ticket.tags.length > 0 && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>Tags</Typography>
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                {ticket.tags.map((tag, index) => (
                  <Chip key={index} label={tag} size="small" variant="outlined" />
                ))}
              </Box>
            </Box>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
};

// Dependency Details Dialog Component
const DependencyDetailsDialog: React.FC<{
  open: boolean;
  dependency: TicketDependency | null;
  onClose: () => void;
  onDelete: (id: string) => void;
}> = ({ open, dependency, onClose, onDelete }) => {
  if (!dependency) return null;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Dependency Details</DialogTitle>
      <DialogContent>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, pt: 1 }}>
          <Box>
            <Typography variant="subtitle2" gutterBottom>Type</Typography>
            <Chip
              label={dependency.type.replace('_', ' ').toUpperCase()}
              color="primary"
              size="small"
            />
          </Box>
          
          <Box>
            <Typography variant="subtitle2" gutterBottom>Source Ticket</Typography>
            <Typography variant="body2">{dependency.sourceTicketId}</Typography>
          </Box>
          
          <Box>
            <Typography variant="subtitle2" gutterBottom>Target Ticket</Typography>
            <Typography variant="body2">{dependency.targetTicketId}</Typography>
          </Box>
          
          {dependency.description && (
            <Box>
              <Typography variant="subtitle2" gutterBottom>Description</Typography>
              <Typography variant="body2">{dependency.description}</Typography>
            </Box>
          )}
          
          <Box>
            <Typography variant="subtitle2" gutterBottom>Created</Typography>
            <Typography variant="body2">{new Date(dependency.createdAt).toLocaleDateString()}</Typography>
          </Box>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
        <Button onClick={() => onDelete(dependency.id)} color="error" variant="outlined">
          Delete Dependency
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default DependencyGraph;
