import React, { useState, useEffect } from 'react';
import { Card, CardContent, Typography, Grid, Button, Box, LinearProgress, Chip, Alert, Tabs, Tab, Table, TableBody, TableCell, TableHead, TableRow } from '@mui/material';
import { GitHub, Code, Build, Timeline, TimelineItem, TimelineConnector, TimelineSeparator, TimelineDot, TimelineContent } from '@mui/icons-material';
import { LineChart, BarChart, PieChart, Activity } from 'recharts';
import { format } from 'date-fns';

interface Repository {
    id: string;
    name: string;
    fullName: string;
    language: string;
    stargazersCount: number;
    forksCount: number;
    openIssuesCount: number;
    private: boolean;
    htmlUrl: string;
    description: string;
    createdAt: string;
    updatedAt: string;
}

interface Commit {
    sha: string;
    message: string;
    author: {
        name: string;
        email: string;
        date: string;
    };
    url: string;
}

interface PullRequest {
    number: number;
    title: string;
    state: string;
    user: {
        login: string;
    };
    created_at: string;
    updated_at: string;
    html_url: string;
}

interface BuildStatus {
    id: string;
    status: string;
    conclusion: string;
    created_at: string;
    updated_at: string;
    workflow: string;
}

interface DeploymentMetrics {
    totalDeployments: number;
    successfulDeployments: number;
    failedDeployments: number;
    averageDeploymentTime: number;
    deploymentsThisWeek: number;
}

interface CodeQualityMetrics {
    coverage: number;
    maintainability: number;
    reliability: number;
    security: number;
    testPassRate: number;
    codeSmells: number;
    technicalDebt: number;
}

