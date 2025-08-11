using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TPLinkWebUI.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedHistoryModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to PortHistory table
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "PortHistory",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "PortHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DowntimeDuration",
                table: "PortHistory",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpTime",
                table: "PortHistory",
                type: "TEXT",
                nullable: true);

            // Create SwitchConnectivityHistory table
            migrationBuilder.CreateTable(
                name: "SwitchConnectivityHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsReachable = table.Column<bool>(type: "INTEGER", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    DowntimeDuration = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwitchConnectivityHistory", x => x.Id);
                });

            // Create PortStatisticsHistory table
            migrationBuilder.CreateTable(
                name: "PortStatisticsHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PortNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TxGoodPkt = table.Column<long>(type: "INTEGER", nullable: false),
                    TxBadPkt = table.Column<long>(type: "INTEGER", nullable: false),
                    RxGoodPkt = table.Column<long>(type: "INTEGER", nullable: false),
                    RxBadPkt = table.Column<long>(type: "INTEGER", nullable: false),
                    TxBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    RxBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortStatisticsHistory", x => x.Id);
                });

            // Create UserActivityHistory table
            migrationBuilder.CreateTable(
                name: "UserActivityHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    TargetEntity = table.Column<string>(type: "TEXT", nullable: true),
                    PreviousValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: true),
                    IsSuccess = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityHistory", x => x.Id);
                });

            // Create indexes for SwitchConnectivityHistory
            migrationBuilder.CreateIndex(
                name: "IX_SwitchConnectivityHistory_Timestamp",
                table: "SwitchConnectivityHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SwitchConnectivityHistory_IsReachable",
                table: "SwitchConnectivityHistory",
                column: "IsReachable");

            migrationBuilder.CreateIndex(
                name: "IX_SwitchConnectivityHistory_IpAddress",
                table: "SwitchConnectivityHistory",
                column: "IpAddress");

            // Create indexes for PortStatisticsHistory
            migrationBuilder.CreateIndex(
                name: "IX_PortStatisticsHistory_PortNumber",
                table: "PortStatisticsHistory",
                column: "PortNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PortStatisticsHistory_Timestamp",
                table: "PortStatisticsHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PortStatisticsHistory_ChangeType",
                table: "PortStatisticsHistory",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_PortStatisticsHistory_PortNumber_Timestamp",
                table: "PortStatisticsHistory",
                columns: new[] { "PortNumber", "Timestamp" });

            // Create indexes for UserActivityHistory
            migrationBuilder.CreateIndex(
                name: "IX_UserActivityHistory_UserId",
                table: "UserActivityHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityHistory_Username",
                table: "UserActivityHistory",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityHistory_Timestamp",
                table: "UserActivityHistory",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityHistory_ActionType",
                table: "UserActivityHistory",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityHistory_IsSuccess",
                table: "UserActivityHistory",
                column: "IsSuccess");

            // Add index for UserId in PortHistory
            migrationBuilder.CreateIndex(
                name: "IX_PortHistory_UserId",
                table: "PortHistory",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new tables
            migrationBuilder.DropTable(
                name: "SwitchConnectivityHistory");

            migrationBuilder.DropTable(
                name: "PortStatisticsHistory");

            migrationBuilder.DropTable(
                name: "UserActivityHistory");

            // Drop indexes from PortHistory
            migrationBuilder.DropIndex(
                name: "IX_PortHistory_UserId",
                table: "PortHistory");

            // Drop columns from PortHistory
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PortHistory");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "PortHistory");

            migrationBuilder.DropColumn(
                name: "DowntimeDuration",
                table: "PortHistory");

            migrationBuilder.DropColumn(
                name: "LastUpTime",
                table: "PortHistory");
        }
    }
}