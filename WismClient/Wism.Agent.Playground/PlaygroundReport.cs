namespace Wism.Agent.Playground;

public sealed record PlaygroundReport(
    string Scenario,
    string Status,
    string Outcome,
    int Turns,
    IReadOnlyList<PlayerSummary> Players,
    IReadOnlyList<string> Events,
    string Map);

public sealed record PlayerSummary(
    string Clan,
    bool IsHuman,
    bool IsDead,
    int ArmyCount,
    int CityCount,
    int Gold);

public sealed record WorktreePlan(
    string Root,
    string BaseRef,
    IReadOnlyList<WorktreeAgentPlan> Agents,
    IReadOnlyList<string> Commands);

public sealed record WorktreeAgentPlan(
    string AgentId,
    string Branch,
    string Path);
