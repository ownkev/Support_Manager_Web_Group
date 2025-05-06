using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Support_Manager_Web_Group.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTicketAndAddComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketComments_AspNetUsers_UserID",
                table: "TicketComments");

            migrationBuilder.DeleteData(
                table: "TicketPriorities",
                keyColumn: "PriorityID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TicketPriorities",
                keyColumn: "PriorityID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TicketPriorities",
                keyColumn: "PriorityID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TicketPriorities",
                keyColumn: "PriorityID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TicketStatuses",
                keyColumn: "StatusID",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TicketStatuses",
                keyColumn: "StatusID",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TicketStatuses",
                keyColumn: "StatusID",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TicketStatuses",
                keyColumn: "StatusID",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TicketStatuses",
                keyColumn: "StatusID",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "TicketStatuses",
                keyColumn: "StatusID",
                keyValue: 6);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddForeignKey(
                name: "FK_TicketComments_AspNetUsers_UserID",
                table: "TicketComments",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketComments_AspNetUsers_UserID",
                table: "TicketComments");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Tickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "TicketPriorities",
                columns: new[] { "PriorityID", "PriorityName" },
                values: new object[,]
                {
                    { 1, "Low" },
                    { 2, "Medium" },
                    { 3, "High" },
                    { 4, "Critical" }
                });

            migrationBuilder.InsertData(
                table: "TicketStatuses",
                columns: new[] { "StatusID", "StatusName" },
                values: new object[,]
                {
                    { 1, "Open" },
                    { 2, "Assigned" },
                    { 3, "In Progress" },
                    { 4, "Pending User Response" },
                    { 5, "Resolved" },
                    { 6, "Closed" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_TicketComments_AspNetUsers_UserID",
                table: "TicketComments",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
