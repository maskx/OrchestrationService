-- ============================================
-- {0} Schema
-- {1} Hub
-- ============================================

IF(SCHEMA_ID('{0}') IS NULL)
BEGIN
    EXEC sp_executesql N'CREATE SCHEMA [{0}]'
END
GO

IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}_CommunicationSetting]') AND type in (N'U'))
BEGIN
	CREATE TABLE [{0}].[{1}_CommunicationSetting](
		[Key] [nvarchar](200) NOT NULL,
		[Value] [nvarchar](max) NOT NULL,
	 CONSTRAINT [PK_{1}_OrchestrationServiceSetting] PRIMARY KEY CLUSTERED 
	(
		[Key] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

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

IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[{0}].[{1}_FetchRule]') AND type in (N'U'))
BEGIN
	CREATE TABLE [{0}].[{1}_FetchRule](
		[Id] [uniqueidentifier] NOT NULL,
		[Name] [nvarchar](50) NOT NULL,
		[Concurrency] [int] NOT NULL,
		[What] [nvarchar](1500) NULL,
		[Scope] [nvarchar](1500) NULL,
		[CreatedTimeUtc] [datetime2](7) NOT NULL,
		[UpdatedTimeUtc] [datetime2](7) NOT NULL,
		[Description] [nvarchar](50) NULL,
		[FetchOrder] [nvarchar](1000) NULL,
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
	update [{0}].[{1}_Communication] WITH(READPAST)
	set [Status]=@Status,[LockedUntilUtc]=DATEADD(second,@MessageLockedSeconds,getutcdate()),[Context]=ISNULL(@Context,Context), [ResponseCode]=@ResponseCode,[ResponseContent]=ISNULL(@ResponseContent,ResponseContent),CompletedTime=(case when @Status=4 then getutcdate() else null end)
	where RequestId=@RequestId;
END
GO

CREATE OR ALTER PROCEDURE [{0}].[{1}_FetchCommunicationJob]
	@LockedBy nvarchar(100),
	@MessageLockedSeconds int,
	@MaxCount int
AS
BEGIN
	declare @Now datetime2=GETUTCDATE()
	
	update top(@MaxCount) T WITH(READPAST)
		set T.[Status]=1,T.[LockedUntilUtc]=DATEADD(second,@MessageLockedSeconds,@Now)
	output INSERTED.*
	FROM [{0}].[{1}_Communication] AS T
	where T.[status]<=1 and T.[LockedUntilUtc]<=@Now
END
GO

CREATE OR ALTER PROCEDURE [{0}].[{1}_BuildFetchCommunicationJobSP]
AS
BEGIN
	SET NOCOUNT ON;
	declare @LockedStatusCode nvarchar(10)=N'1'
	declare @FetchRuleId uniqueidentifier
	declare @Concurrency int
	declare @Scope nvarchar(1000)
	declare @What nvarchar(max)
	declare @TIndex int=1
	declare @Names nvarchar(1000)
	declare @Where nvarchar(2000)
	declare @Groupby nvarchar(1000)
	declare @On_Count nvarchar(1000)
	declare @Join_Count nvarchar(50)
	declare @Join_Index nvarchar(50)
	declare @LimitationWhere nvarchar(4000)=N''
	declare @CommonFetchOrder nvarchar(1000)
	declare @Join_FetchOrder nvarchar(1000)=N''
	declare @SQLText NVARCHAR(MAX)
	set @SQLText=N'CREATE OR ALTER PROCEDURE [{0}].[{1}_FetchCommunicationJob]'+'
	@LockedBy nvarchar(100),
	@MessageLockedSeconds int,
	@MaxCount int
AS
BEGIN
	declare @Now datetime2=GETUTCDATE()
	update TT WITH(READPAST)
		set TT.[Status]='+@LockedStatusCode+',TT.[LockedUntilUtc]=DATEADD(second,@MessageLockedSeconds,@Now)
	output INSERTED.*
	FROM (
		select top (@MaxCount) T.* from [{0}].[{1}_Communication] as T with(NOLOCK)'

	select @CommonFetchOrder=STRING_AGG(QUOTENAME([Name])+' '+[Order],',') 
	from [{0}].[{1}_CommunicationSetting]
	CROSS APPLY OPENJSON([Value]) 
		WITH (   
				[Name]   nvarchar(200) '$.field' ,  
				[Order]  nvarchar(200) '$.order'  
			)
	where [key]=N'FetchOrder'
	
	if @CommonFetchOrder is null set @CommonFetchOrder=N'CreateTime'

	declare rule_cursor CURSOR FORWARD_ONLY FOR SELECT [Concurrency],[What],[Scope],[FetchOrder] FROM [{0}].[{1}_FetchRule]
	open rule_cursor
	FETCH NEXT FROM rule_cursor INTO @Concurrency,@What,@Scope,@Join_FetchOrder
	while @@FETCH_STATUS=0
	BEGIN
		if @What is  null and @Scope is  null FETCH NEXT FROM rule_cursor INTO @Concurrency,@What,@Scope,@Join_FetchOrder
		else
		BEGIN
			set @Join_Count='[T'+CAST(@TIndex as nvarchar(10))+'_Count]'		
			set @Join_Index='[T'+CAST(@TIndex as nvarchar(10))+'_Index]'		
			
			if @Join_FetchOrder=N'[]' or @Join_FetchOrder is null 
				set @Join_FetchOrder=@CommonFetchOrder
			else 
				SELECT @Join_FetchOrder=STRING_AGG(QUOTENAME([Name])+' '+[Order],',') 
				from openjson(@Join_FetchOrder) 
					WITH (   
						[Name]   nvarchar(200) '$.field' ,  
						[Order]  nvarchar(200) '$.order'  
					)

			set @LimitationWhere=@LimitationWhere +'
			and (('+@Join_Count+'.Locked<'+CAST(@Concurrency as nvarchar(10))+' and '+@Join_Index+'.RowIndex=1) or '+@Join_Index+'.RowIndex is null)'
		
			if @What is null
			BEGIN
				SELECT 
					@Groupby=STRING_AGG(QUOTENAME(value),','),
					@On_Count=STRING_AGG(@Join_Count+'.'+QUOTENAME(value)+'=T.'+QUOTENAME(value),' and ')
				FROM OPENJSON(@Scope)
				set @SQLText=@SQLText+'
		left join(
			select COUNT(0) as Locked,'+@GroupBy+'
			from [{0}].[{1}_Communication] with(NOLOCK)
			where [status]='+@LockedStatusCode+' and [LockedUntilUtc]>@Now
			group by '+@Groupby+'
		) as '+@Join_Count+' on '+@On_Count+'
		left join(
			select InstanceId,ExecutionId,EventName,ROW_NUMBER() over(partition by '+@Groupby+' order by '+@Join_FetchOrder+') as RowIndex
			from [{0}].[{1}_Communication] with(NOLOCK)
			where [status]<='+@LockedStatusCode+' and [LockedUntilUtc]<@Now
		) as '+@Join_Index+' on T.InstanceId='+@Join_Index+'.InstanceId and T.EventName='+@Join_Index+'.EventName and T.ExecutionId='+@Join_Index+'.ExecutionId
		'
			END
			ELSE
			BEGIN
				SELECT 
					@Names=STRING_AGG(QUOTENAME([Name]),','),
					@Where=STRING_AGG(QUOTENAME([Name])+[Operator]+[Value],' and '),
					@On_Count=STRING_AGG(@Join_Count+'.'+QUOTENAME([Name])+'=T.'+QUOTENAME([Name]),' and ')
				FROM OPENJSON(@What)
				WITH (   
					[Name]   nvarchar(200) '$.name' ,  
					[Operator]  nvarchar(200) '$.operator' ,  
					[Value]     nvarchar(200) '$.value' 
				)
				if @Scope is null
				BEGIN
					set @SQLText=@SQLText+'
		left join(
			select COUNT(0) as Locked,'+@Names+'
			from [{0}].[{1}_Communication] with(NOLOCK)
			where [status]='+@LockedStatusCode+' and [LockedUntilUtc]>@Now and '+@Where+'
			group by '+@Names+'
		) as '+@Join_Count+' on '+@On_Count+'
		left join(
			select InstanceId,ExecutionId,EventName,ROW_NUMBER() over( order by '+@Join_FetchOrder+') as RowIndex
			from [{0}].[{1}_Communication] with(NOLOCK)
			where [status]<='+@LockedStatusCode+' and [LockedUntilUtc]<@Now and '+@Where+'
		) as '+@Join_Index+' on T.InstanceId='+@Join_Index+'.InstanceId and T.EventName='+@Join_Index+'.EventName and T.ExecutionId='+@Join_Index+'.ExecutionId'
				END
				ELSE
				BEGIN
					SELECT 
					@Groupby=STRING_AGG(QUOTENAME(value),','),
					@On_Count=@On_Count+' and '+STRING_AGG(@Join_Count+'.'+QUOTENAME(value)+'=T.'+QUOTENAME(value),' and ') 
				FROM OPENJSON(@Scope)
				set @SQLText=@SQLText+'
		left join(
			select COUNT(0) as Locked,'+@Names+','+@GroupBy+'
			from [{0}].[{1}_Communication] with(NOLOCK)
			where [status]='+@LockedStatusCode+' and T.[LockedUntilUtc]>@Now and '+@Where+'
			group by '+@Names+','+@Groupby+'
		) as '+@Join_Count+' on '+@On_Count+'
		left join(
			select InstanceId,ExecutionId,EventName,ROW_NUMBER() over(partition by '+@Groupby+' order by '+@Join_FetchOrder+') as RowIndex
			from [{0}].[{1}_Communication] with(NOLOCK)
			where [status]<='+@LockedStatusCode+' and T.[LockedUntilUtc]<@Now and '+@Where+'
		) as '+@Join_Index+' on T.InstanceId='+@Join_Index+'.InstanceId and T.EventName='+@Join_Index+'.EventName and T.ExecutionId='+@Join_Index+'.ExecutionId'
				END
			END	
			set @TIndex=@TIndex+1
			FETCH NEXT FROM rule_cursor INTO @Concurrency,@What,@Scope,@Join_FetchOrder
		END
	END
	close rule_cursor
	deallocate rule_cursor

		set @SQLText=@SQLText+'
		where T.[status]<='+@LockedStatusCode+' and T.[LockedUntilUtc]<@Now'
		+@LimitationWhere+'
		order by '+@CommonFetchOrder+' ) AS TT
	where TT.[status]<='+@LockedStatusCode+' and TT.[LockedUntilUtc]<@Now'
	set @SQLText=@SQLText+'
END'
	exec (@SQLText)
END
GO

CREATE OR ALTER PROCEDURE [{0}].[{1}_ConfigCommunicationSetting]
	@Key nvarchar(200),
	@Value nvarchar(max)
AS
BEGIN
	MERGE [{0}].[{1}_CommunicationSetting] with (serializable) TARGET
	USING (VALUES (@Key,@Value)) AS SOURCE ([Key],[Value])
	ON [Target].[Key] = [Source].[Key]
	WHEN MATCHED THEN UPDATE SET [Value]=@Value
	WHEN NOT MATCHED THEN INSERT ([Key],[Value]) VALUES (@Key,@Value);
END
GO