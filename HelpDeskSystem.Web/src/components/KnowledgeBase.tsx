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
  Divider,
  Alert,
  CircularProgress
} from '@mui/material';
import {
  Search,
  ExpandMore,
  Book,
  Lightbulb,
  Build,
  Security,
  Settings,
  Article,
  Category
} from '@mui/icons-material';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import {
  addKnowledgeBaseFeedback,
  getKnowledgeBaseArticles,
  getKnowledgeBaseCategories,
  KnowledgeBaseArticle,
  KnowledgeBaseCategory
} from '../services/api';

const KnowledgeBase: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<number | 'all'>('all');
  const [expandedArticle, setExpandedArticle] = useState<string | false>(false);
  const [feedbackCommentByArticle, setFeedbackCommentByArticle] = useState<Record<number, string>>({});
  const [feedbackSuccess, setFeedbackSuccess] = useState<string>('');
  const queryClient = useQueryClient();

  const { data: categoriesData = [], isLoading: categoriesLoading } = useQuery(
    'kb-categories',
    getKnowledgeBaseCategories
  );

  const { data: articlesData = [], isLoading: articlesLoading, isError: articlesError } = useQuery(
    ['kb-articles', searchTerm, selectedCategory],
    () => getKnowledgeBaseArticles({
      q: searchTerm.trim() || undefined,
      categoryId: selectedCategory === 'all' ? undefined : selectedCategory
    }),
    { keepPreviousData: true }
  );

  const feedbackMutation = useMutation(
    ({ articleId, isHelpful, comment }: { articleId: number; isHelpful: boolean; comment: string }) =>
      addKnowledgeBaseFeedback(articleId, { isHelpful, comment }),
    {
      onSuccess: (_data, vars) => {
        setFeedbackSuccess(`Feedback saved for article #${vars.articleId}.`);
        setFeedbackCommentByArticle((prev) => ({ ...prev, [vars.articleId]: '' }));
        void queryClient.invalidateQueries('kb-articles');
      }
    }
  );

  const categories = useMemo(() => {
    const dynamic = categoriesData
      .slice()
      .sort((a: KnowledgeBaseCategory, b: KnowledgeBaseCategory) => a.displayOrder - b.displayOrder)
      .map((cat: KnowledgeBaseCategory) => ({ name: cat.id, label: cat.name }));

    return [{ name: 'all' as const, label: 'All Categories' }, ...dynamic];
  }, [categoriesData]);

  const getCategoryIcon = (categoryName: string) => {
    const normalized = categoryName.toLowerCase();
    if (normalized.includes('security')) return <Security />;
    if (normalized.includes('setting')) return <Settings />;
    if (normalized.includes('file')) return <Build />;
    if (normalized.includes('start')) return <Lightbulb />;
    if (normalized.includes('ticket')) return <Article />;
    return <Book />;
  };

  const handleArticleExpand = (articleId: string) => (_event: React.SyntheticEvent, isExpanded: boolean) => {
    setExpandedArticle(isExpanded ? articleId : false);
  };

  const helpfulPercent = (article: KnowledgeBaseArticle) => {
    const total = article.helpfulCount + article.unhelpfulCount;
    if (total === 0) return 0;
    return Math.round((article.helpfulCount / total) * 100);
  };

  const filteredArticles = articlesData;
  const loading = categoriesLoading || articlesLoading;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Knowledge Base
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Find answers to common questions and submit real article feedback.
      </Typography>

      {feedbackSuccess && (
        <Alert severity="success" sx={{ mb: 2 }} onClose={() => setFeedbackSuccess('')}>
          {feedbackSuccess}
        </Alert>
      )}

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
            )
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
                <ListItem key={String(category.name)} disablePadding>
                  <ListItemButton
                    selected={selectedCategory === category.name}
                    onClick={() => setSelectedCategory(category.name)}
                  >
                    <ListItemIcon>{category.name === 'all' ? <Category /> : getCategoryIcon(category.label)}</ListItemIcon>
                    <ListItemText primary={category.label} />
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          </Paper>
        </Grid>

        <Grid item xs={12} md={9}>
          {loading && (
            <Box sx={{ py: 4, display: 'flex', justifyContent: 'center' }}>
              <CircularProgress />
            </Box>
          )}

          {!loading && articlesError && (
            <Alert severity="error" sx={{ mb: 2 }}>
              Failed to load knowledge base articles.
            </Alert>
          )}

          {!loading && !articlesError && (
            <>
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
                        {getCategoryIcon(article.categoryName)}
                        <Box sx={{ ml: 2, flex: 1 }}>
                          <Typography variant="h6">{article.title}</Typography>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mt: 1 }}>
                            <Chip size="small" label={article.categoryName} />
                            <Typography variant="caption" color="text.secondary">
                              Helpful: {article.helpfulCount} | Not Helpful: {article.unhelpfulCount} | {helpfulPercent(article)}% helpful
                            </Typography>
                          </Box>
                        </Box>
                      </Box>
                    </AccordionSummary>
                    <AccordionDetails>
                      <Typography variant="body1" paragraph>
                        {article.body || article.summary}
                      </Typography>

                      <Divider sx={{ my: 2 }} />

                      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 2, flexWrap: 'wrap' }}>
                        <Box>
                          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                            Keywords: {article.searchKeywords || 'N/A'}
                          </Typography>
                          <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                            Published: {article.publishedAtUtc ? new Date(article.publishedAtUtc).toLocaleString() : 'N/A'}
                          </Typography>
                        </Box>
                        <Box sx={{ minWidth: 320, width: { xs: '100%', sm: 360 } }}>
                          <TextField
                            fullWidth
                            size="small"
                            placeholder="Optional comment for your review"
                            value={feedbackCommentByArticle[article.id] ?? ''}
                            onChange={(e) =>
                              setFeedbackCommentByArticle((prev) => ({ ...prev, [article.id]: e.target.value }))
                            }
                            sx={{ mb: 1 }}
                          />
                          <Box>
                            <Button
                              variant="outlined"
                              size="small"
                              sx={{ mr: 1 }}
                              disabled={feedbackMutation.isLoading}
                              onClick={() =>
                                feedbackMutation.mutate({
                                  articleId: article.id,
                                  isHelpful: true,
                                  comment: feedbackCommentByArticle[article.id] ?? ''
                                })
                              }
                            >
                              Helpful
                            </Button>
                            <Button
                              variant="outlined"
                              size="small"
                              disabled={feedbackMutation.isLoading}
                              onClick={() =>
                                feedbackMutation.mutate({
                                  articleId: article.id,
                                  isHelpful: false,
                                  comment: feedbackCommentByArticle[article.id] ?? ''
                                })
                              }
                            >
                              Not Helpful
                            </Button>
                          </Box>
                        </Box>
                      </Box>
                    </AccordionDetails>
                  </Accordion>
                ))
              )}
            </>
          )}
        </Grid>
      </Grid>

      {!loading && filteredArticles.length > 0 && (
        <Paper sx={{ p: 3, mt: 3 }}>
          <Typography variant="h6" gutterBottom>
            Top Reviewed Articles
          </Typography>
          <Grid container spacing={2}>
            {[...filteredArticles]
              .sort((a, b) => (b.helpfulCount + b.unhelpfulCount) - (a.helpfulCount + a.unhelpfulCount))
              .slice(0, 3)
              .map((article) => (
                <Grid item xs={12} md={4} key={article.id}>
                  <Card variant="outlined">
                    <CardContent>
                      <Box display="flex" alignItems="center" mb={1}>
                        {getCategoryIcon(article.categoryName)}
                        <Typography variant="subtitle2" sx={{ ml: 1 }}>
                          {article.categoryName}
                        </Typography>
                      </Box>
                      <Typography variant="body2" gutterBottom>
                        {article.title}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        Helpful: {article.helpfulCount} | Not Helpful: {article.unhelpfulCount}
                      </Typography>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
          </Grid>
        </Paper>
      )}
    </Box>
  );
};

export default KnowledgeBase;
