using MeetApp.FunctionBiz;
using static MeetApp.AzureOpenAIHub.Model.EventModels;
using System.Globalization;
using System.Data.SqlClient;
using Dapper;
using StructureMap.Query;
using MeetApp.AzureOpenAIHub.Enums;

namespace MeetApp.AzureOpenAIHub.DataManager
{
    public class DataManagerHelper
    {
        // Configuration flags
        private readonly bool EnableNotes = false;
        private readonly bool EnableImageArchive = false;
        private readonly bool EnableGamification = true;
        private readonly bool EnableRegistrationSystem = false;
        private readonly bool EnableEventWebPage = false;
        private readonly bool EnableMessagingfeature = true;
        private readonly bool EnableMyExtraInformation = true;
        private readonly bool EnableActivityFeeds = true;
        private readonly bool EnableWebApp = true;
        private readonly bool EnableParticipantList = true;
        private readonly bool EnableUserEditedParticipant = true;

        public async Task FeedEventInformation(EventGenerationRequest request, EventGenerationResponse model)
        {
            model.ApplicationId = 1;
            model.CreatedBy = "Ventla-AI";

            var dal = new ShardDal(MobilizeIT.Common.Settings.Instance.ConnectionString);
            var shards = await dal.GetShardsFromDBThrougMemoryCache();
            var shard = dal.GetShardForAppId(model.ApplicationId, shards);
            var myMeetAppShardsConnectionString = shard.ConnectionString;

            await using var connection = new SqlConnection(myMeetAppShardsConnectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                await CreateEventAsync(connection, transaction, model);
                await CreateInformationsAsync(connection, transaction, model);
                await CreateProgramsAsync(connection, transaction, model);
                await CreateNotificationsAsync(connection, transaction, model);
                await CreateProgramInformationAssociationsAsync(connection, transaction, model);
                await CreateGamificationAllRulesEventsAsync(connection, transaction, model);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private async Task CreateEventAsync(SqlConnection connection, SqlTransaction transaction, EventGenerationResponse model)
        {
            var dateFormat = "MMMM dd, yyyy hh:mm tt";
            if (!DateTime.TryParseExact(model.Event.Date, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                throw new ArgumentException("Invalid date format");

            var timeZone = TimeZoneInfo.Local;
            var windowsTimeZone = timeZone.Id;
            var ianaTimeZone = windowsTimeZone switch
            {
                "India Standard Time" => "Asia/Kolkata",
                _ => "UTC"
            };
            var locationType = 1;
            var displayParticipantsInApp = true;
            var appInstance = new ApplicationInstanceModel
            {
                Name = model.Event.Title,
                Description = model.Event.Description,
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = model.CreatedBy,
                ApplicationId = model.ApplicationId,
                EnableNotes = EnableNotes,
                EnableImageArchive = EnableImageArchive,
                EnableGamification = EnableGamification,
                EnableRegistrationSystem = EnableRegistrationSystem,
                EnableEventWebPage = EnableEventWebPage,
                EnableMessagingfeature = EnableMessagingfeature,
                EnableMyExtraInformation = EnableMyExtraInformation,
                EnableActivityFeeds = EnableActivityFeeds,
                EnableWebApp = EnableWebApp,
                EnableParticipantList = EnableParticipantList,
                EnableUserEditedParticipant = EnableUserEditedParticipant,
                StartDate = parsedDate,
                EndDate = parsedDate.AddHours(8), // Full day event
                IanaTimeZone = ianaTimeZone,
                WindowsTimeZone = windowsTimeZone,
                ApplicationCode = string.Empty,
                LocationType = locationType,
                DisplayParticipantsInApp = displayParticipantsInApp,
            };

            string insertEventSql = @"
                INSERT INTO ApplicationInstance (
                    Name, ApplicationId, Description, CreatedUtc, CreatedBy,
                    EnableNotes, EnableImageArchive, EnableGamification, EnableRegistrationSystem,
                    EnableEventWebPage, EnableMessagingfeature, EnableMyExtraInformation,
                    EnableActivityFeeds, EnableWebApp, EnableParticipantList, EnableUserEditedParticipant,
                    StartDate, EndDate, IanaTimeZone, WindowsTimeZone, ApplicationCode ,LocationType ,DisplayParticipantsInApp
                ) VALUES (
                    @Name, @ApplicationId, @Description, @CreatedUtc, @CreatedBy,
                    @EnableNotes, @EnableImageArchive, @EnableGamification, @EnableRegistrationSystem,
                    @EnableEventWebPage, @EnableMessagingfeature, @EnableMyExtraInformation,
                    @EnableActivityFeeds, @EnableWebApp, @EnableParticipantList, @EnableUserEditedParticipant,
                    @StartDate, @EndDate, @IanaTimeZone, @WindowsTimeZone, @ApplicationCode ,@LocationType ,@DisplayParticipantsInApp
                );
                SELECT CAST(SCOPE_IDENTITY() AS int);";

            model.Event.EventId = await connection.ExecuteScalarAsync<int>(insertEventSql, appInstance, transaction);
        }

        private async Task CreateInformationsAsync(SqlConnection connection, SqlTransaction transaction, EventGenerationResponse model)
        {
            var informationContentType = "Normal";
            foreach (var info in model.Informations ?? new())
            {
                // Step 1: Insert into CustomCategory (Information)
                var insertInformationSql = @"
                    INSERT INTO CustomCategory (
                        Name, Description, ContentType, Created, Modified,
                        IsDeleted, Address, City, Latitude, Longitude, ApplicationId ,ContentTypeV2
                    ) VALUES (
                        @Name, @Description, @ContentType, @Created, @Modified,
                        0, '', '', 0, 0,@ApplicationId , @ContentTypeV2
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                info.InformationId = await connection.ExecuteScalarAsync<int>(insertInformationSql, new
                {
                    Name = info.Title,
                    Description = info.Description,
                    ContentType = informationContentType,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow,
                    ApplicationId = model.ApplicationId,
                    ContentTypeV2 = info.ContentTypeV2
                }, transaction);

                // Step 2: Link to ApplicationInstance_CustomCategory
                var linkSql = @"
                    INSERT INTO ApplicationInstance_CustomCategory (
                        ApplicationInstanceId, CustomCategoryId, IsDeleted, ModifiedUtc, SortOrder
                    ) VALUES (
                        @ApplicationInstanceId, @CustomCategoryId, 0, @ModifiedUtc, @SortOrder
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                info.ApplicationInstanceCustomCategoryId = await connection.ExecuteScalarAsync<int>(linkSql, new
                {
                    ApplicationInstanceId = model.Event.EventId,
                    CustomCategoryId = info.InformationId,
                    ModifiedUtc = DateTime.UtcNow,
                    SortOrder = -1
                }, transaction);
            }
        }

        private async Task CreateProgramsAsync(SqlConnection connection, SqlTransaction transaction, EventGenerationResponse model)
        {
            foreach (var program in model.Programs ?? new())
            {
                // Parse program date and times
                var programDate = DateTime.TryParseExact(program.Date, "MMMM dd, yyyy hh:mm tt",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedProgramDate)
                    ? parsedProgramDate : DateTime.Now;

                var startTime = ParseTimeString(program.StartTime, programDate);
                var endTime = ParseTimeString(program.EndTime, programDate);

                // 1. Insert Program
                var insertProgramSql = @"
                    INSERT INTO Program (
                        ApplicationInstanceId, Title, Description, StartDate, EndDate, 
                        Created, Modified, IsDeleted, Address, City, Latitude, Longitude, Location
                    ) VALUES (
                        @ApplicationInstanceId, @Title, @Description, @StartDate, @EndDate,
                        @Created, @Modified, 0, '', '', 0, 0, ''
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                program.ProgramId = await connection.ExecuteScalarAsync<int>(insertProgramSql, new
                {
                    ApplicationInstanceId = model.Event.EventId,
                    Title = program.Title,
                    Description = program.Description,
                    StartDate = startTime,
                    EndDate = endTime,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                }, transaction);

                // 2. Insert Program Tags
                await CreateProgramTagsAsync(connection, transaction, program, model.Event.EventId);

                // 3. Insert Session Questions
                await CreateProgramQuestionsAsync(connection, transaction, program, model.Event.EventId);

                // 4. Insert Evaluation Questions
                await CreateProgramEvaluationQuestionsAsync(connection, transaction, program, model.Event.EventId);
            }
        }

        private async Task CreateProgramTagsAsync(SqlConnection connection, SqlTransaction transaction, ProgramInfo program, int eventId)
        {
            if (program.ProgramTags == null) return;

            foreach (var tag in program.ProgramTags)
            {
                // First, create or get ApplicationInstanceTag
                var insertTagSql = @"
                    IF NOT EXISTS (SELECT 1 FROM ApplicationInstanceTag WHERE ApplicationInstanceId = @ApplicationInstanceId AND TagName = @TagName)
                    BEGIN
                        INSERT INTO ApplicationInstanceTag (ApplicationInstanceId, TagName)
                        VALUES (@ApplicationInstanceId, @TagName);
                    END
                    SELECT Id FROM ApplicationInstanceTag WHERE ApplicationInstanceId = @ApplicationInstanceId AND TagName = @TagName;";

                var tagId = await connection.ExecuteScalarAsync<int>(insertTagSql, new
                {
                    ApplicationInstanceId = eventId,
                    TagName = tag,
                    Created = DateTime.UtcNow,
                    Modified = DateTime.UtcNow
                }, transaction);

                // Then link to program
                var linkTagSql = @"
                    INSERT INTO ProgramTag (ProgramId, ApplicationInstanceTagId)
                    VALUES (@ProgramId, @ApplicationInstanceTagId);";

                await connection.ExecuteAsync(linkTagSql, new
                {
                    ProgramId = program.ProgramId,
                    ApplicationInstanceTagId = tagId
                }, transaction);
            }
        }

        private async Task CreateProgramQuestionsAsync(SqlConnection connection, SqlTransaction transaction, ProgramInfo program, int eventId)
        {
            if (program.SessionQuestions == null) return;

            foreach (var question in program.SessionQuestions)
            {
                // Determine question type based on whether it has options
                var questionTypeId = question.Options != null && question.Options.Any() ? 2 : 1; // 2 = Choice, 1 = Text

                var questionInsertSql = @"
                    INSERT INTO ProgramQuestion (
                        QuestionTitle, TypeId, IsAnonymous, IsRequired, IsDeleted,
                        AddedDate, AddedBy, ModifiedDate, ModifiedBy, ApplicationInstanceId, GlobalProgramQuestion
                    ) VALUES (
                        @QuestionTitle, @TypeId, @IsAnonymous, @IsRequired, 0,
                        @AddedDate, @AddedBy, @ModifiedDate, @ModifiedBy, @ApplicationInstanceId, 0
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                var questionId = await connection.ExecuteScalarAsync<int>(questionInsertSql, new
                {
                    QuestionTitle = question.Question,
                    TypeId = questionTypeId,
                    IsAnonymous = false,
                    IsRequired = false,
                    AddedDate = DateTime.UtcNow,
                    AddedBy = "Ventla-AI",
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedBy = "Ventla-AI",
                    ApplicationInstanceId = eventId
                }, transaction);

                // Link question to program
                var linkQuestionSql = @"
                    INSERT INTO ProgramProgramQuestion (ProgramId, ProgramQuestionId, SortOrder, IsDeleted)
                    VALUES (@ProgramId, @ProgramQuestionId, @SortOrder, 0);";

                await connection.ExecuteAsync(linkQuestionSql, new
                {
                    ProgramId = program.ProgramId,
                    ProgramQuestionId = questionId,
                    SortOrder = 1
                }, transaction);

                // Insert options if they exist
                if (question.Options != null)
                {
                    foreach (var (option, index) in question.Options.Select((opt, idx) => (opt, idx)))
                    {
                        var insertOptionSql = @"
                            INSERT INTO ProgramQuestionOption (
                                ProgramQuestionId, OptionName, SortOrder, IsDeleted
                            ) VALUES (
                                @ProgramQuestionId, @OptionName, @SortOrder, 0
                            );";

                        await connection.ExecuteAsync(insertOptionSql, new
                        {
                            ProgramQuestionId = questionId,
                            OptionName = option,
                            SortOrder = index + 1
                        }, transaction);
                    }
                }
            }
        }

        private async Task CreateProgramEvaluationQuestionsAsync(SqlConnection connection, SqlTransaction transaction, ProgramInfo program, int eventId)
        {
            if (program.EvaluationQuestions == null) return;

            foreach (var (eval, index) in program.EvaluationQuestions.Select((e, i) => (e, i)))
            {
                // Determine evaluation type - star rating or text
                var evaluationType = eval.Question.ToLower().Contains("star") || eval.Question.ToLower().Contains("rate") ? "Grade" : "Text";
                var gradeRange = evaluationType == "Grade" ? 5 : 0;

                var insertEvalSql = @"
                    INSERT INTO ProgramEvaluation (
                        SortOrder, Question, Type, GradeRange, Anynomous, ApplicationInstanceId, IsDeleted, Modified
                    ) VALUES (
                        @SortOrder, @Question, @Type, @GradeRange, @Anynomous, @ApplicationInstanceId, 0, @Modified
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                var evaluationId = await connection.ExecuteScalarAsync<int>(insertEvalSql, new
                {
                    SortOrder = index + 1,
                    Question = eval.Question,
                    Type = evaluationType,
                    GradeRange = gradeRange,
                    Anynomous = true,
                    ApplicationInstanceId = eventId,
                    Modified = DateTime.UtcNow
                }, transaction);

                // Link evaluation to program
                var linkEvalSql = @"
                    INSERT INTO ProgramProgramEvaluation (ProgramId, ProgramEvaluationId)
                    VALUES (@ProgramId, @ProgramEvaluationId);";

                await connection.ExecuteAsync(linkEvalSql, new
                {
                    ProgramId = program.ProgramId,
                    ProgramEvaluationId = evaluationId
                }, transaction);
            }
        }

        private async Task CreateNotificationsAsync(SqlConnection connection, SqlTransaction transaction, EventGenerationResponse model)
        {
            if (model.Notifications == null) return;

            foreach (var notification in model.Notifications)
            {
                var insertNotificationSql = @"
                    INSERT INTO Notification (
                        Created, Title, Description, NotificationTypeId, IsDeleted, Modified,
                        SentSmsNotifications, SentApnsNotifications, SentC2DmNotifications,
                        IsVisibleToClients, EnableClientShowResults, SortOrder, EnableResponseAnonymously,
                        ApplicationInstanceId, IsRespondOnlyOnce, ParentQuestionnaireId, ShowCorrectFeedbackToClients,SelectedChartTypeId
                    ) VALUES (
                        @Created, @Title, @Description, @NotificationTypeId, 0, @Modified,
                        0, 0, 0, 0, 0, -1, 0, @ApplicationInstanceId, 0, 0, 0, @SelectedChartTypeId
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS int);";

                var notificationId = await connection.ExecuteScalarAsync<int>(insertNotificationSql, new
                {
                    Created = DateTime.UtcNow,
                    Title = notification.Title,
                    Description = notification.Description,
                    NotificationTypeId = notification.NotificationTypeId,
                    Modified = DateTime.UtcNow,
                    ApplicationInstanceId = model.Event.EventId,
                    SelectedChartTypeId = GetNotificationChartTypeId(notification.NotificationTypeId)
                }, transaction);

                // Insert notification options if they exist
                if (notification.Options != null && notification.Options.Any())
                {
                    foreach (var option in notification.Options)
                    {
                        var insertOptionSql = @"
                            INSERT INTO NotificationOption (
                                NotificationId, Text, IsCorrectOption, SortOrder
                            ) VALUES (
                                @NotificationId, @Text, @IsCorrectOption, @SortOrder
                            );";

                        await connection.ExecuteAsync(insertOptionSql, new
                        {
                            NotificationId = notificationId,
                            Text = option.Text,
                            IsCorrectOption = option.IsCorrectOption,
                            SortOrder = option.SortOrder
                        }, transaction);
                    }
                }
            }
        }

        private DateTime ParseTimeString(string timeString, DateTime baseDate)
        {
            if (DateTime.TryParseExact($"{baseDate:yyyy-MM-dd} {timeString}",
                "yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return result;
            }

            // Fallback parsing
            if (DateTime.TryParseExact($"{baseDate:yyyy-MM-dd} {timeString}",
                "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result2))
            {
                return result2;
            }

            return baseDate; // Fallback to base date if parsing fails
        }

        private async Task CreateProgramInformationAssociationsAsync(SqlConnection connection, SqlTransaction transaction, EventGenerationResponse model)
        {
            if (model.Programs == null || model.Informations == null) return;

            // Create associations between programs and information based on relevance
            foreach (var program in model.Programs)
            {
                foreach (var information in model.Informations)
                {
                    // Determine if this information is relevant to this program
                    if (ShouldAssociateInformationWithProgram(program, information, model))
                    {
                        var insertAssociationSql = @"
                            INSERT INTO Program_ApplicationInstanceCustomCategory (
                                ProgramId, ApplicationInstanceCustomCategoryId, 
                                ProgramTitle, CustomCategoryTitle, Title
                            ) VALUES (
                                @ProgramId, @ApplicationInstanceCustomCategoryId,
                                @ProgramTitle, @CustomCategoryTitle, @Title
                            );";

                        await connection.ExecuteAsync(insertAssociationSql, new
                        {
                            ProgramId = program.ProgramId,
                            ApplicationInstanceCustomCategoryId = information.ApplicationInstanceCustomCategoryId,
                            ProgramTitle = program.Title,
                            CustomCategoryTitle = information.Title,
                            Title = $"{program.Title} - {information.Title}"
                        }, transaction);
                    }
                }
            }
        }
        private bool ShouldAssociateInformationWithProgram(ProgramInfo program, Information information, EventGenerationResponse model)
        {
            // Logic to determine if information should be associated with a program
            // You can customize this logic based on your business requirements

            // Example 1: Associate speaker information with programs that mention speakers
            if (information.Title.ToLower().Contains("speaker") &&
                (program.Description.ToLower().Contains("speaker") ||
                 program.Description.ToLower().Contains("keynote") ||
                 program.Description.ToLower().Contains("presentation")))
            {
                return true;
            }

            // Example 2: Associate sponsor information with all programs
            if (information.Title.ToLower().Contains("sponsor"))
            {
                return true;
            }

            // Example 3: Associate resources with workshop/technical programs
            if (information.Title.ToLower().Contains("resource") &&
                (program.Title.ToLower().Contains("workshop") ||
                 program.Title.ToLower().Contains("technical") ||
                 program.Title.ToLower().Contains("hands-on")))
            {
                return true;
            }

            // Example 4: Associate event highlights with opening/closing sessions
            if (information.Title.ToLower().Contains("highlight") &&
                (program.Title.ToLower().Contains("opening") ||
                 program.Title.ToLower().Contains("closing") ||
                 program.Title.ToLower().Contains("keynote")))
            {
                return true;
            }

            // Example 5: Associate based on matching tags
            if (information.InformationTypes?.Speaker != null && program.ProgramTags != null)
            {
                foreach (var speaker in information.InformationTypes.Speaker)
                {
                    if (program.ProgramTags.Any(tag => tag.ToLower().Contains(speaker.ToLower().Split(' ')[0])))
                    {
                        return true;
                    }
                }
            }

            // Default: Associate first information item with first program, second with second, etc.
            // This ensures every program has at least one information association
            var programIndex = model.Programs?.ToList().IndexOf(program) ?? 0;
            var informationIndex = model.Informations?.ToList().IndexOf(information) ?? 0;

            return programIndex == informationIndex;
        }

        // Inserts all gamification rule entries for a given event
        private async Task CreateGamificationAllRulesEventsAsync(SqlConnection connection, SqlTransaction transaction, EventGenerationResponse model)
        {
            // Fetch all available gamification rules from the database
            var gamificationRulesList = await GetGamificationRules(connection, transaction);

            // Iterate through each rule to create a corresponding GamificationRulesEvent entry
            foreach (var rules in gamificationRulesList)
            {
                // Step 1: Insert into Gamification rules
                var insertGamificationRulesSql = @"
                INSERT INTO GamificationRulesEvent (
                    EventId, GamificationRuleId, Points, IsEnabled,
                    CreatedBy, CreatedOn, ModifiedBy
                ) VALUES (
                    @EventId, @GamificationRuleId, @Points, @IsEnabled,
                    @CreatedBy, @CreatedOn, @ModifiedBy
                );";

                // Execute the insert query with parameters using Dapper
                await connection.ExecuteScalarAsync(insertGamificationRulesSql, new
                {
                    EventId = model.Event.EventId,
                    GamificationRuleId = rules.Id,
                    Points = rules.DefaultPoints,
                    IsEnabled = true,
                    CreatedBy = "Ventla-AI",
                    CreatedOn = DateTime.UtcNow,
                    ModifiedBy = "Ventla-AI",
                }, transaction);
            }
        }

        // Retrieves the list of all gamification rules from the database
        private async Task<List<GamificationRules>> GetGamificationRules(SqlConnection connection, SqlTransaction transaction)
        {
            var gamificationRulesSql = @"SELECT * FROM GamificationRules;";
            var gamificationRules = await connection.QueryAsync<GamificationRules>(
                gamificationRulesSql,
                transaction: transaction
            );
            return gamificationRules.ToList();
        }

        // Determines which chart type to use based on the notification type
        private int GetNotificationChartTypeId(int notificationTypeId)
        {
            int selectedChartTypeId;
            switch (notificationTypeId)
            {
                case 1:
                case 4:
                    selectedChartTypeId = (int)NotificationChartType.TextListing;
                    break;

                case 2:
                case 3:
                case 5:
                    selectedChartTypeId = (int)NotificationChartType.BarChart;
                    break;

                case 6:
                    selectedChartTypeId = (int)NotificationChartType.RankingBarChart;
                    break;

                case 7:
                    selectedChartTypeId = (int)NotificationChartType.SliderChart;
                    break;

                default:
                    selectedChartTypeId = (int)NotificationChartType.BarChart;
                    break;
            }
            return selectedChartTypeId;
        }
    }
}