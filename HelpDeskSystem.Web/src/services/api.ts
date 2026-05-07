import axios from 'axios';

export interface DashboardStats {
  totalTickets: number;
  openTickets: number;
  resolvedTickets: number;
  averageResolutionTime: number;
}

export interface Ticket {
  id: number;
  ticketNumber: string;
  title: string;
  description: string;
  status: string;
  priority: string;
  category: string;
  createdAt: string;
  updatedAt: string;
  createdByUserId?: number;
  assignedToUserId?: number | null;
}

export interface TicketComment {
  id: number;
  content: string;
  createdAt: string;
  senderUserId: number;
  isInternal: boolean;
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  priorityId: number;
  categoryId: number;
}

export interface TicketMetadataOption {
  id: number;
  name: string;
}

export interface AssignableUser {
  id: number;
  email: string;
  fullName: string;
}

export interface KnowledgeBaseCategory {
  id: number;
  tenantId: number;
  name: string;
  description: string;
  isPublic: boolean;
  displayOrder: number;
}

export interface KnowledgeBaseArticle {
  id: number;
  tenantId: number;
  categoryId: number;
  categoryName: string;
  slug: string;
  title: string;
  summary: string;
  body: string;
  searchKeywords: string;
  isPublished: boolean;
  publishedAtUtc?: string | null;
  versionCount: number;
  helpfulCount: number;
  unhelpfulCount: number;
}

const statusMap: Record<number, string> = {
  1: 'New',
  2: 'InProgress',
  3: 'Waiting',
  4: 'Resolved',
  5: 'Closed',
  6: 'Reopened',
  7: 'Escalated'
};

const normalizeStatus = (status: string | number | null | undefined): string => {
  if (typeof status === 'number') {
    return statusMap[status] ?? 'Unknown';
  }

  if (!status) {
    return 'Unknown';
  }

  return status;
};

const normalizeTicket = (raw: any): Ticket => ({
  id: raw.id,
  ticketNumber: raw.ticketNumber,
  title: raw.title,
  description: raw.description,
  status: normalizeStatus(raw.status),
  priority: raw.priorityName ?? raw.priority ?? 'Unknown',
  category: raw.categoryName ?? raw.category ?? 'Unknown',
  createdAt: raw.createdAtUtc ?? raw.createdAt,
  updatedAt: raw.updatedAtUtc ?? raw.updatedAt ?? raw.createdAtUtc ?? raw.createdAt,
  createdByUserId: raw.createdByUserId,
  assignedToUserId: raw.assignedToUserId ?? null
});

export const getDashboardStats = async (): Promise<DashboardStats> => {
  const response = await axios.get('/api/tickets');
  const rawTickets = Array.isArray(response.data) ? response.data : response.data?.data ?? [];
  const tickets = rawTickets.map(normalizeTicket);

  const totalTickets = tickets.length;
  const openTickets = tickets.filter((t) => t.status !== 'Closed').length;
  const resolvedTickets = tickets.filter((t) => t.status === 'Resolved' || t.status === 'Closed').length;

  const resolvedDurationsHours = tickets
    .filter((t) => t.status === 'Closed' && !!t.updatedAt && !!t.createdAt)
    .map((t) => {
      const created = new Date(t.createdAt).getTime();
      const closed = new Date(t.updatedAt).getTime();
      if (Number.isNaN(created) || Number.isNaN(closed) || closed <= created) {
        return 0;
      }

      return (closed - created) / 3600000;
    })
    .filter((hours) => hours > 0);

  const averageResolutionTime = resolvedDurationsHours.length > 0
    ? resolvedDurationsHours.reduce((sum, hours) => sum + hours, 0) / resolvedDurationsHours.length
    : 0;

  return {
    totalTickets,
    openTickets,
    resolvedTickets,
    averageResolutionTime
  };
};

export const getRecentTickets = async (): Promise<Ticket[]> => {
  const response = await axios.get('/api/tickets');
  const rawTickets = Array.isArray(response.data) ? response.data : response.data?.data ?? [];

  return rawTickets
    .map(normalizeTicket)
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 10);
};

export const createTicket = async (ticketData: CreateTicketRequest): Promise<Ticket> => {
  const response = await axios.post('/api/tickets', ticketData);
  return normalizeTicket(response.data);
};

