using System;
using System.Collections.Generic;

namespace HelpDeskSystem.API.DTOs.Scaling
{
    public class LoadBalancerConfigurationDto
    {
        public string? Algorithm { get; set; }
        public TimeSpan? HealthCheckInterval { get; set; }
        public int? FailureThreshold { get; set; }
        public TimeSpan? RecoveryTimeout { get; set; }
    }

    public class AddServerNodeDto
    {
        public string Name { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string InstanceType { get; set; } = string.Empty;
    }

    public class AutoScalingConfigurationDto
    {
        public TimeSpan? ScaleUpCooldown { get; set; }
        public TimeSpan? ScaleDownCooldown { get; set; }
        public double? CpuThreshold { get; set; }
        public double? MemoryThreshold { get; set; }
        public TimeSpan? ResponseTimeThreshold { get; set; }
    }

    public class CreateScalingPolicyDto
    {
        public string Name { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public double ThresholdHigh { get; set; }
        public double ThresholdLow { get; set; }
        public TimeSpan ScaleOutCooldown { get; set; }
        public TimeSpan ScaleInCooldown { get; set; }
        public int MaxNodes { get; set; }
        public int MinNodes { get; set; }
    }

    public class UpdateScalingPolicyDto : CreateScalingPolicyDto { }

    public class DatabaseConfigurationDto
    {
        public int? ReadReplicas { get; set; }
        public double? ReadWriteSplitRatio { get; set; }
        public int? MaxConnections { get; set; }
        public int? MinConnections { get; set; }
        public TimeSpan? ConnectionTimeout { get; set; }
        public bool EnableSharding { get; set; }
    }

    public class CacheConfigurationDto
    {
        public string? CacheType { get; set; }
        public bool? ClusterEnabled { get; set; }
        public int? ReplicationFactor { get; set; }
        public TimeSpan? DefaultExpiration { get; set; }
        public int? MaxMemoryUsage { get; set; }
        public string? EvictionPolicy { get; set; }
    }

    public class WarmupCacheDto
    {
        public List<string> CacheKeys { get; set; } = new();
    }

    public class InvalidateCacheDto
    {
        public string Pattern { get; set; } = string.Empty;
    }

    public class BaselineDto
    {
        public TimeSpan Duration { get; set; }
    }

    public class CreateResourcePoolDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double TotalCapacity { get; set; }
    }

    public class GlobalConfigurationDto
    {
        public List<string> Regions { get; set; } = new();
        public string? PrimaryRegion { get; set; }
        public string? DisasterRecoveryRegion { get; set; }
    }

    public class HAConfigurationDto
    {
        public string? Mode { get; set; }
        public TimeSpan? FailoverTimeout { get; set; }
        public TimeSpan? HealthCheckInterval { get; set; }
    }

    public class ScalingDecisionDto
    {
        public List<string> RecommendedActions { get; set; } = new();
    }

    public class ContainerScalingDto
    {
        public string ServiceName { get; set; } = string.Empty;
        public int Replicas { get; set; }
        public string Strategy { get; set; } = "RollingUpdate";
    }

    public class CloudScalingDto
    {
        public string Provider { get; set; } = string.Empty;
        public string Action { get; set; } = "ScaleUp";
        public string ResourceType { get; set; } = "Instance";
        public int TargetCount { get; set; }
        public string InstanceType { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }

    public class PerformanceAlertDto
    {
        public string Metric { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double Threshold { get; set; }
        public string Severity { get; set; } = "Warning";
        public string Description { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
        public bool AutoRemediationEnabled { get; set; }
        public List<string> NotificationChannels { get; set; } = new();
    }

    public class PerformanceReportRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CapacityForecastDto
    {
        public TimeSpan Horizon { get; set; }
    }

    public class CapacityPlanDto
    {
        public string Name { get; set; } = string.Empty;
        public TimeSpan Horizon { get; set; }
        public Dictionary<string, double> Targets { get; set; } = new();
        public List<string> Actions { get; set; } = new();
    }

    public class IncidentDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Critical";
        public string Component { get; set; } = string.Empty;
        public List<string> AffectedResources { get; set; } = new();
    }

    public class RecoveryActionDto
    {
        public string Action { get; set; } = string.Empty;
        public string Type { get; set; } = "Auto";
    }
}
