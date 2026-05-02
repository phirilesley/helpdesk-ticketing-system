import React, { useState, useEffect } from 'react';
import { Card, CardContent, Typography, Grid, Button, Box, Chip, Alert, Tabs, Tab, LinearProgress, CircularProgress } from '@mui/material';
import { Business, Timeline, Timeline, TimelineItem, TimelineConnector, TimelineDot, TimelineContent } from '@mui/icons-material';
import { 
    PeopleOutline,
    Security,
    Assessment,
    Timeline as Timeline,
    CheckCircle,
    Warning,
    Info
} from '@mui/icons-material';
import { format } from 'date-fns';

interface HRMetrics {
    totalEmployees: number;
    activeEmployees: number;
    onboardingInProgress: number;
    offboardingInProgress: number;
    turnoverRate: number;
            averageTenure: number;
            satisfactionScore: number;
}

interface SecurityMetrics {
    totalIncidents: number;
    resolvedIncidents: number;
    activeThreats: number;
    vulnerabilities: number;
            severity: {
                low: number;
                medium: number;
                high: number;
                critical: number;
            };
            responseTime: {
                average: number;
                p95: number;
                p99: number;
            };
            complianceScore: number;
        }

interface ITOMMetrics {
    totalServices: number;
            healthyServices: number;
            incidents: number;
            averageResolutionTime: number;
            uptime: number;
            resourceUtilization: {
                cpu: number;
                memory: number;
                storage: number;
                network: number;
            };
        }

interface GRCMetrics {
    totalRisks: number;
            activeRisks: number;
            mitigatedRisks: number;
            complianceScore: number;
            openAudits: number;
            completedAudits: number;
            controlEffectiveness: number;
        }

interface WorkplaceMetrics {
    totalServices: number;
            activeServices: number;
            utilizationRate: number;
            satisfactionScore: number;
            activeRequests: number;
            responseTime: number;
        }

interface FieldServiceMetrics {
    totalRequests: number;
            activeRequests: number;
            resolutionTime: number;
            firstCallResolution: number;
            customerSatisfaction: number;
            technicianUtilization: number;
            routeOptimizationSavings: number;
        }

interface LowCodeMetrics {
    totalApplications: number;
            activeApplications: number;
            totalWorkflows: number;
            totalForms: number;
            totalReports: number;
            apiEndpoints: number;
            integrations: number;
        }

interface IntegrationMetrics {
    totalConnectors: number;
            activeConnectors: number;
            apiCalls: number;
            successRate: number;
            averageLatency: number;
            dataSyncErrors: number;
        }

interface EnterpriseDashboard {
    hrMetrics: HRMetrics;
    securityMetrics: SecurityMetrics;
    itomMetrics: ITOMMetrics;
    grcMetrics: GRCMetrics;
    workplaceMetrics: WorkplaceMetrics;
    fieldServiceMetrics: FieldServiceMetrics;
    lowCodeMetrics: LowCodeMetrics;
    integrationMetrics: IntegrationMetrics;
}