export const getTickets = async (params: {
  page?: number;
  status?: string;
  search?: string;
}): Promise<{ data: Ticket[]; total: number }> => {
  const response = await axios.get('/api/tickets', { params });
  const rawTickets = Array.isArray(response.data) ? response.data : response.data?.data ?? [];
  const tickets = rawTickets.map(normalizeTicket);

  return { data: tickets, total: tickets.length };
};

export const getTicket = async (id: number): Promise<Ticket> => {
  const response = await axios.get(`/api/tickets/${id}`);
  return normalizeTicket(response.data);
};

export const getTicketComments = async (ticketId: number): Promise<TicketComment[]> => {
  const response = await axios.get(`/api/tickets/${ticketId}/comments`);
  const comments = Array.isArray(response.data) ? response.data : [];
  return comments.map((message: any) => ({
    id: message.id,
    content: message.message ?? '',
    createdAt: message.createdAtUtc ?? new Date().toISOString(),
    senderUserId: message.senderUserId ?? 0,
    isInternal: !!message.isInternalNote
  }));
};

export const addComment = async (
  ticketId: number,
  comment: {
    content: string;
    isInternal: boolean;
  }
): Promise<TicketComment> => {
  const response = await axios.post(`/api/tickets/${ticketId}/comments`, comment);
  return {
    id: response.data.id,
    content: response.data.message ?? comment.content,
    createdAt: response.data.createdAtUtc ?? new Date().toISOString(),
    senderUserId: response.data.senderUserId ?? 0,
    isInternal: !!response.data.isInternalNote
  };
};

export const assignTicket = async (ticketId: number, userId: number, reason: string): Promise<void> => {
  await axios.post(`/api/tickets/${ticketId}/assign`, { userId, reason });
};

export const changeTicketStatus = async (
  ticketId: number,
  status: 'New' | 'InProgress' | 'Waiting' | 'Resolved' | 'Closed' | 'Reopened' | 'Escalated',
  comment: string
): Promise<void> => {
  await axios.post(`/api/tickets/${ticketId}/status`, { status, comment });
};

export const getAssignableAgents = async (): Promise<AssignableUser[]> => {
  const response = await axios.get('/api/users/assignable-agents');
  return Array.isArray(response.data) ? response.data : [];
};

export const getTicketCategories = async (): Promise<TicketMetadataOption[]> => {
  const response = await axios.get('/api/tickets/metadata/categories');
  return Array.isArray(response.data) ? response.data : [];
};

export const getTicketPriorities = async (): Promise<TicketMetadataOption[]> => {
  const response = await axios.get('/api/tickets/metadata/priorities');
  return Array.isArray(response.data) ? response.data : [];
};

