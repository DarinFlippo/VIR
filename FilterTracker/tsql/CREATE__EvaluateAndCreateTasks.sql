-- =======================================================
-- Create Stored Procedure Template for Azure SQL Database
-- ======================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:      <Author, , Name>
-- Create Date: <Create Date, , >
-- Description: <Description, , >
-- =============================================
CREATE PROCEDURE EvaluateAndCreateTasks
AS
BEGIN
    SET NOCOUNT ON
	DECLARE @now DATETIME
	DECLARE @task_type_id INTEGER
	DECLARE @task_id INTEGER
	DECLARE @org_id INTEGER
	DECLARE @prev_org_id INTEGER
	DECLARE @patient_filter_id INTEGER
	DECLARE @patient_id INTEGER
	DECLARE @org_admin_userid INTEGER
	DECLARE @last_contact DATETIME

	SELECT @now = CONVERT(date, GETUTCDATE())

    -- Contact Attempt Due
	DECLARE csr1 CURSOR FAST_FORWARD FOR 
	SELECT a.PatientId, a.Id as PatientFilterId, a.OrganizationId 
	FROM PatientFilters a
	LEFT OUTER JOIN Patients b on a.PatientId = b.Id
	LEFT OUTER JOIN PatientContactAttempts c on c.PatientFilterId = a.Id
	WHERE b.Active = 1
		AND a.ActualRemovalDate IS NULL
		AND NOT EXISTS(SELECT * FROM Tasks WHERE TaskTypeId = 8 AND PatientFilterId = a.Id AND ClosedDate IS NULL)
	ORDER BY a.OrganizationId, a.PatientId, a.Id
	
	FETCH NEXT FROM csr1   
	INTO @patient_id, @patient_filter_id, @org_id

	SET @prev_org_id = @org_id

	WHILE @@FETCH_STATUS = 0  
	BEGIN
		IF @org_id <> @prev_org_id BEGIN
			SELECT TOP 1 @org_admin_userid = a.Id 
			FROM Users a
			JOIN UserRoles b on a.Id = b.UserId
			JOIN Roles c on c.Id = b.RoleId
			WHERE c.Name = 'OrganizationAdmins'

			SET @prev_org_id = @org_id
		END

		SELECT @last_contact = MAX(CreateTimestamp) FROM PatientContactAttempts
		WHERE PatientFilterId = @patient_filter_id

		IF @last_contact IS NOT NULL AND DATEADD(DD, 30, @last_contact) >= @now BEGIN

			INSERT INTO dbo.Tasks (TaskTypeId, OrganizationId, PatientId, PatientFilterId, AssignedUserId, TargetCloseDate, CreateTimestamp, CreateUserId, UpdateTimestamp, UpdateUserId)
			VALUES(8, @org_id, @patient_id, @patient_filter_id, @org_admin_userid, DATEADD(DD, 10, @now), @now, 1, @now, 1)
		
		END
		
		SELECT @org_id = NULL, @patient_id = NULL, @patient_filter_id = NULL, @task_id = NULL, @task_type_id = NULL

		FETCH NEXT FROM csr1   
		INTO @patient_id, @patient_filter_id, @org_id
	END

	CLOSE csr1;
	DEALLOCATE csr1;
	
	
	-- Register Letter Sent

	-- Retrieval Date Overdue
	DECLARE csr3 CURSOR FAST_FORWARD FOR 
	SELECT a.PatientId, a.Id as PatientFilterId, a.OrganizationId FROM PatientFilters a
	JOIN Patients b on a.PatientId = b.Id
	WHERE b.Active = 1
		AND a.ActualRemovalDate IS NULL
		AND (a.TargetRemovalDate >= @now OR DATEADD(dd, 90, a.ProcedureDate) >= @now)
		AND NOT EXISTS(SELECT * FROM Tasks WHERE TaskTypeId = 1 AND PatientFilterId = a.Id AND ClosedDate IS NULL)
	ORDER BY a.OrganizationId, a.PatientId, a.Id

	OPEN csr3
	
	FETCH NEXT FROM csr3   
	INTO @patient_id, @patient_filter_id, @org_id

	SET @prev_org_id = @org_id
  
	WHILE @@FETCH_STATUS = 0  
	BEGIN
		IF @org_id <> @prev_org_id BEGIN
			SELECT TOP 1 @org_admin_userid = a.Id 
			FROM Users a
			JOIN UserRoles b on a.Id = b.UserId
			JOIN Roles c on c.Id = b.RoleId
			WHERE c.Name = 'OrganizationAdmins'

			SET @prev_org_id = @org_id
		END

		INSERT INTO dbo.Tasks (TaskTypeId, OrganizationId, PatientId, PatientFilterId, AssignedUserId, TargetCloseDate, CreateTimestamp, CreateUserId, UpdateTimestamp, UpdateUserId)
		VALUES(1, @org_id, @patient_id, @patient_filter_id, @org_admin_userid, DATEADD(DD, 10, @now), @now, 1, @now, 1)

		SELECT @org_id = NULL, @patient_id = NULL, @patient_filter_id = NULL, @task_id = NULL, @task_type_id = NULL

		FETCH NEXT FROM csr3 
		INTO @patient_id, @patient_filter_id, @org_id
	END

	CLOSE csr3;
	DEALLOCATE csr3;

END
GO
