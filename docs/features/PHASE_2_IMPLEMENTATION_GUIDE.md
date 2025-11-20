# Phase 2 Implementation Guide
## Jira Integration + TensorFlow.NET ML

**Status**: 🚧 Entities Created, Services Pending
**Completion**: 20% (Architecture + Entities)

---

## ✅ Completed (Part 1)

### 1. Architecture Design
- ✅ Created `PHASE_2_JIRA_ML_ARCHITECTURE.md` with full design
- ✅ Data model designed
- ✅ Service interfaces defined
- ✅ Integration flow documented

### 2. Entities Created
✅ **Location**: `/src/HRMS.Core/Entities/Tenant/`

```
JiraIntegration.cs          - Tenant configuration for Jira
JiraWorkLog.cs              - Synced work logs from Jira
JiraIssueAssignment.cs      - Synced issue assignments
```

✅ **Registered in**: `TenantDbContext.cs` (lines 80-83)

---

## 🚧 Remaining Work

### Part 1: Jira Integration (Remaining)

#### Step 1: Create Database Migration
```bash
cd /workspaces/HRAPP
DOTNET_ENVIRONMENT=Development \
  JWT_SECRET="temporary-dev-secret-32-chars-minimum!" \
  ConnectionStrings__DefaultConnection="Host=localhost;Database=hrms_master;Username=postgres;Password=postgres" \
  dotnet ef migrations add AddJiraIntegration \
  --project src/HRMS.Infrastructure \
  --startup-project src/HRMS.API \
  --context TenantDbContext
```

#### Step 2: Create DTOs
**Location**: `/src/HRMS.Application/DTOs/JiraIntegrationDtos/`

Create these files:
```
JiraConfigDto.cs           - Configuration request
JiraWorkLogDto.cs          - Work log response
JiraIssueDto.cs            - Issue response
JiraSyncRequestDto.cs      - Sync parameters
JiraSyncResponseDto.cs     - Sync results
JiraWebhookPayloadDto.cs   - Webhook structure
```

**Example DTOs**:

```csharp
// JiraConfigDto.cs
public class JiraConfigDto
{
    public required string JiraInstanceUrl { get; set; }
    public required string JiraUserEmail { get; set; }
    public required string JiraApiToken { get; set; }
    public Dictionary<string, Guid>? ProjectMappings { get; set; }
}

// JiraWorkLogDto.cs
public class JiraWorkLogDto
{
    public string JiraWorkLogId { get; set; }
    public string JiraIssueKey { get; set; }
    public string IssueSummary { get; set; }
    public DateTime StartedAt { get; set; }
    public decimal TimeSpentHours { get; set; }
    public string? Description { get; set; }
}

// JiraSyncResponseDto.cs
public class JiraSyncResponseDto
{
    public int WorkLogsSynced { get; set; }
    public int IssuesSynced { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime SyncedAt { get; set; }
}
```

#### Step 3: Create Interface
**Location**: `/src/HRMS.Application/Interfaces/IJiraIntegrationService.cs`

```csharp
public interface IJiraIntegrationService
{
    // Configuration
    Task<bool> ConfigureJiraAsync(JiraConfigDto config, Guid tenantId);
    Task<bool> TestConnectionAsync(Guid tenantId);
    Task<JiraIntegration?> GetConfigurationAsync(Guid tenantId);

    // Sync Operations
    Task<JiraSyncResponseDto> SyncWorkLogsAsync(
        Guid tenantId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    Task<JiraSyncResponseDto> SyncIssueAssignmentsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    // Project Mappings
    Task MapJiraProjectAsync(
        string jiraProjectKey,
        Guid hrmsProjectId,
        Guid tenantId);

    // Data Retrieval
    Task<List<JiraWorkLogDto>> GetWorkLogsForEmployeeAsync(
        Guid employeeId,
        DateTime date,
        Guid tenantId);

    Task<List<JiraIssueDto>> GetActiveIssuesAsync(
        Guid employeeId,
        Guid tenantId);

    // Webhook Handler
    Task HandleWebhookAsync(
        string webhookPayload,
        string signature,
        Guid tenantId);
}
```

