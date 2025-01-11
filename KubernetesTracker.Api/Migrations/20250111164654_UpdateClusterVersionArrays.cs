using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KubernetesTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClusterVersionArrays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KernelVersion",
                table: "Clusters");

            migrationBuilder.DropColumn(
                name: "KubeletVersion",
                table: "Clusters");

            migrationBuilder.AddColumn<List<string>>(
                name: "KernelVersions",
                table: "Clusters",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "KubeletVersions",
                table: "Clusters",
                type: "text[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KernelVersions",
                table: "Clusters");

            migrationBuilder.DropColumn(
                name: "KubeletVersions",
                table: "Clusters");

            migrationBuilder.AddColumn<string>(
                name: "KernelVersion",
                table: "Clusters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "KubeletVersion",
                table: "Clusters",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
