# Java Master: Project Context

## Learn, Lessons and Practice (current state)

### Learn.html
- Loads the path from `GET /api/learn/path`.
- Node status: `Completed`, `Unlocked` (one node at a time) or `Locked`.
- Node 4 (Java Basics 1) links to `/lesson/Java Basics 1-Practice.html`.
- Completed Practice and Test nodes show the big check only. The small check badge rule was removed from `Learn-style.css`.

### Lesson 1, 2, 3 (`wwwroot/lesson/`)
- Use the shared lesson engine: `GET /api/learn/nodes/{id}`, `POST /api/learn/questions/{id}/check`, `POST /api/learn/nodes/{id}/complete`.
- Mascot jumps (`jumpModel`) on a correct answer only.
- Lesson 1: key terms in bold (`<b>`), question text cannot be selected.
- Lesson 2 and 3: intro text left-aligned. Wrong-answer explanations are short nudges, stored in `Questions.Explanation`.

### Java Basics 1 Practice (`wwwroot/lesson/Java Basics 1-Practice.html`)
- Node 4. Questions: `java-basics-1-v2.q1` to `q5` (from `Data/PracticeContent.cs`).
- Each item: learner types the statement(s) in the editor. Checked by `POST /api/learn/questions/{id}/check`.
- Fixed `class Main { ... }` template. Only the middle lines are editable.
- Question text and code box cannot be copied or pasted.
- Hint: `jumpMascot(true)`. Correct: applause and jump. Wrong: thinking and nudge.
- Finish calls `POST /api/learn/nodes/4/complete`. First time shows Finish, then Completed with XP and coins. Replay shows Completed only.
- Settings and exit popups, background music, sound effects, Java logo on the `Practice.java` tab.

### Backend
- `Controllers/LearnController.cs`: `path`, `nodes/{id}`, `questions/{id}/check`, `nodes/{id}/complete`. Every node, practice included, passes by answering every question correctly.
- `Data/PracticeContent.cs`: seeds Java Basics 1 questions and outro. Called from `Program.cs` after `LessonContent.SeedAsync`.
- `Data/LessonContent.cs`: seeds Lesson 1 to 4 intro, outro and questions.
- `Services/AnswerChecker.cs`: matches answers per question type.
- `Services/LearnRules.cs`: node locking rule (shared).

### Database (JavaMaster)
- `LearnNodes`: Lesson 2 and 3 `IntroText` restored, rocket removed. Node 4 reward: 60 XP, 15 coins.
- `Questions`: `variables.*` and `data-types.*` explanations shortened. Practice questions are `java-basics-1-v2.*`.

### Pending cleanup (old compiler, do not rely on)
- `Controllers/PracticeController.cs`, `Services/PracticeChecks.cs`, `Services/PracticeModels.cs`, `Services/JavaRunnerService.cs`, `Services/FriendlyErrorService.cs`.
- `Entities/PracticePass.cs` and its migration, `Data/ErrorRuleSeed.cs`, `Entities/ErrorRule.cs` and the ErrorRules migration.
- `Program.cs` still registers `AddMemoryCache`, `JavaRunnerService` and `FriendlyErrorService`. Remove together with the files above, then run `dotnet build`.

## Decks, Lobby and Building (current state)

### Database
- `Decks`: Id, OwnerUserId, Name (max 30), Share (`public` or `private`), CoverNumber (1 to 6), ShareCode (6 characters, unique, random), CreatedAt, UpdatedAt. Dropped: Description, ItemCount, QuestionTypes.
- `DeckCards`: Id, DeckId (cascade delete), Position (from 0, unique per deck), Type, Question (max 200), Data (JSON, the part that depends on the type).
- Migrations: `ReworkDecks` (new columns, `DeckCards`, deleted old test decks), `ShareCodeSix` (ShareCode 8 to 6 characters).
- Deck cards are separate from Learn `Questions`, `NodeQuestions` and `AnswerChecker`.
- Limits live in `Services/GameRules.cs` (Deck constants): name 30, 1 to 50 cards, question 200, options 60, matching 2 to 8 pairs with 40 each, code 1000, blanks 1 to 5, answers and decoys 40, up to 5 decoys.