#### Step 4: Implement JiraIntegrationService
**Location**: `/src/HRMS.Infrastructure/Services/JiraIntegrationService.cs`

**Key Components**:

```csharp
public class JiraIntegrationService : IJiraIntegrationService
{
    private readonly TenantDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<JiraIntegrationService> _logger;

    // 1. Configure Jira Connection
    public async Task<bool> ConfigureJiraAsync(JiraConfigDto config, Guid tenantId)
    {
        // Encrypt API token
        var encryptedToken = await _encryptionService.EncryptAsync(config.JiraApiToken);

        var integration = new JiraIntegration
        {
            TenantId = tenantId,
            JiraInstanceUrl = config.JiraInstanceUrl,
            JiraUserEmail = config.JiraUserEmail,
            JiraApiTokenEncrypted = encryptedToken,
            WebhookSecret = GenerateWebhookSecret(),
            ProjectMappingsJson = JsonSerializer.Serialize(config.ProjectMappings)
        };

        _context.JiraIntegrations.Add(integration);
        await _context.SaveChangesAsync();

        return true;
    }

    // 2. Sync Work Logs from Jira
    public async Task<JiraSyncResponseDto> SyncWorkLogsAsync(
        Guid tenantId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        var config = await GetConfigurationAsync(tenantId);
        if (config == null) throw new InvalidOperationException("Jira not configured");

        // Decrypt API token
        var apiToken = await _encryptionService.DecryptAsync(config.JiraApiTokenEncrypted);

        // Call Jira REST API
        var workLogs = await FetchWorkLogsFromJiraAsync(
            config.JiraInstanceUrl,
            config.JiraUserEmail,
            apiToken,
            fromDate,
            toDate);

        int synced = 0;
        var errors = new List<string>();

        foreach (var jiraLog in workLogs)
        {
            try
            {
                // Map Jira project to HRMS project
                var projectId = await MapJiraProjectToHrmsAsync(
                    jiraLog.ProjectKey,
                    config.ProjectMappingsJson);

                if (!projectId.HasValue)
                {
                    errors.Add($"No mapping for Jira project: {jiraLog.ProjectKey}");
                    continue;
                }

                // Find employee by Jira username
                var employeeId = await FindEmployeeByJiraUsernameAsync(
                    jiraLog.AuthorUsername,
                    tenantId);

                if (!employeeId.HasValue)
                {
                    errors.Add($"No employee found for Jira user: {jiraLog.AuthorUsername}");
                    continue;
                }

                // Check if already synced
                var exists = await _context.JiraWorkLogs
                    .AnyAsync(w => w.JiraWorkLogId == jiraLog.Id && w.TenantId == tenantId);

                if (exists) continue;

                // Create work log
                var workLog = new JiraWorkLog
                {
                    TenantId = tenantId,
                    JiraWorkLogId = jiraLog.Id,
                    EmployeeId = employeeId.Value,
                    ProjectId = projectId.Value,
                    JiraIssueKey = jiraLog.IssueKey,
                    JiraIssueSummary = jiraLog.IssueSummary,
                    JiraIssueType = jiraLog.IssueType,
                    StartedAt = jiraLog.StartedAt,
                    TimeSpentHours = jiraLog.TimeSpentSeconds / 3600m,
                    Description = jiraLog.Comment,
                    JiraAuthorUsername = jiraLog.AuthorUsername,
                    SyncedAt = DateTime.UtcNow
                };

                _context.JiraWorkLogs.Add(workLog);
                synced++;
            }
            catch (Exception ex)
            {
                errors.Add($"Error syncing work log {jiraLog.Id}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Update sync timestamp
        config.LastSyncAt = DateTime.UtcNow;
        config.TotalWorkLogsSynced += synced;
        await _context.SaveChangesAsync(cancellationToken);

        return new JiraSyncResponseDto
        {
            WorkLogsSynced = synced,
            Errors = errors,
            SyncedAt = DateTime.UtcNow
        };
    }

    // 3. Fetch from Jira REST API
    private async Task<List<JiraWorkLogResponse>> FetchWorkLogsFromJiraAsync(
        string jiraUrl,
        string email,
        string apiToken,
        DateTime fromDate,
        DateTime toDate)
    {
        // Create Basic Auth header
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{email}:{apiToken}"));

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);

        // Jira JQL query: worklogDate >= startDate AND worklogDate <= endDate
        var jql = $"worklogDate >= '{fromDate:yyyy-MM-dd}' AND worklogDate <= '{toDate:yyyy-MM-dd}'";
        var url = $"{jiraUrl}/rest/api/3/search?jql={Uri.EscapeDataString(jql)}&fields=worklog,summary,issuetype&maxResults=1000";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jiraResponse = JsonSerializer.Deserialize<JiraSearchResponse>(content);

        // Extract work logs from issues
        var workLogs = new List<JiraWorkLogResponse>();
        foreach (var issue in jiraResponse.Issues)
        {
            foreach (var worklog in issue.Fields.Worklog.Worklogs)
            {
                workLogs.Add(new JiraWorkLogResponse
                {
                    Id = worklog.Id,
                    IssueKey = issue.Key,
                    IssueSummary = issue.Fields.Summary,
                    IssueType = issue.Fields.IssueType.Name,
                    ProjectKey = issue.Key.Split('-')[0],
                    StartedAt = DateTime.Parse(worklog.Started),
                    TimeSpentSeconds = worklog.TimeSpentSeconds,
                    Comment = worklog.Comment,
                    AuthorUsername = worklog.Author.AccountId
                });
            }
        }

        return workLogs;
    }
}
```

