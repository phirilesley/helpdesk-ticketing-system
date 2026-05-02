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
                    ref = $"refs/heads/{branchName}",
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
                    ref = "main",
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
                    ref = deployment.Ref,
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

    #region GitHub API Models

    public class GitHubRepository
    {
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public bool Private { get; set; }
        public GitHubOwner Owner { get; set; }
        public string HtmlUrl { get; set; }
        public string Description { get; set; }
        public bool Fork { get; set; }
        public string Url { get; set; }
        public string ForksUrl { get; set; }
        public string KeysUrl { get; set; }
        public string CollaboratorsUrl { get; set; }
        public string TeamsUrl { get; set; }
        public string HooksUrl { get; set; }
        public string IssueEventsUrl { get; set; }
        public string EventsUrl { get; set; }
        public string AssigneesUrl { get; set; }
        public string BranchesUrl { get; set; }
        public string TagsUrl { get; set; }
        public string BlobsUrl { get; set; }
        public string GitTagsUrl { get; set; }
        public string GitRefsUrl { get; set; }
        public string TreesUrl { get; set; }
        public string StatusesUrl { get; set; }
        public string LanguagesUrl { get; set; }
        public string StargazersUrl { get; set; }
        public string ContributorsUrl { get; set; }
        public string SubscribersUrl { get; set; }
        public string SubscriptionUrl { get; set; }
        public string CommitsUrl { get; set; }
        public string GitCommitsUrl { get; set; }
        public string CommentsUrl { get; set; }
        public string IssueCommentUrl { get; set; }
        public string ContentsUrl { get; set; }
        public string CompareUrl { get; set; }
        public string MergesUrl { get; set; }
        public string ArchiveUrl { get; set; }
        public string DownloadsUrl { get; set; }
        public string IssuesUrl { get; set; }
        public string PullsUrl { get; set; }
        public string MilestonesUrl { get; set; }
        public string NotificationsUrl { get; set; }
        public string LabelsUrl { get; set; }
        public string ReleasesUrl { get; set; }
        public string DeploymentsUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? PushedAt { get; set; }
        public string GitUrl { get; set; }
        public string SshUrl { get; set; }
        public string CloneUrl { get; set; }
        public string SvnUrl { get; set; }
        public string Homepage { get; set; }
        public int Size { get; set; }
        public int StargazersCount { get; set; }
        public int WatchersCount { get; set; }
        public string Language { get; set; }
        public bool HasIssues { get; set; }
        public bool HasProjects { get; set; }
        public bool HasDownloads { get; set; }
        public bool HasWiki { get; set; }
        public bool HasPages { get; set; }
        public int ForksCount { get; set; }
        public bool Archived { get; set; }
        public bool Disabled { get; set; }
        public int OpenIssuesCount { get; set; }
        public GitHubLicense License { get; set; }
        public List<GitHubTopic> Topics { get; set; }
        public string DefaultBranch { get; set; }
    }

    public class GitHubOwner
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string AvatarUrl { get; set; }
        public string GravatarId { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string FollowersUrl { get; set; }
        public string FollowingUrl { get; set; }
        public string GistsUrl { get; set; }
        public string StarredUrl { get; set; }
        public string SubscriptionsUrl { get; set; }
        public string OrganizationsUrl { get; set; }
        public string ReposUrl { get; set; }
        public string EventsUrl { get; set; }
        public string ReceivedEventsUrl { get; set; }
        public string Type { get; set; }
        public bool SiteAdmin { get; set; }
    }

    public class GitHubLicense
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string SpdxId { get; set; }
        public string Url { get; set; }
        public string NodeId { get; set; }
    }

    public class GitHubTopic
    {
        public string Name { get; set; }
    }

    public class GitHubCommit
    {
        public string Sha { get; set; }
        public string NodeId { get; set; }
        public string CommitUrl { get; set; }
        public string HtmlUrl { get; set; }
        public GitHubCommitDetails Commit { get; set; }
        public GitHubAuthor Author { get; set; }
        public GitHubCommitter Committer { get; set; }
        public List<GitHubParent> Parents { get; set; }
        public GitHubStats Stats { get; set; }
        public List<GitHubFile> Files { get; set; }
    }

    public class GitHubCommitDetails
    {
        public GitHubAuthorInfo Author { get; set; }
        public GitHubAuthorInfo Committer { get; set; }
        public string Message { get; set; }
        public GitHubCommitTree Tree { get; set; }
        public string Url { get; set; }
        public int CommentCount { get; set; }
        public GitHubVerification Verification { get; set; }
    }

    public class GitHubAuthorInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime Date { get; set; }
    }

    public class GitHubCommitTree
    {
        public string Sha { get; set; }
        public string Url { get; set; }
    }

    public class GitHubVerification
    {
        public bool Verified { get; set; }
        public string Reason { get; set; }
        public string Signature { get; set; }
        public string Payload { get; set; }
    }

    public class GitHubParent
    {
        public string Sha { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
    }

    public class GitHubStats
    {
        public int Total { get; set; }
        public int Additions { get; set; }
        public int Deletions { get; set; }
    }

    public class GitHubFile
    {
        public string Sha { get; set; }
        public string Filename { get; set; }
        public string Status { get; set; }
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int Changes { get; set; }
        public string BlobUrl { get; set; }
        public string RawUrl { get; set; }
        public string ContentsUrl { get; set; }
        public string Patch { get; set; }
    }

    public class GitHubBranch
    {
        public string Name { get; set; }
        public GitHubCommit Commit { get; set; }
        public bool Protected { get; set; }
        public GitHubProtection Protection { get; set; }
        public GitHubProtectionUrl ProtectionUrl { get; set; }
    }

    public class GitHubProtection
    {
        public bool Enabled { get; set; }
        public bool RequiredStatusChecks { get; set; }
        public bool EnforceAdmins { get; set; }
        public bool RequiredPullRequestReviews { get; set; }
        public bool Restrictions { get; set; }
    }

    public class GitHubProtectionUrl
    {
        public string Url { get; set; }
    }

    public class GitHubPullRequest
    {
        public string Url { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string HtmlUrl { get; set; }
        public string DiffUrl { get; set; }
        public string PatchUrl { get; set; }
        public string IssueUrl { get; set; }
        public int Number { get; set; }
        public string State { get; set; }
        public bool Locked { get; set; }
        public string Title { get; set; }
        public GitHubUser User { get; set; }
        public GitHubUser Assignee { get; set; }
        public List<GitHubUser> Assignees { get; set; }
        public List<GitHubUser> RequestedReviewers { get; set; }
        public List<GitHubUser> RequestedTeams { get; set; }
        public GitHubLabel[] Labels { get; set; }
        public GitHubMilestone Milestone { get; set; }
        public bool Draft { get; set; }
        public GitHubPullRequestHead Head { get; set; }
        public GitHubPullRequestBase Base { get; set; }
        public GitHubPullRequestLinks _Links { get; set; }
        public GitHubAuthor Author { get; set; }
        public GitHubCommitter Committer { get; set; }
        public List<GitHubReview> ReviewComments { get; set; }
        public int ReviewCommentCount { get; set; }
        public List<GitHubCommit> Commits { get; set; }
        public List<GitHubFile> Files { get; set; }
        public GitHubPatch Patch { get; set; }
        public int Comments { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime? MergedAt { get; set; }
        public bool Merged { get; set; }
        public GitHubUser MergedBy { get; set; }
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int ChangedFiles { get; set; }
    }

    public class GitHubUser
    {
        public string Login { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string AvatarUrl { get; set; }
        public string GravatarId { get; set; }
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string FollowersUrl { get; set; }
        public string FollowingUrl { get; set; }
        public string GistsUrl { get; set; }
        public string StarredUrl { get; set; }
        public string SubscriptionsUrl { get; set; }
        public string OrganizationsUrl { get; set; }
        public string ReposUrl { get; set; }
        public string EventsUrl { get; set; }
        public string ReceivedEventsUrl { get; set; }
        public string Type { get; set; }
        public bool SiteAdmin { get; set; }
    }

    public class GitHubLabel
    {
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string Description { get; set; }
        public bool Default { get; set; }
    }

    public class GitHubMilestone
    {
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string LabelsUrl { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public int Number { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public GitHubUser Creator { get; set; }
        public int OpenIssues { get; set; }
        public int ClosedIssues { get; set; }
        public string State { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DueOn { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    public class GitHubPullRequestHead
    {
        public string Label { get; set; }
        public string Ref { get; set; }
        public string Sha { get; set; }
        public GitHubUser User { get; set; }
        public GitHubRepository Repo { get; set; }
    }

    public class GitHubPullRequestBase
    {
        public string Label { get; set; }
        public string Ref { get; set; }
        public string Sha { get; set; }
        public GitHubUser User { get; set; }
        public GitHubRepository Repo { get; set; }
    }

    public class GitHubPullRequestLinks
    {
        public GitHubLink Self { get; set; }
        public GitHubLink Html { get; set; }
        public GitHubLink Issue { get; set; }
        public GitHubLink Comments { get; set; }
        public GitHubLink ReviewComments { get; set; }
        public GitHubLink ReviewComment { get; set; }
        public GitHubLink Commits { get; set; }
        public GitHubLink Statuses { get; set; }
        public GitHubLink PullRequest { get; set; }
    }

    public class GitHubLink
    {
        public string Href { get; set; }
    }

    public class GitHubStatus
    {
        public string Url { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string State { get; set; }
        public string TargetUrl { get; set; }
        public string Description { get; set; }
        public string Context { get; set; }
        public GitHubUser Creator { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class GitHubWorkflowRun
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string HeadBranch { get; set; }
        public string HeadSha { get; set; }
        public GitHubWorkflowRunStatus Status { get; set; }
        public GitHubWorkflowRunConclusion Conclusion { get; set; }
        public GitHubWorkflowRunWorkflow Workflow { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? RunStartedAt { get; set; }
        public List<GitHubWorkflowRunJob> Jobs { get; set; }
        public GitHubWorkflowRunCheckSuiteUrl CheckSuiteUrl { get; set; }
        public GitHubWorkflowRunLogsUrl LogsUrl { get; set; }
        public GitHubWorkflowRunCheckSuiteUrl CheckSuiteUrl { get; set; }
        public GitHubWorkflowRunArtifactsUrl ArtifactsUrl { get; set; }
        public GitHubWorkflowRunCancelUrl CancelUrl { get; set; }
        public GitHubWorkflowRunRerunUrl RerunUrl { get; set; }
        public GitHubWorkflowRunWorkflowUrl WorkflowUrl { get; set; }
        public GitHubWorkflowRunHtmlUrl HtmlUrl { get; set; }
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
        public int CheckRunUrl { get; set; }
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

    public class GitHubDeploymentRequest
    {
        public string Ref { get; set; }
        public string Task { get; set; }
        public bool AutoMerge { get; set; }
        public List<string> RequiredContexts { get; set; }
        public object Payload { get; set; }
        public string Environment { get; set; }
        public string Description { get; set; }
    }

    public class GitHubDeployment
    {
        public string Url { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string Sha { get; set; }
        public string Ref { get; set; }
        public string Task { get; set; }
        public GitHubDeploymentPayload Payload { get; set; }
        public string Environment { get; set; }
        public string Description { get; set; }
        public GitHubUser Creator { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public GitHubDeploymentStatuses Statuses { get; set; }
    }

    public class GitHubDeploymentPayload
    {
        public object Data { get; set; }
    }

    public class GitHubDeploymentStatuses
    {
        public string Url { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string State { get; set; }
        public string TargetUrl { get; set; }
        public string Description { get; set; }
        public GitHubUser Creator { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class GitHubRelease
    {
        public string Url { get; set; }
        public string HtmlUrl { get; set; }
        public string AssetsUrl { get; set; }
        public string UploadUrl { get; set; }
        public string TarballUrl { get; set; }
        public string ZipballUrl { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string TagName { get; set; }
        public string TargetCommitish { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime PublishedAt { get; set; }
        public GitHubAuthor Author { get; set; }
        public List<GitHubAsset> Assets { get; set; }
    }

    public class GitHubReleaseRequest
    {
        public string TagName { get; set; }
        public string TargetCommitish { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public bool Draft { get; set; }
        public bool Prerelease { get; set; }
    }

    public class GitHubAsset
    {
        public string Url { get; set; }
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public GitHubAuthor Uploader { get; set; }
        public string ContentType { get; set; }
        public string State { get; set; }
        public int Size { get; set; }
        public int DownloadCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string BrowserDownloadUrl { get; set; }
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
