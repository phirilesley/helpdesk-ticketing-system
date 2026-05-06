import React, { useState } from 'react';
import {
  AppBar,
  Box,
  CssBaseline,
  Drawer,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Toolbar,
  Typography,
  Avatar,
  Menu,
  MenuItem,
  Divider
} from '@mui/material';
import {
  Menu as MenuIcon,
  Dashboard,
  Assessment,
  ConfirmationNumber,
  Add,
  Person,
  Settings,
  Book,
  AccountCircle,
  Logout,
  AdminPanelSettings,
  SupportAgent
} from '@mui/icons-material';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

const drawerWidth = 272;

const Layout: React.FC = () => {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();

  const handleDrawerToggle = () => {
    setMobileOpen(!mobileOpen);
  };

  const handleProfileMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleProfileMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = () => {
    logout();
    handleProfileMenuClose();
  };

  const menuItems = [
    { text: 'Customer Portal', icon: <SupportAgent />, path: '/app/customer-portal' },
    { text: 'Dashboard', icon: <Dashboard />, path: '/app/dashboard' },
    { text: 'Analytics', icon: <Assessment />, path: '/app/admin' },
    { text: 'Enterprise', icon: <AdminPanelSettings />, path: '/app/enterprise', adminOnly: true },
    { text: 'My Tickets', icon: <ConfirmationNumber />, path: '/app/tickets' },
    { text: 'Create Ticket', icon: <Add />, path: '/app/tickets/create' },
    { text: 'Knowledge Base', icon: <Book />, path: '/app/knowledge-base' },
    { text: 'Profile', icon: <Person />, path: '/app/profile' },
    { text: 'Settings', icon: <Settings />, path: '/app/settings' },
  ];

  const drawer = (
    <div>
      <Toolbar sx={{ borderBottom: '1px solid #e2e8f0' }}>
        <Typography variant="h6" noWrap component="div" sx={{ color: '#0f172a' }}>
          NOVA SERVICE DESK
        </Typography>
      </Toolbar>
      <List>
        {menuItems.filter(item => !item.adminOnly || (user?.roles?.includes('Admin') || user?.roles?.includes('SuperAdmin'))).map((item) => (
          <ListItem key={item.text} disablePadding>
            <ListItemButton
              selected={location.pathname === item.path}
              onClick={() => navigate(item.path)}
              sx={{
                mx: 1,
                my: 0.4,
                borderRadius: 2,
                '&.Mui-selected': {
                  background: 'linear-gradient(90deg,#ccfbf1 0%,#dbeafe 100%)',
                  color: '#0f172a',
                  '& .MuiListItemIcon-root': { color: '#0f766e' }
                }
              }}
            >
              <ListItemIcon>{item.icon}</ListItemIcon>
              <ListItemText primary={item.text} />
            </ListItemButton>
          </ListItem>
        ))}
      </List>
    </div>
  );

  return (
    <Box sx={{ display: 'flex' }}>
      <CssBaseline />
      <AppBar
        position="fixed"
        sx={{
          width: { sm: `calc(100% - ${drawerWidth}px)` },
          ml: { sm: `${drawerWidth}px` },
          bgcolor: 'rgba(255,255,255,0.9)',
          color: '#0f172a',
          boxShadow: '0 6px 28px rgba(15,23,42,0.08)',
          borderBottom: '1px solid #e2e8f0',
          backdropFilter: 'blur(8px)'
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            aria-label="open drawer"
            edge="start"
            onClick={handleDrawerToggle}
            sx={{ mr: 2, display: { sm: 'none' } }}
          >
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" noWrap component="div" sx={{ flexGrow: 1 }}>
            Customer Portal
          </Typography>
          <IconButton
            size="large"
            aria-label="account of current user"
            aria-controls="menu-appbar"
            aria-haspopup="true"
            onClick={handleProfileMenuOpen}
            color="default"
          >
            <Avatar sx={{ width: 32, height: 32 }}>
              {user?.firstName?.[0] || <AccountCircle />}
            </Avatar>
          </IconButton>
          <Menu
            id="menu-appbar"
            anchorEl={anchorEl}
            anchorOrigin={{
              vertical: 'top',
              horizontal: 'right',
            }}
            keepMounted
            transformOrigin={{
              vertical: 'top',
              horizontal: 'right',
            }}
            open={Boolean(anchorEl)}
            onClose={handleProfileMenuClose}
          >
            <MenuItem disabled>
              <Typography variant="body2">
                {user?.firstName} {user?.lastName}
              </Typography>
            </MenuItem>
            <Divider />
            <MenuItem onClick={handleLogout}>
              <ListItemIcon>
                <Logout fontSize="small" />
              </ListItemIcon>
              <ListItemText>Logout</ListItemText>
            </MenuItem>
          </Menu>
        </Toolbar>
      </AppBar>
      <Box
        component="nav"
        sx={{ width: { sm: drawerWidth }, flexShrink: { sm: 0 } }}
        aria-label="mailbox folders"
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{
            keepMounted: true,
          }}
          sx={{
            display: { xs: 'block', sm: 'none' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth, borderRight: '1px solid #e2e8f0', background: 'linear-gradient(180deg,#ffffff 0%,#f8fafc 100%)' },
          }}
        >
          {drawer}
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: 'none', sm: 'block' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: drawerWidth, borderRight: '1px solid #e2e8f0', background: 'linear-gradient(180deg,#ffffff 0%,#f8fafc 100%)' },
          }}
          open
        >
          {drawer}
        </Drawer>
      </Box>
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          p: 3,
          width: { sm: `calc(100% - ${drawerWidth}px)` },
        }}
      >
        <Toolbar />
        <Outlet />
      </Box>
    </Box>
  );
};

export default Layout;
