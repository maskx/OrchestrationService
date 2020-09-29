-- ============================================
-- {0} Schema
-- {1} Hub
-- ============================================

------------------- BEGIN CREATE SCHEMA
IF(SCHEMA_ID('{0}') IS NULL)
BEGIN
    EXEC sp_executesql N'CREATE SCHEMA [{0}]'
END
------------------- END CREATE SCHEMA
------------------- BEGIN CREATE Communication table
IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}_Communication]') AND type in (N'U'))
BEGIN
	CREATE TABLE [{0}].[{1}_Communication](
		[InstanceId] [nvarchar](50) NOT NULL,
		[ExecutionId] [nvarchar](50) NOT NULL,
		[EventName] [nvarchar](50) NOT NULL,
		[Processor] [nvarchar](50) NULL,
		[RequestTo] [nvarchar](50) NULL,
		[RequestOperation] [nvarchar](50) NULL,
		[RequestContent] [nvarchar](max) NULL,
		[RequestProperty] [nvarchar](max) NULL,
		[Status] [int] NULL,
		[LockedUntilUtc] [datetime2](7) NULL,
		[ResponseContent] [nvarchar](max) NULL,
		[ResponseCode] [int] NULL,
		[RequestId] [nvarchar](50) NULL,
		[CompletedTime] [datetime2](7) NULL,
		[CreateTime] [datetime2](7) NULL,
		[Context] [nvarchar](max) NULL,
	 CONSTRAINT [PK_{1}_Communication] PRIMARY KEY CLUSTERED 
	(
		[InstanceId] ASC,
		[ExecutionId] ASC,
		[EventName] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
------------------- END CREATE Communication table

------------------- BEGIN CREATE FetchRule table
IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}_FetchRule]') AND type in (N'U'))
BEGIN
	CREATE TABLE [{0}].[{1}_FetchRule](
		[Id] [uniqueidentifier] NOT NULL,
		[Name] [nvarchar](50) NOT NULL,
		[What] [nvarchar](1500) NOT NULL,
		[CreatedTimeUtc] [datetime2](7) NOT NULL,
		[UpdatedTimeUtc] [datetime2](7) NOT NULL,
		[Description] [nvarchar](50) NULL,
	 CONSTRAINT [PK_{1}_FetchRule] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY]
	
	ALTER TABLE [{0}].[{1}_FetchRule] ADD  CONSTRAINT [DF_{1}_FetchRule_Id]  DEFAULT (newsequentialid()) FOR [Id]
	ALTER TABLE [{0}].[{1}_FetchRule] ADD  CONSTRAINT [DF_{1}_FetchRule_CreatedTimeUtc]  DEFAULT (getutcdate()) FOR [CreatedTimeUtc]
	ALTER TABLE [{0}].[{1}_FetchRule] ADD  CONSTRAINT [DF_{1}_FetchRule_UpdatedTimeUtc]  DEFAULT (getutcdate()) FOR [UpdatedTimeUtc]
END
GO
------------------- END CREATE FetchRule table

------------------- BEGIN CREATE FetchRuleLimitation table
IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}_FetchRuleLimitation]') AND type in (N'U'))
BEGIN
	CREATE TABLE [{0}].[{1}_FetchRuleLimitation](
		[Id] [uniqueidentifier] NOT NULL,
		[FetchRuleId] [uniqueidentifier] NOT NULL,
		[Concurrency] [int] NOT NULL,
		[Scope] [nvarchar](1500) NULL,
	 CONSTRAINT [PK_{1}_FetchRuleWhat] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY]
	ALTER TABLE [{0}].[{1}_FetchRuleLimitation] ADD  CONSTRAINT [DF_{1}_FetchRuleWhat_Id]  DEFAULT (newsequentialid()) FOR [Id]
END
GO
------------------- END CREATE FetchRuleLimitation table
------------------- BEGION CREATE UpdateCommunication
CREATE OR ALTER PROCEDURE [{0}].[{1}_UpdateCommunication]
	@RequestId nvarchar(50),
	@Status int,
	@ResponseCode int,
	@MessageLockedSeconds int,
	@ResponseContent nvarchar(max)=null,
	@Context nvarchar(max)=NULL
