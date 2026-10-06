-- Upgrade the existing OracleInitialCreate schema to the unified application model.
-- Run once as the APIVAULT schema owner, with the API stopped. Oracle DDL commits
-- implicitly; the AV_BAK_20261006_* tables retain the original rows for recovery.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
SET DEFINE OFF
SET ECHO OFF

PROMPT Checking the Oracle baseline and application data...
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables
    WHERE table_name IN ('API_PROJECT', 'PROJECT_REGISTRY', 'API_ASSET',
                         'SEC_SCREEN', 'ROLE_PERMISSION', '__EFMigrationsHistory');
    IF n <> 6 THEN
        RAISE_APPLICATION_ERROR(-20001, 'Expected Oracle baseline tables are missing.');
    END IF;

    SELECT COUNT(*) INTO n FROM user_tables
    WHERE table_name IN ('VENDOR', 'AV_BAK_20261006_API_PROJECT',
                         'AV_BAK_20261006_PROJECT', 'AV_BAK_20261006_ASSET',
                         'AV_BAK_20261006_SCREEN', 'AV_BAK_20261006_ROLE_PERM',
                         'AV_BAK_20261006_HISTORY');
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20002, 'Upgrade or backup objects already exist; inspect before rerunning.');
    END IF;

    SELECT COUNT(*) INTO n FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20260731202610_OracleInitialCreate';
    IF n <> 1 THEN
        RAISE_APPLICATION_ERROR(-20003, 'OracleInitialCreate is not recorded exactly once.');
    END IF;

    SELECT COUNT(*) INTO n FROM "__EFMigrationsHistory"
    WHERE "MigrationId" = '20261006044500_OracleRegistrySync';
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20004, 'OracleRegistrySync is already recorded.');
    END IF;

    SELECT COUNT(*) INTO n FROM user_tab_columns
    WHERE (table_name = 'API_ASSET' AND column_name = 'ApiProjectId')
       OR (table_name = 'PROJECT_REGISTRY' AND column_name IN ('OwnershipType', 'VendorId'));
    IF n <> 1 THEN
        RAISE_APPLICATION_ERROR(-20005, 'Schema is not at the expected pre-upgrade shape.');
    END IF;

    SELECT COUNT(*) INTO n FROM (
        SELECT UPPER(TRIM("Code")) FROM "API_PROJECT"
        GROUP BY UPPER(TRIM("Code")) HAVING COUNT(*) > 1
    );
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20006, 'Duplicate API_PROJECT codes after normalization.');
    END IF;

    SELECT COUNT(*) INTO n FROM (
        SELECT UPPER(TRIM("Code")) FROM "PROJECT_REGISTRY"
        GROUP BY UPPER(TRIM("Code")) HAVING COUNT(*) > 1
    );
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20007, 'Duplicate PROJECT_REGISTRY codes after normalization.');
    END IF;

    SELECT COUNT(*) INTO n FROM "API_PROJECT"
    WHERE LENGTH(TRIM("Code")) > 50 OR LENGTH("Name") > 200;
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20008, 'An API_PROJECT code or name exceeds the unified limit.');
    END IF;

    SELECT COUNT(*) INTO n
    FROM "API_PROJECT" ap JOIN "PROJECT_REGISTRY" p ON ap."Id" = p."Id"
    WHERE UPPER(TRIM(ap."Code")) <> UPPER(TRIM(p."Code"));
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20009, 'Unrelated legacy and registry rows share an ID.');
    END IF;

    SELECT COUNT(*) INTO n
    FROM "API_ASSET" a
    LEFT JOIN "API_PROJECT" ap ON ap."Id" = a."ApiProjectId"
    LEFT JOIN "PROJECT_REGISTRY" p
      ON UPPER(TRIM(p."Code")) = UPPER(TRIM(ap."Code"))
    WHERE ap."Id" IS NULL OR
          (p."Id" IS NULL AND EXISTS (
              SELECT 1 FROM "PROJECT_REGISTRY" other
              WHERE other."Id" = ap."Id"));
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20010, 'An API asset cannot be mapped safely.');
    END IF;

    SELECT COUNT(*) INTO n FROM "API_ASSET"
    WHERE "OwnershipType" = 'ThirdParty' AND TRIM("VendorName") IS NULL;
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20011, 'Third-party assets need a vendor name.');
    END IF;

    SELECT COUNT(*) INTO n FROM "API_ASSET"
    WHERE LENGTH(TRIM("VendorName")) > 200;
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20014, 'A vendor name exceeds the 200-character limit.');
    END IF;

    SELECT COUNT(*) INTO n FROM (
        SELECT p."Id", UPPER(TRIM(a."Name"))
        FROM "API_ASSET" a
        JOIN "API_PROJECT" ap ON ap."Id" = a."ApiProjectId"
        JOIN "PROJECT_REGISTRY" p
          ON UPPER(TRIM(p."Code")) = UPPER(TRIM(ap."Code"))
        GROUP BY p."Id", UPPER(TRIM(a."Name"))
        HAVING COUNT(*) > 1
    );
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20015, 'API names would collide after relinking.');
    END IF;

    SELECT COUNT(*) INTO n FROM (
        SELECT ap."Id"
        FROM "API_ASSET" a JOIN "API_PROJECT" ap ON ap."Id" = a."ApiProjectId"
        WHERE a."OwnershipType" = 'ThirdParty'
        GROUP BY ap."Id"
        HAVING COUNT(DISTINCT UPPER(TRIM(a."VendorName"))) > 1
    );
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20012, 'A source system has multiple third-party vendors.');
    END IF;

    SELECT COUNT(*) INTO n FROM (
        SELECT UPPER(TRIM("VendorName"))
        FROM "API_ASSET"
        WHERE TRIM("VendorName") IS NOT NULL
        GROUP BY UPPER(TRIM("VendorName"))
    );
    IF n > 9999 THEN
        RAISE_APPLICATION_ERROR(-20016, 'More than 9999 vendor names need migration.');
    END IF;

    SELECT COUNT(*) INTO n FROM "SEC_SCREEN"
    WHERE "Route" IN ('/admin/vendors', '/admin/reset-password')
      AND "Code" NOT IN ('VENDORS', 'RESET_PASSWORD');
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20013, 'A new security route is already assigned.');
    END IF;
