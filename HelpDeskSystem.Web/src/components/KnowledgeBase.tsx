import React, { useMemo, useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  TextField,
  Button,
  Chip,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  ListItemButton,
  Paper,
  Grid,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  InputAdornment,
  Divider
} from '@mui/material';
import {
  Search,
  ExpandMore,
  Book,
  HelpOutline,
  Lightbulb,
  Build,
  Security,
  Settings,
  Article,
  Category
} from '@mui/icons-material';

interface ArticleItem {
  id: number;
  title: string;
  category: string;
  content: string;
  tags: string[];
  views: number;
  helpful: number;
  lastUpdated: string;
}

const KnowledgeBase: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('all');
  const [expandedArticle, setExpandedArticle] = useState<string | false>(false);

  const articles: ArticleItem[] = useMemo(() => [
    {
      id: 1,
      title: 'How to Reset Your Password',
      category: 'Account',
      content: "To reset your password, click the 'Forgot Password' link on the login page. Enter your email address and follow the instructions sent to your email.",
      tags: ['password', 'login', 'account'],
      views: 1250,
      helpful: 89,
      lastUpdated: '2024-01-15'
    },
    {
      id: 2,
      title: 'Creating a New Support Ticket',
      category: 'Getting Started',
      content: "Click 'Create Ticket' in navigation, complete title, description, priority, and category, then submit.",
      tags: ['ticket', 'create', 'support'],
      views: 980,
      helpful: 92,
      lastUpdated: '2024-01-20'
    },
    {
      id: 3,
      title: 'Understanding Ticket Statuses',
      category: 'Tickets',
      content: 'Ticket statuses: New, InProgress, Waiting, Resolved, Closed, Reopened, and Escalated.',
      tags: ['status', 'workflow', 'process'],
      views: 756,
      helpful: 85,
      lastUpdated: '2024-01-18'
    },
    {
      id: 4,
      title: 'File Attachment Guidelines',
      category: 'Files',
      content: 'Attach files up to 10MB. Common document and image formats are supported.',
      tags: ['files', 'attachments', 'upload'],
      views: 623,
      helpful: 78,
      lastUpdated: '2024-01-12'
    },
    {
      id: 5,
      title: 'Two-Factor Authentication Setup',
      category: 'Security',
      content: 'Enable MFA under profile security settings and verify your authenticator app code.',
      tags: ['2fa', 'security', 'authentication'],
      views: 445,
      helpful: 91,
      lastUpdated: '2024-01-22'
    }
  ], []);

  const categories = [
    { name: 'all', label: 'All Categories', icon: <Category /> },
    { name: 'Getting Started', label: 'Getting Started', icon: <Lightbulb /> },
    { name: 'Account', label: 'Account', icon: <HelpOutline /> },
    { name: 'Tickets', label: 'Tickets', icon: <Article /> },
    { name: 'Files', label: 'Files', icon: <Build /> },
    { name: 'Security', label: 'Security', icon: <Security /> },
    { name: 'Settings', label: 'Settings', icon: <Settings /> }
  ];

  const filteredArticles = useMemo(
    () => articles.filter((article) => {
      const search = searchTerm.trim().toLowerCase();
      const matchesSearch = !search
        || article.title.toLowerCase().includes(search)
        || article.content.toLowerCase().includes(search)
        || article.tags.some((tag) => tag.toLowerCase().includes(search));
      const matchesCategory = selectedCategory === 'all' || article.category === selectedCategory;
      return matchesSearch && matchesCategory;
    }),
    [articles, searchTerm, selectedCategory]
  );

  const handleArticleExpand = (articleId: string) => (_event: React.SyntheticEvent, isExpanded: boolean) => {
    setExpandedArticle(isExpanded ? articleId : false);
  };

  const getCategoryIcon = (categoryName: string) => {
    const category = categories.find((cat) => cat.name === categoryName);
    return category?.icon || <Book />;
  };

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Knowledge Base
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Find answers to common questions and use guides.
      </Typography>

      <Paper sx={{ p: 2, mb: 3 }}>
        <TextField
          fullWidth
          placeholder="Search articles, topics, or keywords..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <Search />
              </InputAdornment>
            ),
          }}
        />
      </Paper>

      <Grid container spacing={3}>
        <Grid item xs={12} md={3}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Categories
            </Typography>
            <List>
              {categories.map((category) => (
                <ListItem key={category.name} disablePadding>
                  <ListItemButton
                    selected={selectedCategory === category.name}
                    onClick={() => setSelectedCategory(category.name)}
                  >
                    <ListItemIcon>{category.icon}</ListItemIcon>
                    <ListItemText primary={category.label} />
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          </Paper>
        </Grid>

        <Grid item xs={12} md={9}>
          <Typography variant="h6" gutterBottom>
            {filteredArticles.length} articles found
          </Typography>

          {filteredArticles.length === 0 ? (
            <Paper sx={{ p: 4, textAlign: 'center' }}>
              <Typography variant="h6" color="text.secondary">
                No articles found
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                Try different search keywords or categories.
              </Typography>
            </Paper>
          ) : (
            filteredArticles.map((article) => (
              <Accordion
                key={article.id}
                expanded={expandedArticle === article.id.toString()}
                onChange={handleArticleExpand(article.id.toString())}
                sx={{ mb: 1 }}
              >
                <AccordionSummary expandIcon={<ExpandMore />}>
                  <Box sx={{ display: 'flex', alignItems: 'center', width: '100%' }}>
                    {getCategoryIcon(article.category)}
                    <Box sx={{ ml: 2, flex: 1 }}>
                      <Typography variant="h6">{article.title}</Typography>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 1 }}>
                        <Chip size="small" label={article.category} />
                        <Typography variant="caption" color="text.secondary">
                          {article.views} views | {article.helpful}% helpful
                        </Typography>
                      </Box>
                    </Box>
                  </Box>
                </AccordionSummary>
                <AccordionDetails>
                  <Typography variant="body1" paragraph>
                    {article.content}
                  </Typography>

                  <Divider sx={{ my: 2 }} />

                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Box>
                      <Typography variant="caption" color="text.secondary">
                        Tags: {article.tags.join(', ')}
                      </Typography>
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                        Last updated: {article.lastUpdated}
                      </Typography>
                    </Box>
                    <Box>
                      <Button variant="outlined" size="small" sx={{ mr: 1 }}>
                        Helpful
                      </Button>
                      <Button variant="outlined" size="small">
                        Not Helpful
                      </Button>
                    </Box>
                  </Box>
                </AccordionDetails>
              </Accordion>
            ))
          )}
        </Grid>
      </Grid>

      <Paper sx={{ p: 3, mt: 3 }}>
        <Typography variant="h6" gutterBottom>
          Popular Articles
        </Typography>
        <Grid container spacing={2}>
          {[...articles]
            .sort((a, b) => b.views - a.views)
            .slice(0, 3)
            .map((article) => (
              <Grid item xs={12} md={4} key={article.id}>
                <Card variant="outlined">
                  <CardContent>
                    <Box display="flex" alignItems="center" mb={1}>
                      {getCategoryIcon(article.category)}
                      <Typography variant="subtitle2" sx={{ ml: 1 }}>
                        {article.category}
                      </Typography>
                    </Box>
                    <Typography variant="body2" gutterBottom>
                      {article.title}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {article.views} views | {article.helpful}% helpful
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            ))}
        </Grid>
      </Paper>
    </Box>
  );
};

export default KnowledgeBase;
