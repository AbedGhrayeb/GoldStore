// <copyright file="20260905064857_InitialPostgres.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class InitialPostgres : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "permissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_permissions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "platform_recovery_codes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                platform_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                is_used = table.Column<bool>(type: "boolean", nullable: false),
                used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_platform_recovery_codes", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "platform_users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                phone_number_verified = table.Column<bool>(type: "boolean", nullable: false),
                two_factor_enabled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_platform_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_roles", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "subscription_plans",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                key = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                maximum_active_users = table.Column<int>(type: "integer", nullable: true),
                maximum_posted_invoices_per_period = table.Column<int>(type: "integer", nullable: true),
                maximum_active_branches = table.Column<int>(type: "integer", nullable: true),
                maximum_storage_bytes = table.Column<long>(type: "bigint", nullable: true),
                is_trial = table.Column<bool>(type: "boolean", nullable: false),
                duration_in_months = table.Column<int>(type: "integer", nullable: false),
                price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_subscription_plans", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tenants",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                key = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                trial_ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                cancellation_read_only_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenants", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_role_permissions", x => x.id);
                table.ForeignKey(
                    name: "fk_role_permissions_permissions_permission_id",
                    column: x => x.permission_id,
                    principalTable: "permissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_role_permissions_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "categories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                parent_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_categories", x => x.id);
                table.ForeignKey(
                    name: "fk_categories_categories_parent_category_id",
                    column: x => x.parent_category_id,
                    principalTable: "categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_categories_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "expense_categories",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_expense_categories", x => x.id);
                table.ForeignKey(
                    name: "fk_expense_categories_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "financial_accounts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                account_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_financial_accounts", x => x.id);
                table.ForeignKey(
                    name: "fk_financial_accounts_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "gold_ledger_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_gold_ledger_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_gold_ledger_entries_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "inventory_adjustments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_inventory_adjustments", x => x.id);
                table.ForeignKey(
                    name: "fk_inventory_adjustments_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "invoice_number_sequences",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                document_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                next_number = table.Column<int>(type: "integer", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_invoice_number_sequences", x => x.id);
                table.ForeignKey(
                    name: "fk_invoice_number_sequences_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "suppliers",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                primary_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                secondary_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                bank_account_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_suppliers", x => x.id);
                table.ForeignKey(
                    name: "fk_suppliers_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tenant_settings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                logo_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                locale = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                invoice_number_prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                enabled_features = table.Column<List<string>>(type: "text[]", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_settings", x => x.id);
                table.ForeignKey(
                    name: "fk_tenant_settings_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tenant_subscriptions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                subscription_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                billing_cycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                billing_provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                billing_provider_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_tenant_subscriptions", x => x.id);
                table.ForeignKey(
                    name: "fk_tenant_subscriptions_subscription_plans_subscription_plan_id",
                    column: x => x.subscription_plan_id,
                    principalTable: "subscription_plans",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_tenant_subscriptions_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "user_recovery_codes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                is_used = table.Column<bool>(type: "boolean", nullable: false),
                used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_recovery_codes", x => x.id);
                table.ForeignKey(
                    name: "fk_user_recovery_codes_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "text", nullable: false),
                first_name = table.Column<string>(type: "text", nullable: false),
                last_name = table.Column<string>(type: "text", nullable: false),
                password_hash = table.Column<string>(type: "text", nullable: false),
                phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                whatsapp_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                phone_number_verified = table.Column<bool>(type: "boolean", nullable: false),
                two_factor_enabled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                security_stamp = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
                locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
                table.ForeignKey(
                    name: "fk_users_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "debts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_debts", x => x.id);
                table.ForeignKey(
                    name: "fk_debts_financial_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_debts_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "expenses",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                expense_category_id = table.Column<Guid>(type: "uuid", nullable: true),
                account_id = table.Column<Guid>(type: "uuid", nullable: true),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                expense_date = table.Column<DateOnly>(type: "date", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_expenses", x => x.id);
                table.ForeignKey(
                    name: "fk_expenses_expense_categories_expense_category_id",
                    column: x => x.expense_category_id,
                    principalTable: "expense_categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_expenses_financial_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_expenses_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "financial_transactions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                exchange_rate = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                base_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                transaction_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_financial_transactions", x => x.id);
                table.ForeignKey(
                    name: "fk_financial_transactions_financial_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_financial_transactions_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "supplier_deliveries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                manufacturing_fee_per_gram = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                total_manufacturing_fee = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                manufacturing_fee_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_deliveries", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_deliveries_suppliers_supplier_id",
                    column: x => x.supplier_id,
                    principalTable: "suppliers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_deliveries_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "supplier_financial_transactions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_financial_transactions", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_financial_transactions_financial_accounts_account_",
                    column: x => x.account_id,
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_financial_transactions_suppliers_supplier_id",
                    column: x => x.supplier_id,
                    principalTable: "suppliers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_financial_transactions_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "supplier_gold_ledger_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_gold_ledger_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_gold_ledger_entries_suppliers_supplier_id",
                    column: x => x.supplier_id,
                    principalTable: "suppliers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_gold_ledger_entries_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "supplier_manufacturing_ledger_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                reference_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_manufacturing_ledger_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_manufacturing_ledger_entries_suppliers_supplier_id",
                    column: x => x.supplier_id,
                    principalTable: "suppliers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_manufacturing_ledger_entries_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "supplier_manufacturing_payments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_manufacturing_payments", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_manufacturing_payments_financial_accounts_account_",
                    column: x => x.account_id,
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_manufacturing_payments_suppliers_supplier_id",
                    column: x => x.supplier_id,
                    principalTable: "suppliers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_manufacturing_payments_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "supplier_scrap_gold_payments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_scrap_gold_payments", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_scrap_gold_payments_suppliers_supplier_id",
                    column: x => x.supplier_id,
                    principalTable: "suppliers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_scrap_gold_payments_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "employees",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                first_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                last_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                role = table.Column<string>(type: "text", nullable: false),
                salary = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                salary_cycle = table.Column<int>(type: "integer", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_employees", x => x.id);
                table.ForeignKey(
                    name: "fk_employees_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_employees_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                replaced_by_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refresh_tokens", x => x.id);
                table.ForeignKey(
                    name: "fk_refresh_tokens_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_refresh_tokens_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_permissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_permissions", x => x.id);
                table.ForeignKey(
                    name: "fk_user_permissions_permissions_permission_id",
                    column: x => x.permission_id,
                    principalTable: "permissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_user_permissions_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_user_permissions_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_roles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_roles", x => x.id);
                table.ForeignKey(
                    name: "fk_user_roles_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_user_roles_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_user_roles_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "debt_ledger_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                debt_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_debt_ledger_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_debt_ledger_entries_debts_debt_id",
                    column: x => x.debt_id,
                    principalTable: "debts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_debt_ledger_entries_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "supplier_financial_ledger_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_financial_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                movement_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_financial_ledger_entries", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_financial_ledger_entries_supplier_financial_transa",
                    column: x => x.supplier_financial_transaction_id,
                    principalTable: "supplier_financial_transactions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_financial_ledger_entries_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "supplier_financial_payments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                supplier_financial_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_supplier_financial_payments", x => x.id);
                table.ForeignKey(
                    name: "fk_supplier_financial_payments_financial_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_financial_payments_supplier_financial_transactions",
                    column: x => x.supplier_financial_transaction_id,
                    principalTable: "supplier_financial_transactions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_supplier_financial_payments_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "customer_purchase_invoices",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                invoice_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                seller_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                seller_id_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                seller_phone = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true),
                seller_year_of_birth = table.Column<int>(type: "integer", maxLength: 13, nullable: true),
                seller_address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                total_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                amount_paid = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                payment_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                seller_account_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_customer_purchase_invoices", x => x.id);
                table.ForeignKey(
                    name: "fk_customer_purchase_invoices_employees_employee_id",
                    column: x => x.employee_id,
                    principalTable: "employees",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_customer_purchase_invoices_financial_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_customer_purchase_invoices_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "salary_payments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                account_id = table.Column<Guid>(type: "uuid", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                salary_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                discount_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                scheduled_date = table.Column<DateOnly>(type: "date", nullable: false),
                notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_salary_payments", x => x.id);
                table.ForeignKey(
                    name: "fk_salary_payments_employees_employee_id",
                    column: x => x.employee_id,
                    principalTable: "employees",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_salary_payments_financial_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_salary_payments_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "sales_invoices",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                invoice_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                customer_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                total_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                amount_paid = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                remaining_balance = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                payment_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                customer_account_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                employee_id = table.Column<Guid>(type: "uuid", nullable: true),
                account_id = table.Column<Guid>(type: "uuid", nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sales_invoices", x => x.id);
                table.ForeignKey(
                    name: "fk_sales_invoices_employees_employee_id",
                    column: x => x.employee_id,
                    principalTable: "employees",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_sales_invoices_financial_accounts_account_id",
                    column: x => x.account_id,
                    principalTable: "financial_accounts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_sales_invoices_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "customer_purchase_invoice_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                customer_purchase_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: true),
                karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                price_per_gram = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_customer_purchase_invoice_items", x => x.id);
                table.ForeignKey(
                    name: "fk_customer_purchase_invoice_items_categories_category_id",
                    column: x => x.category_id,
                    principalTable: "categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_customer_purchase_invoice_items_customer_purchase_invoices_",
                    column: x => x.customer_purchase_invoice_id,
                    principalTable: "customer_purchase_invoices",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_customer_purchase_invoice_items_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "sales_invoice_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                sales_invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                category_id = table.Column<Guid>(type: "uuid", nullable: true),
                karat = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                equivalent21k_weight_in_grams = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                price_per_gram = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                gold_amount = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                last_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sales_invoice_items", x => x.id);
                table.ForeignKey(
                    name: "fk_sales_invoice_items_categories_category_id",
                    column: x => x.category_id,
                    principalTable: "categories",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_sales_invoice_items_sales_invoices_sales_invoice_id",
                    column: x => x.sales_invoice_id,
                    principalTable: "sales_invoices",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_sales_invoice_items_tenants_tenant_id",
                    column: x => x.tenant_id,
                    principalTable: "tenants",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_categories_parent_category_id",
            table: "categories",
            column: "parent_category_id");

        migrationBuilder.CreateIndex(
            name: "ix_categories_tenant_id_name",
            table: "categories",
            columns: new[] { "tenant_id", "name" });

        migrationBuilder.CreateIndex(
            name: "ix_categories_tenant_id_parent_category_id_name",
            table: "categories",
            columns: new[] { "tenant_id", "parent_category_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_customer_purchase_invoice_items_category_id",
            table: "customer_purchase_invoice_items",
            column: "category_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_purchase_invoice_items_customer_purchase_invoice_id",
            table: "customer_purchase_invoice_items",
            column: "customer_purchase_invoice_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_purchase_invoice_items_tenant_id_customer_purchase",
            table: "customer_purchase_invoice_items",
            columns: new[] { "tenant_id", "customer_purchase_invoice_id" });

        migrationBuilder.CreateIndex(
            name: "ix_customer_purchase_invoices_account_id",
            table: "customer_purchase_invoices",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_purchase_invoices_employee_id",
            table: "customer_purchase_invoices",
            column: "employee_id");

        migrationBuilder.CreateIndex(
            name: "ix_customer_purchase_invoices_tenant_id_date",
            table: "customer_purchase_invoices",
            columns: new[] { "tenant_id", "date" });

        migrationBuilder.CreateIndex(
            name: "ix_customer_purchase_invoices_tenant_id_invoice_number",
            table: "customer_purchase_invoices",
            columns: new[] { "tenant_id", "invoice_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_debt_ledger_entries_debt_id",
            table: "debt_ledger_entries",
            column: "debt_id");

        migrationBuilder.CreateIndex(
            name: "ix_debt_ledger_entries_tenant_id_debt_id_created_at_utc",
            table: "debt_ledger_entries",
            columns: new[] { "tenant_id", "debt_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_debts_account_id",
            table: "debts",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_debts_tenant_id_created_at_utc",
            table: "debts",
            columns: new[] { "tenant_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_debts_tenant_id_name",
            table: "debts",
            columns: new[] { "tenant_id", "name" });

        migrationBuilder.CreateIndex(
            name: "ix_employees_tenant_id",
            table: "employees",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_employees_user_id",
            table: "employees",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_expense_categories_tenant_id_name",
            table: "expense_categories",
            columns: new[] { "tenant_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_expenses_account_id",
            table: "expenses",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_expenses_expense_category_id",
            table: "expenses",
            column: "expense_category_id");

        migrationBuilder.CreateIndex(
            name: "ix_expenses_tenant_id_account_id_expense_date",
            table: "expenses",
            columns: new[] { "tenant_id", "account_id", "expense_date" });

        migrationBuilder.CreateIndex(
            name: "ix_expenses_tenant_id_expense_category_id_expense_date",
            table: "expenses",
            columns: new[] { "tenant_id", "expense_category_id", "expense_date" });

        migrationBuilder.CreateIndex(
            name: "ix_expenses_tenant_id_expense_date",
            table: "expenses",
            columns: new[] { "tenant_id", "expense_date" });

        migrationBuilder.CreateIndex(
            name: "ix_financial_accounts_tenant_id_name_currency",
            table: "financial_accounts",
            columns: new[] { "tenant_id", "name", "currency" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_financial_transactions_account_id",
            table: "financial_transactions",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_financial_transactions_tenant_id_account_id_created_at_utc",
            table: "financial_transactions",
            columns: new[] { "tenant_id", "account_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_financial_transactions_tenant_id_reference_type_reference_id",
            table: "financial_transactions",
            columns: new[] { "tenant_id", "reference_type", "reference_id" });

        migrationBuilder.CreateIndex(
            name: "ix_gold_ledger_entries_tenant_id_karat_created_at_utc",
            table: "gold_ledger_entries",
            columns: new[] { "tenant_id", "karat", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_gold_ledger_entries_tenant_id_reference_type_reference_id",
            table: "gold_ledger_entries",
            columns: new[] { "tenant_id", "reference_type", "reference_id" });

        migrationBuilder.CreateIndex(
            name: "ix_inventory_adjustments_tenant_id_created_at_utc",
            table: "inventory_adjustments",
            columns: new[] { "tenant_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_inventory_adjustments_tenant_id_type_created_at_utc",
            table: "inventory_adjustments",
            columns: new[] { "tenant_id", "type", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_invoice_number_sequences_tenant_id_document_type_period",
            table: "invoice_number_sequences",
            columns: new[] { "tenant_id", "document_type", "period" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_permissions_key",
            table: "permissions",
            column: "key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_platform_recovery_codes_platform_user_id_code_hash",
            table: "platform_recovery_codes",
            columns: new[] { "platform_user_id", "code_hash" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_platform_users_email",
            table: "platform_users",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_platform_users_phone_number",
            table: "platform_users",
            column: "phone_number",
            unique: true,
            filter: "\"phone_number\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_tenant_id",
            table: "refresh_tokens",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_token_hash",
            table: "refresh_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_user_id",
            table: "refresh_tokens",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_role_permissions_permission_id",
            table: "role_permissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "ix_role_permissions_role_id_permission_id",
            table: "role_permissions",
            columns: new[] { "role_id", "permission_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_roles_key",
            table: "roles",
            column: "key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_salary_payments_account_id",
            table: "salary_payments",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_salary_payments_employee_id",
            table: "salary_payments",
            column: "employee_id");

        migrationBuilder.CreateIndex(
            name: "ix_salary_payments_tenant_id_employee_id_payment_date",
            table: "salary_payments",
            columns: new[] { "tenant_id", "employee_id", "payment_date" });

        migrationBuilder.CreateIndex(
            name: "ix_salary_payments_tenant_id_payment_date",
            table: "salary_payments",
            columns: new[] { "tenant_id", "payment_date" });

        migrationBuilder.CreateIndex(
            name: "ix_sales_invoice_items_category_id",
            table: "sales_invoice_items",
            column: "category_id");

        migrationBuilder.CreateIndex(
            name: "ix_sales_invoice_items_sales_invoice_id",
            table: "sales_invoice_items",
            column: "sales_invoice_id");

        migrationBuilder.CreateIndex(
            name: "ix_sales_invoice_items_tenant_id_sales_invoice_id",
            table: "sales_invoice_items",
            columns: new[] { "tenant_id", "sales_invoice_id" });

        migrationBuilder.CreateIndex(
            name: "ix_sales_invoices_account_id",
            table: "sales_invoices",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_sales_invoices_employee_id",
            table: "sales_invoices",
            column: "employee_id");

        migrationBuilder.CreateIndex(
            name: "ix_sales_invoices_tenant_id_date",
            table: "sales_invoices",
            columns: new[] { "tenant_id", "date" });

        migrationBuilder.CreateIndex(
            name: "ix_sales_invoices_tenant_id_invoice_number",
            table: "sales_invoices",
            columns: new[] { "tenant_id", "invoice_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_subscription_plans_key",
            table: "subscription_plans",
            column: "key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_supplier_deliveries_supplier_id",
            table: "supplier_deliveries",
            column: "supplier_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_deliveries_tenant_id_supplier_id_created_at_utc",
            table: "supplier_deliveries",
            columns: new[] { "tenant_id", "supplier_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_supplier_financial_ledger_entries_supplier_financial_transa",
            table: "supplier_financial_ledger_entries",
            column: "supplier_financial_transaction_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_financial_ledger_entries_tenant_id_supplier_financ",
            table: "supplier_financial_ledger_entries",
            columns: new[] { "tenant_id", "supplier_financial_transaction_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_supplier_financial_payments_account_id",
            table: "supplier_financial_payments",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_financial_payments_supplier_financial_transaction_",
            table: "supplier_financial_payments",
            column: "supplier_financial_transaction_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_financial_payments_tenant_id_supplier_financial_tr",
            table: "supplier_financial_payments",
            columns: new[] { "tenant_id", "supplier_financial_transaction_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_supplier_financial_transactions_account_id",
            table: "supplier_financial_transactions",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_financial_transactions_supplier_id",
            table: "supplier_financial_transactions",
            column: "supplier_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_financial_transactions_tenant_id_supplier_id_creat",
            table: "supplier_financial_transactions",
            columns: new[] { "tenant_id", "supplier_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_supplier_gold_ledger_entries_supplier_id",
            table: "supplier_gold_ledger_entries",
            column: "supplier_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_gold_ledger_entries_tenant_id_supplier_id_created_",
            table: "supplier_gold_ledger_entries",
            columns: new[] { "tenant_id", "supplier_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_supplier_manufacturing_ledger_entries_supplier_id",
            table: "supplier_manufacturing_ledger_entries",
            column: "supplier_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_manufacturing_ledger_entries_tenant_id_supplier_id",
            table: "supplier_manufacturing_ledger_entries",
            columns: new[] { "tenant_id", "supplier_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_supplier_manufacturing_payments_account_id",
            table: "supplier_manufacturing_payments",
            column: "account_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_manufacturing_payments_supplier_id",
            table: "supplier_manufacturing_payments",
            column: "supplier_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_manufacturing_payments_tenant_id_supplier_id_creat",
            table: "supplier_manufacturing_payments",
            columns: new[] { "tenant_id", "supplier_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_supplier_scrap_gold_payments_supplier_id",
            table: "supplier_scrap_gold_payments",
            column: "supplier_id");

        migrationBuilder.CreateIndex(
            name: "ix_supplier_scrap_gold_payments_tenant_id_supplier_id_created_",
            table: "supplier_scrap_gold_payments",
            columns: new[] { "tenant_id", "supplier_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_suppliers_tenant_id_name",
            table: "suppliers",
            columns: new[] { "tenant_id", "name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tenant_settings_tenant_id",
            table: "tenant_settings",
            column: "tenant_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_tenant_subscriptions_subscription_plan_id",
            table: "tenant_subscriptions",
            column: "subscription_plan_id");

        migrationBuilder.CreateIndex(
            name: "ix_tenant_subscriptions_tenant_id_ends_at_utc",
            table: "tenant_subscriptions",
            columns: new[] { "tenant_id", "ends_at_utc" });

        migrationBuilder.CreateIndex(
            name: "ix_tenants_key",
            table: "tenants",
            column: "key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_permissions_permission_id",
            table: "user_permissions",
            column: "permission_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_permissions_tenant_id_user_id_permission_id",
            table: "user_permissions",
            columns: new[] { "tenant_id", "user_id", "permission_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_permissions_user_id",
            table: "user_permissions",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_recovery_codes_tenant_id",
            table: "user_recovery_codes",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_recovery_codes_user_id_code_hash",
            table: "user_recovery_codes",
            columns: new[] { "user_id", "code_hash" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_roles_role_id",
            table: "user_roles",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_roles_tenant_id_user_id_role_id",
            table: "user_roles",
            columns: new[] { "tenant_id", "user_id", "role_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_roles_user_id",
            table: "user_roles",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_email",
            table: "users",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_users_tenant_id",
            table: "users",
            column: "tenant_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "customer_purchase_invoice_items");

        migrationBuilder.DropTable(
            name: "debt_ledger_entries");

        migrationBuilder.DropTable(
            name: "expenses");

        migrationBuilder.DropTable(
            name: "financial_transactions");

        migrationBuilder.DropTable(
            name: "gold_ledger_entries");

        migrationBuilder.DropTable(
            name: "inventory_adjustments");

        migrationBuilder.DropTable(
            name: "invoice_number_sequences");

        migrationBuilder.DropTable(
            name: "platform_recovery_codes");

        migrationBuilder.DropTable(
            name: "platform_users");

        migrationBuilder.DropTable(
            name: "refresh_tokens");

        migrationBuilder.DropTable(
            name: "role_permissions");

        migrationBuilder.DropTable(
            name: "salary_payments");

        migrationBuilder.DropTable(
            name: "sales_invoice_items");

        migrationBuilder.DropTable(
            name: "supplier_deliveries");

        migrationBuilder.DropTable(
            name: "supplier_financial_ledger_entries");

        migrationBuilder.DropTable(
            name: "supplier_financial_payments");

        migrationBuilder.DropTable(
            name: "supplier_gold_ledger_entries");

        migrationBuilder.DropTable(
            name: "supplier_manufacturing_ledger_entries");

        migrationBuilder.DropTable(
            name: "supplier_manufacturing_payments");

        migrationBuilder.DropTable(
            name: "supplier_scrap_gold_payments");

        migrationBuilder.DropTable(
            name: "tenant_settings");

        migrationBuilder.DropTable(
            name: "tenant_subscriptions");

        migrationBuilder.DropTable(
            name: "user_permissions");

        migrationBuilder.DropTable(
            name: "user_recovery_codes");

        migrationBuilder.DropTable(
            name: "user_roles");

        migrationBuilder.DropTable(
            name: "customer_purchase_invoices");

        migrationBuilder.DropTable(
            name: "debts");

        migrationBuilder.DropTable(
            name: "expense_categories");

        migrationBuilder.DropTable(
            name: "categories");

        migrationBuilder.DropTable(
            name: "sales_invoices");

        migrationBuilder.DropTable(
            name: "supplier_financial_transactions");

        migrationBuilder.DropTable(
            name: "subscription_plans");

        migrationBuilder.DropTable(
            name: "permissions");

        migrationBuilder.DropTable(
            name: "roles");

        migrationBuilder.DropTable(
            name: "employees");

        migrationBuilder.DropTable(
            name: "financial_accounts");

        migrationBuilder.DropTable(
            name: "suppliers");

        migrationBuilder.DropTable(
            name: "users");

        migrationBuilder.DropTable(
            name: "tenants");
    }
}
