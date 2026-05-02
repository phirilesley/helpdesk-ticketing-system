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
}

export interface CreateTicketRequest {
  title: string;
  description: string;
  priority: string;
  category: string;
}

export const getDashboardStats = async (): Promise<DashboardStats> => {
  const response = await axios.get('/api/dashboard/stats');
  return response.data;
};

export const getRecentTickets = async (): Promise<Ticket[]> => {
  const response = await axios.get('/api/dashboard/recent-tickets');
  return response.data;
};

export const createTicket = async (ticketData: CreateTicketRequest): Promise<Ticket> => {
  const response = await axios.post('/api/tickets', ticketData);
  return response.data;
};

export const getTickets = async (params: {
  page?: number;
  status?: string;
  search?: string;
}): Promise<{ data: Ticket[]; total: number }> => {
  const response = await axios.get('/api/tickets', { params });
  return response.data;
};

export const getTicket = async (id: number): Promise<Ticket> => {
  const response = await axios.get(`/api/tickets/${id}`);
  return response.data;
};

export const addComment = async (ticketId: number, comment: {
  content: string;
  isInternal: boolean;
}): Promise<any> => {
  const response = await axios.post(`/api/tickets/${ticketId}/comments`, comment);
  return response.data;
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
