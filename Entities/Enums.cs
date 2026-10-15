namespace JavaMaster.Api.Entities;

public enum UserRole { Student, Professor, Admin }
public enum QuestionType { MultipleChoice, FillBlank, ArrangeTokens, FindBug, FillSlots }
public enum QuestionPurpose { Lesson, Daily, Pool }
public enum NodeType { Lesson, Practice, Test }
public enum ProgressStatus { Locked, Unlocked, Completed }