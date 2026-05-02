import React, { useState, useEffect } from 'react';
import { Card, CardContent, Typography, Grid, Button, Box, Chip, Alert, Tabs, Tab, Table, TableBody, TableCell, TableHead, TableRow, Badge, LinearProgress } from '@mui/material';
import { 
    Security, 
    Assessment, 
    Timeline, 
    TimelineItem, 
    TimelineConnector, 
    TimelineDot, 
    TimelineContent,
    Warning,
    CheckCircle,
    Error,
    Info
} from '@mui/icons-material';
import { format } from 'date-fns';

interface Incident {
    id: number;
    incidentNumber: string;
    title: string;
    description: string;
    status: IncidentStatus;
    priority: string;
    category: string;
    impact: string;
    urgency: string;
    assignedToUserId?: string;
    assignmentGroup?: string;
    createdAt: string;
    updatedAt: string;
    resolvedAt?: string;
    closedAt?: string;
    resolution?: string;
    resolutionCode?: string;
    satisfactionRating?: string;
}

interface Problem {
    id: number;
    problemNumber: string;
    title: string;
    description: string;
    status: ProblemStatus;
    priority: string;
    category: string;
    createdAt: string;
    updatedAt: string;
    rootCause?: string;
    permanentFix?: string;
    fixedAt?: DateTime;
    relatedIncidents: number[];
}

interface ChangeRequest {
    id: number;
    changeNumber: string;
    title: string;
    description: string;
    status: ChangeStatus;
    priority: string;
    category: string;
    riskAssessment?: string;
    impactAssessment?: string;
    createdAt: string;
    updatedAt: string;
    scheduledDate?: DateTime;
                estimatedDuration?: TimeSpan;
                implementedAt?: DateTime;
                reviewAt?: DateTime;
}

interface SLABreach {
    id: number;
    slaId: number;
    breachDetails: string;
    breachTime: DateTime;
    recordedAt: DateTime;
    status: BreachStatus;
    resolvedAt?: DateTime;
}

interface ITILComplianceReport {
    reportPeriod: string;
    generatedAt: DateTime;
    framework: string;
    incidentManagementCompliance: double;
    problemManagementCompliance: double;
    changeManagementCompliance: double;
    sLACompliance: double;
    overallComplianceScore: double;
    recommendations: ComplianceRecommendation[];
}

interface ComplianceRecommendation {
    recommendation: string;
    priority: string;
    category: string;
    action: string;
    dueDate?: Date;
}

interface ITILMetrics {
    totalIncidents: number;
    resolvedIncidents: number;
    averageResolutionTime: number;
    customerSatisfaction: double;
    totalProblems: number;
    resolvedProblems: number;
    totalChanges: number;
    successfulChanges: number;
    failedChanges: number;
    totalSLABreaches: number;
    activeAudits: number;
    complianceScore: double;
}

