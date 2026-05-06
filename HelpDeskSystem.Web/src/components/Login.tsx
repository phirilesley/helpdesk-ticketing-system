import React, { useState } from 'react';
import {
  Grid,
  TextField,
  Button,
  Typography,
  Box,
  Alert,
  CircularProgress,
  Paper,
  Chip
} from '@mui/material';
import { useAuth } from '../hooks/useAuth';

const Login: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const { login, isLoading } = useAuth();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    
    try {
      await login(email, password);
    } catch (err: any) {
      setError(err.message || 'Login failed');
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', p: { xs: 2, md: 4 }, position: 'relative', overflow: 'hidden' }}>
      <Box sx={{ position: 'absolute', top: -120, right: -80, width: 360, height: 360, borderRadius: '50%', background: 'radial-gradient(circle at 30% 30%, #bfdbfe 0%, #60a5fa 45%, rgba(96,165,250,0.12) 75%, transparent 100%)', filter: 'blur(4px)' }} />
      <Box sx={{ position: 'absolute', bottom: -160, left: -100, width: 420, height: 420, borderRadius: '50%', background: 'radial-gradient(circle at 45% 45%, #c7d2fe 0%, #818cf8 42%, rgba(129,140,248,0.12) 76%, transparent 100%)', filter: 'blur(6px)' }} />
      <Paper
        elevation={0}
        sx={{
          minHeight: { xs: 'auto', md: '88vh' },
          borderRadius: 4,
          overflow: 'hidden',
          border: '1px solid #dbe2ef',
          background: '#ffffff',
          boxShadow: '0 30px 70px rgba(15, 23, 42, 0.10)',
          position: 'relative',
          zIndex: 1
        }}
      >
        <Grid container sx={{ minHeight: { xs: 'auto', md: '88vh' } }}>
          <Grid item xs={12} md={7} sx={{ p: { xs: 3, md: 6 }, bgcolor: '#f8fbff' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.2, mb: 2 }}>
              <Box sx={{ width: 42, height: 42, borderRadius: 2, background: 'linear-gradient(135deg, #0ea5e9 0%, #2563eb 45%, #4f46e5 100%)', display: 'grid', placeItems: 'center', boxShadow: '0 10px 24px rgba(37,99,235,0.28)' }}>
                <svg width="24" height="24" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                  <path d="M4 7.5C4 6.12 5.12 5 6.5 5h11C18.88 5 20 6.12 20 7.5v6c0 1.38-1.12 2.5-2.5 2.5H10l-3.8 3.2c-.66.56-1.7.09-1.7-.78V16A2.5 2.5 0 0 1 4 13.5v-6Z" fill="white"/>
                  <circle cx="9" cy="10.5" r="1.2" fill="#2563eb"/>
                  <circle cx="12" cy="10.5" r="1.2" fill="#2563eb"/>
                  <circle cx="15" cy="10.5" r="1.2" fill="#2563eb"/>
                </svg>
              </Box>
              <Box>
                <Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontWeight: 700, color: '#0f172a', lineHeight: 1.1 }}>
                  NOVA HELP DESK
                </Typography>
                <Typography sx={{ fontSize: 12, color: '#475569', letterSpacing: 0.3 }}>
                  Service Operations Platform
                </Typography>
              </Box>
            </Box>
            <Chip label="Enterprise Service Desk" sx={{ mb: 2, bgcolor: '#dbeafe', color: '#1d4ed8', fontWeight: 600 }} />
            <Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: { xs: 32, md: 46 }, fontWeight: 700, lineHeight: 1.05, color: '#0f172a', maxWidth: 560 }}>
              Support operations with the speed of a modern help desk.
            </Typography>
            <Typography sx={{ mt: 2, color: '#334155', maxWidth: 560 }}>
              One workspace for ticket queues, automation, knowledge, SLAs, and cross-team escalation.
            </Typography>
            <Grid container spacing={2} sx={{ mt: 3, maxWidth: 620 }}>
              <Grid item xs={12} sm={4}><Paper sx={{ p: 2.2, borderRadius: 2, border: '1px solid #dbe2ef' }}><Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: 26, fontWeight: 700 }}>98.7%</Typography><Typography color="text.secondary">SLA attainment</Typography></Paper></Grid>
              <Grid item xs={12} sm={4}><Paper sx={{ p: 2.2, borderRadius: 2, border: '1px solid #dbe2ef' }}><Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: 26, fontWeight: 700 }}>4.2m</Typography><Typography color="text.secondary">Median first response</Typography></Paper></Grid>
              <Grid item xs={12} sm={4}><Paper sx={{ p: 2.2, borderRadius: 2, border: '1px solid #dbe2ef' }}><Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: 26, fontWeight: 700 }}>24/7</Typography><Typography color="text.secondary">Omnichannel intake</Typography></Paper></Grid>
            </Grid>
          </Grid>

          <Grid item xs={12} md={5} sx={{ p: { xs: 3, md: 6 }, display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'linear-gradient(180deg,#ffffff 0%, #f8fafc 100%)' }}>
            <Paper
              elevation={0}
              sx={{
                width: '100%',
                maxWidth: 440,
                p: { xs: 3, md: 3.5 },
                borderRadius: 3,
                border: '1px solid #dbe2ef',
                background: 'linear-gradient(160deg, rgba(255,255,255,0.96) 0%, rgba(248,250,252,0.96) 100%)',
                boxShadow: '0 16px 36px rgba(15, 23, 42, 0.09)',
                animation: 'fadeIn .55s ease-out'
              }}
            >
            <Box sx={{ width: '100%' }}>
              <Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: 30, fontWeight: 700, color: '#0f172a' }}>
                Sign in
              </Typography>
              <Typography color="text.secondary" sx={{ mt: 0.5, mb: 2.5 }}>
                Continue to your support workspace.
              </Typography>

              {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

              <Box component="form" onSubmit={handleSubmit}>
                <TextField
                  margin="normal"
                  required
                  fullWidth
                  id="email"
                  label="Work Email"
                  name="email"
                  autoComplete="email"
                  autoFocus
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={isLoading}
                  sx={{ '& .MuiOutlinedInput-root': { borderRadius: 2 } }}
                />
                <TextField
                  margin="normal"
                  required
                  fullWidth
                  name="password"
                  label="Password"
                  type="password"
                  id="password"
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  disabled={isLoading}
                  sx={{ '& .MuiOutlinedInput-root': { borderRadius: 2 } }}
                />
                <Button
                  type="submit"
                  fullWidth
                  variant="contained"
                  sx={{ mt: 3, mb: 1.5, py: 1.4, borderRadius: 2, textTransform: 'none', fontWeight: 700, fontSize: 16, background: 'linear-gradient(90deg,#0284c7 0%, #2563eb 45%, #4338ca 100%)' }}
                  disabled={isLoading}
                >
                  {isLoading ? <CircularProgress size={24} color="inherit" /> : 'Sign In to Help Desk'}
                </Button>
                <Typography sx={{ textAlign: 'center', color: '#64748b', fontSize: 13 }}>
                  Secure workspace with role-based access and audit logging.
                </Typography>
              </Box>
            </Box>
            </Paper>
          </Grid>
        </Grid>
      </Paper>
    </Box>
  );
};

export default Login;
