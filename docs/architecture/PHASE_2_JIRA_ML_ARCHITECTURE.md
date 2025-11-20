# Phase 2: Jira Integration & TensorFlow.NET ML
## Advanced Intelligence for Timesheet Allocation

**Date**: 2025-11-20
**Status**: 🚧 In Progress
**Phase**: 2 of 3

---

## 🎯 Objectives

### 1. Jira Integration
**Goal**: Use Jira work logs and issue assignments to improve allocation suggestions

**Data Sources**:
- Active issues assigned to employee
- Work logs entered in Jira
- Issue time estimates
- Sprint commitments
- Project-issue mappings

**Expected Improvement**: +15% confidence boost, 90%+ acceptance rate

### 2. TensorFlow.NET ML
**Goal**: Replace rules-based allocation with deep learning model

**Capabilities**:
- Multi-feature pattern recognition
- Temporal analysis (time series)
- Employee behavior clustering
- Anomaly detection (statistical outliers)
- Continuous learning from feedback

**Expected Improvement**: 95%+ acceptance rate, <2min employee time

---

## 📊 Architecture

### Current State (Phase 1)
```
ProjectAllocationEngine (Rules-Based)
├─ Work Pattern Analysis (70%)
├─ Project Membership (30%)
└─ Future: Calendar, Git, Jira (0%)

Confidence Calculation: Manual weights and thresholds
```

### Target State (Phase 2)
```
ProjectAllocationEngine (Hybrid: Rules + ML)
├─ Work Pattern Analysis (40%)
├─ Project Membership (20%)
├─ Jira Work Logs (20%)        ⭐ NEW
├─ TensorFlow.NET Model (20%)  ⭐ NEW
└─ Future: Calendar, Git (0%)

Confidence Calculation: ML-predicted probabilities
```

---

## 🔗 Jira Integration Design

### A. Data Model

#### 1. JiraIntegration (Tenant Config)
```csharp
public class JiraIntegration
{
    Guid TenantId
    string JiraInstanceUrl          // "https://company.atlassian.net"
    string JiraApiToken             // Encrypted OAuth token
    bool IsEnabled
    DateTime? LastSyncAt
    string WebhookSecret            // For webhook validation

    // Mappings
    Dictionary<string, Guid> ProjectMappings  // Jira Project → HRMS Project
}
```

#### 2. JiraWorkLog (Sync Data)
```csharp
public class JiraWorkLog
{
    string JiraWorkLogId           // Jira's work log ID
    Guid EmployeeId
    Guid ProjectId                 // Mapped HRMS project
    string JiraIssueKey            // "PROJ-123"
    string JiraIssueSummary
    DateTime StartedAt
    decimal TimeSpentHours
    string Description
    DateTime SyncedAt
}
```

#### 3. JiraIssueAssignment
```csharp
public class JiraIssueAssignment
{
    string JiraIssueKey
    Guid EmployeeId
    Guid ProjectId
    string IssueType               // "Story", "Bug", "Task"
    string Status                  // "In Progress", "Done"
    decimal? EstimateHours
    decimal? RemainingHours
    DateTime? DueDate
    bool IsActive
}
```

### B. Services

#### 1. JiraIntegrationService
```csharp
public interface IJiraIntegrationService
{
    // Configuration
    Task<bool> ConfigureJiraAsync(JiraConfigDto config);
    Task<bool> TestConnectionAsync(Guid tenantId);

    // Sync
    Task SyncWorkLogsAsync(Guid tenantId, DateTime from, DateTime to);
    Task SyncIssueAssignmentsAsync(Guid tenantId);

    // Mappings
    Task MapJiraProjectToHrmsAsync(string jiraProjectKey, Guid hrmsProjectId);

    // Data retrieval
    Task<List<JiraWorkLog>> GetWorkLogsForEmployeeAsync(Guid employeeId, DateTime date);
    Task<List<JiraIssueAssignment>> GetActiveIssuesAsync(Guid employeeId);
}
```