AS
BEGIN
	SET NOCOUNT ON;
	update NoRule_Communication WITH(READPAST)
	set [Status]=@Status,[LockedUntilUtc]=DATEADD(second,@MessageLockedSeconds,getutcdate()),[Context]=ISNULL(@Context,Context), [ResponseCode]=@ResponseCode,[ResponseContent]=ISNULL(@ResponseContent,ResponseContent),CompletedTime=(case when @Status=4 then getutcdate() else null end)
	where RequestId=@RequestId;
END
GO
------------------- END CREATE UpdateCommunication
------------------- BEGIN CREATE DEFAULT FetchCommunicationJob
CREATE OR ALTER PROCEDURE [{0}].[{1}_FetchCommunicationJob]
	@LockedBy nvarchar(100),
	@MessageLockedSeconds int,
	@MaxCount int
AS
BEGIN
	declare @Count int=0;
	
	update top(@MaxCount-@Count) T WITH(READPAST)
		set T.[Status]=4,T.[LockedUntilUtc]=DATEADD(second,@MessageLockedSeconds,getutcdate())
	output INSERTED.*
	FROM [{0}].[{1}_Communication] AS T
	where T.[status]<=4 and T.[LockedUntilUtc]<=getutcdate()
END
GO
------------------- ENDCREATE DEFAULT FetchCommunicationJob
------------------- BEGIN CREATE BuildFetchCommunicationJobSP
CREATE OR ALTER PROCEDURE [{0}].[{1}_BuildFetchCommunicationJobSP]
	@SchemaName nvarchar(20),
	@HubName nvarchar(20)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	declare @CommunicationTableFullName nvarchar(100)=@SchemaName+'.'+@HubName+N'_Communication'
	declare @LockedStatusCode nvarchar(10)=N'4'
	declare @FetchRuleId uniqueidentifier
	declare @What nvarchar(max)
	declare @Names nvarchar(1000)
	declare @Where nvarchar(2000)
	declare @LimitationWhere nvarchar(4000)
	declare @Concurrency int
	declare @Scope nvarchar(1000)
	declare @on  nvarchar(1000)
	declare @TIndex int
	declare @InnerTable nvarchar(10)
	declare @Inner nvarchar(max)
	declare @SQLText NVARCHAR(MAX)
	set @SQLText=N'CREATE OR ALTER PROCEDURE '+@SchemaName+'.'+@HubName+N'_FetchCommunicationJob'+'
	@LockedBy nvarchar(100),
	@MessageLockedSeconds int,
	@MaxCount int
