-- Normalize legacy APP_USER.Role values to the UserRole enum spellings.
-- SEC_ROLE.Code values intentionally remain uppercase security codes.
WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK
SET DEFINE OFF
SET ECHO OFF

PROMPT Checking user role values...
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM user_tables
    WHERE table_name = 'AV_BAK_20261006_USER_ROLE';
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20030, 'Role backup table already exists; inspect before rerunning.');
    END IF;

    SELECT COUNT(*) INTO n FROM "APP_USER"
    WHERE "Role" NOT IN (
        'Admin', 'ApiOwner', 'Tester', 'Viewer', 'SuperAdmin',
        'ADMIN', 'API_OWNER', 'TESTER', 'VIEWER', 'SUPER_ADMIN'
    );
    IF n <> 0 THEN
        RAISE_APPLICATION_ERROR(-20031, 'An APP_USER role has no defined enum mapping.');
    END IF;
END;
/

PROMPT Backing up current user role values...
CREATE TABLE "AV_BAK_20261006_USER_ROLE" AS
SELECT "Id", "Username", "Role" FROM "APP_USER";

UPDATE "APP_USER"
SET "Role" = CASE "Role"
    WHEN N'ADMIN' THEN N'Admin'
    WHEN N'API_OWNER' THEN N'ApiOwner'
    WHEN N'TESTER' THEN N'Tester'
    WHEN N'VIEWER' THEN N'Viewer'
    WHEN N'SUPER_ADMIN' THEN N'SuperAdmin'
    ELSE "Role"
END
WHERE "Role" IN (N'ADMIN', N'API_OWNER', N'TESTER', N'VIEWER', N'SUPER_ADMIN');

DECLARE
    actual_count NUMBER;
    backup_count NUMBER;
    invalid_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO actual_count FROM "APP_USER";
    SELECT COUNT(*) INTO backup_count FROM "AV_BAK_20261006_USER_ROLE";
    SELECT COUNT(*) INTO invalid_count FROM "APP_USER"
    WHERE "Role" NOT IN (N'Admin', N'ApiOwner', N'Tester', N'Viewer', N'SuperAdmin');

    IF actual_count <> backup_count OR invalid_count <> 0 THEN
        RAISE_APPLICATION_ERROR(-20032, 'Role normalization verification failed.');
    END IF;
END;
/

COMMIT;
PROMPT User roles now match the UserRole enum. Keep the backup table for recovery.
EXIT SUCCESS