#### 2. JiraWebhookHandler
```csharp
public interface IJiraWebhookHandler
{
    Task HandleWorkLogCreatedAsync(JiraWebhookPayload payload);
    Task HandleWorkLogUpdatedAsync(JiraWebhookPayload payload);
    Task HandleIssueAssignedAsync(JiraWebhookPayload payload);
    Task HandleIssueUpdatedAsync(JiraWebhookPayload payload);
}
```

### C. Integration Flow

```
┌─────────────────────────────────────────────────────┐
│ JIRA                                                │
│  - User logs work: "PROJ-123 - 3 hours"            │
│  - Issue assigned: "PROJ-124 → John Doe"           │
└────────────────┬────────────────────────────────────┘
                 ↓ (Webhook or Sync)
┌─────────────────────────────────────────────────────┐
│ JiraWebhookHandler / JiraIntegrationService         │
│  - Validate webhook signature                       │
│  - Map Jira Project → HRMS Project                  │
│  - Store in JiraWorkLog / JiraIssueAssignment       │
└────────────────┬────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────────────────┐
│ ProjectAllocationEngine                             │
│  - Query JiraWorkLog for employee + date            │
│  - If work log exists → High confidence suggestion  │
│  - If issue assigned → Medium confidence            │
│  - Combine with patterns for final score            │
└─────────────────────────────────────────────────────┘
```

### D. Allocation Strategy with Jira

```csharp
// NEW: Jira Work Log Strategy (20% weight)
var jiraWorkLogs = await GetJiraWorkLogsAsync(employeeId, date);
foreach (var log in jiraWorkLogs)
{
    suggestions.Add(new AllocationSuggestion
    {
        ProjectId = log.ProjectId,
        SuggestedHours = log.TimeSpentHours,
        ConfidenceScore = 95,  // Very high - employee self-reported in Jira
        Source = "JiraWorkLog",
        Reason = $"You logged {log.TimeSpentHours}h on {log.JiraIssueKey} in Jira",
        Evidence = new {
            jira_issue_key = log.JiraIssueKey,
            jira_summary = log.JiraIssueSummary
        }
    });
}

// NEW: Jira Active Issues Strategy (20% weight)
var activeIssues = await GetActiveJiraIssuesAsync(employeeId);
foreach (var issue in activeIssues)
{
    // Only suggest if employee worked on this issue recently
    if (HasRecentActivity(issue))
    {
        suggestions.Add(new AllocationSuggestion
        {
            ProjectId = issue.ProjectId,
            SuggestedHours = EstimateHoursForToday(issue),
            ConfidenceScore = 75,
            Source = "JiraIssue",
            Reason = $"You have active issue: {issue.JiraIssueKey}",
            Evidence = new {
                jira_issue_key = issue.JiraIssueKey,
                status = issue.Status,
                remaining_hours = issue.RemainingHours
            }
        });
    }
}
```

---

## 🧠 TensorFlow.NET ML Design

### A. Model Architecture

#### Input Features (15 features)
```
Employee Features (5):
├─ employee_id_embedding (128-dim)
├─ employee_tenure_days
├─ employee_department_id
├─ employee_avg_hours_per_day
└─ employee_project_count

Project Features (5):
├─ project_id_embedding (128-dim)
├─ project_age_days
├─ project_member_count
├─ project_is_billable
└─ project_budget_utilization

Temporal Features (3):
├─ day_of_week (one-hot, 7-dim)
├─ is_holiday
└─ days_since_last_worked_on_project

Historical Features (2):
├─ times_worked_on_project_last_30d
└─ avg_hours_on_project_last_30d
```

#### Model Structure
```
Input Layer (15 features)
    ↓
Embedding Layers
├─ Employee ID → 128-dim dense vector
└─ Project ID → 128-dim dense vector
    ↓
Feature Concatenation (15 + 128 + 128 = 271 features)
    ↓
Dense Layer (256 neurons, ReLU)
    ↓
Dropout (0.3)
    ↓
Dense Layer (128 neurons, ReLU)
    ↓
Dropout (0.2)
    ↓
Dense Layer (64 neurons, ReLU)
    ↓
Output Layer (2 neurons)
├─ hours_prediction (regression, linear activation)
└─ confidence_score (classification, sigmoid activation)
```

