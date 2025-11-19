START TRANSACTION;
ALTER TABLE tenant_default."Employees" RENAME COLUMN "Address" TO "AddressLine1";

ALTER TABLE tenant_default."Employees" ADD "AddressLine2" character varying(500);

ALTER TABLE tenant_default."Employees" ADD "Village" character varying(100);

ALTER TABLE tenant_default."Employees" ADD "District" character varying(100);

ALTER TABLE tenant_default."Employees" ADD "Region" character varying(100);

ALTER TABLE tenant_default."Employees" ADD "Country" character varying(100) NOT NULL DEFAULT 'Mauritius';

CREATE INDEX "IX_Employees_District_tenant_default" ON tenant_default."Employees" ("District");

CREATE INDEX "IX_Employees_Region_tenant_default" ON tenant_default."Employees" ("Region");

ALTER TABLE tenant_siraaj."Employees" RENAME COLUMN "Address" TO "AddressLine1";

ALTER TABLE tenant_siraaj."Employees" ADD "AddressLine2" character varying(500);

ALTER TABLE tenant_siraaj."Employees" ADD "Village" character varying(100);

ALTER TABLE tenant_siraaj."Employees" ADD "District" character varying(100);

ALTER TABLE tenant_siraaj."Employees" ADD "Region" character varying(100);

ALTER TABLE tenant_siraaj."Employees" ADD "Country" character varying(100) NOT NULL DEFAULT 'Mauritius';

CREATE INDEX "IX_Employees_District_tenant_siraaj" ON tenant_siraaj."Employees" ("District");

CREATE INDEX "IX_Employees_Region_tenant_siraaj" ON tenant_siraaj."Employees" ("Region");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251107041234_UpgradeEmployeeAddressForMauritiusCompliance', '9.0.10');

COMMIT;

