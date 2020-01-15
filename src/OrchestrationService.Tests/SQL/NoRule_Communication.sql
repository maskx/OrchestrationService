
CREATE TABLE [dbo].[NoRule_Communication](
	[InstanceId] [nvarchar](50) NOT NULL,
	[ExecutionId] [nvarchar](50) NOT NULL,
	[EventName] [nvarchar](50) NOT NULL,
	[Processor] [nvarchar](50) NULL,
	[RequestTo] [nvarchar](50) NULL,
	[RequestOperation] [nvarchar](50) NULL,
	[RequsetContent] [nvarchar](max) NULL,
	[RequestProperty] [nvarchar](max) NULL,
	[Status] [nvarchar](50) NULL,
	[LockedUntilUtc] [datetime2](7) NULL,
	[ResponseContent] [nvarchar](max) NULL,
	[ResponseCode] [int] NULL,
	[RequestId] [nvarchar](50) NULL,
	[CompletedTime] [datetime2](7) NULL,
	[CreateTime] [datetime2](7) NULL,
	[NextFetchTime] [datetime2](7) NULL,
 CONSTRAINT [PK_NoRule_Communication] PRIMARY KEY CLUSTERED 
(
	[InstanceId] ASC,
	[ExecutionId] ASC,
	[EventName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