END;
/

PROMPT Preserving original rows in backup tables...
CREATE TABLE "AV_BAK_20261006_API_PROJECT" AS SELECT * FROM "API_PROJECT";
CREATE TABLE "AV_BAK_20261006_PROJECT" AS SELECT * FROM "PROJECT_REGISTRY";
CREATE TABLE "AV_BAK_20261006_ASSET" AS SELECT * FROM "API_ASSET";
CREATE TABLE "AV_BAK_20261006_SCREEN" AS SELECT * FROM "SEC_SCREEN";
CREATE TABLE "AV_BAK_20261006_ROLE_PERM" AS SELECT * FROM "ROLE_PERMISSION";
CREATE TABLE "AV_BAK_20261006_HISTORY" AS SELECT * FROM "__EFMigrationsHistory";

PROMPT Creating the vendor registry and merging applications...
CREATE TABLE "VENDOR" (
    "Id" RAW(16) NOT NULL,
    "Code" NVARCHAR2(50) NOT NULL,
    "Name" NVARCHAR2(200) NOT NULL,
    "Description" NCLOB,
    "ContactPerson" NVARCHAR2(200),
    "SupportEmail" NVARCHAR2(320),
    "SupportPhone" NVARCHAR2(100),
    "WebsiteUrl" NVARCHAR2(1000),
    "IsActive" NUMBER(1) NOT NULL,
    "CreatedAtUtc" TIMESTAMP(6) NOT NULL,
    "CreatedBy" NVARCHAR2(200) NOT NULL,
    "UpdatedAtUtc" TIMESTAMP(6),
    "UpdatedBy" NVARCHAR2(200),
    CONSTRAINT "PK_VENDOR" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_VENDOR_Code" ON "VENDOR" ("Code");
CREATE UNIQUE INDEX "IX_VENDOR_Name" ON "VENDOR" ("Name");

INSERT INTO "VENDOR" ("Id", "Code", "Name", "IsActive", "CreatedAtUtc", "CreatedBy")
SELECT SYS_GUID(), 'MIG-' || LPAD(ROW_NUMBER() OVER (ORDER BY v."Name"), 4, '0'),
       v."Name", 1, CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS TIMESTAMP),
       'migration:OracleRegistrySync'