#### Step 5: Extend ProjectAllocationEngine with Jira

**Location**: `/src/HRMS.Infrastructure/Services/ProjectAllocationEngine.cs`

Add new method:

```csharp
private async Task<List<AllocationSuggestion>> GenerateFromJiraAsync(
    Guid employeeId,
    DateTime date,
    Guid tenantId,
    CancellationToken cancellationToken)
{
    var suggestions = new List<AllocationSuggestion>();

    // 1. Check Jira work logs for this date
    var workLogs = await _context.JiraWorkLogs
        .Include(w => w.Project)
        .Where(w => w.EmployeeId == employeeId
            && w.TenantId == tenantId
            && w.StartedAt.Date == date.Date
            && !w.WasConverted)
        .ToListAsync(cancellationToken);

    foreach (var log in workLogs)
    {
        suggestions.Add(new AllocationSuggestion
        {
            ProjectId = log.ProjectId,
            ProjectCode = log.Project?.ProjectCode ?? "",
            ProjectName = log.Project?.ProjectName ?? "",
            SuggestedHours = log.TimeSpentHours,
            ConfidenceScore = 95, // Very high - employee logged in Jira
            Source = "JiraWorkLog",
            Reason = $"You logged {log.TimeSpentHours:F1}h on {log.JiraIssueKey} in Jira",
            Evidence = new Dictionary<string, object>
            {
                { "jira_issue_key", log.JiraIssueKey },
                { "jira_summary", log.JiraIssueSummary ?? "" },
                { "jira_work_log_id", log.JiraWorkLogId }
            }
        });
    }

    // 2. Check active Jira issues (if no work logs)
    if (!suggestions.Any())
    {
        var activeIssues = await _context.JiraIssueAssignments
            .Include(i => i.Project)
            .Where(i => i.EmployeeId == employeeId
                && i.TenantId == tenantId
                && i.IsActive
                && i.Status == "In Progress")
            .ToListAsync(cancellationToken);

        foreach (var issue in activeIssues)
        {
            // Estimate hours for today (remaining/days until due)
            var estimatedHours = EstimateHoursForToday(issue);

            suggestions.Add(new AllocationSuggestion
            {
                ProjectId = issue.ProjectId,
                ProjectCode = issue.Project?.ProjectCode ?? "",
                ProjectName = issue.Project?.ProjectName ?? "",
                SuggestedHours = estimatedHours,
                ConfidenceScore = 75,
                Source = "JiraIssue",
                Reason = $"You have active issue: {issue.JiraIssueKey} ({issue.Status})",
                Evidence = new Dictionary<string, object>
                {
                    { "jira_issue_key", issue.JiraIssueKey },
                    { "status", issue.Status },
                    { "remaining_hours", issue.RemainingHours ?? 0 }
                }
            });
        }
    }

    return suggestions;
}

private decimal EstimateHoursForToday(JiraIssueAssignment issue)
{
    if (!issue.RemainingHours.HasValue) return 2.0m; // Default

    // If due date is set, distribute remaining hours
    if (issue.DueDate.HasValue)
    {
        var daysRemaining = (issue.DueDate.Value - DateTime.UtcNow).TotalDays;
        if (daysRemaining <= 0) return Math.Min(issue.RemainingHours.Value, 4.0m);
        if (daysRemaining < 1) return issue.RemainingHours.Value;

        return Math.Min(issue.RemainingHours.Value / (decimal)daysRemaining, 8.0m);
    }

    // Default: 20% of remaining hours per day
    return Math.Min(issue.RemainingHours.Value * 0.2m, 4.0m);
}
```

