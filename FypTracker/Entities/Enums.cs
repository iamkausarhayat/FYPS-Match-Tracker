namespace FypTracker.Entities
{
    // Who is using the app
    public enum UserRole
    {
        Student = 1,
        Admin = 2
    }

    // What a similarity match points to
    public enum MatchSource
    {
        DepartmentProject = 1,
        ResearchPaper = 2
    }

    // How much text we have for a project
    public enum DataQuality
    {
        Full = 1,        // title + a real description (abstract)
        TitleOnly = 2    // only the title, so a match is less reliable
    }

    // How a university's projects get into our database
    public enum UniversitySourceType
    {
        Manual = 1,      // added by hand or by CSV file
        DSpace = 2       // read from the university's public DSpace repository API
    }
}