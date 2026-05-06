using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using HelpDeskSystem.Application.Interfaces;

namespace HelpDeskSystem.DevOps.Services
{
    public interface IGitHubIntegrationService
    {
        Task<List<GitHubRepository>> GetRepositories();
        Task<List<GitHubCommit>> GetCommits(string owner, string repo, string branch = "main");
        Task<List<GitHubBranch>> GetBranches(string owner, string repo);
        Task<List<GitHubPullRequest>> GetPullRequests(string owner, string repo);
        Task<GitHubPullRequest> CreatePullRequest(string owner, string repo, string title, string description, string head, string @base);
        Task<GitHubCommit> CreateCommit(string owner, string repo, string message, string tree, string[] parents);
        Task<GitHubBranch> CreateBranch(string owner, string repo, string branchName, string fromBranch = "main");
        Task<GitHubStatus> CreateCommitStatus(string owner, string repo, string sha, GitHubStatus status);
        Task<GitHubWorkflowRun> TriggerWorkflow(string owner, string repo, string workflowId, Dictionary<string, object> inputs);
        Task<List<GitHubWorkflowRun>> GetWorkflowRuns(string owner, string repo);
        Task<GitHubDeployment> CreateDeployment(string owner, string repo, GitHubDeploymentRequest deployment);
        Task<List<GitHubRelease>> GetReleases(string owner, string repo);
        Task<GitHubRelease> CreateRelease(string owner, string repo, GitHubReleaseRequest release);
    }

    public class GitHubIntegrationService : IGitHubIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubIntegrationService> _logger;
        private readonly string _accessToken;
        private readonly string _apiBaseUrl = "https://api.github.com";

