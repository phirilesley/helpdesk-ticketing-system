import React, { useEffect, useMemo, useState } from 'react';
import axios from 'axios';
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  Paper,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tabs,
  Typography
} from '@mui/material';

type Row = Record<string, any>;

interface TabPanelProps {
  value: number;
  index: number;
  children: React.ReactNode;
}

const TabPanel: React.FC<TabPanelProps> = ({ value, index, children }) => {
  if (value !== index) return null;
  return <Box sx={{ pt: 2 }}>{children}</Box>;
};

const EnterpriseAdmin: React.FC = () => {
  const [tab, setTab] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [identityProviders, setIdentityProviders] = useState<Row[]>([]);
  const [abacRules, setAbacRules] = useState<Row[]>([]);
  const [connectors, setConnectors] = useState<Row[]>([]);
  const [outboundMessages, setOutboundMessages] = useState<Row[]>([]);
  const [outboundReceipts, setOutboundReceipts] = useState<Row[]>([]);
  const [workflowDefs, setWorkflowDefs] = useState<Row[]>([]);
  const [slaPauseRules, setSlaPauseRules] = useState<Row[]>([]);
  const [slaBreachActions, setSlaBreachActions] = useState<Row[]>([]);
  const [legalHolds, setLegalHolds] = useState<Row[]>([]);
  const [dsrItems, setDsrItems] = useState<Row[]>([]);
  const [integrationApps, setIntegrationApps] = useState<Row[]>([]);
  const [webhooks, setWebhooks] = useState<Row[]>([]);
  const [marketplaceCatalog, setMarketplaceCatalog] = useState<Row[]>([]);
  const [marketplaceInstalls, setMarketplaceInstalls] = useState<Row[]>([]);
  const [integrationTemplates, setIntegrationTemplates] = useState<Row[]>([]);
  const [regionPolicies, setRegionPolicies] = useState<Row[]>([]);
  const [syntheticChecks, setSyntheticChecks] = useState<Row[]>([]);
  const [runbookSteps, setRunbookSteps] = useState<Row[]>([]);
  const [projects, setProjects] = useState<Row[]>([]);
  const [releases, setReleases] = useState<Row[]>([]);
  const [sprintMetrics, setSprintMetrics] = useState<Row[]>([]);
  const [dependencyGraphSummary, setDependencyGraphSummary] = useState<Row[]>([]);
  const [plans, setPlans] = useState<Row[]>([]);
  const [subscriptions, setSubscriptions] = useState<Row[]>([]);
  const [invoices, setInvoices] = useState<Row[]>([]);
  const [usage, setUsage] = useState<Row[]>([]);
  const [operationsSummary, setOperationsSummary] = useState<Row[]>([]);

  const tabs = useMemo(() => [
    'Identity',
    'Access',
    'Omnichannel',
    'Delivery',
    'Workflow',
    'SLA+',
    'Compliance',
    'Integrations',
    'Regions',
    'Projects',
    'Billing'
  ], []);

  useEffect(() => {
    void loadAll();
  }, []);

  const loadAll = async () => {
    setLoading(true);
    setError('');
    try {
      const [
        idpRes,
        abacRes,
        connRes,
        outboundRes,
        wfRes,
        slaPauseRes,
        slaActionRes,
        holdRes,
        dsrRes,
        appRes,
        whRes,
        marketRes,
        installRes,
        regionPolicyRes,
        syntheticRes,
        projRes,
        relRes,
        spRes,
        depGraphRes,
        planRes,
        subRes,
        invoiceRes,
        usageRes
      ] = await Promise.all([
        axios.get('/api/admin/identity/providers'),
        axios.get('/api/admin/identity/abac'),
        axios.get('/api/omnichannel/connectors'),
        axios.get('/api/omnichannel/outbound/messages'),
        axios.get('/api/workflow-builder/definitions'),
        axios.get('/api/sla/advanced/pause-rules'),
        axios.get('/api/sla/advanced/breach-actions'),
        axios.get('/api/admin/compliance/legal-holds'),
        axios.get('/api/admin/compliance/dsr'),
        axios.get('/api/integrations/apps'),
        axios.get('/api/integrations/webhooks'),
        axios.get('/api/integrations/marketplace/catalog'),
        axios.get('/api/integrations/marketplace/installs'),
        axios.get('/api/regions/policies'),
        axios.get('/api/regions/synthetic/checks'),
        axios.get('/api/projects'),
        axios.get('/api/projects/releases'),
        axios.get('/api/projects/agile-metrics'),
        axios.get('/api/projects/dependencies/graph'),
        axios.get('/api/billing/plans'),
        axios.get('/api/billing/subscriptions'),
        axios.get('/api/billing/invoices'),
        axios.get('/api/billing/usage')
      ]);

      const outboundRows = outboundRes.data ?? [];

      setIdentityProviders(idpRes.data ?? []);
      setAbacRules(abacRes.data ?? []);
      setConnectors(connRes.data ?? []);
      setOutboundMessages(outboundRows);
      setWorkflowDefs(wfRes.data ?? []);
      setSlaPauseRules(slaPauseRes.data ?? []);
      setSlaBreachActions(slaActionRes.data ?? []);
      setLegalHolds(holdRes.data ?? []);
      setDsrItems(dsrRes.data ?? []);
      setIntegrationApps(appRes.data ?? []);
      setWebhooks(whRes.data ?? []);
      setMarketplaceCatalog(marketRes.data ?? []);
      setMarketplaceInstalls(installRes.data ?? []);
      setRegionPolicies(regionPolicyRes.data ?? []);
      setSyntheticChecks(syntheticRes.data ?? []);
      setProjects(projRes.data ?? []);
      setReleases(relRes.data ?? []);
      setSprintMetrics(spRes.data ?? []);
      setDependencyGraphSummary([{
        nodes: depGraphRes?.data?.nodes?.length ?? 0,
        edges: depGraphRes?.data?.edges?.length ?? 0
      }]);
      setPlans(planRes.data ?? []);
      setSubscriptions(subRes.data ?? []);
      setInvoices(invoiceRes.data ?? []);
      setUsage(usageRes.data ?? []);

      const latestMessageId = outboundRows.length > 0 ? outboundRows[0].id : null;
      const [templatesResult, opsResult, runbookResult, receiptResult] = await Promise.allSettled([
        axios.get('/api/integrations/templates'),
        axios.get('/api/operations/dashboard'),
        axios.get('/api/regions/runbook-template'),
        latestMessageId ? axios.get(`/api/omnichannel/outbound/receipts/${latestMessageId}`) : Promise.resolve({ data: [] })
      ]);

      if (templatesResult.status === 'fulfilled') {
        setIntegrationTemplates(templatesResult.value.data ?? []);
      } else {
        setIntegrationTemplates([]);
      }

      if (opsResult.status === 'fulfilled') {
        setOperationsSummary([opsResult.value.data ?? {}]);
      } else {
        setOperationsSummary([]);
      }

      if (runbookResult.status === 'fulfilled') {
        const steps = runbookResult.value.data?.steps ?? [];
        setRunbookSteps(Array.isArray(steps) ? steps.map((step: string, index: number) => ({ step: index + 1, detail: step })) : []);
      } else {
        setRunbookSteps([]);
      }

      if (receiptResult.status === 'fulfilled') {
        setOutboundReceipts(receiptResult.value.data ?? []);
      } else {
        setOutboundReceipts([]);
      }
    } catch (e: any) {
      setError(e?.response?.data?.error ?? 'Failed to load enterprise modules.');
    } finally {
      setLoading(false);
    }
  };

  const runAction = async (fn: () => Promise<any>, okMessage: string) => {
    setError('');
    setSuccess('');
    try {
      await fn();
      setSuccess(okMessage);
      await loadAll();
    } catch (e: any) {
      setError(e?.response?.data?.error ?? 'Action failed.');
    }
  };

  const resolveTestConnector = () => connectors.find((x) => {
    const provider = String(x.providerKey ?? '').toLowerCase();
    return provider === 'slack' || provider === 'meta_whatsapp' || provider === 'twilio';
  });

  const queueOutboundTest = async () => {
    const connector = resolveTestConnector();
    if (!connector?.id) {
      throw new Error('No outbound connector found. Seed or create slack/meta_whatsapp/twilio connector first.');
    }

    await axios.post('/api/omnichannel/outbound/queue', {
      connectorId: connector.id,
      ticketId: 1,
      idempotencyKey: `ui-${Date.now()}`,
      recipientAddress: 'integration-test-recipient',
      subject: 'Outbound Pipeline Test',
      content: `Outbound test message via ${connector.providerKey}.`,
      metadataJson: JSON.stringify({ source: 'enterprise-admin-ui' }),
      maxAttempts: 5
    });
  };

  const renderTable = (rows: Row[], columns: string[]) => (
    <TableContainer component={Paper} variant="outlined">
      <Table size="small">
        <TableHead>
          <TableRow>
            {columns.map((column) => <TableCell key={column}>{column}</TableCell>)}
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.length === 0 && (
            <TableRow>
              <TableCell colSpan={columns.length} align="center">No records</TableCell>
            </TableRow>
          )}
          {rows.map((row) => (
            <TableRow key={row.id ?? JSON.stringify(row)}>
              {columns.map((column) => {
                const value = row[column];
                if (typeof value === 'boolean') {
                  return (
                    <TableCell key={column}>
                      <Chip size="small" color={value ? 'success' : 'default'} label={value ? 'Yes' : 'No'} />
                    </TableCell>
                  );
                }
                return <TableCell key={column}>{value == null ? '' : String(value)}</TableCell>;
              })}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h4">Enterprise Admin</Typography>
        <Button variant="outlined" onClick={() => void loadAll()}>Refresh</Button>
      </Box>

      {loading && <CircularProgress size={24} />}
      {error && <Alert sx={{ mt: 1 }} severity="error">{error}</Alert>}
      {success && <Alert sx={{ mt: 1 }} severity="success">{success}</Alert>}

      <Box sx={{ borderBottom: 1, borderColor: 'divider', mt: 2 }}>
        <Tabs value={tab} onChange={(_, value) => setTab(value)} variant="scrollable">
          {tabs.map((t) => <Tab key={t} label={t} />)}
        </Tabs>
      </Box>

      <TabPanel value={tab} index={0}>
        <Box sx={{ mb: 2, display: 'flex', gap: 1 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/admin/identity/providers', {
              name: `OIDC ${Date.now()}`,
              protocol: 1,
              issuer: 'https://accounts.google.com',
              authorityOrMetadataUrl: 'https://accounts.google.com',
              clientId: 'replace-me',
              clientSecret: 'replace-me',
              audience: '',
              enforceSso: false,
              isEnabled: false
            }), 'Identity provider created.')}
          >
            Add OIDC Provider
          </Button>
        </Box>
        {renderTable(identityProviders, ['id', 'name', 'protocol', 'issuer', 'isEnabled'])}
      </TabPanel>

      <TabPanel value={tab} index={1}>
        <Box sx={{ mb: 2 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/admin/identity/abac', {
              name: `ABAC ${Date.now()}`,
              resource: 'ticket',
              action: 'read',
              conditionJson: '{"roles":["Admin"]}',
              effect: 2,
              priority: 100,
              isEnabled: true
            }), 'ABAC rule created.')}
          >
            Add ABAC Rule
          </Button>
        </Box>
        {renderTable(abacRules, ['id', 'name', 'resource', 'action', 'effect', 'priority', 'isEnabled'])}
      </TabPanel>

      <TabPanel value={tab} index={2}>
        <Box sx={{ mb: 2 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/omnichannel/connectors', {
              name: `WebForm ${Date.now()}`,
              channelType: 6,
              providerKey: 'portal',
              configJson: '{"source":"portal"}',
              inboundSigningSecret: '',
              status: 2
            }), 'Omnichannel connector created.')}
          >
            Add Connector
          </Button>
        </Box>
        {renderTable(connectors, ['id', 'name', 'providerKey', 'channelType', 'status'])}
      </TabPanel>

      <TabPanel value={tab} index={3}>
        <Box sx={{ mb: 2, display: 'flex', gap: 1 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(queueOutboundTest, 'Outbound test message queued.')}
          >
            Queue Outbound Test
          </Button>
          <Button
            variant="outlined"
            onClick={() => void runAction(() => axios.post('/api/regions/synthetic/run'), 'Synthetic region checks executed.')}
          >
            Trigger Synthetic Checks
          </Button>
        </Box>
        <Typography variant="h6" sx={{ mb: 1 }}>Outbound Messages</Typography>
        {renderTable(outboundMessages, ['id', 'connectorId', 'ticketId', 'status', 'attemptCount', 'maxAttempts', 'providerMessageId', 'lastError', 'createdAtUtc'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Latest Message Receipts</Typography>
        {renderTable(outboundReceipts, ['id', 'outboundChannelMessageId', 'providerMessageId', 'status', 'receivedAtUtc'])}
      </TabPanel>

      <TabPanel value={tab} index={4}>
        <Box sx={{ mb: 2 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/workflow-builder/definitions', {
              name: `Workflow ${Date.now()}`,
              version: 1,
              isPublished: false,
              graphJson: '{"nodes":[{"id":"start","type":"start"},{"id":"guard-1","type":"guard"}],"edges":[{"from":"start","to":"guard-1"}]}'
            }), 'Workflow definition created.')}
          >
            Add Workflow
          </Button>
        </Box>
        {renderTable(workflowDefs, ['id', 'name', 'version', 'isPublished'])}
      </TabPanel>

      <TabPanel value={tab} index={5}>
        <Box sx={{ mb: 2, display: 'flex', gap: 1 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/sla/advanced/pause-rules', {
              name: `Pause Rule ${Date.now()}`,
              conditionJson: '{"status":"WaitingForCustomer"}',
              pauseResponseSla: false,
              pauseResolutionSla: true,
              isEnabled: true
            }), 'SLA pause rule created.')}
          >
            Add Pause Rule
          </Button>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/sla/advanced/breach-actions', {
              name: `Breach Action ${Date.now()}`,
              breachType: 'resolution',
              triggerAfterBreachMinutes: 30,
              executionOrder: 10,
              actionType: 'notify_role',
              actionConfigJson: '{"role":"Admin"}',
              isEnabled: true
            }), 'SLA breach action created.')}
          >
            Add Breach Action
          </Button>
        </Box>
        <Typography variant="h6" sx={{ mb: 1 }}>Pause Rules</Typography>
        {renderTable(slaPauseRules, ['id', 'name', 'conditionJson', 'pauseResponseSla', 'pauseResolutionSla', 'isEnabled'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Breach Actions</Typography>
        {renderTable(slaBreachActions, ['id', 'name', 'breachType', 'triggerAfterBreachMinutes', 'executionOrder', 'actionType', 'isEnabled'])}
      </TabPanel>

      <TabPanel value={tab} index={6}>
        <Box sx={{ mb: 2, display: 'flex', gap: 1 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/admin/compliance/legal-holds', {
              caseNumber: `LH-${Date.now()}`,
              name: 'Default Hold',
              scopeJson: '{"entities":["tickets"]}',
              expiresAtUtc: null,
              isActive: true
            }), 'Legal hold created.')}
          >
            Add Legal Hold
          </Button>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/admin/compliance/dsr', {
              requestType: 1,
              status: 1,
              subjectEmail: 'user@example.com',
              referenceNumber: '',
              notes: 'Generated from admin UI'
            }), 'DSR request created.')}
          >
            Add DSR
          </Button>
        </Box>
        <Typography variant="h6" sx={{ mb: 1 }}>Legal Holds</Typography>
        {renderTable(legalHolds, ['id', 'caseNumber', 'name', 'isActive'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Data Subject Requests</Typography>
        {renderTable(dsrItems, ['id', 'referenceNumber', 'requestType', 'status', 'subjectEmail'])}
      </TabPanel>

      <TabPanel value={tab} index={7}>
        <Box sx={{ mb: 2, display: 'flex', gap: 1 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/integrations/apps', {
              name: `Integration ${Date.now()}`,
              provider: 'custom',
              configJson: '{"key":"value"}',
              isEnabled: false
            }), 'Integration app created.')}
          >
            Add Integration
          </Button>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/integrations/webhooks', {
              name: `Webhook ${Date.now()}`,
              endpointUrl: 'https://example.com/webhook',
              eventFiltersJson: '["ticket.created"]',
              secret: '',
              isEnabled: false,
              maxAttempts: 5,
              retryBackoffSeconds: 30,
              timeoutSeconds: 20
            }), 'Webhook subscription created.')}
          >
            Add Webhook
          </Button>
        </Box>
        <Typography variant="h6" sx={{ mb: 1 }}>Integration Apps</Typography>
        {renderTable(integrationApps, ['id', 'name', 'provider', 'isEnabled'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Webhook Subscriptions</Typography>
        {renderTable(webhooks, ['id', 'name', 'endpointUrl', 'isEnabled', 'maxAttempts', 'retryBackoffSeconds', 'timeoutSeconds', 'lastDeliveryAtUtc'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Marketplace Catalog</Typography>
        {renderTable(marketplaceCatalog, ['id', 'appKey', 'name', 'category', 'provider', 'minPlanName', 'isActive'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Tenant Installs</Typography>
        {renderTable(marketplaceInstalls, ['id', 'marketplaceAppId', 'status', 'installedVersion'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Integration Templates</Typography>
        {renderTable(integrationTemplates, ['key', 'category', 'displayName', 'authMode'])}
      </TabPanel>

      <TabPanel value={tab} index={8}>
        <Box sx={{ mb: 2, display: 'flex', gap: 1 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/regions/policies', {
              primaryRegion: 'af-south',
              secondaryRegion: 'eu-west',
              failoverMode: 1,
              autoFailbackEnabled: false,
              isActive: true,
              runbookUrl: 'https://runbooks.example.com/helpdesk/multi-region-failover',
              monitoringConfigJson: '{"syntheticUrls":["https://status.example.com/health"]}'
            }), 'Region policy upserted.')}
          >
            Upsert Region Policy
          </Button>
          <Button
            variant="outlined"
            onClick={() => void runAction(() => axios.post('/api/regions/synthetic/run'), 'Synthetic checks executed.')}
          >
            Run Synthetic Checks
          </Button>
        </Box>
        <Typography variant="h6" sx={{ mb: 1 }}>Tenant Region Policies</Typography>
        {renderTable(regionPolicies, ['id', 'primaryRegion', 'secondaryRegion', 'failoverMode', 'autoFailbackEnabled', 'isActive', 'runbookUrl'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Synthetic Checks</Typography>
        {renderTable(syntheticChecks, ['id', 'region', 'checkType', 'passed', 'durationMs', 'detail', 'checkedAtUtc'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Runbook Template</Typography>
        {renderTable(runbookSteps, ['step', 'detail'])}
      </TabPanel>

      <TabPanel value={tab} index={9}>
        <Box sx={{ mb: 2 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/projects', {
              key: `P${Date.now().toString().slice(-4)}`,
              name: `Project ${Date.now()}`,
              workflowConfigJson: '{"workflow":"default"}'
            }), 'Project created.')}
          >
            Add Project
          </Button>
        </Box>
        <Typography variant="h6" sx={{ mb: 1 }}>Projects</Typography>
        {renderTable(projects, ['id', 'key', 'name'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Releases</Typography>
        {renderTable(releases, ['id', 'projectId', 'name', 'targetDateUtc'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Sprint Metrics</Typography>
        {renderTable(sprintMetrics, ['id', 'projectId', 'sprintName', 'plannedStoryPoints', 'completedStoryPoints'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Dependency Graph Summary</Typography>
        {renderTable(dependencyGraphSummary, ['nodes', 'edges'])}
      </TabPanel>

      <TabPanel value={tab} index={10}>
        <Box sx={{ mb: 2 }}>
          <Button
            variant="contained"
            onClick={() => void runAction(() => axios.post('/api/billing/plans', {
              name: `Plan ${Date.now()}`,
              monthlyPriceUsd: 99,
              includedAgentSeats: 10,
              includedTicketsPerMonth: 1000,
              entitlementsJson: '{}',
              isActive: true
            }), 'Billing plan created.')}
          >
            Add Plan
          </Button>
        </Box>
        <Typography variant="h6" sx={{ mb: 1 }}>Plans</Typography>
        {renderTable(plans, ['id', 'name', 'monthlyPriceUsd', 'includedAgentSeats', 'includedTicketsPerMonth', 'isActive'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Subscriptions</Typography>
        {renderTable(subscriptions, ['id', 'billingPlanId', 'status', 'currentPeriodEndUtc', 'autoRenew'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Invoices</Typography>
        {renderTable(invoices, ['id', 'invoiceNumber', 'status', 'periodStartUtc', 'periodEndUtc', 'totalUsd', 'dueAtUtc', 'paidAtUtc'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Usage</Typography>
        {renderTable(usage, ['id', 'metricName', 'usageDateUtc', 'quantity'])}
        <Divider sx={{ my: 2 }} />
        <Typography variant="h6" sx={{ mb: 1 }}>Operations Summary</Typography>
        {renderTable(operationsSummary, ['generatedAtUtc', 'openTickets', 'inboundBacklog', 'webhookPending', 'webhookFailures24h', 'slaBreaches24h'])}
      </TabPanel>
    </Box>
  );
};

export default EnterpriseAdmin;
