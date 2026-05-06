import React from 'react';
import { Box, Button, Card, CardContent, Chip, Grid, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';
import SupportAgentRoundedIcon from '@mui/icons-material/SupportAgentRounded';
import LibraryBooksRoundedIcon from '@mui/icons-material/LibraryBooksRounded';
import ManageSearchRoundedIcon from '@mui/icons-material/ManageSearchRounded';
import AddTaskRoundedIcon from '@mui/icons-material/AddTaskRounded';
import ChatRoundedIcon from '@mui/icons-material/ChatRounded';

const CustomerPortal: React.FC = () => {
  return (
    <Box>
      <Card sx={{ mb: 3, borderRadius: 4, border: '1px solid #dbeafe', background: 'linear-gradient(140deg,#0f172a 0%,#0b3b4f 52%, #0f766e 100%)', color: '#f8fafc', boxShadow: '0 28px 48px rgba(2,6,23,.24)' }}>
        <CardContent sx={{ p: { xs: 2.3, md: 4 } }}>
          <Chip label="Customer Portal" sx={{ bgcolor: 'rgba(255,255,255,.17)', color: '#fff', fontWeight: 700 }} />
          <Typography sx={{ mt: 1.4, fontFamily: 'Space Grotesk, sans-serif', fontSize: { xs: 28, md: 44 }, fontWeight: 700, lineHeight: 1.05 }}>
            Resolve issues faster with guided support.
          </Typography>
          <Typography sx={{ mt: 1.2, opacity: 0.92, maxWidth: 840 }}>
            Log complaints, track every ticket stage, browse knowledge articles, and move to real-time support channels when needed.
          </Typography>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.4} sx={{ mt: 2.6 }}>
            <Button component={Link} to="/app/tickets/create" variant="contained" sx={{ textTransform: 'none', fontWeight: 700, py: 1.3, px: 2.5, bgcolor: '#f8fafc', color: '#0f172a', '&:hover': { bgcolor: '#e2e8f0' } }}>
              Log a Complaint
            </Button>
            <Button component={Link} to="/app/tickets" variant="outlined" sx={{ textTransform: 'none', fontWeight: 700, py: 1.3, px: 2.5, borderColor: 'rgba(255,255,255,.5)', color: '#fff' }}>
              Track My Tickets
            </Button>
          </Stack>
        </CardContent>
      </Card>

      <Grid container spacing={2.2}>
        <Grid item xs={12} md={6}>
          <Card sx={{ borderRadius: 3, border: '1px solid #dbeafe', height: '100%' }}>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1}>
                <AddTaskRoundedIcon sx={{ color: '#0369a1' }} />
                <Typography variant="h6">File a Complaint</Typography>
              </Stack>
              <Typography sx={{ mt: .9, color: '#475569' }}>
                Start a new complaint ticket with priority, category, and full description.
              </Typography>
              <Button component={Link} to="/app/tickets/create" variant="contained" sx={{ mt: 2, textTransform: 'none', fontWeight: 700, background: 'linear-gradient(90deg,#0f766e 0%,#0369a1 100%)' }}>
                Create Ticket
              </Button>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card sx={{ borderRadius: 3, border: '1px solid #dbeafe', height: '100%' }}>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1}>
                <ManageSearchRoundedIcon sx={{ color: '#0369a1' }} />
                <Typography variant="h6">Ticket Status & Timeline</Typography>
              </Stack>
              <Typography sx={{ mt: .9, color: '#475569' }}>
                Track open/resolved items, see priority and SLA movement, and review updates from support.
              </Typography>
              <Button component={Link} to="/app/tickets" variant="outlined" sx={{ mt: 2, textTransform: 'none', fontWeight: 700, borderColor: '#0369a1', color: '#0369a1' }}>
                View My Tickets
              </Button>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card sx={{ borderRadius: 3, border: '1px solid #dbeafe', height: '100%' }}>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1}>
                <LibraryBooksRoundedIcon sx={{ color: '#0369a1' }} />
                <Typography variant="h6">Knowledge Base</Typography>
              </Stack>
              <Typography sx={{ mt: .9, color: '#475569' }}>
                Search self-service guides and troubleshooting playbooks before submitting a new ticket.
              </Typography>
              <Button component={Link} to="/app/knowledge-base" variant="outlined" sx={{ mt: 2, textTransform: 'none', fontWeight: 700, borderColor: '#0369a1', color: '#0369a1' }}>
                Browse Articles
              </Button>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={6}>
          <Card sx={{ borderRadius: 3, border: '1px solid #dbeafe', height: '100%' }}>
            <CardContent>
              <Stack direction="row" alignItems="center" spacing={1}>
                <ChatRoundedIcon sx={{ color: '#0369a1' }} />
                <Typography variant="h6">Live Chat & Escalation</Typography>
              </Stack>
              <Typography sx={{ mt: .9, color: '#475569' }}>
                Chat is available through integrated channels. If live chat is not configured for your tenant, use ticket escalation from any ticket detail page.
              </Typography>
              <Stack direction="row" spacing={1.1} sx={{ mt: 2 }}>
                <Button component={Link} to="/app/tickets" variant="contained" sx={{ textTransform: 'none', fontWeight: 700, background: 'linear-gradient(90deg,#0f766e 0%,#0369a1 100%)' }}>
                  Open Support Channel
                </Button>
                <Button component={Link} to="/app/dashboard" variant="text" sx={{ textTransform: 'none', fontWeight: 700 }}>
                  View Ops Dashboard
                </Button>
              </Stack>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Card sx={{ mt: 2.2, borderRadius: 3, border: '1px solid #dbeafe' }}>
        <CardContent>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: .8 }}>
            <SupportAgentRoundedIcon sx={{ color: '#0369a1' }} />
            <Typography variant="h6">What is available now?</Typography>
          </Stack>
          <Typography sx={{ color: '#334155' }}>
            Complaint logging, ticket tracking, profile, and knowledge base are available now in this build. Chat is integrated as an omnichannel capability and depends on your configured channel providers.
          </Typography>
        </CardContent>
      </Card>
    </Box>
  );
};

export default CustomerPortal;