        public GitHubIntegrationService(HttpClient httpClient, ILogger<GitHubIntegrationService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _accessToken = configuration["GitHub:AccessToken"];
            
            _httpClient.BaseAddress = new Uri(_apiBaseUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "HelpDeskSystem");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_accessToken}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        }

        public async Task<List<GitHubRepository>> GetRepositories()
        {
            try
            {
                var response = await _httpClient.GetAsync("/user/repos");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var repos = JsonSerializer.Deserialize<List<GitHubRepository>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Retrieved {Count} repositories from GitHub", repos.Count);
                return repos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching repositories from GitHub");
                throw;
            }
        }

        public async Task<List<GitHubCommit>> GetCommits(string owner, string repo, string branch = "main")
        {
            try
            {
                var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/commits?sha={branch}");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var commits = JsonSerializer.Deserialize<List<GitHubCommit>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Retrieved {Count} commits from {Owner}/{Repo}", commits.Count, owner, repo);
                return commits;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching commits from {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        public async Task<List<GitHubBranch>> GetBranches(string owner, string repo)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/branches");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var branches = JsonSerializer.Deserialize<List<GitHubBranch>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Retrieved {Count} branches from {Owner}/{Repo}", branches.Count, owner, repo);
                return branches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching branches from {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        public async Task<List<GitHubPullRequest>> GetPullRequests(string owner, string repo)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/pulls?state=all");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var pullRequests = JsonSerializer.Deserialize<List<GitHubPullRequest>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Retrieved {Count} pull requests from {Owner}/{Repo}", pullRequests.Count, owner, repo);
                return pullRequests;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pull requests from {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        public async Task<GitHubPullRequest> CreatePullRequest(string owner, string repo, string title, string description, string head, string @base)
        {
            try
            {
                var prRequest = new
                {
                    title = title,
                    body = description,
                    head = head,
                    @base = @base
                };

                var json = JsonSerializer.Serialize(prRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/pulls", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var pullRequest = JsonSerializer.Deserialize<GitHubPullRequest>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Created pull request #{Number} in {Owner}/{Repo}", pullRequest.Number, owner, repo);
                return pullRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pull request in {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        public async Task<GitHubBranch> CreateBranch(string owner, string repo, string branchName, string fromBranch = "main")
        {
            try
            {
                // Get the reference for the source branch
                var sourceResponse = await _httpClient.GetAsync($"/repos/{owner}/{repo}/git/refs/heads/{fromBranch}");
                sourceResponse.EnsureSuccessStatusCode();
                
                var sourceJson = await sourceResponse.Content.ReadAsStringAsync();
                var sourceRef = JsonSerializer.Deserialize<GitHubReference>(sourceJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Create new branch reference
                var newBranchRef = new
                {
                    @ref = $"refs/heads/{branchName}",
                    sha = sourceRef.Object.Sha
                };

                var json = JsonSerializer.Serialize(newBranchRef);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/git/refs", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var branch = JsonSerializer.Deserialize<GitHubBranch>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Created branch {BranchName} in {Owner}/{Repo}", branchName, owner, repo);
                return branch;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch {BranchName} in {Owner}/{Repo}", branchName, owner, repo);
                throw;
            }
        }

        public async Task<GitHubCommit> CreateCommit(string owner, string repo, string message, string tree, string[] parents)
        {
            try
            {
                var commitRequest = new
                {
                    message = message,
                    tree = tree,
                    parents = parents
                };

                var json = JsonSerializer.Serialize(commitRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/git/commits", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var commit = JsonSerializer.Deserialize<GitHubCommit>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Created commit {Sha} in {Owner}/{Repo}", commit.Sha, owner, repo);
                return commit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating commit in {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        public async Task<GitHubStatus> CreateCommitStatus(string owner, string repo, string sha, GitHubStatus status)
        {
            try
            {
                var statusRequest = new
                {
                    state = status.State,
                    target_url = status.TargetUrl,
                    description = status.Description,
                    context = status.Context
                };

                var json = JsonSerializer.Serialize(statusRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/statuses/{sha}", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var createdStatus = JsonSerializer.Deserialize<GitHubStatus>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Created status {State} for commit {Sha} in {Owner}/{Repo}", status.State, sha, owner, repo);
                return createdStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating status for commit {Sha} in {Owner}/{Repo}", sha, owner, repo);
                throw;
            }
        }

        public async Task<GitHubWorkflowRun> TriggerWorkflow(string owner, string repo, string workflowId, Dictionary<string, object> inputs)
        {
            try
            {
                var triggerRequest = new
                {
                    @ref = "main",
                    inputs = inputs
                };

                var json = JsonSerializer.Serialize(triggerRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/actions/workflows/{workflowId}/dispatches", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var workflowRun = JsonSerializer.Deserialize<GitHubWorkflowRun>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Triggered workflow {WorkflowId} in {Owner}/{Repo}", workflowId, owner, repo);
                return workflowRun;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering workflow {WorkflowId} in {Owner}/{Repo}", workflowId, owner, repo);
                throw;
            }
        }

        public async Task<List<GitHubWorkflowRun>> GetWorkflowRuns(string owner, string repo)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/actions/runs");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var workflowResponse = JsonSerializer.Deserialize<GitHubWorkflowResponse>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Retrieved {Count} workflow runs from {Owner}/{Repo}", workflowResponse.WorkflowRuns.Count, owner, repo);
                return workflowResponse.WorkflowRuns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching workflow runs from {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        public async Task<GitHubDeployment> CreateDeployment(string owner, string repo, GitHubDeploymentRequest deployment)
        {
            try
            {
                var deploymentRequest = new
                {
                    @ref = deployment.Ref,
                    task = deployment.Task,
                    auto_merge = deployment.AutoMerge,
                    required_contexts = deployment.RequiredContexts,
                    payload = deployment.Payload,
                    environment = deployment.Environment,
                    description = deployment.Description
                };

                var json = JsonSerializer.Serialize(deploymentRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/deployments", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var createdDeployment = JsonSerializer.Deserialize<GitHubDeployment>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Created deployment {Id} in {Owner}/{Repo}", createdDeployment.Id, owner, repo);
                return createdDeployment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deployment in {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        public async Task<List<GitHubRelease>> GetReleases(string owner, string repo)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/releases");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var releases = JsonSerializer.Deserialize<List<GitHubRelease>>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Retrieved {Count} releases from {Owner}/{Repo}", releases.Count, owner, repo);
                return releases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching releases from {Owner}/{Repo}", owner, repo);
                throw;
            }
        }

        public async Task<GitHubRelease> CreateRelease(string owner, string repo, GitHubReleaseRequest release)
        {
            try
            {
                var releaseRequest = new
                {
                    tag_name = release.TagName,
                    target_commitish = release.TargetCommitish,
                    name = release.Name,
                    body = release.Body,
                    draft = release.Draft,
                    prerelease = release.Prerelease
                };

                var json = JsonSerializer.Serialize(releaseRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"/repos/{owner}/{repo}/releases", content);
                response.EnsureSuccessStatusCode();
                
                var responseJson = await response.Content.ReadAsStringAsync();
                var createdRelease = JsonSerializer.Deserialize<GitHubRelease>(responseJson, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogInformation("Created release {TagName} in {Owner}/{Repo}", release.TagName, owner, repo);
                return createdRelease;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating release {TagName} in {Owner}/{Repo}", release.TagName, owner, repo);
                throw;
            }
        }
    }

    #region Unique GitHub API Models (not in DevOpsModels.cs)

    public class GitHubLink
    {
        public string Href { get; set; }
    }

    public class GitHubCommitter
    {
        public string Login { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class GitHubPatch
    {
        public string Content { get; set; }
    }

    public class GitHubPullRequestHead
    {
        public string Ref { get; set; }
        public string Sha { get; set; }
    }

    public class GitHubPullRequestBase
    {
        public string Ref { get; set; }
        public string Sha { get; set; }
    }

    public class GitHubPullRequestLinks
    {
        public GitHubLink Html { get; set; }
        public GitHubLink Commits { get; set; }
        public GitHubLink Statuses { get; set; }
        public GitHubLink PullRequest { get; set; }
    }

    public class GitHubReview
    {
        public long Id { get; set; }
        public string Body { get; set; }
        public string State { get; set; }
    }

    public class GitHubLabel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
    }

    public class GitHubMilestone
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string State { get; set; }
    }

    public enum GitHubWorkflowRunStatus
    {
        Queued,
        InProgress,
        Completed
    }

    public enum GitHubWorkflowRunConclusion
    {
        Success,
        Failure,
        Neutral,
        Cancelled,
        Skipped,
        TimedOut,
        ActionRequired
    }

    public class GitHubWorkflowRunWorkflow
    {
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string HtmlUrl { get; set; }
        public string BadgeUrl { get; set; }
    }

    public class GitHubWorkflowRunJob
    {
        public int Id { get; set; }
        public string RunId { get; set; }
        public string RunUrl { get; set; }
        public string NodeId { get; set; }
        public string HeadSha { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public GitHubWorkflowRunJobStatus Status { get; set; }
        public GitHubWorkflowRunJobConclusion Conclusion { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Name { get; set; }
        public List<string> Steps { get; set; }
        public string CheckRunUrl { get; set; }
        public List<string> Labels { get; set; }
        public int RunnerId { get; set; }
        public string RunnerName { get; set; }
        public string RunnerGroupId { get; set; }
        public string RunnerGroupName { get; set; }
    }

    public enum GitHubWorkflowRunJobStatus
    {
        Queued,
        InProgress,
        Completed
    }

    public enum GitHubWorkflowRunJobConclusion
    {
        Success,
        Failure,
        Neutral,
        Cancelled,
        Skipped,
        TimedOut,
        ActionRequired
    }

    public class GitHubWorkflowResponse
    {
        public int TotalCount { get; set; }
        public List<GitHubWorkflowRun> WorkflowRuns { get; set; }
    }

    public class GitHubReference
    {
        public string Ref { get; set; }
        public string NodeId { get; set; }
        public string Url { get; set; }
        public GitHubReferenceObject Object { get; set; }
    }

    public class GitHubReferenceObject
    {
        public string Sha { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
    }

    #endregion
}