FROM (
    SELECT MIN(TRIM("VendorName")) AS "Name"
    FROM "API_ASSET"
    WHERE TRIM("VendorName") IS NOT NULL
    GROUP BY UPPER(TRIM("VendorName"))
) v;

ALTER TABLE "PROJECT_REGISTRY"
    MODIFY ("BusinessAreaId" NULL, "OwnerTeamId" NULL);
ALTER TABLE "PROJECT_REGISTRY"
    ADD ("OwnershipType" NVARCHAR2(30) DEFAULT 'Internal' NOT NULL,
         "VendorId" RAW(16));

INSERT INTO "PROJECT_REGISTRY"
    ("Id", "Code", "Name", "Description", "Criticality", "Status",
     "BusinessAreaId", "OwnerTeamId", "OwnershipType", "VendorId",
     "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy")
SELECT ap."Id", ap."Code", ap."Name", ap."Description", 'Medium',
       CASE WHEN ap."IsActive" = 1 THEN 'Active' ELSE 'Inactive' END,
       NULL, NULL, 'Internal', NULL,
       ap."CreatedAtUtc", ap."CreatedBy", ap."UpdatedAtUtc", ap."UpdatedBy"
FROM "API_PROJECT" ap
WHERE NOT EXISTS (
    SELECT 1 FROM "PROJECT_REGISTRY" p
    WHERE UPPER(TRIM(p."Code")) = UPPER(TRIM(ap."Code"))
);

PROMPT Relinking API assets to the unified registry...
ALTER TABLE "API_ASSET"
    DROP CONSTRAINT "FK_API_ASSET_API_PROJECT_ApiProjectId";

UPDATE "API_ASSET" a
SET a."ApiProjectId" = (
    SELECT p."Id"
    FROM "API_PROJECT" ap
    JOIN "PROJECT_REGISTRY" p
      ON UPPER(TRIM(p."Code")) = UPPER(TRIM(ap."Code"))
    WHERE ap."Id" = a."ApiProjectId"
);

ALTER TABLE "API_ASSET"
    RENAME COLUMN "ApiProjectId" TO "PublishingApplicationId";
ALTER INDEX "IX_API_ASSET_ApiProjectId"
    RENAME TO "IX_API_ASSET_PublishingApplicationId";
ALTER INDEX "IX_API_ASSET_Name_ApiProjectId"
    RENAME TO "IX_API_ASSET_Name_PublishingApplicationId";

ALTER TABLE "API_ASSET"
    ADD CONSTRAINT "FK_API_ASSET_PROJECT_REGISTRY_PublishingApplicationId"
    FOREIGN KEY ("PublishingApplicationId") REFERENCES "PROJECT_REGISTRY" ("Id");

UPDATE "PROJECT_REGISTRY" p
SET p."OwnershipType" = 'ThirdParty'
WHERE EXISTS (
    SELECT 1 FROM "API_ASSET" a
    WHERE a."PublishingApplicationId" = p."Id"
      AND a."OwnershipType" = 'ThirdParty'
);

UPDATE "PROJECT_REGISTRY" p
SET p."VendorId" = (
    SELECT v."Id"
    FROM "API_ASSET" a
    JOIN "VENDOR" v ON UPPER(TRIM(v."Name")) = UPPER(TRIM(a."VendorName"))
    WHERE a."PublishingApplicationId" = p."Id"
      AND a."OwnershipType" = 'ThirdParty'
    FETCH FIRST 1 ROW ONLY
)
WHERE p."OwnershipType" = 'ThirdParty';

CREATE INDEX "IX_PROJECT_REGISTRY_VendorId"
    ON "PROJECT_REGISTRY" ("VendorId");