### API (`Controllers/DecksController.cs`, `/api/decks`)
- `GET /` list of your decks (no cards). Fields: id, name, share, cover, shareCode, items, types, createdAt, updatedAt.
- `GET /{id}` one deck with `cards`.
- `GET /code/{code}` finds a Public deck by code, returns only `{ id }`. Any user.
- `POST /` create (Publish). `PUT /{id}` save: replaces all cards and moves `updatedAt`.
- `PATCH /{id}/share` sets `public` or `private`. Does not move `updatedAt`.
- The owner comes from `ICurrentUserService` only. Other users get 404 "Deck not found." (the code lookup is the one exception).
- Every limit is validated on the server, with a message like "Card 2: options must all be different."
- Card JSON: `type`, `question`, plus `options` and `correct` (multiple-choice), `answer` (true-false), `pairs` of `item` and `answer` (matching), `parts` of `{text}` or `{blank}` and `answers` (fill-blank-code), the same plus `decoys` (drag-drop-code). `blank` is a 0-based index into `answers`.

### Pages and scripts
- `js/api.js`: `JM.decks`, `JM.deck`, `JM.deckByCode`, `JM.createDeck`, `JM.updateDeck`, `JM.setDeckShare`. Nothing uses `sessionStorage` for decks anymore.
- `Building-deck.html`: Publish creates the deck (POST). With `?id=<deck id>` it loads the deck for editing and the top button reads Save (PUT). Leaving with unsaved changes warns.
- `Lobby.html`: Community Play (Publish a Deck, Play a Shared Deck), Class Quiz (Live Quiz card), Your Decks (New Deck, Sort, deck cards). The 3 dots menu: Edit (opens Building-deck with the id), Share, Archive (not built).
- `Deck overview.html?id=`: loads the deck from the API. Share and Edit buttons work. Study deck does nothing yet.
- `js/share-deck.js` (`JMShare`): `open` (Share Deck window with Public or Private and Generate a code, then the Deck Code window), `openPublish` (Publish a Deck window: lists Public decks only, then the Deck Code window), `openJoin` (Play a Shared Deck window: 6 code boxes, spinner, then the Deck overview, or a failed view with Retry).

## Decks

- Name: 1 to 30 characters. Cover: 1 to 6.
- 1 to 50 cards per deck. A deck is Public or Private (default set when published).
- Share code: 6 characters, random, unique per deck. Public decks can be opened with the code.
- Question types: multiple-choice, true-false, matching, fill-blank-code, drag-drop-code.
- Question: up to 200 characters.
- Multiple choice: 4 options, up to 60 characters each, all different (case ignored), 1 correct.
- True or false: the answer is true or false.
- Matching: 2 to 8 pairs, up to 40 characters each, items different and answers different.
- Fill in the blanks code: up to 1000 characters of code, 1 to 5 blanks, each answer up to 40 characters.
- Drag and drop code: the same as fill in the blanks, plus up to 5 decoys of up to 40 characters, all different, none equal to an answer, case-sensitive.
- Publish creates the deck. Save replaces its cards and updates "last edited". Changing Public or Private does not update "last edited".
- Only the owner can see, change or share a deck. Deleting and archiving are not built yet.

### Not built yet
- Delete and archive a deck.
- Opening a shared deck as another user: the Deck overview still loads only your own decks, so Play a Shared Deck works for the owner only.
- Hide Edit on the overview for non-owners when that exists.
- Study deck button, Host a live game, Join Quiz backend.
- The Join Quiz page and the Play a Shared Deck window are separate things.

## Project Structure

