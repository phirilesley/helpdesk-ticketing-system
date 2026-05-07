import React from 'react';
import { Box, Button, Card, CardContent, Chip, Container, Grid, Stack, Typography } from '@mui/material';
import { Link } from 'react-router-dom';
import BoltRoundedIcon from '@mui/icons-material/BoltRounded';
import ForumRoundedIcon from '@mui/icons-material/ForumRounded';
import LibraryBooksRoundedIcon from '@mui/icons-material/LibraryBooksRounded';
import TrackChangesRoundedIcon from '@mui/icons-material/TrackChangesRounded';

const featureCards = [
  {
    title: 'Complaint Logging',
    text: 'Capture issues in under a minute with structured forms and smart category suggestions.',
    icon: <BoltRoundedIcon sx={{ color: '#ffffff' }} />
  },
  {
    title: 'Live Ticket Tracking',
    text: 'Customers get timeline visibility and SLA state without opening support emails.',
    icon: <TrackChangesRoundedIcon sx={{ color: '#ffffff' }} />
  },
  {
    title: 'Knowledge Base',
    text: 'Deflect repetitive tickets with searchable guides, FAQs, and operational runbooks.',
    icon: <LibraryBooksRoundedIcon sx={{ color: '#ffffff' }} />
  },
  {
    title: 'Omnichannel Chat',
    text: 'Unify web chat, email, and messaging handoffs into one support conversation context.',
    icon: <ForumRoundedIcon sx={{ color: '#ffffff' }} />
  },
];

const Landing: React.FC = () => {
  return (
    <Box sx={{ minHeight: '100vh', background: 'radial-gradient(1000px 600px at 10% 10%, #d1fae5 0%, #eef2ff 55%, #f8fafc 100%)' }}>
      <Container maxWidth="lg" sx={{ py: { xs: 4, md: 8 } }}>
        <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: { xs: 5, md: 8 } }}>
          <Stack direction="row" spacing={1.4} alignItems="center">
            <Box sx={{ width: 44, height: 44, borderRadius: 2, display: 'grid', placeItems: 'center', background: 'linear-gradient(135deg,#0f766e 0%,#0ea5e9 55%,#1d4ed8 100%)', boxShadow: '0 14px 26px rgba(15,118,110,.35)' }}>
              <ForumRoundedIcon sx={{ color: '#fff' }} />
            </Box>
            <Typography variant="h6" sx={{ color: '#0f172a', letterSpacing: 0.2 }}>SMART PANDA HELP DESK</Typography>
          </Stack>
          <Stack direction="row" spacing={1.5}>
            <Button component={Link} to="/login" variant="text" sx={{ color: '#0f172a', fontWeight: 600 }}>Sign In</Button>
            <Button component={Link} to="/app/customer-portal" variant="contained" sx={{ textTransform: 'none', fontWeight: 700, px: 2.4, background: 'linear-gradient(90deg,#0f766e 0%,#0369a1 100%)' }}>Open Portal</Button>
          </Stack>
        </Stack>

        <Grid container spacing={4} alignItems="center">
          <Grid item xs={12} md={7}>
            <Chip label="Customer Support Platform" sx={{ mb: 2, bgcolor: '#ccfbf1', color: '#115e59', fontWeight: 700 }} />
            <Typography sx={{ fontSize: { xs: 38, md: 60 }, lineHeight: 1.02, color: '#0f172a', maxWidth: 760, fontFamily: 'Space Grotesk, sans-serif', fontWeight: 700 }}>
              Modern support your teams and customers actually enjoy using.
            </Typography>
            <Typography sx={{ mt: 2.2, color: '#334155', fontSize: 18, maxWidth: 640 }}>
              A single platform for complaints, SLAs, workflows, chat, and knowledge. Built for speed and operational clarity.
            </Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mt: 3.2 }}>
              <Button component={Link} to="/app/customer-portal" variant="contained" sx={{ py: 1.4, px: 3.1, fontWeight: 700, textTransform: 'none', background: 'linear-gradient(90deg,#0f766e 0%,#0369a1 100%)' }}>
                Go to Customer Portal
              </Button>
              <Button component={Link} to="/app/knowledge-base" variant="outlined" sx={{ py: 1.4, px: 3.1, fontWeight: 700, textTransform: 'none', borderColor: '#0f766e', color: '#0f766e' }}>
                Explore Knowledge Base
              </Button>
            </Stack>
          </Grid>

          <Grid item xs={12} md={5}>
            <Card sx={{ borderRadius: 4, border: '1px solid #dbeafe', boxShadow: '0 30px 60px rgba(2, 6, 23, 0.12)', overflow: 'hidden' }}>
              <CardContent sx={{ p: 3.2, background: 'linear-gradient(165deg, #0f172a 0%, #0b3b4f 55%, #0f766e 100%)', color: '#f8fafc' }}>
                <Typography sx={{ fontSize: 28, fontFamily: 'Space Grotesk, sans-serif', fontWeight: 700 }}>Portal Snapshot</Typography>
                <Typography sx={{ opacity: 0.9, mt: 1 }}>Track open tickets, chat with support, and view SLA progress from one page.</Typography>
                <Stack spacing={1.2} sx={{ mt: 2.8 }}>
                  <Box sx={{ p: 1.3, borderRadius: 2, bgcolor: 'rgba(255,255,255,.12)' }}>Average first response: 4m</Box>
                  <Box sx={{ p: 1.3, borderRadius: 2, bgcolor: 'rgba(255,255,255,.12)' }}>Portal deflection: 37%</Box>
                  <Box sx={{ p: 1.3, borderRadius: 2, bgcolor: 'rgba(255,255,255,.12)' }}>SLA attainment: 98.7%</Box>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        <Grid container spacing={2.3} sx={{ mt: { xs: 4, md: 6 } }}>
          {featureCards.map((card) => (
            <Grid item xs={12} sm={6} md={3} key={card.title}>
              <Card sx={{ height: '100%', border: '1px solid #dbeafe', borderRadius: 3, boxShadow: '0 10px 24px rgba(2, 6, 23, 0.05)' }}>
                <CardContent>
                  <Box sx={{ width: 38, height: 38, borderRadius: 1.5, display: 'grid', placeItems: 'center', background: 'linear-gradient(135deg,#0f766e 0%,#1d4ed8 100%)', mb: 1.4 }}>
                    {card.icon}
                  </Box>
                  <Typography sx={{ fontWeight: 700, color: '#0f172a' }}>{card.title}</Typography>
                  <Typography sx={{ mt: .8, color: '#475569', fontSize: 14 }}>{card.text}</Typography>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      </Container>
    </Box>
  );
};

export default Landing;