export const uploadAttachment = async (ticketId: number, file: File): Promise<any> => {
  const formData = new FormData();
  formData.append('file', file);

  const response = await axios.post(`/api/attachments/tickets/${ticketId}/upload`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
  return response.data;
};

export const getKnowledgeBaseCategories = async (): Promise<KnowledgeBaseCategory[]> => {
  const response = await axios.get('/api/knowledge-base/categories');
  return Array.isArray(response.data) ? response.data : [];
};

export const getKnowledgeBaseArticles = async (params: { q?: string; categoryId?: number }): Promise<KnowledgeBaseArticle[]> => {
  const response = await axios.get('/api/knowledge-base/articles', { params });
  return Array.isArray(response.data) ? response.data : [];
};

export const addKnowledgeBaseFeedback = async (
  articleId: number,
  payload: { isHelpful: boolean; comment: string }
): Promise<void> => {
  await axios.post(`/api/knowledge-base/articles/${articleId}/feedback`, payload);
};

// Twilio Integration APIs
export const sendSmsMessage = async (data: {
  to: string;
  message: string;
  fromNumber?: string;
}): Promise<{ messageId: string; status: string }> => {
  const response = await axios.post('/api/twilio/sms/send', data);
  return response.data;
};

export const initiateCall = async (data: {
  to: string;
  fromNumber?: string;
  webhookUrl?: string;
}): Promise<{ callId: string; status: string }> => {
  const response = await axios.post('/api/twilio/call/initiate', data);
  return response.data;
};

export const getTwilioLogs = async (params: {
  type: 'calls' | 'messages';
  from?: string;
  to?: string;
}): Promise<any[]> => {
  const response = await axios.get(`/api/twilio/logs/${params.type}`, { params });
  return response.data;
};

// WhatsApp Integration APIs
export const sendWhatsAppMessage = async (data: {
  to: string;
  message: string;
  templateName?: string;
  templateVariables?: Record<string, string>;
}): Promise<{ messageId: string; status: string }> => {
  const response = await axios.post('/api/whatsapp/messages/send', data);
  return response.data;
};

export const sendWhatsAppMedia = async (data: {
  to: string;
  mediaUrl: string;
  mediaType: string;
  caption?: string;
}): Promise<{ messageId: string; status: string }> => {
  const response = await axios.post('/api/whatsapp/messages/send-media', data);
  return response.data;
};

// Microsoft Teams Integration APIs
export const sendTeamsMessage = async (data: {
  channelId: string;
  message: string;
  format?: string;
}): Promise<{ messageId: string; status: string }> => {
  const response = await axios.post('/api/teams/messages/send', data);
  return response.data;
};

export const createTeamsMeeting = async (data: {
  subject: string;
  startTime: string;
  endTime: string;
  attendeeIds: string[];
}): Promise<any> => {
  const response = await axios.post('/api/teams/meetings/create', data);
  return response.data;
};

// Slack Integration APIs
export const sendSlackMessage = async (data: {
  channelId: string;
  message: string;
  format?: string;
}): Promise<{ messageId: string; status: string }> => {
  const response = await axios.post('/api/slack/messages/send', data);
  return response.data;
};

export const sendSlackBlockMessage = async (data: {
  channelId: string;
  blocks: any[];
}): Promise<{ messageId: string; status: string }> => {
  const response = await axios.post('/api/slack/messages/send-blocks', data);
  return response.data;
};

// Workflow Engine APIs
export interface WorkflowDefinition {
  id?: number;
  name: string;
  description?: string;
  category?: string;
  isActive?: boolean;
  trigger?: any;
  nodes?: any[];
  connections?: any[];
}

export interface WorkflowExecution {
  id: string;
  workflowId: number;
  status: string;
  startedAt: string;
  completedAt?: string;
  steps: any[];
}

export const getWorkflows = async (): Promise<WorkflowDefinition[]> => {
  const response = await axios.get('/api/workflow');
  return response.data;
};

export const createWorkflow = async (workflow: WorkflowDefinition): Promise<WorkflowDefinition> => {
  const response = await axios.post('/api/workflow', workflow);
  return response.data;
};

export const updateWorkflow = async (id: number, workflow: WorkflowDefinition): Promise<WorkflowDefinition> => {
  const response = await axios.put(`/api/workflow/${id}`, workflow);
  return response.data;
};

export const executeWorkflow = async (workflowId: number, context?: any): Promise<WorkflowExecution> => {
  const response = await axios.post(`/api/workflow/${workflowId}/execute`, { context });
  return response.data;
};

export const getWorkflowExecution = async (executionId: string): Promise<WorkflowExecution> => {
  const response = await axios.get(`/api/workflow/execution/${executionId}`);
  return response.data;
};

export const getWorkflowNodes = async (): Promise<any[]> => {
  const response = await axios.get('/api/workflow/nodes');
  return response.data;
};

// Project Management APIs
export interface Project {
  id: string;
  name: string;
  description?: string;
  status: string;
  startDate: string;
  endDate: string;
  budget?: number;
  priority?: string;
}

export interface GanttTask {
  id: string;
  title: string;
  description?: string;
  startDate: string;
  endDate: string;
  progress: number;
  status: string;
  priority: string;
  assignee?: any;
  dependencies: string[];
  estimatedHours?: number;
  milestone?: boolean;
}

export const getProjects = async (): Promise<Project[]> => {
  const response = await axios.get('/api/projects');
  return response.data;
};

export const createProject = async (project: Partial<Project>): Promise<Project> => {
  const response = await axios.post('/api/projects', project);
  return response.data;
};

export const getGanttTasks = async (projectId: string): Promise<GanttTask[]> => {
  const response = await axios.get(`/api/projects/${projectId}/gantt-tasks`);
  return response.data;
};

export const createGanttTask = async (projectId: string, task: Partial<GanttTask>): Promise<GanttTask> => {
  const response = await axios.post(`/api/projects/${projectId}/gantt-tasks`, task);
  return response.data;
};

export const updateGanttTask = async (projectId: string, taskId: string, task: Partial<GanttTask>): Promise<GanttTask> => {
  const response = await axios.put(`/api/projects/${projectId}/gantt-tasks/${taskId}`, task);
  return response.data;
};

// Dependency Management APIs
export interface TicketDependency {
  id: string;
  sourceTicketId: string;
  targetTicketId: string;
  type: string;
  description?: string;
}

export const getTicketDependencies = async (ticketId?: string): Promise<TicketDependency[]> => {
  const url = ticketId ? `/api/tickets/${ticketId}/dependencies` : '/api/dependencies';
  const response = await axios.get(url);
  return response.data;
};

export const createTicketDependency = async (dependency: Partial<TicketDependency>): Promise<TicketDependency> => {
  const response = await axios.post('/api/dependencies', dependency);
  return response.data;
};

export const deleteTicketDependency = async (dependencyId: string): Promise<void> => {
  await axios.delete(`/api/dependencies/${dependencyId}`);
};

// Agile Management APIs
export interface Sprint {
  id: string;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  status: string;
  capacity: number;
  velocity: number;
  focusFactor: number;
}

export interface SprintTask {
  id: string;
  title: string;
  description?: string;
  status: string;
  priority: string;
  estimate: number;
  assignee?: any;
  type: string;
}

export const getSprints = async (projectId: string): Promise<Sprint[]> => {
  const response = await axios.get(`/api/projects/${projectId}/sprints`);
  return response.data;
};

export const createSprint = async (projectId: string, sprint: Partial<Sprint>): Promise<Sprint> => {
  const response = await axios.post(`/api/projects/${projectId}/sprints`, sprint);
  return response.data;
};

export const getSprintTasks = async (sprintId: string): Promise<SprintTask[]> => {
  const response = await axios.get(`/api/sprints/${sprintId}/tasks`);
  return response.data;
};

export const createSprintTask = async (sprintId: string, task: Partial<SprintTask>): Promise<SprintTask> => {
  const response = await axios.post(`/api/sprints/${sprintId}/tasks`, task);
  return response.data;
};

// Resource Allocation APIs
export interface Resource {
  id: string;
  name: string;
  email: string;
  role: string;
  department: string;
  skills: string[];
  capacity: number;
  availability: number;
  utilization: number;
}

export interface Allocation {
  id: string;
  resourceId: string;
  projectId: string;
  allocationPercentage: number;
  startDate: string;
  endDate: string;
  estimatedHours: number;
  priority: string;
}

export const getResources = async (): Promise<Resource[]> => {
  const response = await axios.get('/api/resources');
  return response.data;
};

export const getAllocations = async (): Promise<Allocation[]> => {
  const response = await axios.get('/api/allocations');
  return response.data;
};

export const createAllocation = async (allocation: Partial<Allocation>): Promise<Allocation> => {
  const response = await axios.post('/api/allocations', allocation);
  return response.data;
};

export const updateAllocation = async (id: string, allocation: Partial<Allocation>): Promise<Allocation> => {
  const response = await axios.put(`/api/allocations/${id}`, allocation);
  return response.data;
};

// Enterprise Admin APIs
export interface IdentityProvider {
  id: string;
  name: string;
  type: string;
  metadataUrl: string;
  ssoUrl: string;
  isActive: boolean;
}

export const getIdentityProviders = async (): Promise<IdentityProvider[]> => {
  const response = await axios.get('/api/admin/identity/providers');
  return response.data;
};

export const createIdentityProvider = async (provider: Partial<IdentityProvider>): Promise<IdentityProvider> => {
  const response = await axios.post('/api/admin/identity/providers', provider);
  return response.data;
};

export const testSamlConnection = async (providerId: string): Promise<{ success: boolean; message: string }> => {
  const response = await axios.post(`/api/admin/identity/providers/${providerId}/test`);
  return response.data;
};

// Analytics APIs
export const getAdvancedAnalytics = async (): Promise<any> => {
  const response = await axios.get('/api/advancedanalytics/performance-report');
  return response.data;
};

export const getUsageMetrics = async (timeframe: string): Promise<any> => {
  const response = await axios.get(`/api/billing/usage?timeframe=${timeframe}`);
  return response.data;
};

// Utility function for error handling
export const handleApiError = (error: any): string => {
  if (error.response?.data?.message) {
    return error.response.data.message;
  }
  if (error.message) {
    return error.message;
  }
  return 'An unexpected error occurred';
};
