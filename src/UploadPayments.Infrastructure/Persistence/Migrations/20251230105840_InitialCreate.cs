using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UploadPayments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

            migrationBuilder.CreateTable(
                name: "payment_uploads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    content_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    total_rows = table.Column<int>(type: "integer", nullable: true),
                    processed_rows = table.Column<int>(type: "integer", nullable: false),
                    succeeded_rows = table.Column<int>(type: "integer", nullable: false),
                    failed_rows = table.Column<int>(type: "integer", nullable: false),
                    raw_csv_bytes = table.Column<byte[]>(type: "bytea", nullable: false),
                    headers_json = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_uploads", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "validation_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    field_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    rule_type = table.Column<int>(type: "integer", nullable: false),
                    parameters_json = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    message_template = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_validation_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_upload_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chunk_index = table.Column<int>(type: "integer", nullable: false),
                    row_start = table.Column<int>(type: "integer", nullable: false),
                    row_end = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    locked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    locked_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    heartbeat_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    processed_rows = table.Column<int>(type: "integer", nullable: false),
                    succeeded_rows = table.Column<int>(type: "integer", nullable: false),
                    failed_rows = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_upload_chunks", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_upload_chunks_payment_uploads_upload_id",
                        column: x => x.upload_id,
                        principalTable: "payment_uploads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_upload_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_run_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    locked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    locked_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    heartbeat_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_upload_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_upload_jobs_payment_uploads_upload_id",
                        column: x => x.upload_id,
                        principalTable: "payment_uploads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_upload_row_errors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    field_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    is_error = table.Column<bool>(type: "boolean", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_upload_row_errors", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_upload_row_errors_payment_uploads_upload_id",
                        column: x => x.upload_id,
                        principalTable: "payment_uploads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_upload_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upload_id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_number = table.Column<int>(type: "integer", nullable: false),
                    mapped_fields_json = table.Column<string>(type: "text", nullable: false),
                    extras_json = table.Column<string>(type: "text", nullable: false),
                    raw_row_json = table.Column<string>(type: "text", nullable: false),
                    validation_status = table.Column<int>(type: "integer", nullable: false),
                    error_count = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_upload_rows", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_upload_rows_payment_uploads_upload_id",
                        column: x => x.upload_id,
                        principalTable: "payment_uploads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_upload_chunks_upload_id_chunk_index",
                table: "payment_upload_chunks",
                columns: new[] { "upload_id", "chunk_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_upload_jobs_status_next_run_at_utc",
                table: "payment_upload_jobs",
                columns: new[] { "status", "next_run_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_upload_jobs_upload_id",
                table: "payment_upload_jobs",
                column: "upload_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_upload_row_errors_upload_id_is_error_row_number",
                table: "payment_upload_row_errors",
                columns: new[] { "upload_id", "is_error", "row_number" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_upload_row_errors_upload_id_row_number",
                table: "payment_upload_row_errors",
                columns: new[] { "upload_id", "row_number" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_upload_rows_upload_id_row_number",
                table: "payment_upload_rows",
                columns: new[] { "upload_id", "row_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_uploads_token",
                table: "payment_uploads",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_upload_chunks");

            migrationBuilder.DropTable(
                name: "payment_upload_jobs");

            migrationBuilder.DropTable(
                name: "payment_upload_row_errors");

            migrationBuilder.DropTable(
                name: "payment_upload_rows");

            migrationBuilder.DropTable(
                name: "validation_rules");

            migrationBuilder.DropTable(
                name: "payment_uploads");
        }
    }
}
