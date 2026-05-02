import React, { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Switch,
  Alert,
  List,
  ListItem,
  ListItemText,
  ListItemSecondaryAction,
  Paper,
  Grid
} from '@mui/material';
import { Save, Notifications, Security, Language, Help } from '@mui/icons-material';

const Settings: React.FC = () => {
  const [success, setSuccess] = useState('');
  const [error, setError] = useState('');
  
  const [notificationSettings, setNotificationSettings] = useState({
    emailNotifications: true,
    pushNotifications: false,
    smsNotifications: false,
    ticketUpdates: true,
    resolutionAlerts: true,
    systemAnnouncements: false
  });

  const [securitySettings, setSecuritySettings] = useState({
    twoFactorAuth: false,
    sessionTimeout: true,
    loginAlerts: true,
    passwordChangeAlert: true
  });

  const [displaySettings, setDisplaySettings] = useState({
    theme: 'light',
    language: 'en',
    timezone: 'UTC',
    dateFormat: 'MM/DD/YYYY',
    timeFormat: '12h'
  });

  const handleSave = async (section: string) => {
    try {
      setSuccess(`${section} settings saved successfully!`);
      setError('');
    } catch (err: any) {
      setError(`Failed to save ${section} settings`);
      setSuccess('');
    }
  };

  const handleNotificationChange = (setting: string) => (event: React.ChangeEvent<HTMLInputElement>) => {
    setNotificationSettings(prev => ({
      ...prev,
      [setting]: event.target.checked
    }));
  };

  const handleSecurityChange = (setting: string) => (event: React.ChangeEvent<HTMLInputElement>) => {
    setSecuritySettings(prev => ({
      ...prev,
      [setting]: event.target.checked
    }));
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Settings
      </Typography>

      {success && <Alert severity="success" sx={{ mb: 2 }}>{success}</Alert>}
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Grid container spacing={3}>
        {/* Notification Settings */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" mb={2}>
                <Notifications color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">Notifications</Typography>
              </Box>
              
              <List>
                <ListItem>
                  <ListItemText 
                    primary="Email Notifications" 
                    secondary="Receive updates via email" 
                  />
                  <ListItemSecondaryAction>
                    <Switch
                      checked={notificationSettings.emailNotifications}
                      onChange={handleNotificationChange('emailNotifications')}
                    />
                  </ListItemSecondaryAction>
                </ListItem>
                
                <ListItem>
                  <ListItemText 
                    primary="Push Notifications" 
                    secondary="Browser push notifications" 
                  />
                  <ListItemSecondaryAction>
                    <Switch
                      checked={notificationSettings.pushNotifications}
                      onChange={handleNotificationChange('pushNotifications')}
                    />
                  </ListItemSecondaryAction>
                </ListItem>
                
                <ListItem>
                  <ListItemText 
                    primary="Ticket Updates" 
                    secondary="Updates on your tickets" 
                  />
                  <ListItemSecondaryAction>
                    <Switch
                      checked={notificationSettings.ticketUpdates}
                      onChange={handleNotificationChange('ticketUpdates')}
                    />
                  </ListItemSecondaryAction>
                </ListItem>
                
                <ListItem>
                  <ListItemText 
                    primary="Resolution Alerts" 
                    secondary="When tickets are resolved" 
                  />
                  <ListItemSecondaryAction>
                    <Switch
                      checked={notificationSettings.resolutionAlerts}
                      onChange={handleNotificationChange('resolutionAlerts')}
                    />
                  </ListItemSecondaryAction>
                </ListItem>
              </List>
              
              <Box mt={2}>
                <Button 
                  variant="contained" 
                  startIcon={<Save />}
                  onClick={() => handleSave('Notification')}
                  fullWidth
                >
                  Save Notification Settings
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Security Settings */}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" mb={2}>
                <Security color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">Security</Typography>
              </Box>
              
              <List>
                <ListItem>
                  <ListItemText 
                    primary="Two-Factor Authentication" 
                    secondary="Add an extra layer of security" 
                  />
                  <ListItemSecondaryAction>
                    <Switch
                      checked={securitySettings.twoFactorAuth}
                      onChange={handleSecurityChange('twoFactorAuth')}
                    />
                  </ListItemSecondaryAction>
                </ListItem>
                
                <ListItem>
                  <ListItemText 
                    primary="Session Timeout" 
                    secondary="Auto-logout after inactivity" 
                  />
                  <ListItemSecondaryAction>
                    <Switch
                      checked={securitySettings.sessionTimeout}
                      onChange={handleSecurityChange('sessionTimeout')}
                    />
                  </ListItemSecondaryAction>
                </ListItem>
                
                <ListItem>
                  <ListItemText 
                    primary="Login Alerts" 
                    secondary="Notify on new login attempts" 
                  />
                  <ListItemSecondaryAction>
                    <Switch
                      checked={securitySettings.loginAlerts}
                      onChange={handleSecurityChange('loginAlerts')}
                    />
                  </ListItemSecondaryAction>
                </ListItem>
                
                <ListItem>
                  <ListItemText 
                    primary="Password Change Alerts" 
                    secondary="Notify when password changes" 
                  />
                  <ListItemSecondaryAction>
                    <Switch
                      checked={securitySettings.passwordChangeAlert}
                      onChange={handleSecurityChange('passwordChangeAlert')}
                    />
                  </ListItemSecondaryAction>
                </ListItem>
              </List>
              
              <Box mt={2}>
                <Button 
                  variant="contained" 
                  startIcon={<Save />}
                  onClick={() => handleSave('Security')}
                  fullWidth
                >
                  Save Security Settings
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Display Settings */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" mb={2}>
                <Language color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">Display Preferences</Typography>
              </Box>
              
              <Grid container spacing={3}>
                <Grid item xs={12} md={3}>
                  <TextField
                    fullWidth
                    label="Theme"
                    select
                    value={displaySettings.theme}
                    onChange={(e) => setDisplaySettings(prev => ({ ...prev, theme: e.target.value }))}
                    SelectProps={{ native: true }}
                  >
                    <option value="light">Light</option>
                    <option value="dark">Dark</option>
                    <option value="auto">Auto</option>
                  </TextField>
                </Grid>
                
                <Grid item xs={12} md={3}>
                  <TextField
                    fullWidth
                    label="Language"
                    select
                    value={displaySettings.language}
                    onChange={(e) => setDisplaySettings(prev => ({ ...prev, language: e.target.value }))}
                    SelectProps={{ native: true }}
                  >
                    <option value="en">English</option>
                    <option value="es">Spanish</option>
                    <option value="fr">French</option>
                    <option value="de">German</option>
                  </TextField>
                </Grid>
                
                <Grid item xs={12} md={3}>
                  <TextField
                    fullWidth
                    label="Timezone"
                    select
                    value={displaySettings.timezone}
                    onChange={(e) => setDisplaySettings(prev => ({ ...prev, timezone: e.target.value }))}
                    SelectProps={{ native: true }}
                  >
                    <option value="UTC">UTC</option>
                    <option value="EST">Eastern Time</option>
                    <option value="PST">Pacific Time</option>
                    <option value="GMT">Greenwich Mean Time</option>
                  </TextField>
                </Grid>
                
                <Grid item xs={12} md={3}>
                  <TextField
                    fullWidth
                    label="Date Format"
                    select
                    value={displaySettings.dateFormat}
                    onChange={(e) => setDisplaySettings(prev => ({ ...prev, dateFormat: e.target.value }))}
                    SelectProps={{ native: true }}
                  >
                    <option value="MM/DD/YYYY">MM/DD/YYYY</option>
                    <option value="DD/MM/YYYY">DD/MM/YYYY</option>
                    <option value="YYYY-MM-DD">YYYY-MM-DD</option>
                  </TextField>
                </Grid>
              </Grid>
              
              <Box mt={3}>
                <Button 
                  variant="contained" 
                  startIcon={<Save />}
                  onClick={() => handleSave('Display')}
                >
                  Save Display Settings
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* Help & Support */}
        <Grid item xs={12}>
          <Card>
            <CardContent>
              <Box display="flex" alignItems="center" mb={2}>
                <Help color="primary" sx={{ mr: 1 }} />
                <Typography variant="h6">Help & Support</Typography>
              </Box>
              
              <Grid container spacing={2}>
                <Grid item xs={12} md={4}>
                  <Paper sx={{ p: 2, textAlign: 'center' }}>
                    <Typography variant="h6" gutterBottom>
                      User Guide
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      Learn how to use all features
                    </Typography>
                    <Button variant="outlined" fullWidth>
                      View Guide
                    </Button>
                  </Paper>
                </Grid>
                
                <Grid item xs={12} md={4}>
                  <Paper sx={{ p: 2, textAlign: 'center' }}>
                    <Typography variant="h6" gutterBottom>
                      Video Tutorials
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      Watch step-by-step tutorials
                    </Typography>
                    <Button variant="outlined" fullWidth>
                      Watch Videos
                    </Button>
                  </Paper>
                </Grid>
                
                <Grid item xs={12} md={4}>
                  <Paper sx={{ p: 2, textAlign: 'center' }}>
                    <Typography variant="h6" gutterBottom>
                      Contact Support
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                      Get help from our team
                    </Typography>
                    <Button variant="outlined" fullWidth>
                      Contact Us
                    </Button>
                  </Paper>
                </Grid>
              </Grid>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};

export default Settings;
