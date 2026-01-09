// These enums have been moved to UploadPayments.Contracts/Enums.cs
// All entity classes now use enums from the Contracts project
// This file can be deleted after verifying all using statements point to UploadPayments.Contracts

// For backward compatibility, we're just aliasing the types
global using UploadStatus = UploadPayments.Contracts.UploadStatus;
global using JobStatus = UploadPayments.Contracts.JobStatus;
global using JobType = UploadPayments.Contracts.JobType;
global using ChunkStatus = UploadPayments.Contracts.ChunkStatus;
global using RowValidationStatus = UploadPayments.Contracts.RowValidationStatus;
global using RuleScope = UploadPayments.Contracts.RuleScope;
global using RuleType = UploadPayments.Contracts.RuleType;
global using RuleSeverity = UploadPayments.Contracts.RuleSeverity;

namespace UploadPayments.Infrastructure.Persistence.Entities;