Update `GenerateSuggestionsAsync` to include Jira:

```csharp
// In GenerateSuggestionsAsync method, add:
var jiraSuggestions = await GenerateFromJiraAsync(
    employeeId, date, tenantId, cancellationToken);
suggestions.AddRange(jiraSuggestions);
```

#### Step 6: Create API Controller
**Location**: `/src/HRMS.API/Controllers/JiraIntegrationController.cs`

```csharp
[ApiController]
[Route("api/jira-integration")]
[Authorize]
public class JiraIntegrationController : ControllerBase
{
    private readonly IJiraIntegrationService _jiraService;

    [HttpPost("configure")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Configure([FromBody] JiraConfigDto config)
    {
        var tenantId = GetTenantId();
        await _jiraService.ConfigureJiraAsync(config, tenantId);
        return Ok(new { message = "Jira configured successfully" });
    }

    [HttpPost("sync/worklogs")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> SyncWorkLogs(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var tenantId = GetTenantId();
        fromDate ??= DateTime.UtcNow.AddDays(-7);
        toDate ??= DateTime.UtcNow;

        var result = await _jiraService.SyncWorkLogsAsync(
            tenantId, fromDate.Value, toDate.Value);

        return Ok(result);
    }

    [HttpPost("webhooks")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(
        [FromBody] string payload,
        [FromHeader(Name = "X-Hub-Signature-256")] string signature)
    {
        var tenantId = GetTenantIdFromRequest(); // From query param
        await _jiraService.HandleWebhookAsync(payload, signature, tenantId);
        return Ok();
    }
}
```

#### Step 7: Register Service in DI
**Location**: `/src/HRMS.API/Program.cs`

Add after line 412:

```csharp
// Jira Integration (Phase 2)
builder.Services.AddScoped<IJiraIntegrationService, JiraIntegrationService>();
builder.Services.AddHttpClient<IJiraIntegrationService, JiraIntegrationService>();
Log.Information("Jira integration service registered");
```

---

### Part 2: TensorFlow.NET ML Integration

#### Step 1: Add TensorFlow.NET Packages

Edit `/src/HRMS.Infrastructure/HRMS.Infrastructure.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="TensorFlow.NET" Version="0.150.0" />
    <PackageReference Include="SciSharp.TensorFlow.Redist" Version="2.16.0" />
    <PackageReference Include="NumSharp" Version="0.30.0" />
</ItemGroup>
```

Then restore:
```bash
dotnet restore
```

#### Step 2: Create ML Model Structure

**Location**: `/src/HRMS.Infrastructure/ML/TimesheetMLModel.cs`

