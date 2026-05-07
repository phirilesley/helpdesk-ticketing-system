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
  Chip,
  Stack,
  Divider
} from '@mui/material';
import SecurityIcon from '@mui/icons-material/Security';
import BusinessIcon from '@mui/icons-material/Business';
import VerifiedUserIcon from '@mui/icons-material/VerifiedUser';
import LockPersonIcon from '@mui/icons-material/LockPerson';
import HubIcon from '@mui/icons-material/Hub';
import FactCheckIcon from '@mui/icons-material/FactCheck';
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
    <Box
      sx={{
        minHeight: '100vh',
        p: { xs: 2, md: 4 },
        position: 'relative',
        overflow: 'hidden',
        background: 'linear-gradient(145deg,#f8fafc 0%,#ecfeff 42%,#eef2ff 100%)'
      }}
    >
      <Box
        sx={{
          position: 'absolute',
          inset: 0,
          background: 'radial-gradient(780px 380px at 8% 18%, rgba(14,165,233,0.22), transparent 70%), radial-gradient(800px 390px at 90% 80%, rgba(99,102,241,0.20), transparent 70%)',
          animation: 'sway 10s ease-in-out infinite alternate'
        }}
      />

      <Paper
        elevation={0}
        sx={{
          minHeight: { xs: 'auto', md: '90vh' },
          borderRadius: 5,
          overflow: 'hidden',
          border: '1px solid #dbe2ef',
          background: '#ffffff',
          boxShadow: '0 28px 80px rgba(15,23,42,0.16)',
          position: 'relative',
          zIndex: 1
        }}
      >
        <Grid container sx={{ minHeight: { xs: 'auto', md: '90vh' } }}>
          <Grid
            item
            xs={12}
            md={7}
            sx={{
              p: { xs: 3, md: 6 },
              background: 'linear-gradient(165deg,#f0f9ff 0%,#f8fafc 50%,#eef2ff 100%)',
              position: 'relative'
            }}
          >
            <Box
              sx={{
                position: 'absolute',
                width: 280,
                height: 280,
                right: -70,
                top: -50,
                borderRadius: '50%',
                background: 'radial-gradient(circle at 35% 35%, rgba(56,189,248,0.55), rgba(14,116,144,0.08) 70%, transparent 100%)'
              }}
            />

            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.3, mb: 2 }}>
              <Box
                sx={{
                  width: 46,
                  height: 46,
                  borderRadius: 2.5,
                  background: 'linear-gradient(135deg,#0891b2 0%,#2563eb 55%,#4f46e5 100%)',
                  display: 'grid',
                  placeItems: 'center',
                  boxShadow: '0 10px 24px rgba(37,99,235,0.28)'
                }}
              >
                <HubIcon sx={{ color: '#fff' }} />
              </Box>
              <Box>
                <Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontWeight: 700, color: '#0f172a', lineHeight: 1.1 }}>
                  Smart Panda Help Desk
                </Typography>
                <Typography sx={{ fontSize: 12, color: '#475569', letterSpacing: 0.2 }}>
                  Enterprise Service Management Suite
                </Typography>
              </Box>
            </Box>

            <Chip label="Enterprise Login" sx={{ mb: 2, bgcolor: '#dbeafe', color: '#1d4ed8', fontWeight: 700 }} />

            <Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: { xs: 32, md: 50 }, fontWeight: 700, lineHeight: 1.02, color: '#0f172a', maxWidth: 620 }}>
              Mission control for support, ITSM, DevOps, HR, and customer success.
            </Typography>

            <Typography sx={{ mt: 2, color: '#334155', maxWidth: 600, fontSize: 16 }}>
              Drive faster resolutions with workflow automation, multi-channel intake, searchable knowledge, and accountable SLA execution.
            </Typography>

            <Grid container spacing={2} sx={{ mt: 3, maxWidth: 700 }}>
              <Grid item xs={12} sm={4}>
                <Paper sx={{ p: 2.2, borderRadius: 2.5, border: '1px solid #dbe2ef', boxShadow: 'none' }}>
                  <Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: 26, fontWeight: 700 }}>99.2%</Typography>
                  <Typography color="text.secondary">SLA compliance</Typography>
                </Paper>
              </Grid>
              <Grid item xs={12} sm={4}>
                <Paper sx={{ p: 2.2, borderRadius: 2.5, border: '1px solid #dbe2ef', boxShadow: 'none' }}>
                  <Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: 26, fontWeight: 700 }}>3.8m</Typography>
                  <Typography color="text.secondary">First response</Typography>
                </Paper>
              </Grid>
              <Grid item xs={12} sm={4}>
                <Paper sx={{ p: 2.2, borderRadius: 2.5, border: '1px solid #dbe2ef', boxShadow: 'none' }}>
                  <Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: 26, fontWeight: 700 }}>24x7</Typography>
                  <Typography color="text.secondary">Global coverage</Typography>
                </Paper>
              </Grid>
            </Grid>

            <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap" sx={{ mt: 2.5 }}>
              <Chip icon={<SecurityIcon />} label="SOC-ready controls" sx={{ bgcolor: '#ecfeff', color: '#155e75' }} />
              <Chip icon={<VerifiedUserIcon />} label="Role-based access" sx={{ bgcolor: '#ecfdf5', color: '#166534' }} />
              <Chip icon={<BusinessIcon />} label="Cross-team workflows" sx={{ bgcolor: '#eff6ff', color: '#1e40af' }} />
            </Stack>
          </Grid>

          <Grid
            item
            xs={12}
            md={5}
            sx={{
              p: { xs: 3, md: 6 },
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              background: 'linear-gradient(180deg,#ffffff 0%,#f8fafc 100%)'
            }}
          >
            <Paper
              elevation={0}
              sx={{
                width: '100%',
                maxWidth: 460,
                p: { xs: 3, md: 3.8 },
                borderRadius: 3,
                border: '1px solid #dbe2ef',
                background: 'linear-gradient(160deg,rgba(255,255,255,0.97) 0%,rgba(248,250,252,0.97) 100%)',
                boxShadow: '0 18px 36px rgba(15,23,42,0.10)',
                animation: 'fadeIn .6s ease-out'
              }}
            >
              <Typography sx={{ fontFamily: 'Space Grotesk, sans-serif', fontSize: 31, fontWeight: 700, color: '#0f172a' }}>
                Sign in to Smart Panda
              </Typography>
              <Typography color="text.secondary" sx={{ mt: 0.8, mb: 2.5 }}>
                Securely continue to your enterprise workspace.
              </Typography>

              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2, color: '#0f766e' }}>
                <LockPersonIcon fontSize="small" />
                <Typography sx={{ fontSize: 13, fontWeight: 600 }}>Protected login with auditable role access</Typography>
              </Box>

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
                  sx={{ '& .MuiOutlinedInput-root': { borderRadius: 2.2 } }}
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
                  sx={{ '& .MuiOutlinedInput-root': { borderRadius: 2.2 } }}
                />

                <Button
                  type="submit"
                  fullWidth
                  variant="contained"
                  sx={{
                    mt: 3,
                    mb: 1.5,
                    py: 1.45,
                    borderRadius: 2.2,
                    textTransform: 'none',
                    fontWeight: 700,
                    fontSize: 16,
                    background: 'linear-gradient(90deg,#0891b2 0%,#2563eb 45%,#4f46e5 100%)',
                    boxShadow: '0 12px 24px rgba(37,99,235,0.30)'
                  }}
                  disabled={isLoading}
                >
                  {isLoading ? <CircularProgress size={24} color="inherit" /> : 'Sign In'}
                </Button>

                <Divider sx={{ my: 2 }} />

                <Box sx={{ p: 1.6, borderRadius: 2, bgcolor: '#f8fafc', border: '1px dashed #94a3b8' }}>
                  <Typography sx={{ fontSize: 12, color: '#334155', fontWeight: 700 }}>
                    Demo Login
                  </Typography>
                  <Typography sx={{ fontSize: 12, color: '#475569' }}>
                    Username: <strong>superadmin@helpdesk.local</strong>
                  </Typography>
                  <Typography sx={{ fontSize: 12, color: '#475569' }}>
                    Password: <strong>ChangeThisStrongPassword!</strong>
                  </Typography>
                </Box>

                <Box sx={{ mt: 2, p: 1.4, borderRadius: 1.8, bgcolor: '#f0f9ff', border: '1px solid #bae6fd', display: 'flex', alignItems: 'center', gap: 1 }}>
                  <FactCheckIcon sx={{ fontSize: 18, color: '#0369a1' }} />
                  <Typography sx={{ fontSize: 12.5, color: '#0c4a6e' }}>
                    Trusted by operations teams for compliant service delivery.
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