const EnterpriseDashboard: React.FC = () => {
    const [activeTab, setActiveTab] = useState(0);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [dashboard, setDashboard] = useState<EnterpriseDashboard | null>(null);

    useEffect(() => {
        fetchEnterpriseData();
    }, []);

    const fetchEnterpriseData = async () => {
        setLoading(true);
        setError(null);
        try {
            // Fetch all enterprise metrics
            const [hrMetrics, securityMetrics, itomMetrics, grcMetrics, workplaceMetrics, fieldServiceMetrics, lowCodeMetrics, integrationMetrics] = await Promise.all([
                fetch('/api/enterprise/hr/metrics'),
                fetch('/api/security/metrics'),
                fetch('/api/itom/metrics'),
                fetch('/api/grc/metrics'),
                fetch('/api/workplace/analytics'),
                fetch('/api/field-service/analytics'),
                fetch('/api/low-code/metrics'),
                fetch('/api/integration/analytics')
            ]);

            const dashboard = new EnterpriseDashboard
            {
                hrMetrics: hrMetrics,
                securityMetrics: securityMetrics,
                itomMetrics: itomMetrics,
                grcMetrics: grcMetrics,
                workplaceMetrics: workplaceMetrics,
                fieldServiceMetrics: fieldServiceMetrics,
                lowCodeMetrics: lowCodeMetrics,
                integrationMetrics: integrationMetrics
            };

            setDashboard(dashboard);
        } catch (err) {
            setError('Failed to fetch enterprise dashboard data');
            console.error('Error fetching enterprise dashboard data:', err);
        } finally {
            setLoading(false);
        }
    };

    const getMetricColor = (score: number) => {
        if (score >= 90) return 'success';
        if (score >= 70) return 'warning';
        return 'error';
    };

    const getTrendColor = (trend: string) => {
        switch (trend?.toLowerCase()) {
            case 'up':
                return 'success';
            case 'down':
                return 'error';
            case 'stable':
                return 'info';
            default:
                return 'default';
        }
    };

    const renderHRCards = () => (
        <Card>
            <CardContent>
                <Typography variant="h6" component="h3">
                    HR Metrics
                </Typography>
                <Box sx={{ mt: 2 }}>
                    <Grid container spacing={2}>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(hrMetrics.satisfactionScore)}>
                                    {hrMetrics.satisfactionScore}%
                                </Typography>
                                <Typography variant="caption">
                                    Satisfaction Score
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(hrMetrics.turnoverRate)}>
                                    {hrMetrics.turnoverRate}%
                                </Typography>
                                <Typography variant="caption">
                                    Turnover Rate
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                    <Grid container spacing={2}>
                        <Grid item xs={4}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4">
                                    {hrMetrics.totalEmployees}
                                </Typography>
                                <Typography variant="caption">
                                    Total Employees
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={4}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color="success">
                                    {hrMetrics.activeEmployees}
                                </Typography>
                                <Typography variant="caption">
                                    Active Employees
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={4}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4">
                                    {hrMetrics.averageTenure} years
                                </Typography>
                                <Typography variant="caption">
                                    Avg. Tenure
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>
            </CardContent>
        </Card>
    );

    const renderSecurityMetrics = () => (
        <Card>
            <CardContent>
                <Typography variant="h6" component="h3">
                    Security Metrics
                </Typography>
                <Box sx={{ mt: 2 }}>
                    <Grid container spacing={2}>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(securityMetrics.responseTime.p95)}>
                                    {securityMetrics.responseTime.p95}ms
                                </Typography>
                                <Typography variant="caption">
                                    Response Time (95th percentile)
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(securityMetrics.responseTime.p99)}>
                                    {securityMetrics.responseTime.p99}ms
                                </Typography>
                                <Typography variant="caption">
                                    Response Time (99th percentile)
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getTrendColor(securityMetrics.trend)}>
                                    {securityMetrics.trend}
                                </Typography>
                                <Typography variant="caption">
                                    Threat Trend
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>

                <Box sx={{ mt: 2 }}>
                    <Typography variant="h6" component="h4">
                        Security Incidents by Severity
                    </Typography>
                    <Grid container spacing={2}>
                        <Grid item xs={3}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h6" color="error">
                                    {securityMetrics.severity.critical}
                                </Typography>
                                <Typography variant="caption">
                                    Critical
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={3}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h6" color="warning">
                                    {securityMetrics.severity.high}
                                </Typography>
                                <Typography variant="caption">
                                    High
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={3}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h6" color="info">
                                    {securityMetrics.severity.medium}
                                </Typography>
                                <Typography variant="caption">
                                    Medium
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={3}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h6" color="success">
                                    {securityMetrics.severity.low}
                                </Typography>
                                <Typography variant="caption">
                                    Low
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>

                <Box sx={{ mt: 2 }}>
                    <Typography variant="h6" component="h4">
                        Vulnerability Assessment
                    </Typography>
                    <Box sx={{ mt: 2 }}>
                        <Grid container spacing={2}>
                            <Grid item xs={4}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Typography variant="h4" color={getMetricColor(securityMetrics.vulnerabilities.critical)}>
                                        {securityMetrics.vulnerabilities.critical}
                                    </Typography>
                                <Typography variant="caption">
                                    Critical
                                </Typography>
                            </Box>
                        </Grid>
                            <Grid item xs={4}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Typography variant="h4" color={getMetricColor(securityMetrics.vulnerabilities.high)}>
                                        {securityMetrics.vulnerabilities.high}
                                    </Typography>
                                <Typography variant="caption">
                                    High
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={4}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Typography variant="h4" color={getMetricColor(securityMetrics.vulnerabilities.medium)}>
                                        {securityMetrics.vulnerabilities.medium}
                                    </Typography>
                                <Typography variant="caption">
                                    Medium
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={4}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Typography variant="h4" color={getMetricColor(securityMetrics.vulnerabilities.low)}>
                                        {securityMetrics.vulnerabilities.low}
                                    </Typography>
                                <Typography variant="caption">
                                    Low
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>
            </CardContent>
        </Card>
    );

    const renderITOMMetrics = () => (
        <Card>
            <CardContent>
                <Typography variant="h6" component="h3">
                    IT Operations Metrics
                </Typography>
                <Box sx={{ mt: 2 }}>
                    <Grid container spacing={2}}>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(itomMetrics.uptime)}>
                                    {itomMetrics.uptime}%
                                </Typography>
                                <Typography variant="caption">
                                    Uptime
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(itomMetrics.resourceUtilization.cpu)}>
                                    {itomMetrics.resourceUtilization.cpu}%
                                </Typography>
                                <Typography variant="caption">
                                    CPU Utilization
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(itomMetrics.resourceUtilization.memory)}>
                                    {itomMetrics.resourceUtilization.memory}%
                                </Typography>
                                <Typography variant="caption">
                                    Memory Utilization
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                    <Grid container spacing={2}>
                        <Grid item xs={12}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(itomMetrics.resourceUtilization.network)}>
                                    {itomMetrics.resourceUtilization.network}%
                                </Typography>
                                <Typography variant="caption">
                                    Network Utilization
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>
            </CardContent>
        </Card>
    );

    const renderGRCMetrics = () => (
        <Card>
            <CardContent>
                <Typography variant="h6" component="h3">
                    GRC Metrics
                </Typography>
                <Box sx={{ mt: 2 }}>
                    <Grid container spacing={2}}>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(grcMetrics.overallComplianceScore)}>
                                    {grcMetrics.overallComplianceScore}%
                                </Typography>
                                <Typography variant="caption">
                                    Overall Score
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(grcMetrics.incidentManagementCompliance)}>
                                    {grcMetrics.incidentManagementCompliance}%
                                </Typography>
                                <Typography variant="caption">
                                    Incident Management
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(grcMetrics.problemManagementCompliance)}>
                                    {grcMetrics.problemManagementCompliance}%
                                </Typography>
                                <Typography variant="caption">
                                    Problem Management
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(grcMetrics.changeManagementCompliance)}>
                                    {grcMetrics.changeManagementCompliance}%
                                </Typography>
                                <Typography variant="caption">
                                    Change Management
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(grcMetrics.slaCompliance)}>
                                    {grcMetrics.slaCompliance}%
                                </Typography>
                                <Typography variant="caption">
                                    SLA Management
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>
            </CardContent>
        </Card>
    );

    const renderWorkplaceMetrics = () => (
        <Card>
            <CardContent>
                <Typography variant="h6" component="h3">
                    Workplace Metrics
                </Typography>
                <Box sx={{ mt: 2 }}>
                    <Grid container spacing={2}}>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4">
                                    {workplaceMetrics.totalServices}
                                </Typography>
                                <Typography variant="caption">
                                    Total Services
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(workplaceMetrics.utilization)}>
                                    {workplaceMetrics.utilization}%
                                </Typography>
                                <Typography variant="caption">
                                    Utilization Rate
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(workplaceMetrics.satisfactionScore)}>
                                    {workplaceMetrics.satisfactionScore}%
                                </Typography>
                                <Typography variant="caption">
                                    Satisfaction Score
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>
            </CardContent>
        </Card>
    );

    const renderFieldServiceMetrics = () => (
        <Card>
            <CardContent>
                <Typography variant="h6" component="h3">
                    Field Service Metrics
                </Typography>
                <Box sx={{ mt: 2 }}>
                    <Grid container spacing={2}}>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(fieldServiceMetrics.resolutionTime)}>
                                    {fieldServiceMetrics.resolutionTime}min
                                </Typography>
                                <Typography variant="caption">
                                    Avg Resolution Time
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(fieldServiceMetrics.firstCallResolution)}>
                                    {fieldServiceMetrics.firstCallResolution}%
                                </Typography>
                                <Typography variant="caption>
                                    FCR Rate
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(fieldServiceMetrics.customerSatisfaction)}>
                                    {fieldServiceMetrics.customerSatisfaction}%
                                </Typography>
                                <Typography variant="caption>
                                    Customer Satisfaction
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                    </Grid>
                </Box>
            </CardContent>
        </Card>
    );

    const renderLowCodeMetrics = () => (
        <Card>
            <Card>
                <CardContent>
                    <Typography variant="h6" component="h3">
                        Low-Code Platform Metrics
                    </Typography>
                    <Box sx={{ mt: 2 }}>
                        <Grid container spacing={2}}>
                            <Grid item xs={6}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Typography variant="h4" color={getMetricColor(lowCodeMetrics.totalApplications)}>
                                        {lowCodeMetrics.totalApplications}
                                    </Typography>
                                    <Typography variant="caption">
                                        Total Applications
                                    </Typography>
                                </Box>
                            </Grid>
                            <Grid item xs={6}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Typography variant="h4" color={getMetricColor(lowCodeMetrics.activeApplications)}>
                                        {lowCodeMetrics.activeApplications}
                                    </Typography>
                                    <Typography variant="caption">
                                        Active Applications
                                    </Typography>
                                </Box>
                            </Grid>
                            <Grid item xs={6}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Typography variant="h4" color={getMetricColor(lowCodeMetrics.totalWorkflows)}>
                                        {lowCodeMetrics.totalWorkflows}
                                    </Typography>
                                    <Typography variant="caption">
                                        Total Workflows
                                    </Typography>
                                </Box>
                            </Grid>
                        </Grid>
                    </Grid>
                </Box>
            </CardContent>
        </Card>
    );

    const renderIntegrationMetrics = () => (
        <Card>
            <CardContent>
                <Typography variant="h6" component="h3">
                    Integration Hub Metrics
                </Typography>
                <Box sx={{ mt: 2 }}>
                    <Grid container spacing={2}}>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(integrationMetrics.successRate)}>
                                    {integrationMetrics.successRate}%
                                </Typography>
                                <Typography variant="caption">
                                    Success Rate
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(integrationMetrics.averageLatency)}>
                                    {integrationMetrics.averageLatency}ms}
                                </Typography>
                                <Typography variant="caption">
                                    Avg Latency
                                </Typography>
                            </Box>
                        </Grid>
                        <Grid item xs={6}>
                            <Box sx={{ textAlign: 'center' }}>
                                <Typography variant="h4" color={getMetricColor(integrationMetrics.errorRate)}>
                                    {integrationMetrics.errorRate}%
                                </Typography>
                                <Typography variant="caption">
                                    Error Rate
                                </Typography>
                            </Box>
                        </Grid>
                    </Grid>
                </Box>
            </CardContent>
        </Card>
    );

    const renderEnterpriseDashboard = () => (
        <Box sx={{ p: 3 }}>
            <Typography variant="h4" component="h2" gutterBottom={3}>
                Enterprise Dashboard
            </Typography>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
                    {error}
                </Alert>
            )}

            {dashboard ? (
                <Grid container spacing={3}>
                    {/* HR Metrics */}
                    <Grid item xs={12}>
                        {renderHRCards()}
                    </Grid>

                    {/* Security Metrics */}
                    <Grid item xs={12}>
                        {renderSecurityMetrics()}
                    </Grid>

                    {/* ITOM Metrics */}
                    <Grid item xs={12}>
                        {renderITOMMetrics()}
                    </Grid>

                    {/* GRC Metrics */}
                    <Grid item xs={12}>
                        {renderGRCMetrics()}
                    </Grid>

                    {/* Workplace Metrics */}
                    <Grid item xs={12}>
                        {renderWorkplaceMetrics()}
                    </Grid>

                    {/* Field Service Metrics */}
                    <Grid item xs={12}>
                        {renderFieldServiceMetrics()}
                    </Grid>

                    {/* Low-Code Metrics */}
                    <Grid item xs={12}>
                        {renderLowCodeMetrics()}
                    </Grid>

                    {/* Integration Metrics */}
                    <Grid item xs={12}>
                        {renderIntegrationMetrics()}
                    </Grid>

                    {/* Overall Score */}
                    <Grid item xs={12}>
                        <Card>
                            <CardContent>
                                <Typography variant="h6" component="h3">
                                    Overall Enterprise Score
                                </Typography>
                                <Box sx={{ mt: 2, textAlign: 'center' }}>
                                    <CircularProgress
                                        variant="determinate"
                                        value={dashboard.overallScore}
                                        color={getMetricColor(dashboard.overallScore)}
                                        size={80}
                                    />
                                    <Typography variant="h6" color={getMetricColor(dashboard.overallScore)}>
                                        {dashboard.overallScore}%
                                    </Typography>
                                    <Typography variant="caption">
                                        Overall Score
                                    </Typography>
                                </Box>
                            </CardContent>
                        </Card>
                    </Grid>
                </Grid>

                {/* Performance Indicators */}
                <Grid item xs={12}>
                    <Card>
                        <CardContent>
                            <Typography variant="h6" component="h3">
                                Performance Indicators
                            </Typography>
                            <Box sx={{ mt: 2 }}>
                                <Grid container spacing={2}}>
                                    <Grid item xs={4}>
                                        <Box sx={{ textAlign: 'center' }}>
                                            <Typography variant="h4" color="success">
                                                {Math.round(dashboard.overallScore)}%
                                            </Typography>
                                            <Typography variant="caption">
                                                Overall Score
                                            </Typography>
                                        </Box>
                                    </Grid>
                                    <Grid item xs={4}>
                                        <Box sx={{ textAlign: 'center' }}>
                                            <Typography variant="h4" color="warning">
                                                {Math.round(dashboard.overallScore)}%
                                            </Typography>
                                            <Typography variant="caption">
                                                Trend Score
                                            </Typography>
                                        </Box>
                                    </Grid>
                                    <Grid item xs={4}>
                                        <Box sx={{ textAlign: 'center' }}>
                                            <Typography variant="h4" color="info">
                                                {Math.round(dashboard.overallScore)}%
                                            </Typography>
                                            <Typography variant="caption">
                                                Target Score
                                            </Typography>
                                        </Box>
                                    </Grid>
                                </Grid>
                            </Grid>
                                </Grid>
                            </Box>
                        </CardContent>
                        </Card>
                            </Grid>
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
                                onClick={() => setActiveTab(0)}
                            >
                                ITSM Overview
                            </Button>
                            <Button
                                variant="outlined"
                                startIcon={<PeopleOutline />}
                                onClick={() => setActiveTab(1)}
                            >
                                HR Management
                            </Button>
                            <Button
                                variant="outlined"
                                startIcon={<Security />}
                                onClick={() => setActiveTab(2)}
                            >
                                Security Center
                            </Button>
                            <Button
                                variant="outlined"
                                startIcon={<Timeline />}
                                onClick={() => setActiveTab(3)}
                            >
                                Change Center
                            </Button>
                            <Button
                                variant="outlined"
                                startIcon={<Assessment />}
                                onClick={() => setActiveTab(4)}
                            >
                                GRC Center
                            </Button>
                            <Button
                                variant="outlined"
                                startIcon={<BusinessCenter />}
                                onClick={() => setActiveTab(5)}
                            >
                                Workplace Center
                            </Button>
                            <Button
                                variant="outlined"
                                startIcon={<Settings />}
                                onClick={() => setActiveTab(6)}
                            >
                                Low-Code Center
                            </Button>
                        </Box>
                    </CardContent>
                </Card>
            </Grid>
            )}
        </Box>
    );

    return (
        <Box sx={{ p: 3 }}>
            <Typography variant="h4" component="h2">
                Enterprise Dashboard
            </Typography>

            {error && (
                <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
                    {error}
                </Alert>
            )}

            {dashboard ? (
                <Grid container spacing={3}>
                    {dashboard.hrMetrics && renderHRCards()}
                    {dashboard.securityMetrics && renderSecurityMetrics()}
                    {dashboard.itomMetrics && renderITOMMetrics()}
                    {dashboard.grcMetrics && renderGRCMetrics()}
                    {dashboard.workplaceMetrics && renderWorkplaceMetrics()}
                    {dashboard.fieldServiceMetrics && renderFieldServiceMetrics()}
                    {dashboard.lowCodeMetrics && renderLowCodeMetrics()}
                    {dashboard.integrationMetrics && renderIntegrationMetrics()}
                    <Grid item xs={12}>
                        {renderEnterpriseScore()}
                    </Grid>
                </Grid>
            )}
        </Box>
    );
};

export default EnterpriseDashboard;
