namespace DevVault.API.ErrorHandling;

/// <summary>
/// The stable, machine-readable <c>code</c> on every ProblemDetails response. Clients branch on
/// these, not on <c>title</c>/<c>detail</c> text, so treat changing one as a breaking change.
/// </summary>
public static class ErrorCodes
{
    public const string NotFound = "not_found";
    public const string DomainRuleViolated = "domain_rule_violated";
    public const string ValidationFailed = "validation_failed";
    public const string Unexpected = "unexpected_error";
}
