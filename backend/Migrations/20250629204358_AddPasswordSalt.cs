using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPLinkWebUI.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordSalt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CableDiagnosticHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PortNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    StateDescription = table.Column<string>(type: "TEXT", nullable: false),
                    Length = table.Column<int>(type: "INTEGER", nullable: false),
                    IsHealthy = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasIssue = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUntested = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDisconnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    TestTrigger = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CableDiagnosticHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PortHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PortNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    SpeedConfig = table.Column<string>(type: "TEXT", nullable: false),
                    SpeedActual = table.Column<string>(type: "TEXT", nullable: false),
                    FlowControlConfig = table.Column<string>(type: "TEXT", nullable: false),
                    FlowControlActual = table.Column<string>(type: "TEXT", nullable: false),
                    Trunk = table.Column<string>(type: "TEXT", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                    PreviousValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemInfoHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DeviceName = table.Column<string>(type: "TEXT", nullable: false),
                    MacAddress = table.Column<string>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: false),
                    SubnetMask = table.Column<string>(type: "TEXT", nullable: false),
                    Gateway = table.Column<string>(type: "TEXT", nullable: false),
                    FirmwareVersion = table.Column<string>(type: "TEXT", nullable: false),
                    HardwareVersion = table.Column<string>(type: "TEXT", nullable: false),
                    SystemUptime = table.Column<string>(type: "TEXT", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemInfoHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordSalt = table.Column<string>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VlanHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VlanId = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VlanName = table.Column<string>(type: "TEXT", nullable: false),
                    PortMembership = table.Column<string>(type: "TEXT", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                    PreviousValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VlanHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionToken = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CableDiagnosticHistory_PortNumber",
                table: "CableDiagnosticHistory",
                column: "PortNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CableDiagnosticHistory_PortNumber_Timestamp",
                table: "CableDiagnosticHistory",
                columns: new[] { "PortNumber", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_CableDiagnosticHistory_TestTrigger",
                table: "CableDiagnosticHistory",
                column: "TestTrigger");

            migrationBuilder.CreateIndex(
                name: "IX_CableDiagnosticHistory_Timestamp",
                table: "CableDiagnosticHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PortHistory_ChangeType",
                table: "PortHistory",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_PortHistory_PortNumber",
                table: "PortHistory",
                column: "PortNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PortHistory_PortNumber_Timestamp",
                table: "PortHistory",
                columns: new[] { "PortNumber", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_PortHistory_Timestamp",
                table: "PortHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SystemInfoHistory_ChangeType",
                table: "SystemInfoHistory",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_SystemInfoHistory_Timestamp",
                table: "SystemInfoHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_CreatedAt",
                table: "UserSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ExpiresAt",
                table: "UserSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_IsActive",
                table: "UserSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionToken",
                table: "UserSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VlanHistory_ChangeType",
                table: "VlanHistory",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_VlanHistory_Timestamp",
                table: "VlanHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_VlanHistory_VlanId",
                table: "VlanHistory",
                column: "VlanId");

            migrationBuilder.CreateIndex(
                name: "IX_VlanHistory_VlanId_Timestamp",
                table: "VlanHistory",
                columns: new[] { "VlanId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CableDiagnosticHistory");

            migrationBuilder.DropTable(
                name: "PortHistory");

            migrationBuilder.DropTable(
                name: "SystemInfoHistory");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "VlanHistory");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