AS
BEGIN
	declare @Count int=0;
	'
	
	declare rule_cursor cursor Forward_Only for select Id,What from [{0}].[{1}_FetchRule]
	open rule_cursor
	fetch next from rule_cursor into @FetchRuleId,@What
	while @@FETCH_STATUS=0
	begin
		set @Inner=N''
		SELECT 
			@Names=STRING_AGG([Name],','),
			@Where=STRING_AGG('T.'+[Name]+[Operator]+[Value],' and ')
		FROM OPENJSON(@What)
		WITH (   
			[Name]   nvarchar(200) '$.name' ,  
			[Operator]     nvarchar(200) '$.operator' ,  
			[Value]     nvarchar(200) '$.value' 
		) 
		if @LimitationWhere is null set @LimitationWhere=  '('+@Where+')'
		else set @LimitationWhere= @LimitationWhere+ N' or ('+@Where+')'

		declare limitation_cursor cursor forward_only for (
			select
				ROW_NUMBER() OVER(ORDER BY Concurrency ASC) as rownumber,
				Concurrency,
				Scope 
			from [{0}].[{1}_FetchRuleLimitation] 
			where FetchRuleId=@FetchRuleId
		)
		open limitation_cursor
		fetch next from limitation_cursor into @TIndex,@Concurrency,@Scope		
		while @@FETCH_STATUS=0
		begin
			
			set @InnerTable='T'+CAST(@TIndex as nvarchar(10))
			SELECT 
				@on=STRING_AGG(@InnerTable+'.'+[Name]+'=T.'+[Name],' and ')
			FROM OPENJSON(@What)
			WITH ([Name]   nvarchar(200) '$.name')
			if @Scope is null
			begin				
				set @Inner=@Inner+'
		inner join (select COUNT(case when T.[status]='+@LockedStatusCode+' and T.[LockedUntilUtc]>getutcdate() then 1 else null end) as Locked,'+@Names+' from '+@CommunicationTableFullName+' as T WITH(NOLOCK) where '+ @Where +' group by '+@Names+') as '+@InnerTable+' on '+@on
			end
			else
			begin
				select @on=@on+' and '+STRING_AGG(@InnerTable+'.['+value+']=T.['+value+']',' and ')  from string_split(@Scope,',')
				set @Inner=@Inner+'
		inner join (select COUNT(case when T.[status]='+@LockedStatusCode+' and T.[LockedUntilUtc]>getutcdate() then 1 else null end) as Locked,'+@Names+','+@Scope+' from '+@CommunicationTableFullName+' as T WITH(NOLOCK) where '+ @Where +'group by '+@Names+','+@Scope+') as '+@InnerTable+' on '+@on
			end
			fetch next from limitation_cursor into @TIndex,@Concurrency,@Scope
		end
		close limitation_cursor
		deallocate limitation_cursor

		set @SQLText=@SQLText+
				'
	update top(1) T WITH(READPAST) set T.[Status]='+@LockedStatusCode+',T.[LockedUntilUtc]=DATEADD(second,@MessageLockedSeconds,getutcdate())
	output INSERTED.*
	FROM '+@CommunicationTableFullName+' AS T '+@Inner+'
	where T.[status]<='+@LockedStatusCode+' and T.[LockedUntilUtc]<=getutcdate() and '+@Where+'
	set @Count=@Count+@@ROWCOUNT
	if @Count>=@MaxCount return
	'
		fetch next from rule_cursor into @FetchRuleId,@What
	end
	close rule_cursor
	deallocate rule_cursor

	set @SQLText=@SQLText+'
	update top(@MaxCount-@Count) T WITH(READPAST)
		set T.[Status]='+@LockedStatusCode+',T.[LockedUntilUtc]=DATEADD(second,@MessageLockedSeconds,getutcdate())
	output INSERTED.*
	FROM '+@CommunicationTableFullName+' AS T
	where T.[status]<='+@LockedStatusCode+' and T.[LockedUntilUtc]<=getutcdate() '
	if @LimitationWhere is not null
	begin
		set @SQLText=@SQLText+' and not ('+@LimitationWhere+')'
	end
	set @SQLText=@SQLText+'
END'
	exec (@SQLText)
END
GO
------------------- END CREATE BuildFetchCommunicationJobSP

------------------- BEGIN CRATE Trigger for table FetchRule
CREATE OR ALTER TRIGGER [{0}].[Trigger_{1}_FetchRule_BuildStoredProcedures] 
   ON  [{0}].[{1}_FetchRule]
   AFTER INSERT,DELETE,UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

   exec [{0}].[{1}_BuildFetchCommunicationJobSP]
		@SchemaName = N'{0}',
		@HubName = N'{1}'

END
GO

ALTER TABLE [{0}].[{1}_FetchRule] ENABLE TRIGGER [Trigger_{1}_FetchRule_BuildStoredProcedures]
GO
------------------- END CRATE Trigger for table FetchRule

------------------- BEGIN CRATE Trigger for table FetchRuleLimitation
CREATE OR ALTER TRIGGER [{0}].[Trigger_{1}_FetchRuleLimitation_BuildStoredProcedures] 
   ON  [{0}].[{1}_FetchRuleLimitation]
   AFTER INSERT,DELETE,UPDATE
AS 
BEGIN
	SET NOCOUNT ON;

   exec [{0}].[{1}_BuildFetchCommunicationJobSP]
		@SchemaName = N'{0}',
		@HubName = N'{1}'

END
GO

ALTER TABLE [{0}].[{1}_FetchRuleLimitation] ENABLE TRIGGER [Trigger_{1}_FetchRuleLimitation_BuildStoredProcedures]
GO
------------------- END CRATE Trigger for table FetchRule