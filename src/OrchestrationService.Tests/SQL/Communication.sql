
CREATE TABLE [dbo].[Communication](
	[InstanceId] [nvarchar](50) NOT NULL,
	[ExecutionId] [nvarchar](50) NOT NULL,
	[EventName] [nvarchar](50) NOT NULL,
	[SubscriptionId] [nvarchar](50) NULL,
	[ServiceType] [nvarchar](50) NULL,
	[AvailabilityZone] [nvarchar](50) NULL,
	[ManagementUnit] [nvarchar](50) NULL,
	[ResourceId] [nvarchar](50) NULL,
	[Status] [nvarchar](50) NULL,
	[LockedUntilUtc] [datetime2](7) NULL,
	[RequestMethod] [nvarchar](50) NULL,
	[RequestBody] [nvarchar](max) NULL,
	[Operation] [nvarchar](50) NULL,
	[ResponseContent] [nvarchar](max) NULL,
	[ResponseCode] [int] NULL,
	[RequestId] [nvarchar](50) NULL,
	[CorrelationId] [nvarchar](50) NULL,
 CONSTRAINT [PK_Communication] PRIMARY KEY CLUSTERED 
(
	[InstanceId] ASC,
	[ExecutionId] ASC,
	[EventName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO