-- Run as an authorized Oracle DBA after replacing all placeholders.
-- Review tablespace names and password policy with the bank DBA team.

CREATE USER APIVAULT
  IDENTIFIED BY "REPLACE_WITH_STRONG_PASSWORD"
  DEFAULT TABLESPACE USERS
  TEMPORARY TABLESPACE TEMP
  QUOTA UNLIMITED ON USERS;

GRANT CREATE SESSION TO APIVAULT;
GRANT CREATE TABLE TO APIVAULT;
GRANT CREATE SEQUENCE TO APIVAULT;
GRANT CREATE VIEW TO APIVAULT;
GRANT CREATE PROCEDURE TO APIVAULT;
GRANT CREATE TRIGGER TO APIVAULT;

-- Do not grant DBA, SYSDBA, or broad dictionary privileges.
-- After migrations are complete, the DBA may separate migration and runtime users
-- and reduce the runtime user to the exact DML privileges required.
