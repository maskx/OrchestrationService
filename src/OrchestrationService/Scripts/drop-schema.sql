﻿-- ============================================
-- {0} Schema
-- {1} Hub
-- ============================================


DROP PROCEDURE IF EXISTS [{0}].[{1}_BuildFetchCommunicationJobSP]
DROP PROCEDURE IF EXISTS [{0}].[{1}_FetchCommunicationJob]
DROP PROCEDURE IF EXISTS [{0}].[{1}_UpdateCommunication]
DROP PROCEDURE IF EXISTS [{0}].[{1}_ConfigCommunicationSetting]
DROP TABLE IF EXISTS [{0}].[{1}_Communication]
DROP TABLE IF EXISTS [{0}].[{1}_FetchRule]
DROP TABLE IF EXISTS [{0}].[{1}_CommunicationSetting]