```
├── Controllers
│   ├── DailyController.cs
│   ├── DecksController.cs              ← decks API (list, get, code lookup, create, save, share)
│   ├── LearnController.cs              ← learn path, nodes, check, complete
│   ├── MeController.cs
│   ├── PracticeController.cs           ← old compiler, pending removal
│   ├── ShopController.cs
│   ├── StreakController.cs
│   ├── TopicsController.cs
│   └── WeatherForecastController.cs
├── Data
│   ├── AppDbContext.cs
│   ├── DbSeeder.cs
│   ├── ErrorRuleSeed.cs                ← pending removal
│   ├── LessonContent.cs                ← lessons 1 to 4
│   └── PracticeContent.cs              ← Java Basics 1 (v2)
├── Entities
│   ├── Deck.cs
│   ├── DeckCard.cs                     ← one card of a deck (Data is JSON)
│   ├── Enums.cs
│   ├── ErrorRule.cs                    ← pending removal
│   ├── LearnNode.cs
│   ├── LearnSection.cs
│   ├── NodeQuestion.cs
│   ├── PracticePass.cs                 ← pending removal
│   ├── Purchase.cs
│   ├── Question.cs
│   ├── QuestionAttempt.cs
│   ├── Rank.cs
│   ├── ShopItem.cs
│   ├── Topic.cs
│   ├── User.cs
│   ├── UserBadge.cs
│   ├── UserDailyChallenge.cs
│   ├── UserInventory.cs
│   └── UserNodeProgress.cs
├── Migrations
│   ├── 20261014140701_InitialCreate.cs
│   ├── 20261014140701_InitialCreate.Designer.cs
│   ├── 20261014144225_AddLearnPath.cs
│   ├── 20261014144225_AddLearnPath.Designer.cs
│   ├── 20261014192238_AddNodeIntroOutro.cs
│   ├── 20261014192238_AddNodeIntroOutro.Designer.cs
│   ├── 20261015050815_AddRanks.cs
│   ├── 20261015050815_AddRanks.Designer.cs
│   ├── 20261015060158_AddQuestionAttempts.cs
│   ├── 20261015060158_AddQuestionAttempts.Designer.cs
│   ├── 20261024072430_AddShop.cs
│   ├── 20261024072430_AddShop.Designer.cs
│   ├── 20261024183605_AddDailyChallenge.cs
│   ├── 20261024183605_AddDailyChallenge.Designer.cs
│   ├── 20261025173441_AddUserBadges.cs
│   ├── 20261025173441_AddUserBadges.Designer.cs
│   ├── 20261027120536_AddQuestionCode.cs
│   ├── 20261027120536_AddQuestionCode.Designer.cs
│   ├── 20261027173309_FixCaseSensitiveAnswers.cs
│   ├── 20261027173309_FixCaseSensitiveAnswers.Designer.cs
│   ├── 20261029170114_AddProfileFields.cs
│   ├── 20261029170114_AddProfileFields.Designer.cs
│   ├── 20261108080338_AddDecks.cs
│   ├── 20261108080338_AddDecks.Designer.cs
│   ├── 20261113055852_AddErrorRules.cs              ← pending removal
│   ├── 20261113055852_AddErrorRules.Designer.cs     ← pending removal
│   ├── 20261115025343_AddPracticePasses.cs          ← pending removal
│   ├── 20261115025343_AddPracticePasses.Designer.cs ← pending removal
│   ├── 20261126072250_ReworkDecks.cs
│   ├── 20261126072250_ReworkDecks.Designer.cs
│   ├── <timestamp>_ShareCodeSix.cs
│   ├── <timestamp>_ShareCodeSix.Designer.cs
│   └── AppDbContextModelSnapshot.cs
├── Properties
│   └── launchSettings.json
├── Services
│   ├── AnswerChecker.cs                ← matches answers per question type
│   ├── CurrentUserService.cs
│   ├── FriendlyErrorService.cs         ← pending removal
│   ├── GameRules.cs
│   ├── JavaRunnerService.cs            ← pending removal
│   ├── LearnRules.cs                   ← node locking rule
│   ├── PracticeChecks.cs               ← pending removal
│   ├── PracticeModels.cs               ← pending removal
│   ├── RankCalculator.cs
│   ├── StartingKit.cs
│   └── StreakService.cs
├── wwwroot
│   ├── avatars
│   │   ├── Avatar 1.png
│   │   ├── Avatar 2.png
│   │   ├── Avatar 3.png
│   │   ├── Avatar 4.png
│   │   ├── Avatar 5.png
│   │   ├── Avatar 6.png
│   │   ├── Avatar 7.png
│   │   ├── Avatar 8.png
│   │   └── Avatar 9.png
│   ├── cover
│   │   ├── cover 1.jpg
│   │   ├── cover 2.jpg
│   │   ├── cover 3.jpg
│   │   ├── cover 4.jpg
│   │   ├── cover 5.jpg
│   │   └── cover 6.jpg
│   ├── css
│   │   ├── Building-deck-style.css
│   │   ├── Dashboard-navbar-style.css
│   │   ├── Deck-style.css
│   │   ├── display topics.css
│   │   ├── game features.css
│   │   ├── Home-style.css
│   │   ├── Learn-style.css             ← small check badge rule removed
│   │   ├── Lobby-style.css
│   │   ├── navbar-footer.css
│   │   ├── Profile-style.css
│   │   ├── Progress-style.css
│   │   ├── Shop-style.css
│   │   └── style.css
│   ├── icons
│   │   ├── 3d-target.png
│   │   ├── archive.png
│   │   ├── arrow-down.png
│   │   ├── arrow-up.png
│   │   ├── badge.png
│   │   ├── brain.png
│   │   ├── browsing.png
│   │   ├── bulb.png                    ← Hint button
│   │   ├── bullet check.png
│   │   ├── check bulb icon.png
│   │   ├── clock.png
│   │   ├── coffee.png
│   │   ├── coin.png
│   │   ├── college.png
│   │   ├── critical-thinking.png
│   │   ├── curly_braces.png
│   │   ├── currency.png
│   │   ├── done.png
│   │   ├── dropdown_icon.png
│   │   ├── edit_icon.png
│   │   ├── edit.png
│   │   ├── faq_icon.png
│   │   ├── game console icon.png
│   │   ├── game console.png
│   │   ├── gift-box.png
│   │   ├── globe icon.png
│   │   ├── gmail_icon.png
│   │   ├── goal.png
│   │   ├── goback.png
│   │   ├── graduation_icon.png
│   │   ├── green_check.png
│   │   ├── heal potion.png
│   │   ├── heart.png
│   │   ├── hint-heart used.png
│   │   ├── home.png
│   │   ├── idea.png
│   │   ├── joined_icon.png
│   │   ├── kettle.png
│   │   ├── key.png
│   │   ├── letter.png
│   │   ├── level_icon.png
│   │   ├── live quiz.png
│   │   ├── lobby.png
│   │   ├── logout_icon.png
│   │   ├── map-ensign.png
│   │   ├── multiplayer.png
│   │   ├── notification_bell_icon.png
│   │   ├── opportunity.png
│   │   ├── password_icon.png
│   │   ├── plus.png                    ← New Deck button
│   │   ├── profile.png
│   │   ├── programming.png
│   │   ├── pxl coffee.png
│   │   ├── remove.png                  ← Remove button (lessons)
│   │   ├── reset.png                   ← Reset button
│   │   ├── save.png
│   │   ├── school_icon.png
│   │   ├── settings_icon.png
│   │   ├── share.png
│   │   ├── skull.png
│   │   ├── small size java logo.png    ← Practice.java tab
│   │   ├── sort.png                    ← Sort button
│   │   ├── stamp.png
│   │   ├── star_icon.png
│   │   ├── student.png
│   │   ├── target.png
│   │   ├── toga cap.png
│   │   ├── user_icon.png
│   │   ├── victory.png
│   │   └── wired_keyboard.png
│   ├── images
│   │   ├── AbstractImage1.jpg
│   │   ├── AbstractImage2.jpg
│   │   ├── campaign mode.png
│   │   ├── coffee no smoke.png
│   │   ├── coffee streak.gif
│   │   ├── cup no streak.png
│   │   ├── error-code_1.png
│   │   ├── Learn preview.gif
│   │   ├── learn-at-own-pace.png
│   │   ├── no-prior-experience.png
│   │   ├── technical-field.png
│   │   ├── visual-learning.png
│   │   └── what-is-the-output_1.png
│   ├── js
│   │   ├── animation.js
│   │   ├── api.js
│   │   ├── badge-manager.js
│   │   ├── coins-sync.js
│   │   ├── edit-profile.js
│   │   ├── navbar-animation.js
│   │   ├── notification-manager.js
│   │   ├── profile-dropdown.js
│   │   ├── share-deck.js
│   │   └── study-modal.js
│   ├── lesson
│   │   ├── Java Basics 1-Practice.html ← practice page (API version)
│   │   ├── Lesson 1.html               ← bold terms, no-copy, jump
│   │   ├── Lesson 2.html               ← jump, short nudges, left intro
│   │   └── Lesson 3.html               ← jump, short nudges, left intro
│   ├── logo
│   │   ├── horizontal-logo-javamaster.png
│   │   └── java-master-logo.png
│   ├── model
│   │   ├── James Gosling Applause.png  ← correct answer
│   │   ├── James Gosling avatar.png
│   │   ├── James Gosling Bright Idea.png ← Hint
│   │   ├── James Gosling crossed arms.png
│   │   ├── James Gosling Drinking Coffee.png ← Finish screen
│   │   ├── James Gosling Graduate.png
│   │   ├── James Gosling holding a laptop.png
│   │   ├── James Gosling Standing Up.png
│   │   ├── James Gosling Thinking.png  ← wrong answer
│   │   └── James Gosling wave.png      ← intro
│   ├── nodes
│   │   ├── Current node.png
│   │   ├── Lock node.png
│   │   ├── Practice node.png
│   │   ├── Test node.png
│   │   └── Unlock node.png
│   ├── pages
│   │   ├── Building-deck.html
│   │   ├── Daily Challenges.html
│   │   ├── Dashboard-navbar.html
│   │   ├── Deck overview.html
│   │   ├── Deck.html
│   │   ├── FAQ.html
│   │   ├── Features.html
│   │   ├── Home.html
│   │   ├── Join Quiz.html
│   │   ├── Learn.html                  ← learn path (API version)
│   │   ├── Loading-screen.html
│   │   ├── Lobby.html
│   │   ├── Log in page.html
│   │   ├── Notifications.html
│   │   ├── Onboard.html
│   │   ├── PrivacyPolicy.html
│   │   ├── Profile.html
│   │   ├── Progress.html
│   │   ├── Settings.html
│   │   ├── Shop.html
│   │   ├── Sign up page.html
│   │   ├── Terms&Conditions_Page.html
│   │   └── Topics.html
│   ├── pixel art
│   │   ├── features_background.gif
│   │   ├── pxl 1.jpg
│   │   ├── pxl 10.jpg
│   │   ├── pxl 11.jpg
│   │   ├── pxl 2.jpg
│   │   ├── pxl 3.jpg
│   │   ├── pxl 4.jpg
│   │   ├── pxl 5.jpg
│   │   ├── pxl 6.jpg
│   │   ├── pxl 7.jpg
│   │   ├── pxl 8.jpg
│   │   ├── pxl 9.jpg
│   │   └── topic cover.gif
│   ├── rank titles
│   │   ├── beginner.png
│   │   ├── boss.png
│   │   ├── elite.png
│   │   ├── legend.png
│   │   ├── pro.png
│   │   ├── rising.png
│   │   ├── skilled.png
│   │   └── starter.png
│   ├── sounds
│   │   ├── BG_music.mp3
│   │   ├── coins_sound-efx.mp3
│   │   ├── completed_sound-efx.wav
│   │   ├── correct_sound-efx.wav
│   │   ├── defeat_sound-efx.wav
│   │   ├── hp-recharge_sound-efx.wav
│   │   └── wrong_sound-efx.wav
│   └── index.html
├── appsettings.Development.json
├── appsettings.json
├── JavaMaster.Api.csproj
├── JavaMaster.Api.http
├── Program.cs                          ← seeds Lesson and Practice content
└── WeatherForecast.cs
```