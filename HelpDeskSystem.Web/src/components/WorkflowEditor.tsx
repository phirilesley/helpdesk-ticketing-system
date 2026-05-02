import React, { useState, useCallback, useRef, useEffect } from 'react';
import ReactFlow, {
  Node,
  Edge,
  addEdge,
  Connection,
  useNodesState,
  useEdgesState,
  Controls,
  MiniMap,
  Background,
  BackgroundVariant,
  NodeTypes,
  EdgeTypes,
  Panel,
} from 'reactflow';
import 'reactflow/dist/style.css';
import {
  Box,
  Drawer,
  Typography,
  IconButton,
  Button,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Divider,
  Alert,
  Snackbar,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Grid,
  Card,
  CardContent,
  Fab,
} from '@mui/material';
import {
  Save,
  PlayArrow,
  Pause,
  Stop,
  Add,
  Settings,
  Close,
  CheckCircle,
  Error,
  Info,
  Warning,
} from '@mui/icons-material';
import axios from 'axios';

// Custom node types
const nodeTypes: NodeTypes = {
  start: StartNode,
  end: EndNode,
  condition: ConditionNode,
  action: ActionNode,
  email: EmailNode,
  notification: NotificationNode,
  assignment: AssignmentNode,
  approval: ApprovalNode,
  integration: IntegrationNode,
  delay: DelayNode,
};

interface WorkflowEditorProps {
  workflowId?: string;
  onSave?: (workflow: any) => void;
  onExecute?: (workflowId: string) => void;
}

interface WorkflowNode {
  id: string;
  type: string;
  position: { x: number; y: number };
  data: {
    label: string;
    description?: string;
    config?: any;
    status?: 'idle' | 'running' | 'completed' | 'error';
  };
}

interface WorkflowExecution {
  id: string;
  status: string;
  currentStep?: string;
  steps: Array<{
    nodeId: string;
    status: string;
    startedAt: string;
    completedAt?: string;
    error?: string;
  }>;
}

