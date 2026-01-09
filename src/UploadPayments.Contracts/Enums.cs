namespace UploadPayments.Contracts;

public enum UploadStatus
{
    Queued = 0,
    Parsing = 1,
    Validating = 2,
    Completed = 3,
    Failed = 4
}

public enum JobStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

public enum JobType
{
    ParseCsv = 0,
    ValidateChunk = 1,
    FinalizeUpload = 2
}

public enum ChunkStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}

public enum RowValidationStatus
{
    Pending = 0,
    Valid = 1,
    Invalid = 2
}

public enum RuleScope
{
    Row = 0,
    Field = 1
}

public enum RuleType
{
    Required = 0,
    Regex = 1,
    AllowedValues = 2,
    DecimalRange = 3,
    DateFormat = 4
}

public enum RuleSeverity
{
    Warning = 0,
    Error = 1
}