const DevOpsDashboard: React.FC = () => {
    const [activeTab, setActiveTab] = useState(0);
    const [repositories, setRepositories] = useState<Repository[]>([]);
    const [selectedRepo, setSelectedRepo] = useState<Repository | null>(null);
    const [commits, setCommits] = useState<Commit[]>([]);
    const [pullRequests, setPullRequests] = useState<PullRequest[]>([]);
    const [buildStatus, setBuildStatus] = useState<BuildStatus[]>([]);
    const [deploymentMetrics, setDeploymentMetrics] = useState<DeploymentMetrics | null>(null);
    const [qualityMetrics, setQualityMetrics] = useState<CodeQualityMetrics | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        fetchDevOpsData();
    }, []);

    const fetchDevOpsData = async () => {
        setLoading(true);
        setError(null);
        try {
            // Fetch repositories
            const reposResponse = await fetch('/api/devops/github/repositories');
            const reposData = await reposResponse.json();
            setRepositories(reposData);

            // Fetch recent commits for first repo if available
            if (reposData.length > 0) {
                const commitsResponse = await fetch(`/api/devops/github/repositories/${reposData[0].owner.login}/${reposData[0].name}/commits`);
                const commitsData = await commitsResponse.json();
                setCommits(commitsData);

                // Fetch pull requests
                const prsResponse = await fetch(`/api/devops/github/repositories/${reposData[0].owner.login}/${reposData[0].name}/pulls`);
                const prsData = await prsResponse.json();
                setPullRequests(prsData);

                // Fetch workflow runs
                const runsResponse = await fetch(`/api/devops/github/repositories/${reposData[0].owner.login}/${reposData[0].name}/actions/runs`);
                const runsData = await runsResponse.json();
                setBuildStatus(runsData.workflow_runs || []);

                setSelectedRepo(reposData[0]);
            }

            // Fetch deployment metrics
            const deploymentResponse = await fetch('/api/devops/deployments/recent');
            const deploymentData = await deploymentResponse.json();
            setDeploymentMetrics(deploymentData);

            // Fetch code quality metrics
            const qualityResponse = await fetch(`/api/devops/repositories/${reposData[0]?.owner?.login}/${reposData[0]?.name}/quality`);
            const qualityData = await qualityResponse.json();
            setQualityMetrics(qualityData);

        } catch (err) {
            setError('Failed to fetch DevOps data');
            console.error('Error fetching DevOps data:', err);
        } finally {
            setLoading(false);
        }
    };

    const handleRepoSelect = (repo: Repository) => {
        setSelectedRepo(repo);
        // Fetch commits for selected repo
        fetchCommitsForRepo(repo);
        // Fetch pull requests for selected repo
        fetchPullRequestsForRepo(repo);
    };

    const fetchCommitsForRepo = async (repo: Repository) => {
        try {
            const response = await fetch(`/api/devops/github/repositories/${repo.owner.login}/${repo.name}/commits`);
            const data = await response.json();
            setCommits(data);
        } catch (err) {
            console.error('Error fetching commits:', err);
        }
    };

    const fetchPullRequestsForRepo = async (repo: Repository) => {
        try {
            const response = await fetch(`/api/devops/github/repositories/${repo.owner.login}/${repo.name}/pulls`);
            const data = await response.json();
            setPullRequests(data);
        } catch (err) {
            console.error('Error fetching pull requests:', err);
        }
    };

    const triggerWorkflow = async (repo: Repository) => {
        try {
            const response = await fetch(`/api/devops/github/repositories/${repo.owner.login}/${repo.name}/actions/workflows/main/dispatches`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    inputs: {
                        branch: 'main',
                        message: 'Manual trigger from dashboard'
                    }
                })
            });
            const data = await response.json();
            // Refresh workflow runs
            const runsResponse = await fetch(`/api/devops/github/repositories/${repo.owner.login}/${repo.name}/actions/runs`);
            const runsData = await runsResponse.json();
            setBuildStatus(runsData.workflow_runs || []);
        } catch (err) {
            console.error('Error triggering workflow:', err);
        }
    };

    const getStatusColor = (status: string) => {
        switch (status.toLowerCase()) {
            case 'success': return 'success';
            case 'failure': return 'error';
            case 'pending': return 'warning';
            case 'cancelled': return 'info';
            default: return 'default';
        }
    };

    const getQualityColor = (score: number) => {
        if (score >= 90) return 'success';
        if (score >= 70) return 'warning';
        return 'error';
    };

    const renderRepositoryCard = (repo: Repository) => (
        <Card 
            key={repo.id}
            sx={{ cursor: 'pointer', mb: 2 }}
            onClick={() => handleRepoSelect(repo)}
            className={selectedRepo?.id === repo.id ? 'selected' : ''}
        >
            <CardContent>
                <Typography variant="h6" component="h3">
                    {repo.name}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    {repo.description}
                </Typography>
                <Box sx={{ mt: 2 }}>
                    <Grid container spacing={1}>
                        <Grid item xs={6}>
                            <Typography variant="caption">⭐ {repo.stargazersCount}</Typography>
                        </Grid>
                        <Grid item xs={6}>
                            <Typography variant="caption">🍴 {repo.forksCount}</Typography>
                        </Grid>
                    </Grid>
                    <Grid container spacing={1}>
                        <Grid item xs={6}>
                            <Typography variant="caption">🐛 {repo.openIssuesCount}</Typography>
                        </Grid>
                        <Grid item xs={6}>
                            <Chip 
                                label={repo.language} 
                                size="small" 
                                color={repo.private ? "default" : "primary"}
                                variant="outlined"
                            />
                        </Grid>
                    </Grid>
                </Box>
                <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="caption">
                        Updated {format(new Date(repo.updatedAt), 'MMM d, yyyy')}
                    </Typography>
                    <Button 
                        size="small" 
                        variant="outlined"
                        onClick={() => window.open(repo.htmlUrl, '_blank')}
                    >
                        View on GitHub
                    </Button>
                </Box>
            </CardContent>
        </Card>
    );

    return (
        <Box sx={{ p: 3 }}>
            <Typography variant="h4" component="h2" gutterBottom={3}>
                <GitHub sx={{ mr: 1 }} /> DevOps Dashboard
            </Typography>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
                    {error}
                </Alert>
            )}

            {loading ? (
                <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 400 }}>
                    <Typography>Loading DevOps data...</Typography>
                </Box>
            ) : (
                <Box>
                    <Tabs value={activeTab} onChange={(e, newValue) => setActiveTab(newValue as number)}>
                        <Tab label="Overview" />
                        <Tab label="Repositories" />
                        <Tab label="CI/CD" />
                        <Tab label="Code Quality" />
                        <Tab label="Deployments" />
                    </Tabs>

                    {activeTab === 0 && (
                        <Grid container spacing={3}>
                            {/* Key Metrics */}
                            <Grid item xs={12} md={6}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Overview
                                        </Typography>
                                        <Box sx={{ mt: 2 }}>
                                            <Grid container spacing={2}>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="primary">
                                                            {repositories.length}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Total Repositories
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="success">
                                                            {pullRequests.length}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Open PRs
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                            </Grid>
                                            <Grid container spacing={2}>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="info">
                                                            {buildStatus?.length || 0}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Workflow Runs
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="warning">
                                                            {deploymentMetrics?.totalDeployments || 0}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Deployments
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                            </Grid>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* Recent Activity */}
                            <Grid item xs={12} md={6}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Recent Activity
                                        </Typography>
                                        <Timeline sx={{ mt: 2 }}>
                                            {commits.slice(0, 5).map((commit, index) => (
                                                <TimelineItem key={commit.sha}>
                                                    <TimelineDot color="primary" />
                                                    <TimelineContent>
                                                        <Typography variant="subtitle2">
                                                            {commit.message.substring(0, 50)}...
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            {commit.author.name} • {format(new Date(commit.author.date, 'PP')}
                                                        </Typography>
                                                    </TimelineContent>
                                                    {index < commits.length - 1 && (
                                                        <TimelineConnector />
                                                    )}
                                                </TimelineItem>
                                            ))}
                                        </Timeline>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* Quick Actions */}
                            <Grid item xs={12}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Quick Actions
                                        </Typography>
                                        <Box sx={{ mt: 2, display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                                            {selectedRepo && (
                                                <Button
                                                    variant="outlined"
                                                    startIcon={<Build />}
                                                    onClick={() => triggerWorkflow(selectedRepo)}
                                                    disabled={loading}
                                                >
                                                    Trigger Build
                                                </Button>
                                            )}
                                            {selectedRepo && (
                                                <Button
                                                    variant="outlined"
                                                    startIcon={<Code />}
                                                    onClick={() => window.open(`${selectedRepo.htmlUrl}/actions`, '_blank')}
                                                >
                                                    View Actions
                                                </Button>
                                            )}
                                            <Button
                                                variant="outlined"
                                                startIcon={<Activity />}
                                                onClick={() => setActiveTab(2)}
                                            >
                                                View CI/CD
                                            </Button>
                                            <Button
                                                variant="outlined"
                                                startIcon={<GitHub />}
                                                onClick={() => setActiveTab(1)}
                                            >
                                                View Repositories
                                            </Button>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>
                        </Grid>
                    )}

                    {activeTab === 1 && (
                        <Grid container spacing={3}>
                            {repositories.map(repo => renderRepositoryCard(repo))}
                        </Grid>
                    )}

                    {activeTab === 2 && selectedRepo && (
                        <Grid container spacing={3}>
                            {/* Build Status */}
                            <Grid item xs={12}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Build Status - {selectedRepo.name}
                                        </Typography>
                                        <Table size="small">
                                            <TableHead>
                                                <TableRow>
                                                    <TableCell>Status</TableCell>
                                                    <TableCell>Workflow</TableCell>
                                                    <TableCell>Created</TableCell>
                                                    <TableCell>Conclusion</TableCell>
                                                </TableRow>
                                            </TableHead>
                                            <TableBody>
                                                {buildStatus?.map((build) => (
                                                    <TableRow key={build.id}>
                                                        <TableCell>
                                                            <Chip
                                                                label={build.status}
                                                                color={getStatusColor(build.status)}
                                                                size="small"
                                                            />
                                                        </TableCell>
                                                        <TableCell>{build.workflow}</TableCell>
                                                        <TableCell>
                                                            {format(new Date(build.created_at), 'MMM d, HH:mm')}
                                                        </TableCell>
                                                        <TableCell>
                                                            <Chip
                                                                label={build.conclusion || 'N/A'}
                                                                color={getStatusColor(build.conclusion || 'default')}
                                                                size="small"
                                                            />
                                                        </TableCell>
                                                    </TableRow>
                                                ))}
                                            </TableBody>
                                        </Table>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* Pull Requests */}
                            <Grid item xs={12}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Pull Requests - {selectedRepo.name}
                                        </Typography>
                                        <Table size="small">
                                            <TableHead>
                                                <TableRow>
                                                    <TableCell>#</TableCell>
                                                    <TableCell>Title</TableCell>
                                                    <TableCell>Author</TableCell>
                                                    <TableCell>Status</TableCell>
                                                    <TableCell>Created</TableCell>
                                                </TableRow>
                                            </TableHead>
                                            <TableBody>
                                                {pullRequests.map((pr) => (
                                                    <TableRow key={pr.number}>
                                                        <TableCell>#{pr.number}</TableCell>
                                                        <TableCell>{pr.title}</TableCell>
                                                        <TableCell>{pr.user?.login}</TableCell>
                                                        <TableCell>
                                                            <Chip
                                                                label={pr.state}
                                                                color={getStatusColor(pr.state)}
                                                                size="small"
                                                            />
                                                        </TableCell>
                                                        <TableCell>
                                                            {format(new Date(pr.created_at), 'MMM d, HH:mm')}
                                                        </TableCell>
                                                    </TableRow>
                                                ))}
                                            </TableBody>
                                        </Table>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* Commits */}
                            <Grid item xs={12}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Recent Commits - {selectedRepo.name}
                                        </Typography>
                                        <Timeline sx={{ mt: 2 }}>
                                            {commits.slice(0, 10).map((commit, index) => (
                                                <TimelineItem key={commit.sha}>
                                                    <TimelineDot color="primary" />
                                                    <TimelineContent>
                                                        <Typography variant="subtitle2">
                                                            {commit.message}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            {commit.author.name} • {format(new Date(commit.author.date, 'PP')}
                                                        </Typography>
                                                        <Typography variant="body2" sx={{ mt: 1 }}>
                                                            <code>{commit.sha.substring(0, 7)}</code>
                                                        </Typography>
                                                    </TimelineContent>
                                                    {index < commits.length - 1 && (
                                                        <TimelineConnector />
                                                    )}
                                                </TimelineItem>
                                            ))}
                                        </Timeline>
                                    </CardContent>
                                </Card>
                            </Grid>
                        </Grid>
                    )}

                    {activeTab === 3 && qualityMetrics && (
                        <Grid container spacing={3}>
                            {/* Quality Metrics */}
                            <Grid item xs={12}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Code Quality Metrics
                                        </Typography>
                                        <Box sx={{ mt: 2 }}>
                                            <Grid container spacing={2}>
                                                <Grid item xs={3}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <CircularProgress
                                                            variant="determinate"
                                                            value={qualityMetrics.coverage}
                                                            color={getQualityColor(qualityMetrics.coverage)}
                                                            size={60}
                                                        />
                                                        <Typography variant="caption" sx={{ mt: 1 }}>
                                                            Coverage
                                                        </Typography>
                                                        <Typography variant="h6" color={getQualityColor(qualityMetrics.coverage)}>
                                                            {qualityMetrics.coverage}%
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={3}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <CircularProgress
                                                            variant="determinate"
                                                            value={qualityMetrics.maintainability}
                                                            color={getQualityColor(qualityMetrics.maintainability)}
                                                            size={60}
                                                        />
                                                        <Typography variant="caption" sx={{ mt: 1 }}>
                                                            Maintainability
                                                        </Typography>
                                                        <Typography variant="h6" color={getQualityColor(qualityMetrics.maintainability)}>
                                                            {qualityMetrics.maintainability}%
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={3}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <CircularProgress
                                                            variant="determinate"
                                                            value={qualityMetrics.reliability}
                                                            color={getQualityColor(qualityMetrics.reliability)}
                                                            size={60}
                                                        />
                                                        <Typography variant="caption" sx={{ mt: 1 }}>
                                                            Reliability
                                                        </Typography>
                                                        <Typography variant="h6" color={getQualityColor(qualityMetrics.reliability)}>
                                                            {qualityMetrics.reliability}%
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={3}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <CircularProgress
                                                            variant="determinate"
                                                            value={qualityMetrics.security}
                                                            color={getQualityColor(qualityMetrics.security)}
                                                            size={60}
                                                        />
                                                        <Typography variant="caption" sx={{ mt: 1 }}>
                                                            Security
                                                        </Typography>
                                                        <Typography variant="h6" color={getQualityColor(qualityMetrics.security)}>
                                                            {qualityMetrics.security}%
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                            </Grid>
                                        </Box>

                                        <Box sx={{ mt: 3 }}>
                                            <Typography variant="h6" component="h4">
                                                Additional Metrics
                                            </Typography>
                                            <Grid container spacing={2}>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h5" color="success">
                                                            {qualityMetrics.testPassRate}%
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Test Pass Rate
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h5" color="warning">
                                                            {qualityMetrics.codeSmells}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Code Smells
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                            </Grid>
                                        </Box>

                                        <Box sx={{ mt: 2 }}>
                                            <Typography variant="h6" component="h4">
                                                Technical Debt
                                            </Typography>
                                            <Box sx={{ mt: 1 }}>
                                                <LinearProgress
                                                    variant="determinate"
                                                    value={qualityMetrics.technicalDebt}
                                                    color={getQualityColor(100 - qualityMetrics.technicalDebt)}
                                                />
                                                <Typography variant="caption">
                                                    {qualityMetrics.technicalDebt} days of technical debt
                                                </Typography>
                                            </Box>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* Quality Trends */}
                            <Grid item xs={12}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Quality Trends
                                        </Typography>
                                        <Box sx={{ height: 300, mt: 2 }}>
                                            <LineChart
                                                data={[
                                                    { name: 'Coverage', data: qualityMetrics.coverage },
                                                    { name: 'Maintainability', data: qualityMetrics.maintainability },
                                                    { name: 'Reliability', data: qualityMetrics.reliability },
                                                    { name: 'Security', data: qualityMetrics.security }
                                                ]}
                                                margin={{ top: 5, right: 30, bottom: 5, left: 0 }}
                                            />
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>
                        </Grid>
                    )}

                    {activeTab === 4 && deploymentMetrics && (
                        <Grid container spacing={3}>
                            {/* Deployment Overview */}
                            <Grid item xs={12} md={6}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Deployment Overview
                                        </Typography>
                                        <Box sx={{ mt: 2 }}>
                                            <Grid container spacing={2}}>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="success">
                                                            {deploymentMetrics.successfulDeployments}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Success Rate
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="error">
                                                            {deploymentMetrics.failedDeployments}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Failures
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                            </Grid>
                                        </Box>

                                        <Box sx={{ mt: 2 }}>
                                            <Typography variant="h6" component="h4">
                                                Performance Metrics
                                            </Typography>
                                            <Grid container spacing={2}>
                                                <Grid item xs={4}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h5" color="primary">
                                                            {deploymentMetrics.averageDeploymentTime}m
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Avg Deployment Time
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={4}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h5" color="info">
                                                            {deploymentMetrics.deploymentsThisWeek}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            This Week
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={4}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h5">
                                                            {Math.round((deploymentMetrics.successfulDeployments / deploymentMetrics.totalDeployments) * 100)}%
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Success Rate
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                            </Grid>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* Deployment History */}
                            <Grid item xs={12} md={6}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Recent Deployments
                                        </Typography>
                                        <Table size="small">
                                            <TableHead>
                                                <TableRow>
                                                    <TableCell>Repository</TableCell>
                                                    <TableCell>Branch</TableCell>
                                                    <TableCell>Status</TableCell>
                                                    <TableCell>Deployed At</TableCell>
                                                </TableRow>
                                            </TableHead>
                                            <TableBody>
                                                {/* Placeholder for recent deployments */}
                                                <TableRow>
                                                    <TableCell>main</TableCell>
                                                    <TableCell>main</TableCell>
                                                    <TableCell>Success</TableCell>
                                                    <TableCell>2 hours ago</TableCell>
                                                </TableRow>
                                                <TableRow>
                                                    <TableCell>api</TableCell>
                                                    <TableCell>develop</TableCell>
                                                    <TableCell>Failed</TableCell>
                                                    <TableCell>5 hours ago</TableCell>
                                                </TableRow>
                                            </TableBody>
                                        </Table>
                                    </CardContent>
                                </Card>
                            </Grid>
                        </Grid>
                    )}
                </Box>
            )}
    );
};

export default DevOpsDashboard;