// Custom Node Components
function StartNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-start">
      <div className="node-header">
        <PlayArrow style={{ color: '#4CAF50', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function EndNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-end">
      <div className="node-header">
        <Stop style={{ color: '#F44336', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function ConditionNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-condition">
      <div className="node-header">
        <Warning style={{ color: '#2196F3', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.description && (
        <div className="node-description">{data.description}</div>
      )}
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function ActionNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-action">
      <div className="node-header">
        <PlayArrow style={{ color: '#FF9800', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.description && (
        <div className="node-description">{data.description}</div>
      )}
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function EmailNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-email">
      <div className="node-header">
        <Info style={{ color: '#9C27B0', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.description && (
        <div className="node-description">{data.description}</div>
      )}
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function NotificationNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-notification">
      <div className="node-header">
        <Error style={{ color: '#E91E63', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.description && (
        <div className="node-description">{data.description}</div>
      )}
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function AssignmentNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-assignment">
      <div className="node-header">
        <CheckCircle style={{ color: '#00BCD4', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.description && (
        <div className="node-description">{data.description}</div>
      )}
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function ApprovalNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-approval">
      <div className="node-header">
        <CheckCircle style={{ color: '#8BC34A', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.description && (
        <div className="node-description">{data.description}</div>
      )}
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function IntegrationNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-integration">
      <div className="node-header">
        <Settings style={{ color: '#795548', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.description && (
        <div className="node-description">{data.description}</div>
      )}
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function DelayNode({ data }: { data: any }) {
  return (
    <div className="react-flow__node-delay">
      <div className="node-header">
        <Pause style={{ color: '#607D8B', fontSize: 20 }} />
        <span>{data.label}</span>
      </div>
      {data.description && (
        <div className="node-description">{data.description}</div>
      )}
      {data.status && (
        <div className={`node-status ${data.status}`}>
          {getStatusIcon(data.status)}
        </div>
      )}
    </div>
  );
}

function getStatusIcon(status: string) {
  switch (status) {
    case 'running':
      return <PlayArrow style={{ fontSize: 16 }} />;
    case 'completed':
      return <CheckCircle style={{ fontSize: 16, color: '#4CAF50' }} />;
    case 'error':
      return <Error style={{ fontSize: 16, color: '#F44336' }} />;
    default:
      return <Info style={{ fontSize: 16 }} />;
  }
}

const WorkflowEditor: React.FC<WorkflowEditorProps> = ({
  workflowId,
  onSave,
  onExecute,
}) => {
  const [nodes, setNodes, onNodesChange] = useNodesState([]);
  const [edges, setEdges, onEdgesChange] = useEdgesState([]);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [selectedNode, setSelectedNode] = useState<Node | null>(null);
  const [availableNodes, setAvailableNodes] = useState<any[]>([]);
  const [workflow, setWorkflow] = useState<any>(null);
  const [execution, setExecution] = useState<WorkflowExecution | null>(null);
  const [isExecuting, setIsExecuting] = useState(false);
  const [notification, setNotification] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'warning' | 'info';
  }>({ open: false, message: '', severity: 'info' });
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [workflowSettings, setWorkflowSettings] = useState({
    name: '',
    description: '',
    category: '',
    isActive: true,
  });

  const reactFlowWrapper = useRef<HTMLDivElement>(null);
  const [reactFlowInstance, setReactFlowInstance] = useState<any>(null);

  useEffect(() => {
    if (workflowId) {
      loadWorkflow(workflowId);
    }
    loadAvailableNodes();
  }, [workflowId]);

  const loadWorkflow = async (id: string) => {
    try {
      const response = await axios.get(`/api/workflow/${id}`);
      const workflowData = response.data;
      setWorkflow(workflowData);
      setNodes(workflowData.nodes || []);
      setEdges(workflowData.connections || []);
      setWorkflowSettings({
        name: workflowData.name,
        description: workflowData.description,
        category: workflowData.category,
        isActive: workflowData.isActive,
      });
    } catch (error) {
      showNotification('Failed to load workflow', 'error');
    }
  };

  const loadAvailableNodes = async () => {
    try {
      const response = await axios.get('/api/workflow/nodes');
      setAvailableNodes(response.data);
    } catch (error) {
      console.error('Failed to load available nodes:', error);
    }
  };

  const onConnect = useCallback(
    (params: Connection) => setEdges((eds) => addEdge(params, eds)),
    [setEdges]
  );

  const onDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
  }, []);

  const onDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault();

      const reactFlowBounds = reactFlowWrapper.current?.getBoundingClientRect();
      if (!reactFlowBounds || !reactFlowInstance) return;

      const nodeData = JSON.parse(event.dataTransfer.getData('application/reactflow'));
      const position = reactFlowInstance.project({
        x: event.clientX - reactFlowBounds.left,
        y: event.clientY - reactFlowBounds.top,
      });

      const newNode: Node = {
        id: `${nodeData.type}-${Date.now()}`,
        type: nodeData.type,
        position,
        data: {
          label: nodeData.name,
          description: nodeData.description,
          config: {},
        },
      };

      setNodes((nds) => nds.concat(newNode));
    },
    [reactFlowInstance, setNodes]
  );

  const onNodeClick = useCallback((event: React.MouseEvent, node: Node) => {
    setSelectedNode(node);
    setIsDrawerOpen(true);
  }, []);

  const onPaneClick = useCallback(() => {
    setSelectedNode(null);
    setIsDrawerOpen(false);
  }, []);

  const saveWorkflow = async () => {
    try {
      const workflowData = {
        ...workflowSettings,
        nodes,
        connections: edges,
        variables: [],
        settings: {},
      };

      let response;
      if (workflowId) {
        response = await axios.put(`/api/workflow/${workflowId}`, workflowData);
      } else {
        response = await axios.post('/api/workflow', workflowData);
      }

      setWorkflow(response.data);
      showNotification('Workflow saved successfully', 'success');
      onSave?.(response.data);
    } catch (error) {
      showNotification('Failed to save workflow', 'error');
    }
  };

  const executeWorkflow = async () => {
    if (!workflow?.id) {
      showNotification('Please save the workflow first', 'warning');
      return;
    }

    try {
      setIsExecuting(true);
      const response = await axios.post(`/api/workflow/${workflow.id}/execute`, {
        context: {
          tenantId: 1, // Get from auth context
          triggeredBy: 'current-user',
          inputData: {},
        },
      });

      setExecution(response.data);
      showNotification('Workflow execution started', 'success');
      onExecute?.(workflow.id);

      // Start polling for execution status
      pollExecutionStatus(response.data.id);
    } catch (error) {
      showNotification('Failed to execute workflow', 'error');
      setIsExecuting(false);
    }
  };

  const pollExecutionStatus = async (executionId: string) => {
    const pollInterval = setInterval(async () => {
      try {
        const response = await axios.get(`/api/workflow/execution/${executionId}`);
        const executionData = response.data;

        setExecution(executionData);

        // Update node statuses
        const updatedNodes = nodes.map((node) => {
          const step = executionData.steps.find((s: any) => s.nodeId === node.id);
          if (step) {
            return {
              ...node,
              data: {
                ...node.data,
                status: step.status.toLowerCase(),
              },
            };
          }
          return node;
        });

        setNodes(updatedNodes);

        // Stop polling if execution is complete
        if (['completed', 'failed', 'cancelled'].includes(executionData.status)) {
          clearInterval(pollInterval);
          setIsExecuting(false);
          showNotification(
            `Workflow ${executionData.status}`,
            executionData.status === 'completed' ? 'success' : 'error'
          );
        }
      } catch (error) {
        clearInterval(pollInterval);
        setIsExecuting(false);
      }
    }, 2000);
  };

  const showNotification = (message: string, severity: 'success' | 'error' | 'warning' | 'info') => {
    setNotification({ open: true, message, severity });
  };

  const handleNotificationClose = () => {
    setNotification({ ...notification, open: false });
  };

  const updateNodeConfig = (nodeId: string, config: any) => {
    setNodes((nds) =>
      nds.map((node) =>
        node.id === nodeId
          ? { ...node, data: { ...node.data, config } }
          : node
      )
    );
  };

  return (
    <Box sx={{ height: '100vh', position: 'relative' }}>
      <Box ref={reactFlowWrapper} sx={{ height: '100%' }}>
        <ReactFlow
          nodes={nodes}
          edges={edges}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onConnect={onConnect}
          onInit={setReactFlowInstance}
          onDrop={onDrop}
          onDragOver={onDragOver}
          onNodeClick={onNodeClick}
          onPaneClick={onPaneClick}
          nodeTypes={nodeTypes}
          fitView
        >
          <Controls />
          <MiniMap />
          <Background variant={BackgroundVariant.Dots} gap={12} size={1} />
          
          <Panel position="top-left">
            <Card sx={{ p: 2, minWidth: 200 }}>
              <Typography variant="h6" gutterBottom>
                Workflow Editor
              </Typography>
              <Grid container spacing={1}>
                <Grid item xs={12}>
                  <Button
                    fullWidth
                    variant="contained"
                    startIcon={<Save />}
                    onClick={saveWorkflow}
                    sx={{ mb: 1 }}
                  >
                    Save Workflow
                  </Button>
                </Grid>
                <Grid item xs={6}>
                  <Button
                    fullWidth
                    variant="outlined"
                    startIcon={<PlayArrow />}
                    onClick={executeWorkflow}
                    disabled={isExecuting}
                  >
                    {isExecuting ? 'Running...' : 'Execute'}
                  </Button>
                </Grid>
                <Grid item xs={6}>
                  <Button
                    fullWidth
                    variant="outlined"
                    startIcon={<Settings />}
                    onClick={() => setIsSettingsOpen(true)}
                  >
                    Settings
                  </Button>
                </Grid>
              </Grid>
            </Card>
          </Panel>

          <Panel position="top-right">
            <Card sx={{ p: 2, minWidth: 250, maxHeight: 400, overflow: 'auto' }}>
              <Typography variant="h6" gutterBottom>
                Node Palette
              </Typography>
              {availableNodes.map((nodeType) => (
                <Card
                  key={nodeType.type}
                  sx={{
                    mb: 1,
                    cursor: 'grab',
                    '&:hover': { bgcolor: 'action.hover' },
                  }}
                  draggable
                  onDragStart={(event) => {
                    event.dataTransfer.setData(
                      'application/reactflow',
                      JSON.stringify(nodeType)
                    );
                    event.dataTransfer.effectAllowed = 'move';
                  }}
                >
                  <CardContent sx={{ p: 1, '&:last-child': { pb: 1 } }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Box
                        sx={{
                          width: 20,
                          height: 20,
                          bgcolor: nodeType.color,
                          borderRadius: 1,
                        }}
                      />
                      <Typography variant="body2">{nodeType.name}</Typography>
                    </Box>
                    <Typography variant="caption" color="text.secondary">
                      {nodeType.description}
                    </Typography>
                  </CardContent>
                </Card>
              ))}
            </Card>
          </Panel>

          {execution && (
            <Panel position="bottom-left">
              <Card sx={{ p: 2, minWidth: 300 }}>
                <Typography variant="h6" gutterBottom>
                  Execution Status
                </Typography>
                <Typography variant="body2" gutterBottom>
                  Status: <strong>{execution.status}</strong>
                </Typography>
                <Typography variant="body2" gutterBottom>
                  Steps: {execution.steps.length}
                </Typography>
                {execution.currentStep && (
                  <Typography variant="body2" gutterBottom>
                    Current: {execution.currentStep}
                  </Typography>
                )}
                <Divider sx={{ my: 1 }} />
                {execution.steps.slice(-3).map((step, index) => (
                  <Box key={index} sx={{ mb: 1 }}>
                    <Typography variant="caption" color="text.secondary">
                      {step.nodeId}
                    </Typography>
                    <Typography variant="body2">
                      {step.status} - {new Date(step.startedAt).toLocaleTimeString()}
                    </Typography>
                    {step.error && (
                      <Typography variant="caption" color="error">
                        {step.error}
                      </Typography>
                    )}
                  </Box>
                ))}
              </Card>
            </Panel>
          )}
        </ReactFlow>
      </Box>

      {/* Node Configuration Drawer */}
      <Drawer
        anchor="right"
        open={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
      >
        <Box sx={{ width: 400, p: 2 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 2 }}>
            <Typography variant="h6">Node Configuration</Typography>
            <IconButton onClick={() => setIsDrawerOpen(false)}>
              <Close />
            </IconButton>
          </Box>

          {selectedNode && (
            <Box>
              <Typography variant="subtitle1" gutterBottom>
                {selectedNode.data.label}
              </Typography>
              <Typography variant="body2" color="text.secondary" gutterBottom>
                {selectedNode.data.description}
              </Typography>

              <NodeConfigForm
                node={selectedNode}
                onUpdate={(config) => updateNodeConfig(selectedNode.id, config)}
              />
            </Box>
          )}
        </Box>
      </Drawer>

      {/* Workflow Settings Dialog */}
      <Dialog
        open={isSettingsOpen}
        onClose={() => setIsSettingsOpen(false)}
        maxWidth="md"
        fullWidth
      >
        <DialogTitle>Workflow Settings</DialogTitle>
        <DialogContent>
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Workflow Name"
                value={workflowSettings.name}
                onChange={(e) =>
                  setWorkflowSettings({ ...workflowSettings, name: e.target.value })
                }
              />
            </Grid>
            <Grid item xs={12}>
              <TextField
                fullWidth
                label="Description"
                multiline
                rows={3}
                value={workflowSettings.description}
                onChange={(e) =>
                  setWorkflowSettings({ ...workflowSettings, description: e.target.value })
                }
              />
            </Grid>
            <Grid item xs={12}>
              <FormControl fullWidth>
                <InputLabel>Category</InputLabel>
                <Select
                  value={workflowSettings.category}
                  onChange={(e) =>
                    setWorkflowSettings({ ...workflowSettings, category: e.target.value })
                  }
                >
                  <MenuItem value="Ticket Management">Ticket Management</MenuItem>
                  <MenuItem value="Notification">Notification</MenuItem>
                  <MenuItem value="Integration">Integration</MenuItem>
                  <MenuItem value="Automation">Automation</MenuItem>
                </Select>
              </FormControl>
            </Grid>
          </Grid>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setIsSettingsOpen(false)}>Cancel</Button>
          <Button onClick={() => setIsSettingsOpen(false)} variant="contained">
            Save Settings
          </Button>
        </DialogActions>
      </Dialog>

      {/* Notification Snackbar */}
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

// Node Configuration Form Component
const NodeConfigForm: React.FC<{
  node: Node;
  onUpdate: (config: any) => void;
}> = ({ node, onUpdate }) => {
  const [config, setConfig] = useState(node.data.config || {});

  const handleConfigChange = (key: string, value: any) => {
    const newConfig = { ...config, [key]: value };
    setConfig(newConfig);
    onUpdate(newConfig);
  };

  const renderConfigFields = () => {
    switch (node.type) {
      case 'email':
        return (
          <>
            <TextField
              fullWidth
              label="To"
              value={config.to || ''}
              onChange={(e) => handleConfigChange('to', e.target.value)}
              sx={{ mb: 2 }}
            />
            <TextField
              fullWidth
              label="Subject"
              value={config.subject || ''}
              onChange={(e) => handleConfigChange('subject', e.target.value)}
              sx={{ mb: 2 }}
            />
            <TextField
              fullWidth
              label="Message"
              multiline
              rows={4}
              value={config.message || ''}
              onChange={(e) => handleConfigChange('message', e.target.value)}
              sx={{ mb: 2 }}
            />
          </>
        );

      case 'condition':
        return (
          <>
            <TextField
              fullWidth
              label="Condition"
              value={config.condition || ''}
              onChange={(e) => handleConfigChange('condition', e.target.value)}
              sx={{ mb: 2 }}
            />
            <FormControl fullWidth sx={{ mb: 2 }}>
              <InputLabel>Operator</InputLabel>
              <Select
                value={config.operator || 'equals'}
                onChange={(e) => handleConfigChange('operator', e.target.value)}
              >
                <MenuItem value="equals">Equals</MenuItem>
                <MenuItem value="not_equals">Not Equals</MenuItem>
                <MenuItem value="contains">Contains</MenuItem>
                <MenuItem value="greater_than">Greater Than</MenuItem>
                <MenuItem value="less_than">Less Than</MenuItem>
              </Select>
            </FormControl>
            <TextField
              fullWidth
              label="Value"
              value={config.value || ''}
              onChange={(e) => handleConfigChange('value', e.target.value)}
              sx={{ mb: 2 }}
            />
          </>
        );

      case 'delay':
        return (
          <TextField
            fullWidth
            label="Delay (seconds)"
            type="number"
            value={config.delay || 0}
            onChange={(e) => handleConfigChange('delay', parseInt(e.target.value))}
            sx={{ mb: 2 }}
          />
        );

      case 'assignment':
        return (
          <FormControl fullWidth sx={{ mb: 2 }}>
            <InputLabel>Assign To</InputLabel>
            <Select
              value={config.assignTo || ''}
              onChange={(e) => handleConfigChange('assignTo', e.target.value)}
            >
              <MenuItem value="auto">Auto-assign</MenuItem>
              <MenuItem value="least_busy">Least Busy Agent</MenuItem>
              <MenuItem value="round_robin">Round Robin</MenuItem>
            </Select>
          </FormControl>
        );

      default:
        return (
          <Typography variant="body2" color="text.secondary">
            No configuration options available for this node type.
          </Typography>
        );
    }
  };

  return <Box>{renderConfigFields()}</Box>;
};

export default WorkflowEditor;