```csharp
using Tensorflow;
using NumSharp;
using static Tensorflow.Binding;

public class TimesheetMLModel : IDisposable
{
    private Graph _graph;
    private Session _session;
    private Tensor _inputTensor;
    private Tensor _outputHours;
    private Tensor _outputConfidence;

    public TimesheetMLModel()
    {
        BuildModel();
    }

    private void BuildModel()
    {
        _graph = tf.Graph().as_default();

        // Input layer (15 features + 2 embeddings)
        _inputTensor = tf.placeholder(tf.float32, shape: (-1, 271), name: "input");

        // Hidden layers
        var dense1 = tf.layers.dense(_inputTensor, 256, activation: tf.nn.relu_fn, name: "dense1");
        var dropout1 = tf.nn.dropout(dense1, rate: 0.3f, name: "dropout1");

        var dense2 = tf.layers.dense(dropout1, 128, activation: tf.nn.relu_fn, name: "dense2");
        var dropout2 = tf.nn.dropout(dense2, rate: 0.2f, name: "dropout2");

        var dense3 = tf.layers.dense(dropout2, 64, activation: tf.nn.relu_fn, name: "dense3");

        // Output layers
        _outputHours = tf.layers.dense(dense3, 1, name: "output_hours");
        _outputConfidence = tf.layers.dense(dense3, 1, activation: tf.nn.sigmoid_fn, name: "output_confidence");

        _session = tf.Session(_graph);
        _session.run(tf.global_variables_initializer());
    }

    public (float predictedHours, float confidence) Predict(float[] features)
    {
        var inputArray = np.array(features).reshape(1, 271);

        var results = _session.run(
            new[] { _outputHours, _outputConfidence },
            new FeedItem(_inputTensor, inputArray));

        float hours = results[0].ToArray<float>()[0];
        float confidence = results[1].ToArray<float>()[0];

        return (hours, confidence);
    }

    public void Dispose()
    {
        _session?.Dispose();
        _graph?.Dispose();
    }
}
```

#### Step 3: Create Feature Engineering Service

**Location**: `/src/HRMS.Infrastructure/ML/MLFeatureBuilder.cs`

```csharp
public class MLFeatureBuilder
{
    private readonly TenantDbContext _context;

    public async Task<float[]> BuildFeatureVectorAsync(
        Guid employeeId,
        Guid projectId,
        DateTime date,
        Guid tenantId)
    {
        var features = new List<float>();

        // Employee features (5)
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        features.Add(GetEmployeeIdEmbedding(employeeId)); // 128-dim (simplified to hash)
        features.Add((float)(DateTime.UtcNow - employee.HireDate).TotalDays); // Tenure
        features.Add((float)employee.DepartmentId.GetHashCode() % 1000); // Department embedding
        features.Add(await GetEmployeeAvgHoursAsync(employeeId, tenantId)); // Avg hours/day
        features.Add(await GetEmployeeProjectCountAsync(employeeId, tenantId)); // Project count

        // Project features (5)
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId);

        features.Add(GetProjectIdEmbedding(projectId)); // 128-dim
        features.Add((float)(DateTime.UtcNow - project.CreatedAt).TotalDays); // Project age
        features.Add(await GetProjectMemberCountAsync(projectId, tenantId)); // Member count
        features.Add(project.IsBillable ? 1.0f : 0.0f); // Billable flag
        features.Add(await GetProjectBudgetUtilizationAsync(projectId, tenantId)); // Budget %

        // Temporal features (3)
        features.Add((float)date.DayOfWeek); // Day of week
        features.Add(await IsHolidayAsync(date, tenantId) ? 1.0f : 0.0f); // Holiday flag
        features.Add(await GetDaysSinceLastWorkedAsync(employeeId, projectId, date, tenantId)); // Recency

        // Historical features (2)
        features.Add(await GetWorkFrequencyAsync(employeeId, projectId, 30, tenantId)); // Count last 30d
        features.Add(await GetAvgHoursAsync(employeeId, projectId, 30, tenantId)); // Avg hours last 30d

        return features.ToArray();
    }

    // Helper methods for feature extraction...
}
```

#### Step 4: Create ML Prediction Service

**Location**: `/src/HRMS.Infrastructure/ML/MLPredictionService.cs`

```csharp
public class MLPredictionService : IMLPredictionService
{
    private readonly MLFeatureBuilder _featureBuilder;
    private readonly TimesheetMLModel _model;

    public async Task<List<AllocationSuggestion>> GeneratePredictionsAsync(
        Guid employeeId,
        DateTime date,
        Guid tenantId,
        List<Guid> candidateProjects)
    {
        var suggestions = new List<AllocationSuggestion>();

        foreach (var projectId in candidateProjects)
        {
            // Build feature vector
            var features = await _featureBuilder.BuildFeatureVectorAsync(
                employeeId, projectId, date, tenantId);

            // Run ML inference
            var (predictedHours, confidence) = _model.Predict(features);

            // Only suggest if confidence > 60%
            if (confidence >= 0.6f)
            {
                var project = await GetProjectAsync(projectId);

                suggestions.Add(new AllocationSuggestion
                {
                    ProjectId = projectId,
                    ProjectCode = project.ProjectCode,
                    ProjectName = project.ProjectName,
                    SuggestedHours = (decimal)predictedHours,
                    ConfidenceScore = (int)(confidence * 100),
                    Source = "MLModel",
                    Reason = $"ML model predicts {predictedHours:F1}h on this project",
                    Evidence = new Dictionary<string, object>
                    {
                        { "model_version", "v1.0" },
                        { "confidence", confidence }
                    }
                });
            }
        }

        return suggestions.OrderByDescending(s => s.ConfidenceScore).ToList();
    }
}
```

#### Step 5: Integrate ML into ProjectAllocationEngine

In `ProjectAllocationEngine.GenerateSuggestionsAsync`:

```csharp
// Add ML predictions (20% weight)
if (_mlPredictionService != null) // Feature flag
{
    var candidateProjects = await GetCandidateProjectsAsync(employeeId, tenantId);
    var mlSuggestions = await _mlPredictionService.GeneratePredictionsAsync(
        employeeId, date, tenantId, candidateProjects, cancellationToken);

    suggestions.AddRange(mlSuggestions);
}
```

---

## 📝 Testing Checklist

### Jira Integration
- [ ] Configure Jira connection
- [ ] Test Jira API connectivity
- [ ] Sync work logs (7 days)
- [ ] Verify work logs in database
- [ ] Map Jira projects to HRMS projects
- [ ] Generate suggestions with Jira data
- [ ] Test webhook handling
- [ ] Verify employee can see Jira-based suggestions

### ML Integration
- [ ] Install TensorFlow.NET packages
- [ ] Build feature vectors
- [ ] Train model on historical data (90 days)
- [ ] Evaluate model accuracy
- [ ] Test predictions for single employee
- [ ] Integrate predictions into allocation engine
- [ ] Test ensemble (Rules + ML)
- [ ] Monitor prediction latency (<100ms)

### End-to-End
- [ ] Generate timesheet with Jira + ML suggestions
- [ ] Verify confidence scores are higher
- [ ] Test suggestion acceptance workflow
- [ ] Verify feedback loop updates patterns
- [ ] Test with 10,000 employees (load test)
- [ ] Measure accuracy improvement (target: 90%+)

---

## 🎯 Success Metrics

| Metric | Phase 1 (Baseline) | Phase 2 (Target) |
|--------|-------------------|------------------|
| Accuracy | 75-85% | 90-95% |
| Employee Time | 5-8 min/day | 1-2 min/day |
| Auto-Approval | 60-70% | 85-90% |
| Confidence (Avg) | 70% | 85% |

---

## 🚀 Deployment

### Feature Flags
```json
{
  "JiraIntegration": {
    "Enabled": false,  // Enable per tenant
    "AutoSync": true,
    "SyncIntervalMinutes": 60
  },
  "MLPredictions": {
    "Enabled": false,  // Enable after training
    "MinConfidenceThreshold": 0.6,
    "MaxPredictionsPerEmployee": 5
  }
}
```

### Rollout Plan
1. **Week 1**: Enable Jira for 1 pilot tenant
2. **Week 2**: Train ML model on pilot tenant data
3. **Week 3**: Enable ML for pilot tenant, monitor accuracy
4. **Week 4**: If accuracy > 85%, roll out to all tenants

---

## 📚 Additional Resources

- [TensorFlow.NET Documentation](https://github.com/SciSharp/TensorFlow.NET)
- [Jira REST API Reference](https://developer.atlassian.com/cloud/jira/platform/rest/v3/)
- [Phase 2 Architecture](./PHASE_2_JIRA_ML_ARCHITECTURE.md)

---

**Status**: Ready for implementation
**Estimated Time**: 10-12 hours for Jira + ML
**Priority**: High - Delivers 90%+ accuracy target