#### Loss Function
```csharp
// Multi-task loss: Regression + Classification
loss = (hours_loss * 0.7) + (confidence_loss * 0.3)

hours_loss = MeanSquaredError(predicted_hours, actual_hours)
confidence_loss = BinaryCrossentropy(predicted_confidence, was_accepted)
```

### B. Training Pipeline

#### 1. Data Preparation
```csharp
public class MLTrainingDataBuilder
{
    // Build training dataset from historical allocations
    public async Task<TrainingDataset> BuildDatasetAsync(
        Guid tenantId,
        DateTime startDate,
        DateTime endDate)
    {
        var allocations = await GetHistoricalAllocationsAsync(tenantId, startDate, endDate);

        var features = new List<float[]>();
        var labels = new List<(float hours, float wasAccepted)>();

        foreach (var allocation in allocations)
        {
            var feature = await BuildFeatureVectorAsync(allocation);
            features.Add(feature);

            labels.Add((
                hours: (float)allocation.Hours,
                wasAccepted: allocation.SuggestionAccepted == true ? 1.0f : 0.0f
            ));
        }

        return new TrainingDataset(features, labels);
    }
}
```

#### 2. Model Training
```csharp
public class TimesheetMLModel
{
    private Session _session;
    private Graph _graph;

    public async Task TrainAsync(TrainingDataset dataset, int epochs = 100)
    {
        // Split data: 80% train, 20% validation
        var (trainData, valData) = dataset.Split(0.8);

        // Training loop
        for (int epoch = 0; epoch < epochs; epoch++)
        {
            float totalLoss = 0;

            // Mini-batch gradient descent
            foreach (var batch in trainData.GetBatches(batchSize: 32))
            {
                var loss = TrainStep(batch);
                totalLoss += loss;
            }

            // Validation
            if (epoch % 10 == 0)
            {
                var valLoss = Evaluate(valData);
                Console.WriteLine($"Epoch {epoch}: Train Loss={totalLoss}, Val Loss={valLoss}");
            }
        }

        // Save model
        await SaveModelAsync("timesheet_model_v1.pb");
    }
}
```

#### 3. Prediction Service
```csharp
public class MLPredictionService
{
    public async Task<MLAllocationPrediction> PredictAsync(
        Guid employeeId,
        Guid projectId,
        DateTime date)
    {
        // Build feature vector
        var features = await BuildFeatureVectorAsync(employeeId, projectId, date);

        // Run inference
        var (predictedHours, confidence) = _model.Predict(features);

        return new MLAllocationPrediction
        {
            ProjectId = projectId,
            PredictedHours = predictedHours,
            ConfidenceScore = (int)(confidence * 100),
            ModelVersion = "v1.0"
        };
    }
}
```

### C. Integration with Allocation Engine

#### Hybrid Strategy
```csharp
public async Task<List<AllocationSuggestion>> GenerateSuggestionsAsync(
    Guid employeeId,
    DateTime date,
    decimal totalHoursAvailable,
    Guid tenantId)
{
    var suggestions = new List<AllocationSuggestion>();

    // 1. Rules-based strategies (60% weight)
    var patternSuggestions = await GenerateFromWorkPatternsAsync(...);      // 40%
    var membershipSuggestions = await GenerateFromProjectMembershipAsync(...); // 20%

    // 2. Jira integration (20% weight) ⭐ NEW
    var jiraSuggestions = await GenerateFromJiraAsync(employeeId, date, tenantId);

    // 3. ML predictions (20% weight) ⭐ NEW
    var mlSuggestions = await GenerateFromMLModelAsync(employeeId, date, tenantId);

    // Merge all suggestions
    suggestions.AddRange(patternSuggestions);
    suggestions.AddRange(membershipSuggestions);
    suggestions.AddRange(jiraSuggestions);
    suggestions.AddRange(mlSuggestions);

    // Ensemble: Boost confidence if multiple sources agree
    suggestions = ApplyEnsembleBoost(suggestions);

    // Normalize and sort
    suggestions = NormalizeHoursAllocation(suggestions, totalHoursAvailable);
    suggestions = suggestions.OrderByDescending(s => s.ConfidenceScore).ToList();

    return suggestions;
}

private List<AllocationSuggestion> ApplyEnsembleBoost(List<AllocationSuggestion> suggestions)
{
    // Group by project
    var grouped = suggestions.GroupBy(s => s.ProjectId);

    foreach (var group in grouped)
    {
        if (group.Count() >= 2)
        {
            // Multiple sources agree → boost confidence
            foreach (var suggestion in group)
            {
                suggestion.ConfidenceScore = Math.Min(100, suggestion.ConfidenceScore + 10);
                suggestion.Source = "Ensemble";
            }
        }
    }

    return suggestions;
}
```