const ITSMPanel: React.FC = () => {
    const [activeTab, setActiveTab] = useState(0);
    const [incidents, setIncidents] = useState<Incident[]>([]);
    const [problems, setProblems] = useState<Problem[]>([]);
    const [changes, setChanges] = useState<ChangeRequest[]>([]);
    const [slaBreaches, setSLABreaches] = useState<SLABreach[]>([]);
    const [complianceReport, setComplianceReport] = useState<ITILComplianceReport | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [itilMetrics, setITILMetrics] = useState<ITILMetrics | null>(null);

    useEffect(() => {
        fetchITSMData();
    }, []);

    const fetchITSMData = async () => {
        setLoading(true);
        setError(null);
        try {
            // Fetch incidents
            const incidentsResponse = await fetch('/api/itsm/incidents');
            const incidentsData = await incidentsResponse.json();
            setIncidents(incidentsData);

            // Fetch problems
            const problemsResponse = await fetch('/api/itsm/problems');
            const problemsData = await problemsResponse.json();
            setProblems(problemsData);

            // Fetch changes
            const changesResponse = await fetch('/api/itsm/changes');
            const changesData = await changesResponse.json();
            setChanges(changesData);

            // Fetch SLA breaches
            const breachesResponse = await fetch('/api/itsm/sla-breaches');
            const breachesData = await breachesResponse.json();
            setSLABreaches(breachesData);

            // Fetch ITIL compliance report
            const complianceResponse = await fetch('/api/itil/compliance/reports', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    startDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
                    endDate: new Date().toISOString()
                })
            });
            const complianceData = await complianceResponse.json();
            setComplianceReport(complianceData);

            // Calculate ITIL metrics
            const metrics = await fetch('/api/itil/metrics');
            const metricsData = await metrics.json();
            setITILMetrics(metricsData);

        } catch (err) {
            setError('Failed to fetch ITSM data');
            console.error('Error fetching ITSM data:', err);
        } finally {
            setLoading(false);
        }
    };

    const getStatusColor = (status: string) => {
        switch (status?.toLowerCase()) {
            case 'new':
                return 'default';
            case 'inprogress':
                return 'warning';
            case 'resolved':
                return 'success';
            case 'closed':
                return 'info';
            case 'escalated':
                return 'error';
            default:
                return 'default';
        }
    };

    const getPriorityColor = (priority: string) => {
        switch (priority?.toLowerCase()) {
            case 'critical':
                return 'error';
        case 'high':
                return 'warning';
        case 'medium':
                return 'info';
        case 'low':
                return 'success';
        default:
            return 'default';
        }
    };

    const getChangeStatusColor = (status: string) => {
        switch (status?.toLowerCase()) {
            case 'new':
                return 'default';
            case 'assessment':
                return 'warning';
            case 'approved':
                return 'success';
            case 'scheduled':
                return 'info';
            case 'inprogress':
                return 'warning';
            case 'implemented':
                return 'success';
            case 'review':
                return 'warning';
            case 'closed':
                return 'info';
            default:
                return 'default';
        }
    };

    const renderIncidentCard = (incident: Incident) => (
        <Card 
            key={incident.id}
            sx={{ cursor: 'pointer', mb: 2 }}
        >
            <CardContent>
                <Typography variant="h6" component="h3">
                    {incident.incidentNumber}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    {incident.title}
                </Typography>
                <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Chip 
                        label={incident.status} 
                        color={getStatusColor(incident.status)}
                        size="small"
                    />
                    <Chip 
                        label={incident.priority} 
                        color={getPriorityColor(incident.priority)}
                        size="small"
                    />
                    <Chip 
                        label={incident.category} 
                        variant="outlined"
                        size="small"
                    />
                </Box>
                <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="caption">
                        Created: {format(new Date(incident.createdAt), 'MMM d, yyyy')}
                    </Typography>
                    <Typography variant="caption">
                        Updated: {format(new Date(incident.updatedAt), 'MMM d, HH:mm')}
                    </Typography>
                </Box>
                {incident.assignedToUserId && (
                    <Typography variant="caption">
                        Assigned to: {incident.assignedToUserId}
                    </Typography>
                )}
            </CardContent>
        </Card>
    );

    const renderProblemCard = (problem: Problem) => (
        <Card 
            key={problem.id}
            sx={{ cursor: 'pointer', mb: 2 }}
        >
            <CardContent>
                <Typography variant="h6" component="h3">
                    {problem.problemNumber}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    {problem.title}
                </Typography>
                <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Chip 
                        label={problem.status} 
                        color={getStatusColor(problem.status)}
                        size="small"
                    />
                    <Chip 
                        label={problem.priority} 
                        color={getPriorityColor(problem.priority)}
                        size="small"
                    />
                </Box>
                <Box sx={{ mt: 2 }}>
                    <Typography variant="caption">
                        Created: {format(new Date(problem.createdAt), 'MMM d, yyyy')}
                    </Typography>
                    <Typography variant="caption">
                        Updated: {format(new Date(problem.updatedAt), 'MMM d, HH:mm')}
                    </Typography>
                </Box>
                {problem.relatedIncidents.length > 0 && (
                    <Box sx={{ mt: 1 }}>
                        <Typography variant="caption">
                            Related Incidents: {problem.relatedIncidents.length}
                        </Typography>
                    </Box>
                )}
            </CardContent>
        </Card>
    );

    const renderChangeCard = (change: ChangeRequest) => (
        <Card 
            key={change.id}
            sx={{ cursor: 'pointer', mb: 2 }}
        >
            <CardContent>
                <Typography variant="h6" component="h3">
                    {change.changeNumber}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    {change.title}
                </Typography>
                <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Chip 
                        label={change.status} 
                        color={getChangeStatusColor(change.status)}
                        size="small"
                    />
                    <Chip 
                        label={change.priority} 
                        color={getPriorityColor(change.priority)}
                        size="small"
                    />
                </Box>
                {change.scheduledDate && (
                    <Box sx={{ mt: 2 }}>
                        <Typography variant="caption">
                            Scheduled: {format(new Date(change.scheduledDate, 'MMM d, yyyy HH:mm')}
                        </Typography>
                    </Box>
                )}
            </CardContent>
        </Card>
    );

    const renderSLABreachCard = (breach: SLABreach) => (
        <Card 
            key={breach.id}
            sx={{ cursor: 'pointer', mb: 2 }}
        >
            <CardContent>
                <Typography variant="h6" component="h3">
                    SLA Breach
                </Typography>
                <Typography variant="body2" color="text.secondary">
                    {breach.breachDetails}
                </Typography>
                <Box sx={{ mt: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Chip 
                        label={breach.status}
                        color="error"
                        size="small"
                    />
                    <Typography variant="caption">
                        {format(new Date(breach.breachTime, 'MMM d, yyyy HH:mm')}
                    </Typography>
                </Box>
                {breach.resolvedAt && (
                    <Box sx={{ mt: 1 }}>
                        <Typography variant="caption">
                            Resolved: {format(new Date(breach.resolvedAt, 'MMM d, yyyy HH:mm')}
                        </Typography>
                </Box>
            </CardContent>
        </Card>
    );

    return (
        <Box sx={{ p: 3 }}>
            <Typography variant="h4" component="h2" gutterBottom={3}>
                <Security sx={{ mr: 1 }} /> ITSM Panel
            </Typography>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
                    {error}
                </Alert>
            )}

            {loading ? (
                <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 400 }}>
                    <Typography>Loading ITSM data...</Typography>
                </Box>
            ) : (
                <Box>
                    <Tabs value={activeTab} onChange={(e, newValue) => setActiveTab(newValue as number)}>
                        <Tab label="Overview" />
                        <Tab label="Incidents" />
                        <Tab label="Problems" />
                        <Tab label="Changes" />
                        <Tab label="SLA Breaches" />
                        <Tab label="ITIL Compliance" />
                    </Tabs>

                    {activeTab === 0 && (
                        <Grid container spacing={3}>
                            {/* Key Metrics */}
                            <Grid item xs={12} md={3}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            ITIL Overview
                                        </Typography>
                                        <Box sx={{ mt: 2 }}>
                                            <Grid container spacing={2}}>
                                                <Grid item xs={3}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="info">
                                                            {incidents.length}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Total Incidents
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={3}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="success">
                                                            {problems.length}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Total Problems
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={3}>
                                                    <Box sx={{ textAlign: 'center }}>
                                                        <Typography variant="h4" color="warning">
                                                            {changes.length}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Total Changes
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                            </Grid>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* SLA Status */}
                            <Grid item xs={12} md={3}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            SLA Status
                                        </Typography>
                                        <Box sx={{ mt: 2 }}>
                                            <Grid container spacing={2}}>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="success">
                                                            {incidents.Count(i => i.Status === IncidentStatus.Resolved).Count}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            Resolved Incidents
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                                <Grid item xs={6}>
                                                    <Box sx={{ textAlign: 'center' }}>
                                                        <Typography variant="h4" color="error">
                                                            {slaBreaches.length}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            SLA Breaches
                                                        </Typography>
                                                    </Box>
                                                </Grid>
                                            </Grid>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* Compliance Score */}
                            <Grid item xs={12} md={3}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            ITIL Compliance Score
                                        </Typography>
                                        {itilMetrics && (
                                            <Box sx={{ mt: 2 }}>
                                                <Box sx={{ textAlign: 'center' }}>
                                                    <Typography variant="h4" color={itilMetrics.overallComplianceScore >= 90 ? 'success' : itilMetrics.overallComplianceScore >= 70 ? 'warning' : 'error'}>
                                                        {itilMetrics.overallComplianceScore.toFixed(1)}%
                                                    </Typography>
                                                </Box>
                                                <Typography variant="caption">
                                                    Overall Compliance
                                                </Typography>
                                            </Box>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* Recent Activity */}
                            <Grid item xs={12} md={3}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Recent Activity
                                        </Typography>
                                        <Timeline sx={{ mt: 2 }}>
                                            {incidents.slice(0, 5).map((incident, index) => (
                                                <TimelineItem key={incident.id}>
                                                    <TimelineDot color={getStatusColor(incident.status)} />
                                                    <TimelineContent>
                                                        <Typography variant="subtitle2">
                                                            {incident.title}
                                                        </Typography>
                                                        <Typography variant="caption">
                                                            {format(new Date(incident.createdAt, 'MMM d, yyyy')}
                                                        </Typography>
                                                    </TimelineContent>
                                                    {index < incidents.length - 1 && (
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
                                            <Button
                                                variant="contained"
                                                startIcon={<CheckCircle />}
                                                onClick={() => setActiveTab(1)}
                                            >
                                                View Incidents
                                            </Button>
                                            <Button
                                                variant="outlined"
                                                startIcon={<Warning />}
                                                onClick={() => setActiveTab(2)}
                                            >
                                                View Problems
                                            </Button>
                                            <Button
                                                variant="outlined"
                                                startIcon={<Timeline />}
                                                onClick={() => setActiveTab(3)}
                                            >
                                                View Changes
                                            </Button>
                                            <Button
                                                variant="outlined"
                                                startIcon={<Error />}
                                                onClick={() => setActiveTab(4)}
                                            >
                                                SLA Breaches
                                            </Button>
                                            <Button
                                                variant="outlined"
                                                startIcon={<Assessment />}
                                                onClick={() => setActiveTab(5)}
                                            >
                                                Compliance
                                            </Button>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>
                        </Grid>
                    )}

                    {activeTab === 1 && (
                        <Grid container spacing={3}>
                            {incidents.map(incident => renderIncidentCard(incident))}
                        </Grid>
                    )}

                    {activeTab === 2 && (
                        <Grid container spacing={3}>
                            {problems.map(problem => renderProblemCard(problem))}
                        </Grid>
                    )}

                    {activeTab === 3 && (
                        <Grid container spacing={3}>
                            {changes.map(change => renderChangeCard(change))}
                        </Grid>
                    )}

                    {activeTab === 4 && (
                        <Grid container spacing={3}>
                            {slaBreaches.map(breach => renderSLABreachCard(breach))}
                        </Grid>
                    )}

                    {activeTab === 5 && complianceReport && (
                        <Grid container spacing={3}>
                            {/* Compliance Score */}
                            <Grid item xs={12}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            ITIL Compliance Report
                                        </Typography>
                                        <Typography variant="body2" color="text.secondary">
                                            {complianceReport.reportPeriod}
                                        </Typography>
                                        <Box sx={{ mt: 2 }}>
                                            <Typography variant="h4">
                                                Overall Score: {complianceReport.overallComplianceScore.toFixed(1)}%
                                            </Typography>
                                        </Box>
                                        <Box sx={{ mt: 2 }}>
                                            <Typography variant="h6" component="h4">
                                                Compliance Breakdown
                                            </Typography>
                                            <List dense>
                                                <ListItem>
                                                    <ListItemText
                                                        primary={`Incident Management: ${complianceReport.incidentManagementCompliance.toFixed(1)}%`}
                                                    secondary={`${complianceReport.incidentManagementCompliance >= 90 ? '✅' : '⚠️'}`}
                                                />
                                                <ListItem
                                                    primary={`Problem Management: ${complianceReport.problemManagementCompliance.toFixed(1)}%`}
                                                    secondary={`${complianceReport.problemManagementCompliance >= 90 ? '✅' : '⚠️'}`}
                                                />
                                                <ListItem
                                                    primary={`Change Management: ${complianceReport.changeManagementCompliance.toFixed(1)}%`}
                                                    secondary={`${complianceReport.changeManagementCompliance >= 90 ? '✅' : '⚠️'}`}
                                                />
                                                <ListItem
                                                    primary={`SLA Management: ${complianceReport.slaCompliance.toFixed(1)}%`}
                                                    secondary={`${complianceReport.slaCompliance >= 90 ? '✅' : '⚠️'}`}
                                                />
                                            </List>
                                        </Box>
                                    </CardContent>
                                </Card>
                            </Grid>

                            {/* Recommendations */}
                            <Grid item xs={12}>
                                <Card>
                                    <CardContent>
                                        <Typography variant="h6" component="h3">
                                            Recommendations
                                        </Typography>
                                        <List dense>
                                            {complianceReport.recommendations.map((rec, index) => (
                                                <ListItem key={index}>
                                                    <ListItemText
                                                        primary={rec.recommendation}
                                                        secondary={rec.action}
                                                        secondary={rec.category}
                                                    />
                                                    <ListItem>
                                                        <ListItemText
                                                            primary={rec.dueDate ? `Due: ${format(new Date(rec.dueDate, 'MMM d, yyyy')}` : ''}
                                                            secondary={rec.priority}
                                                        />
                                                    <ListItem>
                                                        <ListItemText
                                                            primary={rec.action}
                                                            secondary={rec.category}
                                                        />
                                                    />
                                                ))}
                                        </List>
                                    </CardContent>
                                </Card>
                            </Grid>
                        </Grid>
                    )}
                </Box>
            )}
        </Box>
    );
};

export default ITSMPanel;
