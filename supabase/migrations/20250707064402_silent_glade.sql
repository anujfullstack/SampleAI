-- SavedAINLQ Table Schema for Azure SQL Database
-- This table stores all saved queries, sample queries, and feedback

CREATE TABLE SavedAINLQ (
    -- Primary Key
    Id INT IDENTITY(1,1) PRIMARY KEY,
    
    -- User and Application Context
    UserId NVARCHAR(100) NOT NULL,
    ApplicationId INT NOT NULL,
    EventId INT NOT NULL,
    
    -- Query Information
    Label NVARCHAR(200) NOT NULL,
    NQLQuestion NVARCHAR(MAX) NOT NULL,
    GeneratedSQL NVARCHAR(MAX) NULL,
    
    -- Query Results and Performance
    HasData BIT NOT NULL DEFAULT 0,
    TokensUsed INT NOT NULL DEFAULT 0,
    
    -- Feedback System
    ThumbsUp BIT NULL, -- NULL = no feedback, 1 = thumbs up, 0 = thumbs down
    FeedbackTimestamp DATETIME2 NULL,
    
    -- Timestamps
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastUsed DATETIME2 NULL,
    DeletedAt DATETIME2 NULL,
    
    -- Usage Statistics
    UseCount INT NOT NULL DEFAULT 0,
    
    -- Request Tracking
    RequestId NVARCHAR(100) NULL,
    EventType NVARCHAR(50) NOT NULL DEFAULT 'NLQ_GENERATION',
    
    -- Additional Data
    Metadata NVARCHAR(MAX) NULL, -- JSON string for additional data
    
    -- Status Flags
    IsActive BIT NOT NULL DEFAULT 1,
    IsSampleQuery BIT NOT NULL DEFAULT 0,
    
    -- Categorization
    Category NVARCHAR(100) NULL -- For grouping sample queries
);

-- Indexes for Performance
CREATE INDEX IX_SavedAINLQ_UserId ON SavedAINLQ(UserId);
CREATE INDEX IX_SavedAINLQ_ApplicationId ON SavedAINLQ(ApplicationId);
CREATE INDEX IX_SavedAINLQ_CreatedAt ON SavedAINLQ(CreatedAt DESC);
CREATE INDEX IX_SavedAINLQ_UseCount ON SavedAINLQ(UseCount DESC);
CREATE INDEX IX_SavedAINLQ_ThumbsUp ON SavedAINLQ(ThumbsUp);
CREATE INDEX IX_SavedAINLQ_IsSampleQuery ON SavedAINLQ(IsSampleQuery);
CREATE INDEX IX_SavedAINLQ_IsActive ON SavedAINLQ(IsActive);
CREATE INDEX IX_SavedAINLQ_Category ON SavedAINLQ(Category);

-- Composite Indexes
CREATE INDEX IX_SavedAINLQ_User_App_Active ON SavedAINLQ(UserId, ApplicationId, IsActive);
CREATE INDEX IX_SavedAINLQ_Sample_Active_Category ON SavedAINLQ(IsSampleQuery, IsActive, Category);

-- Insert Sample Queries
INSERT INTO SavedAINLQ (
    UserId, ApplicationId, EventId, Label, NQLQuestion, GeneratedSQL, 
    HasData, TokensUsed, IsActive, IsSampleQuery, Category, UseCount
) VALUES 
-- Participant Queries
('system', 1, 1, 'Top 10 Active Participants', 'List Top 10 all active participants who have provided both an email and a phone number', 'SELECT TOP 10 * FROM Participants WHERE IsDeleted = 0 AND Email IS NOT NULL AND Phone IS NOT NULL', 1, 0, 1, 1, 'Participants', 0),

('system', 1, 1, 'Gmail Participants', 'Fetch top 10 participants who email is Gmail and who are not deleted', 'SELECT TOP 10 FirstName, LastName, Email, Phone FROM Participants WHERE IsDeleted = 0 AND Email LIKE ''%gmail%'' AND Email IS NOT NULL AND Phone IS NOT NULL', 1, 0, 1, 1, 'Participants', 0),

('system', 1, 1, 'LinkedIn Participants', 'Show top 10 participants who have LinkedIn profiles', 'SELECT TOP 10 * FROM Participants WHERE IsDeleted = 0 AND LinkedInProfile IS NOT NULL AND FirstName IS NOT NULL AND Email IS NOT NULL', 1, 0, 1, 1, 'Participants', 0),

('system', 1, 1, 'Participants by Application', 'Give me a top 10 maximum count of Non deleted Participants in Each ApplicationInstanceId', 'SELECT TOP 10 ApplicationInstanceId, COUNT(*) as ParticipantCount FROM Participants WHERE IsDeleted = 0 GROUP BY ApplicationInstanceId ORDER BY ParticipantCount DESC', 1, 0, 1, 1, 'Analytics', 0),

('system', 1, 1, 'Outlook Email Participants', 'Retrieve top 10 participants whose email contains outlook and are active', 'SELECT TOP 10 * FROM Participants WHERE Email LIKE ''%outlook%'' AND IsDeleted = 0', 1, 0, 1, 1, 'Participants', 0),

-- Advanced Queries
('system', 1, 1, 'Latest Participants with A Names', 'List Top 20 latest added participants who are not deleted and whose first or last name starts with A', 'SELECT TOP 20 * FROM Participants WHERE IsDeleted = 0 AND (FirstName LIKE ''A%'' OR LastName LIKE ''A%'') AND Phone IS NOT NULL AND Email IS NOT NULL AND ApplicationId = 1 ORDER BY CreatedDate DESC', 1, 0, 1, 1, 'Advanced', 0),

('system', 1, 1, 'Recent Participants', 'Which participants joined in the last 6 months', 'SELECT * FROM Participants WHERE JoinDate >= DATEADD(MONTH, -6, GETDATE()) AND IsDeleted = 0', 1, 0, 1, 1, 'Time-based', 0),

('system', 1, 1, 'Participants with Complete Info', 'Show participants with both email and phone number', 'SELECT * FROM Participants WHERE Email IS NOT NULL AND Phone IS NOT NULL AND IsDeleted = 0', 1, 0, 1, 1, 'Data Quality', 0),

-- Analytics Queries
('system', 1, 1, 'Monthly Join Statistics', 'How many participants joined per month in 2024', 'SELECT MONTH(JoinDate) as Month, COUNT(*) as JoinCount FROM Participants WHERE YEAR(JoinDate) = 2024 AND IsDeleted = 0 GROUP BY MONTH(JoinDate) ORDER BY Month', 1, 0, 1, 1, 'Analytics', 0),

('system', 1, 1, 'Application Distribution', 'Show participant distribution across applications', 'SELECT ApplicationId, COUNT(*) as ParticipantCount FROM Participants WHERE IsDeleted = 0 GROUP BY ApplicationId ORDER BY ParticipantCount DESC', 1, 0, 1, 1, 'Analytics', 0);

-- Add more sample queries as needed