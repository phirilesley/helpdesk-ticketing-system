using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using HelpDeskSystem.DevOps.Services;
using HelpDeskSystem.API.DTOs.DevOps;

namespace HelpDeskSystem.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/devops")]
    public class DevOpsController : ControllerBase
    {
        private readonly IDevOpsIntegrationService _devOpsService;
        private readonly IGitHubIntegrationService _gitHubService;

        public DevOpsController(IDevOpsIntegrationService devOpsService, IGitHubIntegrationService gitHubService)
        {
            _devOpsService = devOpsService;
            _gitHubService = gitHubService;
        }

        // GitHub Integration
        [HttpGet("github/repositories")]
        public async Task<IActionResult> GetGitHubRepositories()
        {
            var repositories = await _gitHubService.GetRepositories();
            return Ok(repositories);
        }

        [HttpGet("github/repositories/{owner}/{repo}/commits")]
        public async Task<IActionResult> GetGitHubCommits(string owner, string repo, string branch = "main")
        {
            var commits = await _gitHubService.GetCommits(owner, repo, branch);
            return Ok(commits);
        }

        [HttpGet("github/repositories/{owner}/{repo}/branches")]
        public async Task<IActionResult> GetGitHubBranches(string owner, string repo)
        {
            var branches = await _gitHubService.GetBranches(owner, repo);
            return Ok(branches);
        }

        [HttpGet("github/repositories/{owner}/{repo}/pulls")]
        public async Task<IActionResult> GetGitHubPullRequests(string owner, string repo)
        {
            var pullRequests = await _gitHubService.GetPullRequests(owner, repo);
            return Ok(pullRequests);
        }

        [HttpPost("github/repositories/{owner}/{repo}/pulls")]
        public async Task<IActionResult> CreateGitHubPullRequest(string owner, string repo, [FromBody] CreatePullRequestDto request)
        {
            var pullRequest = await _gitHubService.CreatePullRequest(owner, repo, request.Title, request.Description, request.Head, request.Base);
            return Ok(pullRequest);
        }

        [HttpPost("github/repositories/{owner}/{repo}/branches")]
        public async Task<IActionResult> CreateGitHubBranch(string owner, string repo, [FromBody] CreateBranchDto request)
        {
            var branch = await _gitHubService.CreateBranch(owner, repo, request.BranchName, request.FromBranch);
            return Ok(branch);
        }

        [HttpPost("github/repositories/{owner}/{repo}/commits")]
        public async Task<IActionResult> CreateCommit(string owner, string repo, [FromBody] CreateCommitDto request)
        {
            var commit = await _gitHubService.CreateCommit(owner, repo, request.Message, request.Tree, request.Parents);
            return Ok(commit);
        }

        [HttpPost("github/repositories/{owner}/{repo}/statuses/{sha}")]
        public async Task<IActionResult> CreateCommitStatus(string owner, string repo, string sha, [FromBody] CreateStatusDto request)
        {
            var status = new GitHubStatus
            {
                State = request.State,
                TargetUrl = request.TargetUrl,
                Description = request.Description,
                Context = request.Context
            };
            var result = await _gitHubService.CreateCommitStatus(owner, repo, sha, status);
            return Ok(result);
        }

        [HttpPost("github/repositories/{owner}/{repo}/actions/workflows/{workflowId}/dispatches")]
        public async Task<IActionResult> TriggerWorkflow(string owner, string repo, string workflowId, [FromBody] TriggerWorkflowDto request)
        {
            var workflowRun = await _gitHubService.TriggerWorkflow(owner, repo, workflowId, request.Inputs);
            return Ok(workflowRun);
        }

        [HttpGet("github/repositories/{owner}/{repo}/actions/runs")]
        public async Task<IActionResult> GetWorkflowRuns(string owner, string repo)
        {
            var workflowRuns = await _gitHubService.GetWorkflowRuns(owner, repo);
            return Ok(workflowRuns);
        }

        [HttpPost("github/repositories/{owner}/{repo}/deployments")]
        public async Task<IActionResult> CreateDeployment(string owner, string repo, [FromBody] CreateDeploymentDto request)
        {
            var deployment = new GitHubDeploymentRequest
            {
                Ref = request.Ref,
                Task = request.Task,
                Environment = request.Environment,
                Description = request.Description
            };
            var result = await _gitHubService.CreateDeployment(owner, repo, deployment);
            return Ok(result);
        }

        [HttpGet("github/repositories/{owner}/{repo}/releases")]
        public async Task<IActionResult> GetReleases(string owner, string repo)
        {
            var releases = await _gitHubService.GetReleases(owner, repo);
            return Ok(releases);
        }

        [HttpPost("github/repositories/{owner}/{repo}/releases")]
        public async Task<IActionResult> CreateRelease(string owner, string repo, [FromBody] CreateReleaseDto request)
        {
            var release = new GitHubReleaseRequest
            {
                TagName = request.TagName,
                TargetCommitish = request.TargetCommitish,
                Name = request.Name,
                Body = request.Body,
                Draft = request.Draft,
                Prerelease = request.Prerelease
            };
            var result = await _gitHubService.CreateRelease(owner, repo, release);
            return Ok(result);
        }

        // General DevOps Operations
        [HttpGet("repositories")]
        public async Task<IActionResult> GetRepositories()
        {
            var repositories = await _devOpsService.GetRepositories();
            return Ok(repositories);
        }

        [HttpGet("repositories/{repositoryId}/commits")]
        public async Task<IActionResult> GetCommits(string repositoryId, string branch = "main")
        {
            var commits = await _devOpsService.GetCommits(repositoryId, branch);
            return Ok(commits);
        }

        [HttpGet("repositories/{repositoryId}/branches")]
        public async Task<IActionResult> GetBranches(string repositoryId)
        {
            var branches = await _devOpsService.GetBranches(repositoryId);
            return Ok(branches);
        }

        [HttpGet("repositories/{repositoryId}/pulls")]
        public async Task<IActionResult> GetPullRequests(string repositoryId)
        {
            var pullRequests = await _devOpsService.GetPullRequests(repositoryId);
            return Ok(pullRequests);
        }

        [HttpPost("repositories/{repositoryId}/branches")]
        public async Task<IActionResult> CreateBranch(string repositoryId, [FromBody] CreateBranchDto request)
        {
            await _devOpsService.CreateBranch(repositoryId, request.BranchName, request.FromBranch);
            return Ok();
        }

        [HttpPost("repositories/{repositoryId}/pulls")]
        public async Task<IActionResult> CreatePullRequest(string repositoryId, [FromBody] CreatePullRequestDto request)
        {
            await _devOpsService.CreatePullRequest(repositoryId, request.Title, request.Description, request.SourceBranch, request.TargetBranch);
            return Ok();
        }

        [HttpGet("pipelines")]
        public async Task<IActionResult> GetPipelines()
        {
            var pipelines = await _devOpsService.GetPipelines();
            return Ok(pipelines);
        }

        [HttpPost("pipelines/{pipelineId}/trigger")]
        public async Task<IActionResult> TriggerPipeline(string pipelineId, [FromBody] TriggerPipelineDto request)
        {
            await _devOpsService.TriggerPipeline(pipelineId, request.Parameters);
            return Ok();
        }

        [HttpGet("builds/{buildId}")]
        public async Task<IActionResult> GetBuildStatus(string buildId)
        {
            var build = await _devOpsService.GetBuildStatus(buildId);
            return Ok(build);
        }

        [HttpGet("deployments")]
        public async Task<IActionResult> GetDeployments(string environment = null)
        {
            var deployments = await _devOpsService.GetDeployments(environment);
            return Ok(deployments);
        }

        [HttpGet("repositories/{repositoryId}/reviews")]
        public async Task<IActionResult> GetCodeReviews(string repositoryId)
        {
            var reviews = await _devOpsService.GetCodeReviews(repositoryId);
            return Ok(reviews);
        }

        [HttpPost("repositories/{repositoryId}/reviews")]
        public async Task<IActionResult> CreateCodeReview(string repositoryId, [FromBody] CreateCodeReviewDto request)
        {
            await _devOpsService.CreateCodeReview(repositoryId, request.Title, request.Description, request.Reviewers);
            return Ok();
        }

        [HttpPost("reviews/{reviewId}/approve")]
        public async Task<IActionResult> ApproveCodeReview(string reviewId)
        {
            await _devOpsService.ApproveCodeReview(reviewId);
            return Ok();
        }

        [HttpPost("reviews/{reviewId}/request-changes")]
        public async Task<IActionResult> RequestChanges(string reviewId, [FromBody] RequestChangesDto request)
        {
            await _devOpsService.RequestChanges(reviewId, request.Comment);
            return Ok();
        }

        [HttpGet("deployments/recent")]
        public async Task<IActionResult> GetRecentDeployments(string environment = null)
        {
            var deployments = await _devOpsService.GetRecentDeployments(environment);
            return Ok(deployments);
        }

        [HttpPost("deployments/{deploymentId}/track")]
        public async Task<IActionResult> TrackDeployment(string deploymentId, [FromBody] TrackDeploymentDto request)
        {
            var parsed = Enum.TryParse<DeploymentStatus>(request.Status, true, out var status)
                ? status
                : DeploymentStatus.Pending;
            await _devOpsService.TrackDeployment(deploymentId, parsed);
            return Ok();
        }

        [HttpPost("deployments/{deploymentId}/rollback")]
        public async Task<IActionResult> CreateRollbackPlan(string deploymentId)
        {
            var rollbackPlan = await _devOpsService.CreateRollbackPlan(deploymentId);
            return Ok(rollbackPlan);
        }

        [HttpPost("feature-branches")]
        public async Task<IActionResult> CreateFeatureBranch([FromBody] CreateFeatureBranchDto request)
        {
            var featureBranch = await _devOpsService.CreateFeatureBranch(request.TicketId, request.FeatureName);
            return Ok(featureBranch);
        }

        [HttpGet("feature-branches")]
        public async Task<IActionResult> GetFeatureBranches()
        {
            var featureBranches = await _devOpsService.GetFeatureBranches();
            return Ok(featureBranches);
        }

        [HttpPost("feature-branches/{branchId}/merge")]
        public async Task<IActionResult> MergeFeatureBranch(string branchId, [FromBody] MergeFeatureBranchDto request)
        {
            await _devOpsService.MergeFeatureBranch(branchId, request.TargetBranch);
            return Ok();
        }

        [HttpPost("sprints/{sprintId}/deploy")]
        public async Task<IActionResult> PlanSprintDeployment(string sprintId, [FromBody] PlanSprintDeploymentDto request)
        {
            var deployment = await _devOpsService.PlanSprintDeployment(sprintId);
            return Ok(deployment);
        }

        [HttpGet("repositories/{repositoryId}/quality")]
        public async Task<IActionResult> GetCodeQualityReport(string repositoryId, string branch = "main")
        {
            var report = await _devOpsService.GetCodeQualityReport(repositoryId, branch);
            return Ok(report);
        }

        [HttpGet("repositories/{repositoryId}/security")]
        public async Task<IActionResult> GetSecurityScanReport(string repositoryId, string branch = "main")
        {
            var report = await _devOpsService.RunSecurityScan(repositoryId);
            return Ok(report);
        }

        [HttpGet("repositories/{repositoryId}/coverage")]
        public async Task<IActionResult> GetTestCoverageReport(string repositoryId, string branch = "main")
        {
            var report = await _devOpsService.GetTestResults("latest");
            return Ok(report);
        }

        [HttpGet("repositories/{repositoryId}/dependencies")]
        public async Task<IActionResult> GetDependencyReport(string repositoryId)
        {
            return Ok(new { RepositoryId = repositoryId, Message = "Dependency report not implemented in current DevOps service contract." });
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDevOpsDashboard()
        {
            var dashboard = new
            {
                Repositories = await _devOpsService.GetRepositories(),
                Pipelines = await _devOpsService.GetPipelines(),
                Deployments = await _devOpsService.GetRecentDeployments()
            };
            return Ok(dashboard);
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetDevOpsMetrics([FromQuery] DevOpsMetricsFilter filter)
        {
            var metrics = new
            {
                PeriodStart = filter.StartDate,
                PeriodEnd = filter.EndDate,
                Repositories = (await _devOpsService.GetRepositories()).Count,
                ActivePipelines = (await _devOpsService.GetPipelines()).Count,
                RecentDeployments = (await _devOpsService.GetRecentDeployments(filter.Environment)).Count
            };
            return Ok(metrics);
        }
    }
}