### D. Model Retraining

```csharp
public class MLModelRetrainingService
{
    // Retrain model weekly with new data
    public async Task RetrainModelAsync(Guid tenantId)
    {
        // Get last 90 days of data
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-90);

        // Build training dataset
        var dataset = await _dataBuilder.BuildDatasetAsync(tenantId, startDate, endDate);

        // Train new model
        var model = new TimesheetMLModel();
        await model.TrainAsync(dataset, epochs: 200);

        // Evaluate on test set
        var metrics = await EvaluateModelAsync(model);

        // If better than current model, deploy
        if (metrics.Accuracy > _currentModelAccuracy)
        {
            await DeployModelAsync(model, tenantId);

            _logger.LogInformation(
                "New model deployed for tenant {TenantId}: Accuracy={Accuracy}%",
                tenantId, metrics.Accuracy * 100);
        }
    }
}
```

---

## 📊 Expected Performance

### Phase 1 (Current)
- **Accuracy**: 75-85%
- **Employee Time**: 5-8 min/day
- **Auto-approval**: 60-70%

### Phase 2 (Target)
- **Accuracy**: 90-95% (with Jira + ML)
- **Employee Time**: 1-2 min/day
- **Auto-approval**: 85-90%

### Breakdown by Source
| Source | Weight | Confidence | Notes |
|--------|--------|------------|-------|
| Jira Work Logs | 20% | 95% | Self-reported by employee |
| ML Model | 20% | 85-95% | Learns complex patterns |
| Work Patterns | 40% | 70-90% | Historical behavior |
| Project Membership | 20% | 60-75% | Active assignments |

---

## 🔒 Security Considerations

### Jira Integration
- **OAuth 2.0**: Secure token storage (encrypted in database)
- **Webhook Validation**: HMAC signature verification
- **Rate Limiting**: Respect Jira API limits
- **Scopes**: Minimal permissions (read work logs, read issues)

### ML Model
- **Model Storage**: Encrypted at rest
- **Inference Privacy**: No PII in model features (use embeddings)
- **Tenant Isolation**: Separate model per tenant (or tenant embedding)
- **Audit Trail**: Log all predictions in TimesheetIntelligenceEvents

---

## 📝 Implementation Plan

### Part 1: Jira Integration (4-6 hours)
1. ✅ Create entities (JiraIntegration, JiraWorkLog, JiraIssueAssignment)
2. ✅ Create migration
3. ✅ Implement JiraIntegrationService
4. ✅ Implement JiraWebhookHandler
5. ✅ Extend ProjectAllocationEngine
6. ✅ Create API endpoints for configuration
7. ✅ Test with mock Jira data

### Part 2: TensorFlow.NET ML (6-8 hours)
1. ✅ Add TensorFlow.NET packages
2. ✅ Design model architecture
3. ✅ Implement training pipeline
4. ✅ Create feature engineering service
5. ✅ Implement prediction service
6. ✅ Integrate with ProjectAllocationEngine
7. ✅ Create retraining background job
8. ✅ Test with historical data

---

## 🚀 Deployment Strategy

### Rollout
1. **Pilot (Week 1)**: Enable for 1 tenant, monitor accuracy
2. **Beta (Week 2-3)**: Enable for 10% of tenants
3. **General Availability (Week 4)**: Enable for all tenants

### Monitoring
- Track suggestion acceptance rates per source
- Monitor ML model accuracy metrics
- Alert on Jira sync failures
- Track inference latency (<100ms target)

### Rollback Plan
- Feature flags for Jira integration and ML
- Fallback to rules-based engine if accuracy drops
- Disable per-tenant if issues detected

---

**Next**: Start implementation with Jira entities and services
