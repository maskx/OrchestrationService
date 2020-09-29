-- ============================================
-- {0} Schema
-- {1} Hub
-- ============================================

DROP TRIGGER IF EXISTS [{0}].[Trigger_{1}_FetchRule_BuildStoredProcedures]
DROP TRIGGER IF EXISTS [{0}].[Trigger_{1}_FetchRuleLimitation_BuildStoredProcedures]
DROP PROCEDURE IF EXISTS [{0}].[{1}_BuildFetchCommunicationJobSP]
DROP TABLE IF EXISTS [{0}].[{1}_Communication]
DROP TABLE IF EXISTS [{0}].[{1}_FetchRule]
DROP TABLE IF EXISTS [{0}].[{1}_FetchRuleLimitation]
if {0}<>'dbo' DROP SCHEMA IF EXISTS [{0}]