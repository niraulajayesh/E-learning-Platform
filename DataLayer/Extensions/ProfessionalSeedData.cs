using System.Security.Cryptography;
using System.Text;
using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedLayer.Enums;

namespace DataLayer.Extensions;

public static class ProfessionalSeedData
{
    private const string SeedVersionKey = "SeedData.Version";
    private const string SeedVersion = "AsvabPrepV5";
    private static readonly string[] AsvabCategoryNames = { "WK – Word Knowledge", "PC – Paragraph Comprehension", "AR – Arithmetic Reasoning", "MK – Mathematics Knowledge", "GS – General Science", "EI – Electronics Information", "MC – Mechanical Comprehension", "AS – Auto & Shop Information", "AO – Assembling Objects" };

    public static async Task SeedProfessionalDemoDataAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("ProfessionalSeedData");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            logger?.LogInformation("Checking professional demo seed data.");

            if (!await db.Database.CanConnectAsync(cancellationToken))
            {
                logger?.LogWarning("Professional demo seed skipped because the database is not reachable.");
                return;
            }

            var alreadySeeded = await db.SiteSettings.AnyAsync(s => s.Key == SeedVersionKey && s.Value == SeedVersion, cancellationToken);
            var categoryCount = await db.Categories.CountAsync(cancellationToken);
            var hasOnlyAsvabCategories = categoryCount == 9 && await db.Categories.AllAsync(c => AsvabCategoryNames.Contains(c.Name), cancellationToken);
            if (alreadySeeded && hasOnlyAsvabCategories && await db.Courses.AnyAsync(cancellationToken))
            {
                logger?.LogInformation("Professional demo seed data is already current.");
                return;
            }