ALTER TABLE "PROJECT_REGISTRY"
    ADD CONSTRAINT "FK_PROJECT_REGISTRY_VENDOR_VendorId"
    FOREIGN KEY ("VendorId") REFERENCES "VENDOR" ("Id");

PROMPT Updating security navigation...
UPDATE "SEC_SCREEN"
SET "Name" = 'Applications & Systems', "Route" = '/projects', "Icon" = 'AS'
WHERE "Code" = 'PROJECTS';

-- Keep the legacy screen and its permissions for audit, but hide its old route.
UPDATE "SEC_SCREEN" SET "IsActive" = 0 WHERE "Code" = 'API_PROJECTS';

INSERT INTO "SEC_SCREEN"
    ("Id", "Code", "Name", "Route", "Icon", "DisplayOrder",
     "IsActive", "CreatedAtUtc", "CreatedBy")
SELECT SYS_GUID(), 'VENDORS', 'Vendor Companies', '/admin/vendors', 'VN', 75,
       1, CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS TIMESTAMP),
       'migration:OracleRegistrySync'
FROM dual WHERE NOT EXISTS (
    SELECT 1 FROM "SEC_SCREEN" WHERE "Code" = 'VENDORS'
);

INSERT INTO "SEC_SCREEN"
    ("Id", "Code", "Name", "Route", "Icon", "DisplayOrder",
     "IsActive", "CreatedAtUtc", "CreatedBy")
SELECT SYS_GUID(), 'RESET_PASSWORD', 'Reset Password', '/admin/reset-password',
       'PW', 85, 1, CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS TIMESTAMP),
       'migration:OracleRegistrySync'
FROM dual WHERE NOT EXISTS (
    SELECT 1 FROM "SEC_SCREEN" WHERE "Code" = 'RESET_PASSWORD'
);

INSERT INTO "ROLE_PERMISSION"
    ("Id", "RoleId", "ScreenId", "PermissionId", "CreatedAtUtc", "CreatedBy")
SELECT SYS_GUID(), r."Id", s."Id", p."Id",
       CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS TIMESTAMP),
       'migration:OracleRegistrySync'
FROM "SEC_ROLE" r
CROSS JOIN "SEC_SCREEN" s
CROSS JOIN "SEC_PERMISSION" p
WHERE r."Code" IN ('SUPER_ADMIN', 'ADMIN')
  AND s."Code" IN ('VENDORS', 'RESET_PASSWORD')
  AND NOT EXISTS (
      SELECT 1 FROM "ROLE_PERMISSION" rp
      WHERE rp."RoleId" = r."Id" AND rp."ScreenId" = s."Id"
        AND rp."PermissionId" = p."Id"
  );

PROMPT Verifying rows and relationships...
DECLARE
    n NUMBER;
    expected_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM "API_ASSET" a
    WHERE NOT EXISTS (
        SELECT 1 FROM "PROJECT_REGISTRY" p
        WHERE p."Id" = a."PublishingApplicationId"
    );
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20020, 'An API asset has no publishing application.');
    END IF;

    SELECT COUNT(*) INTO n FROM "API_ASSET";
    SELECT COUNT(*) INTO expected_count FROM "AV_BAK_20261006_ASSET";
    IF n <> expected_count THEN
        RAISE_APPLICATION_ERROR(-20021, 'API asset count changed unexpectedly.');
    END IF;

    SELECT COUNT(*) INTO n FROM "PROJECT_REGISTRY";
    SELECT COUNT(*) INTO expected_count FROM "AV_BAK_20261006_PROJECT";
    IF n < expected_count THEN
        RAISE_APPLICATION_ERROR(-20022, 'Application registry count decreased.');
    END IF;

    SELECT COUNT(*) INTO n FROM "PROJECT_REGISTRY"
    WHERE "OwnershipType" = 'ThirdParty' AND "VendorId" IS NULL;
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20023, 'A third-party application has no vendor.');
    END IF;
END;
/

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261006044500_OracleRegistrySync', '10.0.10');
COMMIT;

PROMPT Oracle registry sync completed. Keep the AV_BAK_20261006_* tables until verified.
EXIT SUCCESS
