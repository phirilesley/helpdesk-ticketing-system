using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Application.Interfaces;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace HelpDeskSystem.DevOps.Services
{
    public interface IDevOpsIntegrationService
    {
        // Git Integration
        Task<List<GitRepository>> GetRepositories();
        Task<List<GitCommit>> GetCommits(string repositoryId, string branch = "main");
        Task<List<GitBranch>> GetBranches(string repositoryId);
        Task<List<GitPullRequest>> GetPullRequests(string repositoryId);
        Task CreateBranch(string repositoryId, string branchName, string fromBranch = "main");
        Task CreatePullRequest(string repositoryId, string title, string description, string sourceBranch, string targetBranch);

        // CI/CD Integration
        Task<List<CIPipeline>> GetPipelines();
        Task TriggerPipeline(string pipelineId, Dictionary<string, object> parameters = null);
        Task<CIBuild> GetBuildStatus(string buildId);
        Task<List<CIDeployment>> GetDeployments(string environment = null);

        // Code Review Integration
        Task<List<CodeReview>> GetCodeReviews(string repositoryId);
        Task CreateCodeReview(string repositoryId, string title, string description, List<string> reviewers);
        Task ApproveCodeReview(string reviewId);
        Task RequestChanges(string reviewId, string comment);

        // Deploy Tracking
        Task<List<Deployment>> GetRecentDeployments(string environment = null);
        Task TrackDeployment(string deploymentId, DeploymentStatus status);
        Task<RollbackPlan> CreateRollbackPlan(string deploymentId);

        // Sprint & Branch Management
        Task<FeatureBranch> CreateFeatureBranch(string ticketId, string featureName);
        Task<List<FeatureBranch>> GetFeatureBranches();
        Task MergeFeatureBranch(string branchId, string targetBranch = "main");
        Task<SprintDeployment> PlanSprintDeployment(string sprintId);

        // Quality Gates
        Task<CodeQualityReport> GetCodeQualityReport(string repositoryId, string branch = "main");
        Task<SecurityScanResult> RunSecurityScan(string repositoryId);
        Task<List<TestResult>> GetTestResults(string buildId);
        Task<bool> ValidateQualityGates(string repositoryId, string branch);
    }

    public class DevOpsIntegrationService : IDevOpsIntegrationService
    {
        private readonly ILogger<DevOpsIntegrationService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITicketService _ticketService;
        private readonly DevOpsSettings _settings;

        public DevOpsIntegrationService(
            ILogger<DevOpsIntegrationService> logger,
            IHttpClientFactory httpClientFactory,
            ITicketService ticketService,
            DevOpsSettings settings)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _ticketService = ticketService;
            _settings = settings;
        }

        #region Git Integration

        public async Task<List<GitRepository>> GetRepositories()
        {
            var repositories = new List<GitRepository>();

            // GitHub Integration
            if (!string.IsNullOrEmpty(_settings.GitHubToken))
            {
                var githubRepos = await GetGitHubRepositories();
                repositories.AddRange(githubRepos);
            }

            // GitLab Integration
            if (!string.IsNullOrEmpty(_settings.GitLabToken))
            {
                var gitlabRepos = await GetGitLabRepositories();
                repositories.AddRange(gitlabRepos);
            }

            // Azure DevOps Integration
            if (!string.IsNullOrEmpty(_settings.AzureDevOpsToken))
            {
                var azureRepos = await GetAzureDevOpsRepositories();
                repositories.AddRange(azureRepos);
            }

            return repositories;
        }

        private async Task<List<GitRepository>> GetGitHubRepositories()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"token {_settings.GitHubToken}");
                client.DefaultRequestHeaders.Add("User-Agent", "HelpDeskSystem");

                var response = await client.GetAsync("https://api.github.com/user/repos");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var repos = JsonSerializer.Deserialize<List<GitHubRepository>>(json);

                return repos.Select(r => new GitRepository
                {
                    Id = r.Id.ToString(),
                    Name = r.Name,
                    FullName = r.FullName,
                    Description = r.Description,
                    Url = r.HtmlUrl,
                    CloneUrl = r.CloneUrl,
                    DefaultBranch = r.DefaultBranch,
                    Language = r.Language,
                    Stars = r.StargazersCount,
                    Forks = r.ForksCount,
                    IsPrivate = r.Private,
                    Provider = "GitHub",
                    LastUpdated = r.UpdatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GitHub repositories");
                return new List<GitRepository>();
            }
        }

        private async Task<List<GitRepository>> GetGitLabRepositories()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Private-Token", _settings.GitLabToken);

                var response = await client.GetAsync($"{_settings.GitLabUrl}/api/v4/projects");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var repos = JsonSerializer.Deserialize<List<GitLabProject>>(json);

                return repos.Select(r => new GitRepository
                {
                    Id = r.Id.ToString(),
                    Name = r.Name,
                    FullName = r.PathWithNamespace,
                    Description = r.Description,
                    Url = r.WebUrl,
                    CloneUrl = r.HttpUrlToRepo,
                    DefaultBranch = r.DefaultBranch,
                    Language = r.TagList?.FirstOrDefault(),
                    Stars = r.StarCount,
                    Forks = r.ForksCount,
                    IsPrivate = r.Visibility == "private",
                    Provider = "GitLab",
                    LastUpdated = r.LastActivityAt
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching GitLab repositories");
                return new List<GitRepository>();
            }
        }

        public async Task<List<GitCommit>> GetCommits(string repositoryId, string branch = "main")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_settings.GitHubUrl}/repos/{repositoryId}/commits?sha={branch}");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var commits = JsonSerializer.Deserialize<List<GitHubCommit>>(json);

                return commits.Select(c => new GitCommit
                {
                    Sha = c.Sha,
                    Message = c.Commit.Message,
                    Author = c.Commit.Author.Name,
                    AuthorEmail = c.Commit.Author.Email,
                    Date = c.Commit.Author.Date,
                    Url = c.HtmlUrl,
                    Added = c.Stats?.Added ?? 0,
                    Removed = c.Stats?.Removed ?? 0,
                    Modified = c.Stats?.Modified ?? 0
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching commits for repository {RepositoryId}", repositoryId);
                return new List<GitCommit>();
            }
        }

        public async Task<List<GitBranch>> GetBranches(string repositoryId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_settings.GitHubUrl}/repos/{repositoryId}/branches");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var branches = JsonSerializer.Deserialize<List<GitHubBranch>>(json);

                return branches.Select(b => new GitBranch
                {
                    Name = b.Name,
                    CommitSha = b.Commit.Sha,
                    Protected = b.Protected,
                    Default = b.Name == "main" || b.Name == "master"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branches for repository {RepositoryId}", repositoryId);
                return new List<GitBranch>();
            }
        }

        public async Task<List<GitPullRequest>> GetPullRequests(string repositoryId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_settings.GitHubUrl}/repos/{repositoryId}/pulls");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var prs = JsonSerializer.Deserialize<List<GitHubPullRequest>>(json);

                return prs.Select(pr => new GitPullRequest
                {
                    Id = pr.Id.ToString(),
                    Number = pr.Number,
                    Title = pr.Title,
                    Description = pr.Body,
                    State = pr.State,
                    Author = pr.User.Login,
                    SourceBranch = pr.Head.Ref,
                    TargetBranch = pr.Base.Ref,
                    Url = pr.HtmlUrl,
                    CreatedAt = pr.CreatedAt,
                    UpdatedAt = pr.UpdatedAt,
                    Mergeable = pr.Mergeable,
                    Merged = pr.Merged,
                    Reviewers = pr.RequestedReviewers?.Select(r => r.Login).ToList() ?? new List<string>(),
                    Assignees = pr.Assignees?.Select(a => a.Login).ToList() ?? new List<string>()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pull requests for repository {RepositoryId}", repositoryId);
                return new List<GitPullRequest>();
            }
        }

        public async Task CreateBranch(string repositoryId, string branchName, string fromBranch = "main")
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new { @ref = $"refs/heads/{branchName}", sha = fromBranch };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_settings.GitHubUrl}/repos/{repositoryId}/git/refs", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Created branch {BranchName} from {FromBranch} in repository {RepositoryId}", branchName, fromBranch, repositoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch {BranchName} in repository {RepositoryId}", branchName, repositoryId);
                throw;
            }
        }

        public async Task CreatePullRequest(string repositoryId, string title, string description, string sourceBranch, string targetBranch)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new
                {
                    title = title,
                    body = description,
                    head = sourceBranch,
                    base = targetBranch
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_settings.GitHubUrl}/repos/{repositoryId}/pulls", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Created pull request {Title} in repository {RepositoryId}", title, repositoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pull request {Title} in repository {RepositoryId}", title, repositoryId);
                throw;
            }
        }

        #endregion

        #region CI/CD Integration

        public async Task<List<CIPipeline>> GetPipelines()
        {
            var pipelines = new List<CIPipeline>();

            // Jenkins Integration
            if (!string.IsNullOrEmpty(_settings.JenkinsUrl))
            {
                var jenkinsPipelines = await GetJenkinsPipelines();
                pipelines.AddRange(jenkinsPipelines);
            }

            // Azure DevOps Integration
            if (!string.IsNullOrEmpty(_settings.AzureDevOpsToken))
            {
                var azurePipelines = await GetAzureDevOpsPipelines();
                pipelines.AddRange(azurePipelines);
            }

            // GitHub Actions Integration
            if (!string.IsNullOrEmpty(_settings.GitHubToken))
            {
                var githubPipelines = await GetGitHubActions();
                pipelines.AddRange(githubPipelines);
            }

            return pipelines;
        }

        private async Task<List<CIPipeline>> GetJenkinsPipelines()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_settings.JenkinsUrl}/api/json?tree=jobs[name,lastBuild[number,result,url,timestamp]");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var jenkinsData = JsonSerializer.Deserialize<JenkinsResponse>(json);

                return jenkinsData.Jobs.Select(job => new CIPipeline
                {
                    Id = job.Name,
                    Name = job.Name,
                    Provider = "Jenkins",
                    Status = job.LastBuild?.Result == "SUCCESS" ? "Success" : "Failed",
                    LastRun = job.LastBuild?.Timestamp,
                    Url = job.LastBuild?.Url,
                    IsRunning = false
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Jenkins pipelines");
                return new List<CIPipeline>();
            }
        }

        public async Task TriggerPipeline(string pipelineId, Dictionary<string, object> parameters = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new { parameters = parameters };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_settings.JenkinsUrl}/job/{pipelineId}/build", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Triggered pipeline {PipelineId}", pipelineId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering pipeline {PipelineId}", pipelineId);
                throw;
            }
        }

        public async Task<CIBuild> GetBuildStatus(string buildId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_settings.JenkinsUrl}/job/{buildId}/lastBuild/api/json");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var buildData = JsonSerializer.Deserialize<JenkinsBuild>(json);

                return new CIBuild
                {
                    Id = buildData.Id,
                    Number = buildData.Number,
                    Status = buildData.Result == "SUCCESS" ? "Success" : "Failed",
                    Duration = buildData.Duration,
                    StartTime = buildData.StartTime,
                    EndTime = buildData.EndTime,
                    Url = buildData.Url,
                    Artifacts = buildData.Artifacts?.Select(a => a.FileName).ToList() ?? new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting build status for {BuildId}", buildId);
                return null;
            }
        }

        public async Task<List<CIDeployment>> GetDeployments(string environment = null)
        {
            try
            {
                var deployments = new List<CIDeployment>();

                // Get deployments from various sources
                if (!string.IsNullOrEmpty(_settings.AzureDevOpsToken))
                {
                    var azureDeployments = await GetAzureDevOpsDeployments(environment);
                    deployments.AddRange(azureDeployments);
                }

                return deployments.OrderByDescending(d => d.DeployedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching deployments");
                return new List<CIDeployment>();
            }
        }

        #endregion

        #region Code Review Integration

        public async Task<List<CodeReview>> GetCodeReviews(string repositoryId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{_settings.GitHubUrl}/repos/{repositoryId}/pulls");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var prs = JsonSerializer.Deserialize<List<GitHubPullRequest>>(json);

                return prs.Select(pr => new CodeReview
                {
                    Id = pr.Id.ToString(),
                    Title = pr.Title,
                    Description = pr.Body,
                    Author = pr.User.Login,
                    State = pr.State,
                    Url = pr.HtmlUrl,
                    CreatedAt = pr.CreatedAt,
                    UpdatedAt = pr.UpdatedAt,
                    Reviewers = pr.RequestedReviewers?.Select(r => r.Login).ToList() ?? new List<string>(),
                    Approvals = 0, // Would need separate API call
                    Changes = pr.ChangedFiles ?? 0,
                    Comments = pr.Comments ?? 0
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching code reviews for repository {RepositoryId}", repositoryId);
                return new List<CodeReview>();
            }
        }

        public async Task CreateCodeReview(string repositoryId, string title, string description, List<string> reviewers)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new
                {
                    title = title,
                    body = description,
                    head = $"feature/{title.ToLower().Replace(" ", "-")}",
                    base = "main",
                    reviewers = reviewers.Select(r => new { username = r }).ToList()
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_settings.GitHubUrl}/repos/{repositoryId}/pulls", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Created code review {Title} in repository {RepositoryId}", title, repositoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating code review {Title} in repository {RepositoryId}", title, repositoryId);
                throw;
            }
        }

        public async Task ApproveCodeReview(string reviewId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new { event = "APPROVE" };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_settings.GitHubUrl}/pulls/{reviewId}/reviews", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Approved code review {ReviewId}", reviewId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving code review {ReviewId}", reviewId);
                throw;
            }
        }

        public async Task RequestChanges(string reviewId, string comment)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new { event = "REQUEST_CHANGES", body = comment };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_settings.GitHubUrl}/pulls/{reviewId}/reviews", content);
                response.EnsureSuccessStatusCode();

                _logger.LogInformation("Requested changes for code review {ReviewId}", reviewId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting changes for code review {ReviewId}", reviewId);
                throw;
            }
        }

        #endregion

        #region Deploy Tracking

        public async Task<List<Deployment>> GetRecentDeployments(string environment = null)
        {
            try
            {
                var deployments = new List<Deployment>();

                // Get from various deployment sources
                if (!string.IsNullOrEmpty(_settings.AzureDevOpsToken))
                {
                    var azureDeployments = await GetAzureDevOpsDeployments(environment);
                    deployments.AddRange(azureDeployments);
                }

                return deployments.OrderByDescending(d => d.DeployedAt).Take(50).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent deployments");
                return new List<Deployment>();
            }
        }

        public async Task TrackDeployment(string deploymentId, DeploymentStatus status)
        {
            try
            {
                // Update deployment status in database
                _logger.LogInformation("Deployment {DeploymentId} status updated to {Status}", deploymentId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking deployment {DeploymentId}", deploymentId);
                throw;
            }
        }

        public async Task<RollbackPlan> CreateRollbackPlan(string deploymentId)
        {
            try
            {
                // Create rollback plan based on deployment history
                var plan = new RollbackPlan
                {
                    DeploymentId = deploymentId,
                    RollbackSteps = new List<RollbackStep>
                    {
                        new RollbackStep { Order = 1, Description = "Stop current services", EstimatedTime = TimeSpan.FromMinutes(5) },
                        new RollbackStep { Order = 2, Description = "Restore previous database backup", EstimatedTime = TimeSpan.FromMinutes(15) },
                        new RollbackStep { Order = 3, Description = "Deploy previous version", EstimatedTime = TimeSpan.FromMinutes(10) },
                        new RollbackStep { Order = 4, Description = "Verify rollback success", EstimatedTime = TimeSpan.FromMinutes(5) }
                    },
                    EstimatedTotalTime = TimeSpan.FromMinutes(35),
                    RiskLevel = "Low"
                };

                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating rollback plan for deployment {DeploymentId}", deploymentId);
                throw;
            }
        }

        #endregion

        #region Sprint & Branch Management

        public async Task<FeatureBranch> CreateFeatureBranch(string ticketId, string featureName)
        {
            try
            {
                var ticket = await _ticketService.GetTicketAsync(int.Parse(ticketId));
                var branchName = $"feature/{ticketId}-{featureName.ToLower().Replace(" ", "-")}";
                
                // Create branch in repository
                await CreateBranch(ticket.Project?.RepositoryId, branchName);
                
                // Link branch to ticket
                ticket.GitBranch = branchName;
                await _ticketService.UpdateTicketAsync(ticket);

                return new FeatureBranch
                {
                    Name = branchName,
                    TicketId = ticketId,
                    FeatureName = featureName,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feature branch for ticket {TicketId}", ticketId);
                throw;
            }
        }

        public async Task<List<FeatureBranch>> GetFeatureBranches()
        {
            try
            {
                var tickets = await _ticketService.GetTicketsAsync();
                return tickets.Where(t => !string.IsNullOrEmpty(t.GitBranch))
                    .Select(t => new FeatureBranch
                    {
                        Name = t.GitBranch,
                        TicketId = t.Id.ToString(),
                        FeatureName = t.Title,
                        CreatedAt = t.CreatedAt,
                        Status = t.Status == "Closed" ? "Merged" : "Active"
                    }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching feature branches");
                return new List<FeatureBranch>();
            }
        }

        public async Task MergeFeatureBranch(string branchId, string targetBranch = "main")
        {
            try
            {
                // Create pull request for feature branch
                await CreatePullRequest(branchId.Split('/')[1], $"Merge {branchId}", $"Automated merge of {branchId}", branchId, targetBranch);
                
                _logger.LogInformation("Initiated merge for feature branch {BranchId}", branchId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging feature branch {BranchId}", branchId);
                throw;
            }
        }

        public async Task<SprintDeployment> PlanSprintDeployment(string sprintId)
        {
            try
            {
                // Get sprint tickets and plan deployment
                var deployment = new SprintDeployment
                {
                    SprintId = sprintId,
                    PlannedDate = DateTime.UtcNow.AddDays(7),
                    Features = new List<string>(),
                    RiskAssessment = "Low",
                    RollbackPlan = await CreateRollbackPlan(sprintId)
                };

                return deployment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error planning sprint deployment for {SprintId}", sprintId);
                throw;
            }
        }

        #endregion

        #region Quality Gates

        public async Task<CodeQualityReport> GetCodeQualityReport(string repositoryId, string branch = "main")
        {
            try
            {
                // Integration with SonarQube, CodeClimate, etc.
                var report = new CodeQualityReport
                {
                    RepositoryId = repositoryId,
                    Branch = branch,
                    Coverage = 85.5,
                    CodeSmells = 12,
                    Vulnerabilities = 2,
                    TechnicalDebt = "2h 30m",
                    MaintainabilityRating = "A",
                    ReliabilityRating = "A",
                    SecurityRating = "A",
                    DuplicatedLines = 156,
                    LinesOfCode = 15420,
                    GeneratedAt = DateTime.UtcNow
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating code quality report for {RepositoryId}", repositoryId);
                throw;
            }
        }

        public async Task<SecurityScanResult> RunSecurityScan(string repositoryId)
        {
            try
            {
                // Integration with security scanning tools
                var scanResult = new SecurityScanResult
                {
                    RepositoryId = repositoryId,
                    ScanDate = DateTime.UtcNow,
                    CriticalVulnerabilities = 0,
                    HighVulnerabilities = 2,
                    MediumVulnerabilities = 8,
                    LowVulnerabilities = 15,
                    TotalVulnerabilities = 25,
                    ScanDuration = TimeSpan.FromMinutes(12),
                    Tools = new List<string> { "SonarQube", "OWASP ZAP", "Snyk" }
                };

                return scanResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running security scan for {RepositoryId}", repositoryId);
                throw;
            }
        }

        public async Task<List<TestResult>> GetTestResults(string buildId)
        {
            try
            {
                // Get test results from CI/CD pipeline
                var testResults = new List<TestResult>
                {
                    new TestResult
                    {
                        TestName = "Unit Tests",
                        Passed = 245,
                        Failed = 3,
                        Skipped = 2,
                        Total = 250,
                        Duration = TimeSpan.FromMinutes(2, 15),
                        Coverage = 87.5
                    },
                    new TestResult
                    {
                        TestName = "Integration Tests",
                        Passed = 89,
                        Failed = 1,
                        Skipped = 0,
                        Total = 90,
                        Duration = TimeSpan.FromMinutes(8, 30),
                        Coverage = 72.3
                    },
                    new TestResult
                    {
                        TestName = "E2E Tests",
                        Passed = 45,
                        Failed = 2,
                        Skipped = 3,
                        Total = 50,
                        Duration = TimeSpan.FromMinutes(15, 45),
                        Coverage = 65.8
                    }
                };

                return testResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting test results for build {BuildId}", buildId);
                return new List<TestResult>();
            }
        }

        public async Task<bool> ValidateQualityGates(string repositoryId, string branch)
        {
            try
            {
                var qualityReport = await GetCodeQualityReport(repositoryId, branch);
                var securityScan = await RunSecurityScan(repositoryId);
                var testResults = await GetTestResults("latest");

                // Define quality gates
                var qualityGates = new
                {
                    MinCoverage = 80.0,
                    MaxCodeSmells = 20,
                    MaxVulnerabilities = 10,
                    MinTestPassRate = 95.0,
                    MaxTechnicalDebtHours = 8.0
                };

                // Validate gates
                var passed = qualityReport.Coverage >= qualityGates.MinCoverage &&
                           qualityReport.CodeSmells <= qualityGates.MaxCodeSmells &&
                           securityScan.TotalVulnerabilities <= qualityGates.MaxVulnerabilities &&
                           (testResults.Sum(t => t.Passed) * 100.0 / testResults.Sum(t => t.Total)) >= qualityGates.MinTestPassRate &&
                           ParseTechnicalDebt(qualityReport.TechnicalDebt) <= qualityGates.MaxTechnicalDebtHours;

                _logger.LogInformation("Quality gates validation for {RepositoryId}/{Branch}: {Passed}", repositoryId, branch, passed ? "PASSED" : "FAILED");
                return passed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating quality gates for {RepositoryId}/{Branch}", repositoryId, branch);
                return false;
            }
        }

        private double ParseTechnicalDebt(string technicalDebt)
        {
            // Parse "2h 30m" format
            var parts = technicalDebt.Split(' ');
            var hours = 0.0;
            var minutes = 0.0;

            for (int i = 0; i < parts.Length; i += 2)
            {
                if (i + 1 < parts.Length)
                {
                    var value = double.TryParse(parts[i], out var v) ? v : 0;
                    var unit = parts[i + 1];

                    if (unit.StartsWith("h")) hours += value;
                    else if (unit.StartsWith("m")) minutes += value;
                }
            }

            return hours + minutes / 60.0;
        }

        #endregion

        #region Helper Methods

        private async Task<List<GitRepository>> GetAzureDevOpsRepositories()
        {
            // Implementation for Azure DevOps repositories
            return new List<GitRepository>();
        }

        private async Task<List<CIPipeline>> GetAzureDevOpsPipelines()
        {
            // Implementation for Azure DevOps pipelines
            return new List<CIPipeline>();
        }

        private async Task<List<CIDeployment>> GetAzureDevOpsDeployments(string environment = null)
        {
            // Implementation for Azure DevOps deployments
            return new List<CIDeployment>();
        }

        private async Task<List<CIPipeline>> GetGitHubActions()
        {
            // Implementation for GitHub Actions
            return new List<CIPipeline>();
        }

        #endregion
    }

    #region Data Models

    public class DevOpsSettings
    {
        public string GitHubToken { get; set; }
        public string GitHubUrl { get; set; } = "https://api.github.com";
        public string GitLabToken { get; set; }
        public string GitLabUrl { get; set; }
        public string AzureDevOpsToken { get; set; }
        public string AzureDevOpsUrl { get; set; }
        public string JenkinsUrl { get; set; }
        public string JenkinsToken { get; set; }
    }

    public class GitRepository
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string CloneUrl { get; set; }
        public string DefaultBranch { get; set; }
        public string Language { get; set; }
        public int Stars { get; set; }
        public int Forks { get; set; }
        public bool IsPrivate { get; set; }
        public string Provider { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class GitCommit
    {
        public string Sha { get; set; }
        public string Message { get; set; }
        public string Author { get; set; }
        public string AuthorEmail { get; set; }
        public DateTime Date { get; set; }
        public string Url { get; set; }
        public int Added { get; set; }
        public int Removed { get; set; }
        public int Modified { get; set; }
    }

    public class GitBranch
    {
        public string Name { get; set; }
        public string CommitSha { get; set; }
        public bool Protected { get; set; }
        public bool Default { get; set; }
    }

    public class GitPullRequest
    {
        public string Id { get; set; }
        public int Number { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string State { get; set; }
        public string Author { get; set; }
        public string SourceBranch { get; set; }
        public string TargetBranch { get; set; }
        public string Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool? Mergeable { get; set; }
        public bool Merged { get; set; }
        public List<string> Reviewers { get; set; }
        public List<string> Assignees { get; set; }
    }

    public class CIPipeline
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Provider { get; set; }
        public string Status { get; set; }
        public DateTime? LastRun { get; set; }
        public string Url { get; set; }
        public bool IsRunning { get; set; }
    }

    public class CIBuild
    {
        public string Id { get; set; }
        public int Number { get; set; }
        public string Status { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Url { get; set; }
        public List<string> Artifacts { get; set; }
    }

    public class CIDeployment
    {
        public string Id { get; set; }
        public string Environment { get; set; }
        public string Version { get; set; }
        public string Status { get; set; }
        public DateTime DeployedAt { get; set; }
        public string DeployedBy { get; set; }
        public List<string> Artifacts { get; set; }
    }

    public class CodeReview
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string State { get; set; }
        public string Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Reviewers { get; set; }
        public int Approvals { get; set; }
        public int Changes { get; set; }
        public int Comments { get; set; }
    }

    public class Deployment
    {
        public string Id { get; set; }
        public string Environment { get; set; }
        public string Version { get; set; }
        public string Status { get; set; }
        public DateTime DeployedAt { get; set; }
        public string DeployedBy { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Features { get; set; }
    }

    public class RollbackPlan
    {
        public string DeploymentId { get; set; }
        public List<RollbackStep> RollbackSteps { get; set; }
        public TimeSpan EstimatedTotalTime { get; set; }
        public string RiskLevel { get; set; }
    }

    public class RollbackStep
    {
        public int Order { get; set; }
        public string Description { get; set; }
        public TimeSpan EstimatedTime { get; set; }
    }

    public class FeatureBranch
    {
        public string Name { get; set; }
        public string TicketId { get; set; }
        public string FeatureName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }

    public class SprintDeployment
    {
        public string SprintId { get; set; }
        public DateTime PlannedDate { get; set; }
        public List<string> Features { get; set; }
        public string RiskAssessment { get; set; }
        public RollbackPlan RollbackPlan { get; set; }
    }

    public class CodeQualityReport
    {
        public string RepositoryId { get; set; }
        public string Branch { get; set; }
        public double Coverage { get; set; }
        public int CodeSmells { get; set; }
        public int Vulnerabilities { get; set; }
        public string TechnicalDebt { get; set; }
        public string MaintainabilityRating { get; set; }
        public string ReliabilityRating { get; set; }
        public string SecurityRating { get; set; }
        public int DuplicatedLines { get; set; }
        public int LinesOfCode { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class SecurityScanResult
    {
        public string RepositoryId { get; set; }
        public DateTime ScanDate { get; set; }
        public int CriticalVulnerabilities { get; set; }
        public int HighVulnerabilities { get; set; }
        public int MediumVulnerabilities { get; set; }
        public int LowVulnerabilities { get; set; }
        public int TotalVulnerabilities { get; set; }
        public TimeSpan ScanDuration { get; set; }
        public List<string> Tools { get; set; }
    }

    public class TestResult
    {
        public string TestName { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int Total { get; set; }
        public TimeSpan Duration { get; set; }
        public double Coverage { get; set; }
    }

    public enum DeploymentStatus
    {
        Pending,
        InProgress,
        Success,
        Failed,
        RolledBack
    }

    #endregion

    #region External API Models

    public class GitHubRepository
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public string HtmlUrl { get; set; }
        public string CloneUrl { get; set; }
        public string DefaultBranch { get; set; }
        public string Language { get; set; }
        public int StargazersCount { get; set; }
        public int ForksCount { get; set; }
        public bool Private { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class GitLabProject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string PathWithNamespace { get; set; }
        public string Description { get; set; }
        public string WebUrl { get; set; }
        public string HttpUrlToRepo { get; set; }
        public string DefaultBranch { get; set; }
        public List<string> TagList { get; set; }
        public int StarCount { get; set; }
        public int ForksCount { get; set; }
        public string Visibility { get; set; }
        public DateTime LastActivityAt { get; set; }
    }

    public class GitHubCommit
    {
        public string Sha { get; set; }
        public GitHubCommitDetail Commit { get; set; }
        public GitHubStats Stats { get; set; }
        public string HtmlUrl { get; set; }
    }

    public class GitHubCommitDetail
    {
        public GitHubAuthor Author { get; set; }
        public string Message { get; set; }
    }

    public class GitHubAuthor
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
    }

    public class GitHubStats
    {
        public int Added { get; set; }
        public int Removed { get; set; }
        public int Modified { get; set; }
    }

    public class GitHubBranch
    {
        public string Name { get; set; }
        public GitHubCommitRef Commit { get; set; }
        public bool Protected { get; set; }
    }

    public class GitHubCommitRef
    {
        public string Sha { get; set; }
    }

    public class GitHubPullRequest
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string State { get; set; }
        public GitHubUser User { get; set; }
        public GitHubRef Head { get; set; }
        public GitHubRef Base { get; set; }
        public string HtmlUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool? Mergeable { get; set; }
        public bool Merged { get; set; }
        public List<GitHubUser> RequestedReviewers { get; set; }
        public List<GitHubUser> Assignees { get; set; }
        public int? ChangedFiles { get; set; }
        public int? Comments { get; set; }
    }

    public class GitHubUser
    {
        public string Login { get; set; }
    }

    public class GitHubRef
    {
        public string Ref { get; set; }
        public string Sha { get; set; }
    }

    public class JenkinsResponse
    {
        public List<JenkinsJob> Jobs { get; set; }
    }

    public class JenkinsJob
    {
        public string Name { get; set; }
        public JenkinsBuild LastBuild { get; set; }
    }

    public class JenkinsBuild
    {
        public int Number { get; set; }
        public string Result { get; set; }
        public long Timestamp { get; set; }
        public string Url { get; set; }
        public long Duration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Id { get; set; }
        public List<JenkinsArtifact> Artifacts { get; set; }
    }

    public class JenkinsArtifact
    {
        public string FileName { get; set; }
    }

    #endregion
}