            logger?.LogInformation("Refreshing professional demo seed data.");
            await ClearDemoContentAsync(db, cancellationToken);
            await SeedAsync(db, cancellationToken);
            logger?.LogInformation("Professional demo seed data refresh completed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger?.LogWarning("Professional demo seed skipped because startup seed validation timed out.");
        }
    }

    private static async Task ClearDemoContentAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        db.QuestionBankQuestions.RemoveRange(db.QuestionBankQuestions);
        db.PracticeTestAttempts.RemoveRange(db.PracticeTestAttempts); db.PracticeTestQuestions.RemoveRange(db.PracticeTestQuestions); db.PracticeTests.RemoveRange(db.PracticeTests);
        db.StudyGuideBookmarks.RemoveRange(db.StudyGuideBookmarks); db.StudyGuides.RemoveRange(db.StudyGuides);
        db.Flashcards.RemoveRange(db.Flashcards); db.FlashcardSets.RemoveRange(db.FlashcardSets);
        db.Wishlists.RemoveRange(db.Wishlists); db.Certificates.RemoveRange(db.Certificates); db.Payments.RemoveRange(db.Payments); db.Progress.RemoveRange(db.Progress);
        db.QuizAttempts.RemoveRange(db.QuizAttempts); db.Answers.RemoveRange(db.Answers); db.Questions.RemoveRange(db.Questions); db.Quizzes.RemoveRange(db.Quizzes);
        db.Reviews.RemoveRange(db.Reviews); db.Enrollments.RemoveRange(db.Enrollments); db.Lessons.RemoveRange(db.Lessons); db.Sections.RemoveRange(db.Sections);
        db.Courses.RemoveRange(db.Courses); db.Categories.RemoveRange(db.Categories); db.Banners.RemoveRange(db.Banners); db.Testimonials.RemoveRange(db.Testimonials);
        db.Coupons.RemoveRange(db.Coupons); db.Notifications.RemoveRange(db.Notifications); db.AuditLogs.RemoveRange(db.AuditLogs); db.SiteSettings.RemoveRange(db.SiteSettings.Where(s => s.Key != SeedVersionKey));
        await db.SaveChangesAsync(cancellationToken);
        db.Users.RemoveRange(await db.Users.Where(u => u.Role != UserRole.Admin).ToListAsync(cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await EnsureAdminAsync(db, cancellationToken);
        var categories = CategorySeedData().Select((c, i) => new Category { Name = c.Name, Slug = Slug(c.Name), Description = c.Description, IconUrl = c.Icon, BannerUrl = $"/uploads/category-banners/{Slug(c.Name)}.jpg", IsActive = true, DisplayOrder = i + 1 }).ToList();
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(cancellationToken);

        var instructors = InstructorSeedData().Select(i => new User { Id = Guid.NewGuid(), FirstName = i.FirstName, LastName = i.LastName, Email = i.Email, PasswordHash = HashPassword("Instructor@123"), Role = UserRole.Instructor, Headline = i.Headline, Bio = i.Bio, ProfilePictureUrl = i.Photo, LinkedInUrl = i.LinkedIn, WebsiteUrl = i.Website, IsActive = true, IsEmailVerified = true, LastLoginAt = now.AddDays(-i.LastLoginDaysAgo) }).ToList();
        var students = StudentSeedData().Select(s => new User { Id = Guid.NewGuid(), FirstName = s.FirstName, LastName = s.LastName, Email = s.Email, PasswordHash = HashPassword("Student@123"), Role = UserRole.Student, Headline = s.Headline, Bio = s.Bio, ProfilePictureUrl = s.Photo, IsActive = true, IsEmailVerified = true, LastLoginAt = now.AddDays(-s.LastLoginDaysAgo) }).ToList();
        db.Users.AddRange(instructors); db.Users.AddRange(students);
        await db.SaveChangesAsync(cancellationToken);

        var categoryByName = categories.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var courseSpecs = CourseSeedData();
        var courses = new List<Course>();
        for (var i = 0; i < courseSpecs.Count; i++)
        {
            var spec = courseSpecs[i];
            var course = new Course { Id = Guid.NewGuid(), InstructorId = instructors[i % instructors.Count].Id, CategoryId = categoryByName[spec.Category].Id, Title = spec.Title, Slug = Slug(spec.Title), ShortDescription = spec.ShortDescription, Description = spec.Description, ThumbnailUrl = spec.ThumbnailUrl, PreviewVideoUrl = spec.PreviewVideoUrl, Price = spec.Price, DiscountedPrice = spec.DiscountedPrice, Level = spec.Level, Status = CourseStatus.Published, Language = "English", WhatYouWillLearn = string.Join('\n', spec.Outcomes.Select(o => "- " + o)), Requirements = spec.Requirements, TargetAudience = spec.TargetAudience, IsFeatured = i < 9, IsBestseller = i % 7 == 0, PublishedAt = now.AddDays(-120 + i), AverageRating = spec.Rating, TotalReviews = spec.ReviewCount, TotalEnrollments = spec.Enrollments, CreatedAt = now.AddDays(-160 + i), UpdatedAt = now.AddDays(-8 + i % 6) };
            var section = new Section { Id = Guid.NewGuid(), Course = course, Title = "ASVAB study plan", Description = "Focused ASVAB preparation lessons, examples, and a final knowledge check.", Order = 1 };
            foreach (var lesson in BuildLessons(spec, i)) section.Lessons.Add(lesson);
            course.Sections.Add(section); course.TotalLessons = section.Lessons.Count; course.TotalDurationMinutes = section.Lessons.Sum(l => l.DurationMinutes); courses.Add(course);
        }
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync(cancellationToken);
        SeedReviewsAndEnrollments(db, courses, students, now);
        SeedFlashcards(db, categories);
        SeedStudyGuides(db, categories);
        SeedPracticeTests(db, categories);
        SeedQuestionBank(db, categories);
        SeedCms(db, courses, categories, instructors);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureAdminAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken)) return;
        db.Users.Add(new User { FirstName = "Platform", LastName = "Admin", Email = "admin@learnhub.local", PasswordHash = HashPassword("Admin@123"), Role = UserRole.Admin, Headline = "ASVAB Prep Administrator", Bio = "Required administrator account for managing the ASVAB preparation platform.", IsActive = true, IsEmailVerified = true });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<Lesson> BuildLessons(CourseSeed course, int courseIndex)
    {
        var topics = LessonTopicsByCategory(course.Category);
        var lessons = new List<Lesson>();
        for (var i = 0; i < 6; i++)
        {
            var isQuiz = i == 5;
            var topic = isQuiz ? "Final ASVAB practice quiz" : topics[i % topics.Length];
            var objective = isQuiz ? $"Confirm readiness by answering ASVAB-style questions for {course.Title}." : $"Learn how to solve {topic.ToLowerInvariant()} questions with ASVAB timing and accuracy.";
            var lesson = new Lesson { Id = Guid.NewGuid(), Title = topic, Description = objective, Type = isQuiz ? LessonType.Quiz : LessonType.Video, ContentUrl = isQuiz ? null : $"https://learnhub.example.com/asvab/{Slug(course.Title)}-{i + 1}.mp4", ArticleContent = $"<p><strong>Learning objective:</strong> {objective}</p><p>This lesson uses direct instruction, guided examples, timed drills, and mistake review to prepare students for the {course.Category.Split(' ')[0]} section of the ASVAB.</p>", ResourcesUrl = $"/uploads/resources/asvab/{Slug(course.Title)}-lesson-{i + 1}.pdf", DurationMinutes = isQuiz ? 18 : 24 + ((courseIndex + i) % 8), Order = i + 1, IsFreePreview = i == 0, IsPublished = true };
            if (isQuiz) lesson.Quiz = BuildQuiz(lesson, course);
            lessons.Add(lesson);
        }
        return lessons;
    }

    private static string[] LessonTopicsByCategory(string category) => category switch
    {
        "WK – Word Knowledge" => new[] { "Vocabulary diagnostic and ASVAB word families", "Synonyms and antonyms under time pressure", "Context clues and sentence meaning", "Prefixes, suffixes, and roots", "Eliminating close distractors" },
        "PC – Paragraph Comprehension" => new[] { "Finding the main idea quickly", "Supporting details and evidence", "Inference and author's purpose", "Sequencing and logical conclusions", "Avoiding extreme answer traps" },
        "AR – Arithmetic Reasoning" => new[] { "Translating word problems into equations", "Fractions, ratios, and proportions", "Percent change and practical math", "Rates, time, distance, and work", "Multi-step ASVAB problem strategy" },
        "MK – Mathematics Knowledge" => new[] { "Algebra foundations and expressions", "Linear equations and inequalities", "Geometry formulas and diagrams", "Exponents, roots, and scientific notation", "Functions, graphs, and patterns" },
        "GS – General Science" => new[] { "Biology systems and cell basics", "Chemistry matter and reactions", "Physics forces, energy, and motion", "Earth science and weather systems", "Scientific reasoning and units" },
        "EI – Electronics Information" => new[] { "Voltage, current, and resistance", "Ohm's law and circuit math", "Series and parallel circuits", "Electronic components and symbols", "Safety, switches, and troubleshooting" },
        "MC – Mechanical Comprehension" => new[] { "Force, motion, and simple machines", "Levers, gears, and pulleys", "Pressure, fluids, and hydraulics", "Work, energy, and power", "Mechanical diagrams and tool use" },
        "AS – Auto & Shop Information" => new[] { "Engine systems and basic maintenance", "Shop tools, fasteners, and measuring", "Electrical and fuel systems", "Brakes, steering, and suspension", "Workshop safety and repair procedures" },
        "AO – Assembling Objects" => new[] { "Spatial visualization fundamentals", "Object rotation and orientation", "Matching parts to final assemblies", "Pattern recognition and folding", "Timed assembly problem practice" },
        _ => new[] { "ASVAB basics", "Guided examples", "Timed practice", "Mistake review", "Readiness strategy" }
    };

    private static Quiz BuildQuiz(Lesson lesson, CourseSeed course) => new()
    {
        Id = Guid.NewGuid(), Lesson = lesson, Title = $"{course.Title} ASVAB practice quiz", Description = "A timed ASVAB-style multiple-choice quiz with explanations and difficulty progression.", PassingScore = 75, TimeLimitMinutes = 18, MaxAttempts = 3, ShuffleQuestions = true, ShowAnswersAfterSubmission = true, Questions = QuizQuestionsByCategory(course.Category).Select((q, i) => BuildQuestion(i + 1, q)).ToList()
    };

    private static Question BuildQuestion(int order, QuizQuestionSeed q) => new()
    {
        Id = Guid.NewGuid(), Text = q.Text, Explanation = q.Explanation, Type = QuestionType.MultipleChoice, Points = order <= 2 ? 1 : 2, Order = order,
        Answers = q.Answers.Select((answer, index) => new Answer { Id = Guid.NewGuid(), Text = answer, IsCorrect = index == q.CorrectIndex, Order = index + 1 }).ToList()
    };

    private static QuizQuestionSeed[] QuizQuestionsByCategory(string category) => category switch
    {
        "WK – Word Knowledge" => new[] { Q("The word 'reluctant' most nearly means:",1,"Reluctant means unwilling or hesitant.","eager","hesitant","careless","ordinary"), Q("Choose the best synonym for 'brief'.",0,"Brief means short in time or length.","short","bright","heavy","strict"), Q("In the sentence 'The officer gave a concise briefing,' concise means:",2,"Concise communication is clear and short.","confusing","lengthy","clear and short","angry"), Q("Which word is closest in meaning to 'assist'?",3,"To assist is to help.","delay","inspect","repeat","help"), Q("An antonym for 'scarce' is:",0,"Scarce means limited; abundant means plentiful.","abundant","rare","minor","distant") },
        "PC – Paragraph Comprehension" => new[] { Q("A passage says a team trained daily and improved scores each week. The main idea is:",1,"The sentence emphasizes steady improvement through practice.","training was cancelled","practice led to improvement","scores became worse","the team changed uniforms"), Q("If a paragraph explains causes of engine overheating, the reader should expect details about:",2,"Supporting details should match the topic of overheating causes.","paint color","seat comfort","coolant, airflow, or mechanical issues","radio settings"), Q("A writer states that safety checks prevent accidents. The likely conclusion is:",0,"Safety checks reduce risk before work begins.","checks should be completed before work","checks waste all time","accidents are impossible","tools are never needed"), Q("When a passage contrasts two options, the best answer usually identifies:",3,"Contrast focuses on differences.","the longest word","the first date","a random fact","how the options differ"), Q("An inference must be based on:",1,"Valid inferences come from evidence in the passage.","personal guesses only","evidence in the text","the answer length","outside opinions") },
        "AR – Arithmetic Reasoning" => new[] { Q("A jacket costs $80 and is discounted 25%. What is the sale price?",2,"25% of 80 is 20, so 80 - 20 = 60.","$20","$55","$60","$75"), Q("If 3 notebooks cost $12, how much do 5 notebooks cost at the same rate?",0,"Each notebook is $4; 5 x 4 = $20.","$20","$15","$24","$36"), Q("A vehicle travels 180 miles in 3 hours. What is its average speed?",1,"Speed = distance / time = 180 / 3 = 60 mph.","45 mph","60 mph","90 mph","120 mph"), Q("What is 2/5 of 50?",3,"50 divided by 5 is 10, and 10 x 2 = 20.","10","15","25","20"), Q("A ratio of 2:3 has 25 total parts. What is the smaller share?",0,"2 + 3 = 5 parts; 25 / 5 = 5; smaller share is 2 x 5 = 10.","10","12","15","20") },
        "MK – Mathematics Knowledge" => new[] { Q("Solve for x: x + 7 = 15.",1,"Subtract 7 from both sides: x = 8.","7","8","15","22"), Q("What is the area of a rectangle 6 units long and 4 units wide?",0,"Area = length x width = 6 x 4 = 24.","24","20","10","12"), Q("Simplify: 3^2 + 4.",2,"3 squared is 9; 9 + 4 = 13.","10","11","13","18"), Q("If 2x = 18, then x equals:",3,"Divide both sides by 2 to get x = 9.","6","8","10","9"), Q("The sum of angles in a triangle is:",0,"Every triangle has interior angles totaling 180 degrees.","180 degrees","90 degrees","270 degrees","360 degrees") },
        "GS – General Science" => new[] { Q("Which organ pumps blood through the body?",2,"The heart circulates blood.","lung","kidney","heart","stomach"), Q("Water is made of hydrogen and:",0,"Water is H2O: hydrogen and oxygen.","oxygen","carbon","nitrogen","iron"), Q("Force is commonly measured in:",1,"The SI unit of force is the newton.","watts","newtons","volts","grams"), Q("The process plants use to make food from sunlight is:",3,"Photosynthesis converts light energy into chemical energy.","evaporation","condensation","erosion","photosynthesis"), Q("The Earth layer beneath the crust is the:",0,"The mantle lies below the crust.","mantle","core only","atmosphere","ocean") },
        "EI – Electronics Information" => new[] { Q("Ohm's law is commonly written as:",1,"Voltage equals current times resistance.","P = F/A","V = I x R","A = L x W","D = R x T"), Q("Current is measured in:",0,"Electrical current is measured in amperes.","amperes","ohms","volts","watts only"), Q("A resistor is used to:",2,"A resistor limits current flow in a circuit.","store fuel","increase gravity","limit current","measure distance"), Q("In a series circuit, components are connected:",3,"A series circuit has one path for current.","in separate branches only","without a source","by radio signal","in one path"), Q("A switch controls a circuit by:",0,"A switch opens or closes the circuit path.","opening or closing the path","changing metal into plastic","removing all resistance","storing water") },
        "MC – Mechanical Comprehension" => new[] { Q("A lever helps move a load by using:",2,"Levers use a fulcrum and applied force.","electric charge only","chemical reaction","a fulcrum","magnetism only"), Q("A pulley can make lifting easier by:",0,"Pulleys change force direction and can provide mechanical advantage.","changing force direction","removing weight entirely","creating fuel","stopping gravity"), Q("Pressure equals force divided by:",1,"Pressure = force / area.","speed","area","time","temperature"), Q("Two gears touching each other rotate:",3,"Meshed gears rotate in opposite directions.","in no direction","only upward","at random","in opposite directions"), Q("Work is done when a force causes:",0,"In physics, work requires force causing displacement.","movement","color change","silence","cooling only") },
        "AS – Auto & Shop Information" => new[] { Q("The engine oil dipstick is used to check:",1,"A dipstick measures oil level.","tire width","oil level","paint thickness","radio volume"), Q("A wrench is primarily used to:",0,"Wrenches tighten or loosen nuts and bolts.","turn nuts and bolts","measure voltage","paint metal","cut glass"), Q("Brake pads are part of the:",2,"Brake pads are part of the braking system.","fuel system","cooling system","braking system","exhaust system"), Q("Safety goggles protect the:",3,"Goggles protect eyes from debris and chemicals.","feet","ears only","knees","eyes"), Q("A battery provides a vehicle with:",0,"The battery supplies electrical power for starting and accessories.","electrical power","engine oil","coolant","air pressure") },
        "AO – Assembling Objects" => new[] { Q("Object assembly questions mainly test:",2,"AO tests spatial visualization and part relationships.","vocabulary only","chemistry facts","spatial visualization","typing speed"), Q("When rotating a shape mentally, its size usually:",0,"Rotation changes orientation, not size.","stays the same","must double","disappears","becomes heavier"), Q("The best first step in an assembly problem is to identify:",1,"Anchor points help align parts.","random colors","matching edges or anchor points","the longest answer","unrelated details"), Q("A folded pattern question requires imagining:",3,"The task is visualizing the final 3D form.","a math formula only","a paragraph summary","engine timing","the final 3D form"), Q("If two parts have matching tabs and slots, they likely:",0,"Tabs and slots indicate connection points.","connect together","are unrelated","change color","measure speed") },
        _ => Array.Empty<QuizQuestionSeed>()
    };

    private static QuizQuestionSeed Q(string text, int correctIndex, string explanation, params string[] answers) => new(text, answers, correctIndex, explanation);

    private static void SeedReviewsAndEnrollments(AppDbContext db, List<Course> courses, List<User> students, DateTime now)
    {
        var comments = new[] { "The timed drills made ASVAB practice feel manageable and gave me a clear study path.", "I finally understood how to eliminate wrong answers instead of guessing blindly.", "The explanations are direct, realistic, and easy to review before a practice test.", "Great structure for building confidence across the ASVAB categories.", "The lessons feel focused on the actual test instead of generic school review.", "Strong examples, practical pacing, and helpful quiz explanations.", "This helped me spot patterns in the questions and improve speed.", "The category-based study plan made it easy to focus on weak areas." };
        for (var i = 0; i < courses.Count; i++)
        {
            var course = courses[i];
            for (var j = 0; j < Math.Min(5, students.Count); j++)
            {
                var student = students[(i + j) % students.Count];
                var completion = j % 3 == 0 ? 100 : 35 + ((i + j) % 5) * 12;
                db.Enrollments.Add(new Enrollment { Id = Guid.NewGuid(), CourseId = course.Id, StudentId = student.Id, Status = completion >= 100 ? EnrollmentStatus.Completed : EnrollmentStatus.Active, CompletionPercentage = completion, EnrolledAt = now.AddDays(-70 + i - j), CompletedAt = completion >= 100 ? now.AddDays(-4 - j) : null, LastAccessedAt = now.AddDays(-j) });
                db.Reviews.Add(new Review { Id = Guid.NewGuid(), CourseId = course.Id, StudentId = student.Id, Rating = j == 4 ? 4 : 5, Comment = comments[(i + j) % comments.Length], IsVisible = true });
            }
        }
    }


    private static void SeedFlashcards(AppDbContext db, List<Category> categories)
    {
        foreach (var category in categories)
        {
            var cards = BuildFlashcards(category.Name).Select((card, index) => new Flashcard
            {
                Id = Guid.NewGuid(),
                Front = card.Front,
                Back = card.Back,
                Hint = card.Hint,
                Order = index + 1
            }).ToList();

            db.FlashcardSets.Add(new FlashcardSet
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                Title = $"{category.Name} Flashcard Mastery",
                Description = $"50 focused ASVAB flashcards for {category.Name} review and timed recall practice.",
                IsPublished = true,
                Flashcards = cards
            });
        }
    }

    private static IEnumerable<FlashcardSeed> BuildFlashcards(string category)
    {
        var baseCards = category switch
        {
            "WK – Word Knowledge" => new[] { ("reluctant", "unwilling or hesitant", "Think: not ready"), ("concise", "brief and clear", "Short but complete"), ("scarce", "limited or not plentiful", "Opposite of abundant"), ("assist", "to help", "Aid"), ("rapid", "fast or quick", "Speed word"), ("accurate", "correct or exact", "Precision"), ("observe", "to watch carefully", "Look closely"), ("assemble", "to put parts together", "Build"), ("maintain", "to keep in good condition", "Preserve"), ("essential", "absolutely necessary", "Needed") },
            "PC – Paragraph Comprehension" => new[] { ("Main idea", "the central point of a passage", "What is it mostly about?"), ("Supporting detail", "a fact that explains or proves the main idea", "Evidence"), ("Inference", "a logical conclusion based on text evidence", "Read between lines"), ("Author's purpose", "the reason the writer wrote the passage", "Inform, persuade, explain"), ("Tone", "the writer's attitude toward the subject", "Mood of writing"), ("Sequence", "the order in which events happen", "First, next, last"), ("Conclusion", "a decision supported by the passage", "Text-based result"), ("Context", "surrounding information that clarifies meaning", "Nearby clues"), ("Contrast", "showing differences between ideas", "But/however"), ("Summary", "a short statement of key points", "Brief overview") },
            "AR – Arithmetic Reasoning" => new[] { ("Percent", "a part out of 100", "25% = 25/100"), ("Ratio", "a comparison of two quantities", "2:3"), ("Average", "sum divided by number of values", "Mean"), ("Rate", "a comparison involving units", "miles per hour"), ("Discount", "amount taken off the original price", "Sale reduction"), ("Proportion", "two equal ratios", "a/b = c/d"), ("Fraction", "part of a whole", "Numerator over denominator"), ("Product", "result of multiplication", "Multiply"), ("Difference", "result of subtraction", "Subtract"), ("Estimate", "a close approximate value", "Reasonable answer") },
            "MK – Mathematics Knowledge" => new[] { ("Variable", "a symbol for an unknown value", "Usually x or y"), ("Equation", "a math statement showing two expressions are equal", "Has ="), ("Exponent", "a number showing repeated multiplication", "Power"), ("Area", "space inside a flat shape", "Square units"), ("Perimeter", "distance around a shape", "Add sides"), ("Hypotenuse", "longest side of a right triangle", "Across from right angle"), ("Integer", "a whole number or its negative", "No fraction"), ("Square root", "a value that multiplies by itself to make a number", "Root"), ("Inequality", "a comparison using <, >, <=, or >=", "Not equal statement"), ("Coefficient", "number multiplied by a variable", "Number before x") },
            "GS – General Science" => new[] { ("Cell", "basic unit of life", "Biology building block"), ("Atom", "smallest unit of an element", "Chemistry building block"), ("Gravity", "force pulling objects toward each other", "Earth pulls down"), ("Photosynthesis", "plants making food using sunlight", "Light to sugar"), ("Evaporation", "liquid changing to gas", "Water vapor"), ("Condensation", "gas changing to liquid", "Cloud formation"), ("Molecule", "two or more atoms bonded together", "H2O"), ("Friction", "force that resists motion", "Rubbing force"), ("Ecosystem", "living and nonliving things interacting", "Environment system"), ("Velocity", "speed in a direction", "Motion with direction") },
            "EI – Electronics Information" => new[] { ("Voltage", "electrical pressure that pushes current", "Volts"), ("Current", "flow of electric charge", "Amps"), ("Resistance", "opposition to current flow", "Ohms"), ("Ohm's law", "V = I x R", "Voltage formula"), ("Circuit", "complete path for electric current", "Closed path"), ("Series circuit", "one path for current", "Same current"), ("Parallel circuit", "multiple paths for current", "Branches"), ("Insulator", "material that resists electric flow", "Rubber/plastic"), ("Conductor", "material that allows electric flow", "Copper"), ("Fuse", "safety device that breaks a circuit", "Overload protection") },
            "MC – Mechanical Comprehension" => new[] { ("Lever", "simple machine using a fulcrum", "Seesaw idea"), ("Pulley", "wheel and rope system for lifting", "Changes force direction"), ("Gear", "toothed wheel that transfers motion", "Rotating teeth"), ("Force", "push or pull", "Causes motion"), ("Work", "force applied over distance", "Physics work"), ("Pressure", "force divided by area", "Force/area"), ("Friction", "resistance between surfaces", "Slows motion"), ("Torque", "rotational force", "Twisting force"), ("Mechanical advantage", "force multiplication by a machine", "Easier work"), ("Inclined plane", "sloped surface used to raise objects", "Ramp") },
            "AS – Auto & Shop Information" => new[] { ("Dipstick", "tool for checking engine oil level", "Oil check"), ("Spark plug", "ignites fuel-air mixture", "Engine ignition"), ("Radiator", "helps cool the engine", "Cooling system"), ("Alternator", "charges battery while engine runs", "Electrical charging"), ("Socket wrench", "tool for turning nuts and bolts", "Ratchet tool"), ("Brake pads", "friction parts that help stop a vehicle", "Braking system"), ("Coolant", "fluid that helps control engine temperature", "Antifreeze"), ("Torque wrench", "tool that tightens to a measured force", "Precise tightening"), ("PPE", "personal protective equipment", "Safety gear"), ("Jack", "tool used to lift a vehicle", "Lift tool") },
            "AO – Assembling Objects" => new[] { ("Mental rotation", "imagining an object turned in space", "Rotate in mind"), ("Symmetry", "matching balance across a line or center", "Mirror-like"), ("Tab", "projecting part designed to fit into a slot", "Connector"), ("Slot", "opening designed to receive a tab", "Receiving part"), ("Orientation", "the direction an object faces", "Position"), ("Pattern", "repeated or predictable arrangement", "Visual rule"), ("Assembly", "completed object made from parts", "Put together"), ("Edge matching", "aligning sides that fit together", "Match boundaries"), ("Perspective", "viewpoint from which an object is seen", "Viewing angle"), ("Fold line", "line where a flat shape bends", "3D fold") },
            _ => Array.Empty<(string, string, string)>()
        };

        for (var round = 0; round < 5; round++)
        {
            foreach (var card in baseCards)
            {
                yield return new FlashcardSeed($"{card.Item1} ({round + 1})", card.Item2, card.Item3);
            }
        }
    }

    private static void SeedStudyGuides(AppDbContext db, List<Category> categories)
    {
        foreach (var category in categories)
        {
            var topics = StudyGuideTopics(category.Name);
            for (var i = 0; i < topics.Length; i++)
            {
                var topic = topics[i];
                db.StudyGuides.Add(new StudyGuide
                {
                    Id = Guid.NewGuid(),
                    CategoryId = category.Id,
                    Title = topic,
                    Summary = $"A focused ASVAB study guide for {topic.ToLowerInvariant()} in {category.Name}.",
                    Theory = $"<p>{topic} is a core skill for the {category.Name} section. Start by understanding the rule or idea, then practice recognizing how it appears in short ASVAB-style prompts.</p><p>Strong performance comes from reading carefully, identifying the task, eliminating weak choices, and checking the answer against the original question.</p>",
                    Examples = $"<div class=\"example-box\"><strong>Example:</strong> Review a typical {topic.ToLowerInvariant()} question, identify what it asks, remove two unlikely answers, and choose the option best supported by the rule.</div><div class=\"example-box\"><strong>Practice pattern:</strong> Complete five short items, review every missed answer, then repeat with a timer.</div>",
                    KeyConcepts = $"<ul><li>Know the exact task before solving.</li><li>Use elimination when two answers look similar.</li><li>Watch for units, wording, and answer traps.</li><li>Review explanations, not just final answers.</li></ul>",
                    Tips = $"<ul><li>Spend a few seconds planning before answering.</li><li>Mark difficult items for review during practice.</li><li>Keep a mistake log for repeated patterns.</li><li>Use flashcards and quizzes after reading this guide.</li></ul>",
                    IsPublished = true,
                    DisplayOrder = i + 1
                });
            }
        }
    }

    private static string[] StudyGuideTopics(string category) => category switch
    {
        "WK – Word Knowledge" => new[] { "Vocabulary Roots", "Synonym Strategy", "Antonym Strategy", "Context Clues", "Military Vocabulary", "Word Families", "Definition Precision", "Answer Elimination", "Common Distractors", "Timed Word Review" },
        "PC – Paragraph Comprehension" => new[] { "Main Idea Analysis", "Supporting Details", "Inference Questions", "Author Purpose", "Tone and Attitude", "Sequence of Events", "Fact vs Opinion", "Conclusion Questions", "Extreme Answer Traps", "Timed Passage Reading" },
        "AR – Arithmetic Reasoning" => new[] { "Word Problem Setup", "Percent Problems", "Ratio and Proportion", "Fractions in Context", "Rates and Distance", "Averages", "Money Problems", "Measurement Problems", "Multi-Step Reasoning", "Timed Arithmetic Strategy" },
        "MK – Mathematics Knowledge" => new[] { "Algebra Expressions", "Solving Equations", "Inequalities", "Geometry Formulas", "Triangles and Angles", "Area and Volume", "Exponents", "Roots and Radicals", "Coordinate Graphs", "Formula Memorization" },
        "GS – General Science" => new[] { "Cell Biology", "Human Body Systems", "Basic Chemistry", "Matter and Atoms", "Physics Forces", "Energy and Heat", "Earth Science", "Weather Systems", "Astronomy Basics", "Scientific Method" },
        "EI – Electronics Information" => new[] { "Voltage and Current", "Resistance", "Ohm's Law", "Series Circuits", "Parallel Circuits", "Circuit Symbols", "Conductors and Insulators", "Switches and Fuses", "Meters and Measurement", "Electrical Safety" },
        "MC – Mechanical Comprehension" => new[] { "Force and Motion", "Levers", "Pulleys", "Gears", "Inclined Planes", "Work and Power", "Pressure", "Fluids", "Friction", "Mechanical Advantage" },
        "AS – Auto & Shop Information" => new[] { "Engine Basics", "Vehicle Fluids", "Braking Systems", "Electrical Systems", "Shop Tools", "Measuring Tools", "Fasteners", "Workshop Safety", "Routine Maintenance", "Diagnostic Thinking" },
        "AO – Assembling Objects" => new[] { "Mental Rotation", "Object Orientation", "Edge Matching", "Tabs and Slots", "Pattern Recognition", "Symmetry", "Folded Shapes", "Part-to-Whole Matching", "Perspective", "Timed Visual Strategy" },
        _ => new[] { "ASVAB Overview", "Practice Strategy", "Review Method", "Timed Recall", "Mistake Analysis", "Core Concepts", "Examples", "Tips", "Readiness", "Final Review" }
    };

    private static void SeedPracticeTests(AppDbContext db, List<Category> categories)
    {
        foreach (var category in categories)
        {
            for (var testIndex = 1; testIndex <= 5; testIndex++)
            {
                var test = new PracticeTest
                {
                    Id = Guid.NewGuid(),
                    CategoryId = category.Id,
                    Title = $"{category.Name} Practice Test {testIndex}",
                    Description = $"ASVAB-style {category.Name} practice with scoring and detailed explanations.",
                    IsTimed = testIndex % 2 == 1,
                    TimeLimitMinutes = testIndex % 2 == 1 ? 12 + testIndex : null,
                    IsPublished = true,
                    DisplayOrder = testIndex
                };

                test.Questions = BuildPracticeQuestions(category.Name, testIndex).Select((q, i) => new PracticeTestQuestion
                {
                    Id = Guid.NewGuid(),
                    PracticeTest = test,
                    Text = q.Text,
                    OptionA = q.A,
                    OptionB = q.B,
                    OptionC = q.C,
                    OptionD = q.D,
                    CorrectOption = q.Correct,
                    Explanation = q.Explanation,
                    Order = i + 1
                }).ToList();
                db.PracticeTests.Add(test);
            }
        }
    }

    private static IEnumerable<PracticeQuestionSeed> BuildPracticeQuestions(string category, int testIndex)
    {
        var quiz = QuizQuestionsByCategory(category);
        for (var round = 0; round < 2; round++)
        {
            foreach (var q in quiz)
            {
                yield return new PracticeQuestionSeed($"{q.Text} (Set {testIndex}.{round + 1})", q.Answers[0], q.Answers[1], q.Answers[2], q.Answers[3], ((char)('A' + q.CorrectIndex)).ToString(), q.Explanation);
            }
        }
    }

    private static void SeedQuestionBank(AppDbContext db, List<Category> categories)
    {
        foreach (var category in categories)
        {
            var baseQuestions = QuizQuestionsByCategory(category.Name);
            var order = 1;
            foreach (var difficulty in new[] { "Easy", "Medium", "Hard" })
            {
                for (var cycle = 1; cycle <= 8; cycle++)
                {
                    foreach (var q in baseQuestions.Take(5))
                    {
                        db.QuestionBankQuestions.Add(new QuestionBankQuestion
                        {
                            Id = Guid.NewGuid(),
                            CategoryId = category.Id,
                            Topic = category.Name,
                            Subtopic = $"{difficulty} practice set {cycle}",
                            Difficulty = difficulty,
                            Text = $"[{difficulty}] {q.Text} Practice variation {cycle} for {category.Name}.",
                            OptionA = q.Answers[0],
                            OptionB = q.Answers[1],
                            OptionC = q.Answers[2],
                            OptionD = q.Answers[3],
                            CorrectOption = ((char)('A' + q.CorrectIndex)).ToString(),
                            Explanation = q.Explanation,
                            WrongAnswerExplanation = BuildWrongAnswerExplanation(q),
                            SourceReference = "Professional ASVAB seed bank",
                            Tags = $"{category.Name}, {difficulty}, ASVAB",
                            EstimatedTimeSeconds = difficulty == "Hard" ? 90 : difficulty == "Medium" ? 75 : 60,
                            Status = "Published",
                            CreatedBy = "Seed",
                            ModifiedBy = "Seed",
                            DisplayOrder = order++
                        });
                    }
                }
            }
        }
    }

    private static string BuildWrongAnswerExplanation(QuizQuestionSeed q)
    {
        var labels = new[] { "A", "B", "C", "D" };
        var parts = new List<string>();
        for (var i = 0; i < q.Answers.Length; i++)
        {
            if (i == q.CorrectIndex) continue;
            parts.Add($"Option {labels[i]} is incorrect because '{q.Answers[i]}' does not match the concept being tested or is a distractor for this ASVAB item.");
        }
        return string.Join(" ", parts);
    }
    private static void SeedCms(AppDbContext db, List<Course> courses, List<Category> categories, List<User> instructors)
    {
        db.Banners.AddRange(new[] { new Banner { Title = "Prepare for the ASVAB with confidence", Subtitle = "Focused military aptitude test training across all major ASVAB study categories.", ImageUrl = "https://images.unsplash.com/photo-1506126613408-eca07ce68773?auto=format&fit=crop&w=1600&q=80", ButtonText = "Start ASVAB Prep", ButtonLink = "/Courses", IsActive = true, DisplayOrder = 1 }, new Banner { Title = "Practice smarter before test day", Subtitle = "Build vocabulary, math reasoning, science knowledge, electronics, mechanics, auto shop, and spatial skills.", ImageUrl = "https://images.unsplash.com/photo-1454165804606-c3d57bc86b40?auto=format&fit=crop&w=1600&q=80", ButtonText = "Browse Study Categories", ButtonLink = "/Categories", IsActive = true, DisplayOrder = 2 } });
        db.Testimonials.AddRange(new[] { new Testimonial { StudentName = "Jordan Miller", StudentRole = "ASVAB Candidate", Content = "The category drills helped me turn weak areas into a realistic study plan before my recruiter meeting.", Rating = 5, IsVisible = true, DisplayOrder = 1 }, new Testimonial { StudentName = "Alyssa Grant", StudentRole = "Future Service Member", Content = "Arithmetic Reasoning finally clicked because the lessons explain how to translate word problems quickly.", Rating = 5, IsVisible = true, DisplayOrder = 2 }, new Testimonial { StudentName = "Marcus Lee", StudentRole = "ASVAB Student", Content = "The practice quizzes feel close to the test style and the explanations are clear enough to review fast.", Rating = 5, IsVisible = true, DisplayOrder = 3 }, new Testimonial { StudentName = "Natalie Brooks", StudentRole = "Career Prep Student", Content = "I used the Word Knowledge and Paragraph Comprehension tracks every night and saw steady improvement.", Rating = 5, IsVisible = true, DisplayOrder = 4 }, new Testimonial { StudentName = "Eric Johnson", StudentRole = "Technical Aptitude Student", Content = "The electronics and mechanical sections helped me understand topics I had never studied before.", Rating = 5, IsVisible = true, DisplayOrder = 5 } });
        UpsertSiteSettings(db, new[] { new SiteSetting { Key = SeedVersionKey, Value = SeedVersion, Description = "Marks the ASVAB preparation seed as applied." }, new SiteSetting { Key = "Home.FeaturedCourses", Value = string.Join(',', courses.Where(c => c.IsFeatured).Take(9).Select(c => c.Id)), Description = "Featured ASVAB course ids for homepage display." }, new SiteSetting { Key = "Home.PopularCategories", Value = string.Join(',', categories.Select(c => c.Id)), Description = "ASVAB study category ids for homepage display." }, new SiteSetting { Key = "Home.TopInstructors", Value = string.Join(',', instructors.Take(5).Select(i => i.Id)), Description = "ASVAB instructor ids for homepage display." }, new SiteSetting { Key = "Stats.TotalLearners", Value = courses.Sum(c => c.TotalEnrollments).ToString(), Description = "Seeded ASVAB enrollment count for homepage statistics." }, new SiteSetting { Key = "Home.HeroTitle", Value = "ASVAB Preparation Platform", Description = "Homepage hero title for ASVAB positioning." }, new SiteSetting { Key = "Home.HeroSubtitle", Value = "Military aptitude test training with focused lessons, practice quizzes, and category-based study paths.", Description = "Homepage hero subtitle for ASVAB positioning." } });
    }

    private static void UpsertSiteSettings(AppDbContext db, IEnumerable<SiteSetting> settings)
    {
        var incoming = settings.ToList();
        var keys = incoming.Select(s => s.Key).ToList();
        var existingByKey = db.SiteSettings.Where(s => keys.Contains(s.Key)).ToDictionary(s => s.Key, StringComparer.OrdinalIgnoreCase);
        foreach (var setting in incoming)
        {
            if (existingByKey.TryGetValue(setting.Key, out var existing)) { existing.Value = setting.Value; existing.Description = setting.Description; existing.UpdatedAt = DateTime.UtcNow; continue; }
            db.SiteSettings.Add(setting);
        }
    }

    private static IEnumerable<CategorySeed> CategorySeedData() => new[]
    {
        new CategorySeed("WK – Word Knowledge", "Vocabulary and word meanings. Focus on synonyms, antonyms, definitions, context clues, and vocabulary building.", "bi-book"),
        new CategorySeed("PC – Paragraph Comprehension", "Reading comprehension and understanding passages. Focus on identifying main ideas, supporting details, inference, and logical conclusions.", "bi-file-text"),
        new CategorySeed("AR – Arithmetic Reasoning", "Word-based math problems and real-life applications. Focus on percentages, ratios, fractions, and practical problem solving.", "bi-calculator"),
        new CategorySeed("MK – Mathematics Knowledge", "High school mathematics. Focus on algebra, geometry, equations, exponents, and mathematical reasoning.", "bi-rulers"),
        new CategorySeed("GS – General Science", "Biology, chemistry, physics, and earth science fundamentals.", "bi-globe-americas"),
        new CategorySeed("EI – Electronics Information", "Electrical concepts, circuits, voltage, current, resistance, and electronic components.", "bi-lightning-charge"),
        new CategorySeed("MC – Mechanical Comprehension", "Mechanical and physical principles. Focus on gears, pulleys, force, motion, tools, machines, and energy.", "bi-gear"),
        new CategorySeed("AS – Auto & Shop Information", "Automotive systems, shop tools, maintenance, repair, safety procedures, and workshop knowledge.", "bi-tools"),
        new CategorySeed("AO – Assembling Objects", "Spatial reasoning and object assembly. Focus on visualization, pattern recognition, object rotation, and assembly problems.", "bi-box")
    };

    private static IEnumerable<InstructorSeed> InstructorSeedData() => new[]
    {
        new InstructorSeed("Rachel", "Adams", "rachel.adams@learnhub.local", "ASVAB Verbal Skills Coach", "Rachel specializes in vocabulary building, reading strategy, and test-day pacing for ASVAB candidates.", "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=400&q=80", "https://linkedin.com/in/racheladams", "https://racheladams.testprep", 1),
        new InstructorSeed("David", "Carter", "david.carter@learnhub.local", "Military Math Preparation Instructor", "David teaches arithmetic reasoning and high school math review with practical ASVAB-style problem solving.", "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=400&q=80", "https://linkedin.com/in/davidcarter", "https://davidcarterprep.com", 2),
        new InstructorSeed("Monica", "Reyes", "monica.reyes@learnhub.local", "Science and Technical Aptitude Educator", "Monica helps students prepare for general science, electronics, and mechanical comprehension sections.", "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=400&q=80", "https://linkedin.com/in/monicareyes", "https://monicareyes.education", 3),
        new InstructorSeed("Thomas", "Walker", "thomas.walker@learnhub.local", "Auto, Shop, and Mechanical Skills Trainer", "Thomas brings practical shop experience to automotive, tool, safety, and mechanical ASVAB preparation.", "https://images.unsplash.com/photo-1560250097-0b93528c311a?auto=format&fit=crop&w=400&q=80", "https://linkedin.com/in/thomaswalker", "https://walkerprep.tools", 1),
        new InstructorSeed("Keisha", "Morgan", "keisha.morgan@learnhub.local", "Spatial Reasoning and Test Strategy Coach", "Keisha teaches assembling objects, pattern recognition, and timed test strategy for visual reasoning sections.", "https://images.unsplash.com/photo-1508214751196-bcfd4ca60f91?auto=format&fit=crop&w=400&q=80", "https://linkedin.com/in/keishamorgan", "https://keishamorganprep.com", 4)
    };

    private static IEnumerable<StudentSeed> StudentSeedData() => new[]
    {
        new StudentSeed("Jordan", "Miller", "jordan.miller@learnhub.local", "ASVAB Candidate", "Preparing for enlistment and improving AFQT score readiness.", "", 1), new StudentSeed("Alyssa", "Grant", "alyssa.grant@learnhub.local", "Future Service Member", "Studying math reasoning and vocabulary for ASVAB test day.", "", 2), new StudentSeed("Marcus", "Lee", "marcus.lee@learnhub.local", "ASVAB Student", "Building confidence across technical aptitude sections.", "", 3), new StudentSeed("Natalie", "Brooks", "natalie.brooks@learnhub.local", "Career Prep Student", "Focused on reading comprehension and Word Knowledge.", "", 4), new StudentSeed("Eric", "Johnson", "eric.johnson@learnhub.local", "Technical Aptitude Student", "Practicing electronics, mechanical, and auto shop questions.", "", 5), new StudentSeed("Sofia", "Martinez", "sofia.martinez@learnhub.local", "ASVAB Learner", "Preparing with timed practice quizzes and lesson review.", "", 6), new StudentSeed("Caleb", "Turner", "caleb.turner@learnhub.local", "Military Career Candidate", "Strengthening math, science, and spatial reasoning skills.", "", 7), new StudentSeed("Brianna", "Cole", "brianna.cole@learnhub.local", "Test Prep Student", "Using study categories to organize weekly ASVAB prep.", "", 8), new StudentSeed("Derek", "Hughes", "derek.hughes@learnhub.local", "ASVAB Practice Student", "Reviewing explanations to improve speed and accuracy.", "", 9), new StudentSeed("Tanya", "Price", "tanya.price@learnhub.local", "Future Recruit", "Building a stronger foundation before taking the ASVAB.", "", 10)
    };

    private static List<CourseSeed> CourseSeedData()
    {
        var groups = new (string Category, string[] Titles)[]
        {
            ("WK – Word Knowledge", new[] { "Vocabulary Foundations", "Synonyms and Antonyms Mastery", "Advanced Word Knowledge", "Context Clues Strategies", "ASVAB Word Knowledge Practice Tests" }),
            ("PC – Paragraph Comprehension", new[] { "Reading Comprehension Essentials", "Passage Analysis Techniques", "Critical Reading for ASVAB", "Inference and Main Idea Training", "Timed Paragraph Comprehension Drills" }),
            ("AR – Arithmetic Reasoning", new[] { "Arithmetic Reasoning Fundamentals", "Word Problems Masterclass", "Practical Math Applications", "Ratios, Fractions, and Percentages", "Timed Arithmetic Reasoning Practice" }),
            ("MK – Mathematics Knowledge", new[] { "Algebra Review", "Geometry Essentials", "Mathematics Knowledge Mastery", "Equations and Exponents", "ASVAB Math Formula Review" }),
            ("GS – General Science", new[] { "Biology Fundamentals", "Chemistry Basics", "Physics Principles", "Earth Science Overview", "General Science ASVAB Review" }),
            ("EI – Electronics Information", new[] { "Electronics Fundamentals", "Electrical Circuits Explained", "Current, Voltage and Resistance", "Electronic Components and Symbols", "ASVAB Electronics Practice" }),
            ("MC – Mechanical Comprehension", new[] { "Mechanical Principles", "Force and Motion", "Gears and Pulley Systems", "Tools, Machines, and Energy", "Mechanical Comprehension Practice" }),
            ("AS – Auto & Shop Information", new[] { "Automotive Basics", "Shop Tools and Safety", "Vehicle Maintenance Fundamentals", "Engines, Brakes, and Electrical Systems", "Auto and Shop ASVAB Practice" }),
            ("AO – Assembling Objects", new[] { "Spatial Visualization Skills", "Object Assembly Techniques", "Pattern Recognition Training", "Rotation and Folding Practice", "Timed Assembling Objects Drills" })
        };
        var courses = new List<CourseSeed>();
        foreach (var group in groups) for (var i = 0; i < group.Titles.Length; i++) courses.Add(NewCourse(group.Category, group.Titles[i], i));
        return courses;
    }

    private static CourseSeed NewCourse(string category, string title, int index)
    {
        var level = index switch { 0 => CourseLevel.Beginner, 1 or 2 => CourseLevel.Intermediate, _ => CourseLevel.AllLevels };
        var outcomes = Outcomes(category, title);
        var shortDescription = CourseSummary(category, title);
        var description = $"<p>{shortDescription}</p><p>{CourseDetail(title)}</p><p>Every lesson uses ASVAB-style examples, quick review notes, timed practice habits, and explanation-first quizzes so students understand why an answer is correct.</p>";
        return new CourseSeed(category, title, shortDescription, description, ThumbnailFor(category), $"https://learnhub.example.com/previews/asvab/{Slug(title)}.mp4", 49 + index * 10, 29 + index * 8, level, 4.6 + (index % 3) * 0.1, 24 + index * 7, 420 + index * 135, outcomes, RequirementsFor(category), AudienceFor(category));
    }

    private static string CourseSummary(string category, string title) => category switch
    {
        "WK – Word Knowledge" => title switch
        {
            "Vocabulary Foundations" => "Build a practical ASVAB vocabulary base with high-frequency military aptitude words, definitions, and review drills.",
            "Synonyms and Antonyms Mastery" => "Practice fast synonym and antonym recognition using answer-elimination strategies and word relationship patterns.",
            "Advanced Word Knowledge" => "Strengthen upper-level ASVAB vocabulary through roots, prefixes, suffixes, and difficult distractor choices.",
            "Context Clues Strategies" => "Learn how to infer unfamiliar word meanings from sentence structure, tone, contrast, and nearby clues.",
            _ => "Complete timed Word Knowledge practice sets with realistic vocabulary questions and detailed explanations."
        },
        "PC – Paragraph Comprehension" => title switch
        {
            "Reading Comprehension Essentials" => "Learn the core reading habits needed to understand short ASVAB passages quickly and accurately.",
            "Passage Analysis Techniques" => "Break passages into claims, evidence, sequence, and purpose so answer choices become easier to compare.",
            "Critical Reading for ASVAB" => "Practice inference, tone, and conclusion questions using compact passages modeled after test-day reading tasks.",
            "Inference and Main Idea Training" => "Improve main idea selection and evidence-based inference without overthinking or adding outside assumptions.",
            _ => "Build speed and accuracy with timed Paragraph Comprehension drills and targeted review."
        },
        "AR – Arithmetic Reasoning" => title switch
        {
            "Arithmetic Reasoning Fundamentals" => "Translate everyday word problems into clear arithmetic steps using ASVAB-focused problem-solving routines.",
            "Word Problems Masterclass" => "Master multi-step word problems involving money, time, distance, rates, averages, and proportions.",
            "Practical Math Applications" => "Apply math to real-life ASVAB scenarios including discounts, measurements, fuel use, and work-rate questions.",
            "Ratios, Fractions, and Percentages" => "Review the fraction, ratio, proportion, and percent skills that appear repeatedly in Arithmetic Reasoning.",
            _ => "Practice timed Arithmetic Reasoning sets with explanations that build speed and reduce careless mistakes."
        },
        "MK – Mathematics Knowledge" => title switch
        {
            "Algebra Review" => "Refresh expressions, equations, inequalities, and variables with ASVAB-style algebra examples.",
            "Geometry Essentials" => "Review area, perimeter, volume, angles, triangles, circles, and coordinate-plane geometry for test readiness.",
            "Mathematics Knowledge Mastery" => "Connect algebra, geometry, exponents, roots, and number properties into a complete ASVAB math review.",
            "Equations and Exponents" => "Practice solving equations and simplifying powers, roots, and scientific notation under time pressure.",
            _ => "Memorize and apply the formulas, rules, and shortcuts needed for the Mathematics Knowledge section."
        },
        "GS – General Science" => title switch
        {
            "Biology Fundamentals" => "Review cells, body systems, genetics, ecology, and life science vocabulary commonly tested on the ASVAB.",
            "Chemistry Basics" => "Understand atoms, elements, compounds, states of matter, reactions, and periodic table basics.",
            "Physics Principles" => "Study motion, force, energy, simple machines, heat, sound, light, and electricity fundamentals.",
            "Earth Science Overview" => "Review rocks, weather, atmosphere, oceans, astronomy, and natural cycles for ASVAB science questions.",
            _ => "Complete a broad General Science review with mixed ASVAB-style questions across biology, chemistry, physics, and earth science."
        },
        "EI – Electronics Information" => title switch
        {
            "Electronics Fundamentals" => "Learn basic electrical vocabulary, circuit behavior, safety concepts, and component identification.",
            "Electrical Circuits Explained" => "Understand series circuits, parallel circuits, switches, fuses, conductors, insulators, and circuit diagrams.",
            "Current, Voltage and Resistance" => "Apply Ohm's law and power relationships to solve common ASVAB electronics questions.",
            "Electronic Components and Symbols" => "Recognize resistors, capacitors, diodes, batteries, grounds, meters, and schematic symbols.",
            _ => "Practice ASVAB Electronics Information questions with circuit reasoning and component review."
        },
        "MC – Mechanical Comprehension" => title switch
        {
            "Mechanical Principles" => "Study mechanical advantage, force, motion, pressure, energy, and simple machine behavior.",
            "Force and Motion" => "Build confidence with Newton's laws, friction, gravity, acceleration, momentum, and work concepts.",
            "Gears and Pulley Systems" => "Interpret gear direction, gear ratio, pulley advantage, belts, levers, and rotating systems.",
            "Tools, Machines, and Energy" => "Connect tool use, machines, power, work, and energy transfer to realistic ASVAB scenarios.",
            _ => "Complete Mechanical Comprehension practice with diagrams, physical reasoning, and explanation-based review."
        },
        "AS – Auto & Shop Information" => title switch
        {
            "Automotive Basics" => "Review engine systems, fluids, batteries, tires, gauges, drivetrain basics, and common vehicle terms.",
            "Shop Tools and Safety" => "Learn hand tools, power tools, measuring tools, fasteners, protective equipment, and safe shop habits.",
            "Vehicle Maintenance Fundamentals" => "Understand oil, coolant, brakes, belts, filters, tires, and routine inspection procedures.",
            "Engines, Brakes, and Electrical Systems" => "Study major automotive systems and how they work together in ASVAB-style shop questions.",
            _ => "Practice Auto and Shop Information questions covering tools, safety, maintenance, and vehicle systems."
        },
        "AO – Assembling Objects" => title switch
        {
            "Spatial Visualization Skills" => "Train mental rotation, orientation, and visual matching skills for Assembling Objects questions.",
            "Object Assembly Techniques" => "Learn how to match edges, tabs, slots, shapes, and final assemblies under test timing.",
            "Pattern Recognition Training" => "Practice identifying repeated shapes, symmetry, part relationships, and visual patterns quickly.",
            "Rotation and Folding Practice" => "Build confidence with rotated views, folded shapes, and three-dimensional object reasoning.",
            _ => "Complete timed Assembling Objects drills with visual reasoning strategy and mistake review."
        },
        _ => $"Focused ASVAB preparation for {title.ToLowerInvariant()} with lessons, examples, timed drills, and practice questions."
    };

    private static string CourseDetail(string title) => $"The {title} course is organized as a structured study path with concept review, guided examples, common trap analysis, and a final readiness quiz.";

    private static string[] Outcomes(string category, string title) => category switch
    {
        "WK – Word Knowledge" => new[] { "Define and use high-frequency ASVAB vocabulary", "Identify synonyms and antonyms quickly", "Use roots and context clues to infer meaning", "Eliminate close distractors with confidence" },
        "PC – Paragraph Comprehension" => new[] { "Find main ideas and supporting details", "Make evidence-based inferences", "Recognize author's purpose and tone", "Improve timed reading accuracy" },
        "AR – Arithmetic Reasoning" => new[] { "Translate word problems into equations", "Solve percent, ratio, fraction, and rate questions", "Use practical math shortcuts", "Reduce multi-step problem errors" },
        "MK – Mathematics Knowledge" => new[] { "Solve algebra and equation problems", "Apply geometry formulas correctly", "Simplify exponents, roots, and expressions", "Use formulas under ASVAB timing" },
        "GS – General Science" => new[] { "Review biology, chemistry, physics, and earth science", "Recognize core science vocabulary", "Apply scientific reasoning to test questions", "Answer foundational science questions confidently" },
        "EI – Electronics Information" => new[] { "Understand voltage, current, resistance, and power", "Apply Ohm's law to circuit questions", "Identify components and schematic symbols", "Compare series and parallel circuits" },
        "MC – Mechanical Comprehension" => new[] { "Explain force, motion, work, and energy", "Interpret gears, pulleys, levers, and simple machines", "Apply mechanical advantage concepts", "Read mechanical diagrams more confidently" },
        "AS – Auto & Shop Information" => new[] { "Identify vehicle systems and shop tools", "Recognize maintenance and safety procedures", "Understand engine, brake, and electrical basics", "Answer practical shop knowledge questions accurately" },
        "AO – Assembling Objects" => new[] { "Improve spatial visualization", "Mentally rotate and match objects", "Recognize patterns and assemblies", "Build speed on visual reasoning questions" },
        _ => new[] { "Prepare for ASVAB practice", "Improve accuracy", "Review explanations", "Build test confidence" }
    };

    private static string RequirementsFor(string category) => category switch
    {
        "AR – Arithmetic Reasoning" or "MK – Mathematics Knowledge" => "Basic arithmetic skills are recommended. Students should have paper available for worked examples and timed drills.",
        "EI – Electronics Information" or "MC – Mechanical Comprehension" or "AS – Auto & Shop Information" => "No technical background required. Curiosity about tools, machines, vehicles, or circuits is helpful.",
        "AO – Assembling Objects" => "No prior spatial reasoning study required. Students should be ready to practice visual problems repeatedly.",
        _ => "No prior ASVAB experience required. A notebook and willingness to complete timed practice are recommended."
    };

    private static string AudienceFor(string category) => category switch
    {
        "WK – Word Knowledge" or "PC – Paragraph Comprehension" => "ASVAB candidates improving AFQT verbal readiness, vocabulary, reading speed, and answer-elimination strategy.",
        "AR – Arithmetic Reasoning" or "MK – Mathematics Knowledge" => "ASVAB candidates who want stronger AFQT math performance through focused review and timed practice.",
        "EI – Electronics Information" or "MC – Mechanical Comprehension" or "AS – Auto & Shop Information" => "Students preparing for technical aptitude sections and career-field qualification opportunities.",
        "AO – Assembling Objects" => "Students who want to improve visual reasoning, mental rotation, and object assembly speed.",
        _ => "ASVAB candidates, future service members, and learners improving military aptitude test readiness."
    };

    private static string ThumbnailFor(string category) => category switch
    {
        "WK – Word Knowledge" => "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?auto=format&fit=crop&w=900&q=80", "PC – Paragraph Comprehension" => "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=900&q=80", "AR – Arithmetic Reasoning" => "https://images.unsplash.com/photo-1509228468518-180dd4864904?auto=format&fit=crop&w=900&q=80", "MK – Mathematics Knowledge" => "https://images.unsplash.com/photo-1635070041078-e363dbe005cb?auto=format&fit=crop&w=900&q=80", "GS – General Science" => "https://images.unsplash.com/photo-1532094349884-543bc11b234d?auto=format&fit=crop&w=900&q=80", "EI – Electronics Information" => "https://images.unsplash.com/photo-1518770660439-4636190af475?auto=format&fit=crop&w=900&q=80", "MC – Mechanical Comprehension" => "https://images.unsplash.com/photo-1504917595217-d4dc5ebe6122?auto=format&fit=crop&w=900&q=80", "AS – Auto & Shop Information" => "https://images.unsplash.com/photo-1487754180451-c456f719a1fc?auto=format&fit=crop&w=900&q=80", "AO – Assembling Objects" => "https://images.unsplash.com/photo-1500530855697-b586d89ba3ee?auto=format&fit=crop&w=900&q=80", _ => "https://images.unsplash.com/photo-1454165804606-c3d57bc86b40?auto=format&fit=crop&w=900&q=80"
    };

    private static string Slug(string value) { var builder = new StringBuilder(); foreach (var c in value.ToLowerInvariant()) { if (char.IsLetterOrDigit(c)) builder.Append(c); else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-'); } return builder.ToString().Trim('-'); }
    private static string HashPassword(string password) { using var sha256 = SHA256.Create(); return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(password))); }

    private sealed record PracticeQuestionSeed(string Text, string A, string B, string C, string D, string Correct, string Explanation);
    private sealed record FlashcardSeed(string Front, string Back, string Hint);
    private sealed record CategorySeed(string Name, string Description, string Icon);
    private sealed record InstructorSeed(string FirstName, string LastName, string Email, string Headline, string Bio, string Photo, string LinkedIn, string Website, int LastLoginDaysAgo);
    private sealed record StudentSeed(string FirstName, string LastName, string Email, string Headline, string Bio, string Photo, int LastLoginDaysAgo);
    private sealed record QuizQuestionSeed(string Text, string[] Answers, int CorrectIndex, string Explanation);
    private sealed record CourseSeed(string Category, string Title, string ShortDescription, string Description, string ThumbnailUrl, string PreviewVideoUrl, decimal Price, decimal DiscountedPrice, CourseLevel Level, double Rating, int ReviewCount, int Enrollments, string[] Outcomes, string Requirements, string TargetAudience);
}











