using System.Collections.Generic;

namespace UploadPayments.Api.Contracts;

public sealed record UploadAcceptedDto(
    Guid UploadId,
    Guid Token,
    string StatusUrl,
    string ErrorsUrl);

public sealed record UploadStatusDto(
    Guid UploadId,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    int? TotalRows,
    int ProcessedRows,
    int SucceededRows,
    int FailedRows,
    int ChunksTotal,
    int ChunksSucceeded,
    int ChunksFailed,
    int ChunksRunning,
    int ChunksQueued,
    string? LastError);

public sealed record UploadRowErrorDto(
    int RowNumber,
    string? FieldName,
    string Code,
    string Message,
    string Severity);

public sealed record UploadErrorsPageDto(
    Guid UploadId,
    int? TotalRows,
    int Returned,
    int? NextCursorRow,
    IReadOnlyList<UploadRowErrorDto> Errors);

public sealed record PaymentInstructionDto(
    Guid UploadId,
    int RowNumber,
    IReadOnlyDictionary<string, string?> Fields,
    string ValidationStatus,
    int ErrorCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record PaymentInstructionsPageDto(
    Guid UploadId,
    int? TotalRows,
    int Returned,
    int? NextCursorRow,
    IReadOnlyList<PaymentInstructionDto> Instructions);
