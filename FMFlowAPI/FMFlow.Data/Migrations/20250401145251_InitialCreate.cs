using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FMFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingPlans",
                columns: table => new
                {
                    BillingPlanID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BillingFrequency = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPlans", x => x.BillingPlanID);
                });

            migrationBuilder.CreateTable(
                name: "FMFlowUser",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdentityGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FMFlowUser", x => x.UserID);
                });

            migrationBuilder.CreateTable(
                name: "LeadSource",
                columns: table => new
                {
                    LeadSourceID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadSource", x => x.LeadSourceID);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                columns: table => new
                {
                    ProductID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.ProductID);
                });

            migrationBuilder.CreateTable(
                name: "Sheen",
                columns: table => new
                {
                    SheenID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sheen", x => x.SheenID);
                });

            migrationBuilder.CreateTable(
                name: "State",
                columns: table => new
                {
                    Abbreviation = table.Column<string>(type: "text", nullable: false),
                    StateName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_State", x => x.Abbreviation);
                });

            migrationBuilder.CreateTable(
                name: "TimeZones",
                columns: table => new
                {
                    TimeZoneId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeZones", x => x.TimeZoneId);
                });

            migrationBuilder.CreateTable(
                name: "Billing",
                columns: table => new
                {
                    BillingID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MerchantID = table.Column<long>(type: "bigint", nullable: true),
                    CardID = table.Column<long>(type: "bigint", nullable: true),
                    VaultedCardToken = table.Column<string>(type: "text", nullable: true),
                    ContractID = table.Column<long>(type: "bigint", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingPlanID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing", x => x.BillingID);
                    table.ForeignKey(
                        name: "FK_Billing_BillingPlans_BillingPlanID",
                        column: x => x.BillingPlanID,
                        principalTable: "BillingPlans",
                        principalColumn: "BillingPlanID");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeUsers",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Biography = table.Column<string>(type: "text", nullable: true),
                    Memo = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    DailyGoal = table.Column<decimal>(type: "numeric", nullable: true),
                    BurdenRate = table.Column<decimal>(type: "numeric", nullable: true),
                    Skills = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TwilioNumber = table.Column<string>(type: "text", nullable: true),
                    TwilioCallerID = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeUsers", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_EmployeeUsers_FMFlowUser_UserID",
                        column: x => x.UserID,
                        principalTable: "FMFlowUser",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    LeadID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeadSourceID = table.Column<int>(type: "integer", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    ZipCode = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Mobile = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    OrganizationName = table.Column<string>(type: "text", nullable: true),
                    CustomerType = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.LeadID);
                    table.ForeignKey(
                        name: "FK_Leads_LeadSource_LeadSourceID",
                        column: x => x.LeadSourceID,
                        principalTable: "LeadSource",
                        principalColumn: "LeadSourceID");
                });

            migrationBuilder.CreateTable(
                name: "ProZipcode",
                columns: table => new
                {
                    Zipcode = table.Column<string>(type: "text", nullable: false),
                    StateAbbreviation = table.Column<string>(type: "text", nullable: false),
                    County = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProZipcode", x => x.Zipcode);
                    table.ForeignKey(
                        name: "FK_ProZipcode_State_StateAbbreviation",
                        column: x => x.StateAbbreviation,
                        principalTable: "State",
                        principalColumn: "Abbreviation",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProUserDetail",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    BusinessType = table.Column<string>(type: "text", nullable: false),
                    TaxID = table.Column<string>(type: "text", nullable: false),
                    NumberOfEmployees = table.Column<string>(type: "text", nullable: false),
                    SizeOfJob = table.Column<string>(type: "text", nullable: true),
                    Services = table.Column<string[]>(type: "text[]", nullable: true),
                    AddressOfStore = table.Column<string>(type: "text", nullable: true),
                    ZipCodeOfStore = table.Column<string>(type: "text", nullable: true),
                    BusinessName = table.Column<string>(type: "text", nullable: true),
                    BusinessAddress = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    ZipCode = table.Column<string>(type: "text", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    RequestedReferrals = table.Column<bool>(type: "boolean", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    GoogleReview = table.Column<string>(type: "text", nullable: true),
                    YelpReview = table.Column<string>(type: "text", nullable: true),
                    AccountManagerUserID = table.Column<int>(type: "integer", nullable: true),
                    CardID = table.Column<long>(type: "bigint", nullable: true),
                    ContractID = table.Column<long>(type: "bigint", nullable: true),
                    OnboardingFormStop = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BillingID = table.Column<int>(type: "integer", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FMTimeZoneID = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProUserDetail", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_ProUserDetail_Billing_BillingID",
                        column: x => x.BillingID,
                        principalTable: "Billing",
                        principalColumn: "BillingID");
                    table.ForeignKey(
                        name: "FK_ProUserDetail_EmployeeUsers_AccountManagerUserID",
                        column: x => x.AccountManagerUserID,
                        principalTable: "EmployeeUsers",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_ProUserDetail_FMFlowUser_UserID",
                        column: x => x.UserID,
                        principalTable: "FMFlowUser",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProUserDetail_TimeZones_FMTimeZoneID",
                        column: x => x.FMTimeZoneID,
                        principalTable: "TimeZones",
                        principalColumn: "TimeZoneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProUserToProZipcode",
                columns: table => new
                {
                    Zipcode = table.Column<string>(type: "text", nullable: false),
                    UserID = table.Column<int>(type: "integer", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DateDeleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProUserToProZipcode", x => new { x.Zipcode, x.UserID });
                    table.ForeignKey(
                        name: "FK_ProUserToProZipcode_ProUserDetail_UserID",
                        column: x => x.UserID,
                        principalTable: "ProUserDetail",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProUserToProZipcode_ProZipcode_Zipcode",
                        column: x => x.Zipcode,
                        principalTable: "ProZipcode",
                        principalColumn: "Zipcode",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Billing_BillingPlanID",
                table: "Billing",
                column: "BillingPlanID");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_LeadSourceID",
                table: "Leads",
                column: "LeadSourceID");

            migrationBuilder.CreateIndex(
                name: "IX_ProUserDetail_AccountManagerUserID",
                table: "ProUserDetail",
                column: "AccountManagerUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ProUserDetail_BillingID",
                table: "ProUserDetail",
                column: "BillingID");

            migrationBuilder.CreateIndex(
                name: "IX_ProUserDetail_FMTimeZoneID",
                table: "ProUserDetail",
                column: "FMTimeZoneID");

            migrationBuilder.CreateIndex(
                name: "IX_ProUserToProZipcode_UserID",
                table: "ProUserToProZipcode",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ProZipcode_StateAbbreviation",
                table: "ProZipcode",
                column: "StateAbbreviation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "Product");

            migrationBuilder.DropTable(
                name: "ProUserToProZipcode");

            migrationBuilder.DropTable(
                name: "Sheen");

            migrationBuilder.DropTable(
                name: "LeadSource");

            migrationBuilder.DropTable(
                name: "ProUserDetail");

            migrationBuilder.DropTable(
                name: "ProZipcode");

            migrationBuilder.DropTable(
                name: "Billing");

            migrationBuilder.DropTable(
                name: "EmployeeUsers");

            migrationBuilder.DropTable(
                name: "TimeZones");

            migrationBuilder.DropTable(
                name: "State");

            migrationBuilder.DropTable(
                name: "BillingPlans");

            migrationBuilder.DropTable(
                name: "FMFlowUser");
        }
    }
}
